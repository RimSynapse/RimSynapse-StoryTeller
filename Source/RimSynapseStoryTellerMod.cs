using System;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using RimSynapse.StoryTeller;
using RimSynapse.StoryTeller.Models;

namespace RimSynapse
{
    public class RimSynapseStoryTellerMod : Mod
    {
        public static string Id = "RimSynapse.StoryTeller";
        public static SynapseModHandle ModHandle;

        private Vector2 scrollPosition = Vector2.zero;

        public RimSynapseStoryTellerMod(ModContentPack content) : base(content)
        {
            Log.Message("[RimSynapse-StoryTeller] Initializing narrative intelligence layer.");
            
            ModHandle = SynapseCore.Register("RimSynapseStoryTeller", "RimSynapse StoryTeller");

            // Initialize Harmony for StoryTeller patches
            var harmony = new Harmony(Id);
            harmony.PatchAll();

            Log.Message("[RimSynapse-StoryTeller] Initialization complete.");
        }

        public override string SettingsCategory()
        {
            return "RimSynapse - StoryTeller";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            
            Rect viewRect = new Rect(0, 0, inRect.width - 20f, 2000f);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            listing.Begin(viewRect);

            listing.Label("RimSynapse StoryTeller Debug", tooltip: "Diagnostic view of the Aura Algorithm's faction and narrative state.");
            listing.GapLine();

            if (Current.ProgramState != ProgramState.Playing || Find.World == null)
            {
                listing.Label("Load a save to view StoryTeller diagnostics.");
                listing.End();
                Widgets.EndScrollView();
                return;
            }

            var stWorldComp = Find.World.GetComponent<SynapseStoryTellerWorldComponent>();

            // ── Pacing Overview ──
            listing.Label("── Pacing ──");
            listing.Gap(4f);
            if (stWorldComp != null)
            {
                listing.Label($"Global Pacing Multiplier: {stWorldComp.GlobalPacingMultiplier:F2}");
                listing.Label($"Tension Modifier: {stWorldComp.TensionModifier:F2}");
                listing.Label($"Knowledge Packets In Transit: {stWorldComp.inTransitKnowledge?.Count ?? 0}");
            }
            else
            {
                listing.Label("StoryTeller WorldComponent not found.");
            }

            listing.GapLine();
            listing.Label("── Faction Backstories ──");
            listing.Gap(4f);

            if (listing.ButtonText("Regenerate All Faction Backstories"))
            {
                SynapseFactionEvaluator.ForceRegenerateAll();
                Messages.Message("Faction backstories cleared. They will regenerate in the background.", MessageTypeDefOf.NeutralEvent, false);
            }
            listing.Gap(4f);

            listing.Label("── Faction Status ──");
            listing.Gap(4f);

            if (Find.FactionManager != null)
            {
                foreach (var faction in Find.FactionManager.AllFactions)
                {
                    if (faction == null || faction.IsPlayer || faction.Hidden) continue;
                    if (faction.leader == null || !faction.leader.RaceProps.Humanlike) continue;

                    string leaderName = faction.leader.Name?.ToStringShort ?? "Unknown";
                    int goodwill = faction.PlayerGoodwill;
                    int naturalGoodwill = faction.NaturalGoodwill;

                    // Check leader backstory status
                    var coreComp = faction.leader.TryGetComp<RimSynapse.Comps.SynapseCorePawnComp>();
                    bool hasBackstory = coreComp != null && coreComp.memories.Any(m => m.memoryType == "Backstory");
                    int backstoryCount = coreComp?.memories.Count(m => m.memoryType == "Backstory") ?? 0;

                    string backstoryStatus = hasBackstory
                        ? $"✓ {backstoryCount} memories"
                        : "✗ Pending";

                    // Check faction history status
                    string historyStatus = "✗ Pending";
                    string customGoodwillInfo = "";
                    if (stWorldComp != null)
                    {
                        var tracker = stWorldComp.factionStoryTrackers.Find(f => f.factionId == faction.GetUniqueLoadID());
                        if (tracker != null)
                        {
                            historyStatus = !string.IsNullOrEmpty(tracker.factionHistory)
                                ? $"✓ Generated ({tracker.factionHistory.Length} chars)"
                                : "✗ Pending";

                            if (tracker.customNaturalGoodwill.HasValue)
                            {
                                customGoodwillInfo = $" | Custom NatGoodwill: {tracker.customNaturalGoodwill.Value}";
                            }
                        }
                    }

                    listing.Label($"■ {faction.Name} ({faction.def.LabelCap})");
                    listing.Label($"  Leader: {leaderName} | Goodwill: {goodwill} (Natural: {naturalGoodwill}){customGoodwillInfo}");
                    listing.Label($"  Leader Backstory: {backstoryStatus} | Faction History: {historyStatus}");

                    // Show faction history text if available
                    if (stWorldComp != null)
                    {
                        var tracker = stWorldComp.factionStoryTrackers.Find(f => f.factionId == faction.GetUniqueLoadID());
                        if (tracker != null && !string.IsNullOrEmpty(tracker.factionHistory))
                        {
                            listing.Label($"  History: \"{tracker.factionHistory}\"");
                        }
                    }

                    // Show leader backstory memories if available
                    if (hasBackstory && coreComp != null)
                    {
                        foreach (var memory in coreComp.memories.Where(m => m.memoryType == "Backstory"))
                        {
                            string tags = memory.tags != null ? string.Join(", ", memory.tags) : "";
                            listing.Label($"    • [{tags}] {memory.summary}");
                        }
                    }

                    listing.Gap(6f);
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
}
