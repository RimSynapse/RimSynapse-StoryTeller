using System;
using HarmonyLib;
using Verse;

namespace RimSynapse
{
    public class RimSynapseStoryTellerMod : Mod
    {
        public static string Id = "RimSynapse.StoryTeller";

        public RimSynapseStoryTellerMod(ModContentPack content) : base(content)
        {
            Log.Message("[RimSynapse-StoryTeller] Initializing narrative intelligence layer.");

            // Initialize Harmony for StoryTeller patches
            var harmony = new Harmony(Id);
            harmony.PatchAll();

            Log.Message("[RimSynapse-StoryTeller] Initialization complete.");
        }
    }
}
