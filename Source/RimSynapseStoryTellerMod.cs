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

            Log.Message("[RimSynapse-StoryTeller] Initialization complete.");
        }
    }
}
