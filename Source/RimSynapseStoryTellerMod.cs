using System;
using HarmonyLib;
using Verse;

namespace RimSynapse
{
    public class RimSynapseStoryTellerMod : Mod
    {
        public static string Id = "RimSynapse.StoryTeller";
        public static SynapseModHandle ModHandle;

        public RimSynapseStoryTellerMod(ModContentPack content) : base(content)
        {
            Log.Message("[RimSynapse-StoryTeller] Initializing narrative intelligence layer.");
            
            ModHandle = SynapseCore.Register("RimSynapseStoryTeller", "RimSynapse StoryTeller");

            // Initialize Harmony for StoryTeller patches
            var harmony = new Harmony(Id);
            harmony.PatchAll();

            // Register opportunistic background tasks with scheduling metadata
            SynapseClient.RegisterOpportunisticTask(ModHandle, "StoryTeller_PeriodicInvestigation",
                (System.Func<bool>)RimSynapse.StoryTeller.SynapseStorytellerOpportunistic.TriggerPeriodicInvestigation,
                new RimSynapse.Internal.OpportunisticTaskConfig
                {
                    Label = "12-Hour Storyteller Investigation",
                    Description = "The Aura Algorithm AI evaluates the colony and recent events to decide if an incident should trigger or a world flavor letter should be generated.",
                    Priority = 9, // Highest priority background task, as it impacts gameplay
                    Weight = 3.0f,
                    CooldownTicks = 30000 // 30,000 ticks = 12 in-game hours
                });

            Log.Message("[RimSynapse-StoryTeller] Initialization complete.");
        }
    }
}
