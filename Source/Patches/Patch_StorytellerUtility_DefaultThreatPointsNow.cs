using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller
{
    /// <summary>
    /// Replaces vanilla's wealth-based threat calculation with a combat-competency model.
    /// The LLM's TensionModifier further scales the result for narrative pacing.
    /// </summary>
    [HarmonyPatch(typeof(StorytellerUtility), "DefaultThreatPointsNow")]
    public static class Patch_StorytellerUtility_DefaultThreatPointsNow
    {
        public static void Postfix(IIncidentTarget target, ref float __result)
        {
            if (Find.Storyteller != null && Find.Storyteller.def != null && Find.Storyteller.def.defName == "Synapse")
            {
                var worldComp = Find.World.GetComponent<SynapseStoryTellerWorldComponent>();
                if (worldComp != null)
                {
                    __result = worldComp.CalculateDynamicThreatPoints(target, __result);
                }
            }
        }
    }
}
