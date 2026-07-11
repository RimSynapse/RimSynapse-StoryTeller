using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller.Patches
{
    [HarmonyPatch(typeof(Faction), "TryGenerateNewLeader")]
    public static class Patch_Faction_TryGenerateNewLeader
    {
        public static void Postfix(Faction __instance)
        {
            if (__instance != null && __instance.leader != null)
            {
                // New leader generated, ensure we check and evaluate them
                SynapseFactionEvaluator.EvaluateFaction(__instance);
            }
        }
    }
}
