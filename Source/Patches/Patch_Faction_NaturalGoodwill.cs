using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller.Patches
{
    [HarmonyPatch(typeof(Faction), "NaturalGoodwill", MethodType.Getter)]
    public static class Patch_Faction_NaturalGoodwill
    {
        public static void Postfix(Faction __instance, ref int __result)
        {
            if (__instance == null || __instance.IsPlayer) return;

            var stWorldComp = Find.World?.GetComponent<SynapseStoryTellerWorldComponent>();
            if (stWorldComp == null) return;

            var tracker = stWorldComp.factionStoryTrackers.Find(f => f.factionId == __instance.GetUniqueLoadID());
            if (tracker != null && tracker.customNaturalGoodwill.HasValue)
            {
                __result = tracker.customNaturalGoodwill.Value;
            }
        }
    }
}
