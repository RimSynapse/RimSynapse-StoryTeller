using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimSynapse.Utils;
using Newtonsoft.Json;

namespace RimSynapse.StoryTeller
{
    public static class SynapseStorytellerOpportunistic
    {
        public static bool TriggerPeriodicInvestigation()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null) return false;
            if (Find.Storyteller?.def?.defName != "Synapse") return false;

            var map = Find.CurrentMap;
            
            // Generate snapshot of current state
            string wealth = map.wealthWatcher.WealthTotal.ToString("F0");
            string pop = map.mapPawns.FreeColonistsCount.ToString();
            string mood = map.mapPawns.FreeColonists.Average(p => p.needs?.mood?.CurLevelPercentage ?? 0.5f).ToString("P0");
            
            // Get recent events from Core if possible (optional context)
            string recentEvents = "None recently.";
            var coreWorldComp = Find.World.GetComponent<RimSynapse.SynapseCoreWorldComponent>();
            if (coreWorldComp != null)
            {
                var events = coreWorldComp.GetRecentEvents(5);
                if (events.Any())
                {
                    recentEvents = string.Join("\n", events.Select(e => $"- {e.eventDescription}"));
                }
            }

            string systemPrompt = @"You are Aura Algorithm, the AI Storyteller for RimWorld.
You are running your 12-hour periodic investigation of the colony to decide if you should intervene.
Based on their recent narrative and current struggles, you must choose ONE action:
1. Trigger a specific game incident (e.g., 'TraderCaravanArrival', 'Eclipse', 'RaidEnemy', 'VisitorGroup', 'Quest_TradeRequest').
2. Send a 'FlavorLetter' - a funny or interesting world event report of things happening elsewhere on the rimworld, just to add flavor if they are struggling or nothing major is happening.
3. 'None' - Do nothing.

You MUST respond strictly in valid JSON format:
{
  ""ActionType"": ""Incident | FlavorLetter | None"",
  ""IncidentDefName"": ""(Leave empty if not Incident)"",
  ""FlavorTitle"": ""(Title of the flavor letter, leave empty if not FlavorLetter)"",
  ""FlavorText"": ""(Text of the flavor letter, leave empty if not FlavorLetter)""
}";

            string userMessage = $@"Colony Status:
- Wealth: {wealth}
- Population: {pop}
- Average Mood: {mood}

Recent Events:
{recentEvents}

Analyze the situation and decide on your action.";

            SynapseClient.PromptAsync(
                RimSynapseStoryTellerMod.ModHandle,
                systemPrompt,
                userMessage,
                result =>
                {
                    if (result.success)
                    {
                        try
                        {
                            string json = JsonHelper.ExtractJson(result.content);
                            if (json == null) { RimSynapse.SynapseLog.Warn("storyteller", "[RimSynapse-StoryTeller] No JSON found in investigation response."); return; }

                            var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                            if (parsed != null && parsed.TryGetValue("ActionType", out string actionType))
                            {
                                if (actionType == "Incident" && parsed.TryGetValue("IncidentDefName", out string defName) && !string.IsNullOrEmpty(defName))
                                {
                                    IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(defName);
                                    if (def != null)
                                    {
                                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
                                        Find.Storyteller.incidentQueue.Add(def, Find.TickManager.TicksGame + 2500, parms); // Trigger in 1 hour
                                        RimSynapse.SynapseLog.Info("storyteller", $"[RimSynapse-StoryTeller] Investigation chose to trigger incident: {defName}");
                                    }
                                    else
                                    {
                                        RimSynapse.SynapseLog.Warn("storyteller", $"[RimSynapse-StoryTeller] AI suggested invalid IncidentDefName: {defName}");
                                    }
                                }
                                else if (actionType == "FlavorLetter" && parsed.TryGetValue("FlavorTitle", out string title) && parsed.TryGetValue("FlavorText", out string text))
                                {
                                    Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent);
                                    RimSynapse.SynapseLog.Info("storyteller", "[RimSynapse-StoryTeller] Investigation generated a world flavor letter.");
                                }
                                else
                                {
                                    RimSynapse.SynapseLog.Info("storyteller", "[RimSynapse-StoryTeller] Investigation concluded no action needed.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            RimSynapse.SynapseLog.Warn("storyteller", $"[RimSynapse-StoryTeller] Failed to parse investigation response: {ex.Message}");
                        }
                    }
                },
                new ChatOptions { priority = 1 } // Moderate priority
            );

            return true;
        }
    }
}

