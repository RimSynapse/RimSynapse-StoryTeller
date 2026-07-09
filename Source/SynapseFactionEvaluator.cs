using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimSynapse.Utils;
using RimSynapse.StoryTeller.Models;
using Newtonsoft.Json;

namespace RimSynapse.StoryTeller
{
    public static class SynapseFactionEvaluator
    {
        /// <summary>
        /// Checks all factions for one that needs history generation.
        /// Returns true if an LLM call was actually queued.
        /// 
        /// Coordination logic:
        /// - If Psychology is loaded, leader backstories are OPTIONAL bonus context.
        ///   We don't gate on them — faction history generates first to provide context
        ///   for leader backstories when Psychology processes them later.
        /// - If Psychology is NOT loaded, we generate faction history using only
        ///   faction type, ideology, and vanilla relationship data.
        /// </summary>
        public static bool CheckAllFactions()
        {
            if (Find.FactionManager == null) return false;
            foreach (var faction in Find.FactionManager.AllFactions)
            {
                if (EvaluateFaction(faction))
                    return true; // Queued one — let it finish before doing more
            }
            return false; // No work was queued
        }

        /// <summary>
        /// Returns true if an LLM call was queued for this faction.
        /// </summary>
        public static bool EvaluateFaction(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.Hidden) return false;
            if (faction.leader == null || !faction.leader.RaceProps.Humanlike) return false;

            var stWorldComp = Find.World.GetComponent<SynapseStoryTellerWorldComponent>();
            if (stWorldComp == null) return false;

            var tracker = stWorldComp.GetOrCreateStoryTracker(faction.GetUniqueLoadID());

            if (!string.IsNullOrEmpty(tracker.factionHistory))
            {
                // Already generated history
                return false;
            }

            // Build the prompt — leader backstory is optional bonus context
            GenerateFactionHistory(faction, faction.leader, tracker);
            return true; // LLM call was queued
        }

        private static void GenerateFactionHistory(Faction faction, Pawn leader, FactionStoryTracker tracker)
        {
            string factionName = faction.Name;
            string factionType = faction.def.LabelCap;
            string ideology = faction.ideos?.PrimaryIdeo?.name ?? "No specific ideology";
            
            string traits = leader.story?.traits?.allTraits != null 
                ? string.Join(", ", leader.story.traits.allTraits.Select(t => t.LabelCap)) 
                : "None";

            // Check if Psychology has generated a leader backstory (optional bonus context)
            string leaderBackstorySection = "";
            bool psychologyLoaded = SynapseCore.IsModLoaded("RimSynapsePsychology");
            
            var leaderCoreComp = leader.TryGetComp<RimSynapse.Comps.SynapseCorePawnComp>();
            if (leaderCoreComp != null)
            {
                var backstories = leaderCoreComp.memories.Where(m => m.memoryType == "Backstory").Select(m => m.summary);
                string backstoryText = string.Join("\n", backstories);
                if (!string.IsNullOrEmpty(backstoryText))
                {
                    leaderBackstorySection = $"\nLeader Backstory (AI-generated): \"{backstoryText}\"";
                }
            }

            string systemPrompt = @"You are the RimWorld Diplomatic AI. 
You must analyze the following faction to generate a historical record.
Write a 'Historical Record' (3-4 sentences) that describes the faction's history based on the leader's personality traits, the faction's type, and its ideology.
CRITICAL INSTRUCTION: You will be provided with the faction's current numeric relationships with OTHER major NPC factions. You MUST use these existing numeric values to shape the narrative. If they are deeply hostile to another faction, explain the historical reason. Do NOT generate new relationship numbers. Use the provided ones to write flavor text." +
(string.IsNullOrEmpty(leaderBackstorySection) ? "" : "\nIf a leader backstory is provided, weave it into the faction's history as additional context.") + @"

You MUST respond strictly in valid JSON format:
{
  ""HistoricalRecord"": ""The fierce tribe of ...""
}";

            // Collect relations with other major factions
            string npcRelations = "Relations with other factions:\n";
            foreach (var otherFaction in Find.FactionManager.AllFactionsVisible)
            {
                if (otherFaction == faction || otherFaction.IsPlayer || otherFaction.Hidden) continue;
                int relation = faction.GoodwillWith(otherFaction);
                string relationLabel = relation >= 75 ? "Allied" : relation <= -75 ? "Hostile" : "Neutral";
                npcRelations += $"- {otherFaction.Name} ({otherFaction.def.LabelCap}): {relation} ({relationLabel})\n";
            }

            string userMessage = $@"Faction: {factionName}
Type: {factionType}
Ideology: {ideology}

Leader: {leader.Name.ToStringFull}
Leader Traits: {traits}{leaderBackstorySection}

{npcRelations}

Generate their History.";

            var options = new ChatOptions { priority = 6 };

            SynapseClient.PromptAsync(
                RimSynapseStoryTellerMod.ModHandle,
                systemPrompt,
                userMessage,
                result =>
                {
                    HandleFactionHistoryResult(faction, tracker, result);
                },
                options
            );
        }

        private static void HandleFactionHistoryResult(Faction faction, FactionStoryTracker tracker, ChatResult result)
        {
            if (result.success)
            {
                try
                {
                    string json = JsonHelper.ExtractJson(result.content);
                    if (json == null) { Log.Warning("[RimSynapse-StoryTeller] No JSON found in faction evaluation response."); return; }

                    var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (parsed != null)
                    {
                        string history = "Unknown history.";
                        if (parsed.TryGetValue("HistoricalRecord", out object histObj))
                        {
                            history = histObj.ToString();
                        }

                        tracker.factionHistory = history;

                        Log.Message($"[RimSynapse-StoryTeller] Evaluated Faction {faction.Name}: History generated.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimSynapse-StoryTeller] Failed to parse Faction Evaluation response: {ex.Message}");
                }
            }
        }
    }
}
