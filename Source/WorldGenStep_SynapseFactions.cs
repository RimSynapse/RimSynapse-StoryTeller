using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimSynapse.StoryTeller
{
    public class WorldGenStep_SynapseFactions : WorldGenStep
    {
        public override int SeedPart => 82746193;

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            RimSynapse.SynapseLog.Info("storyteller", "[RimSynapse-StoryTeller] Applying organic modifiers to Faction Natural Goodwill...");
            
            if (Find.FactionManager == null) return;
            var stWorldComp = Find.World.GetComponent<SynapseStoryTellerWorldComponent>();
            if (stWorldComp == null) return;

            foreach (var faction in Find.FactionManager.AllFactions)
            {
                if (faction == null || faction.IsPlayer || faction.Hidden) continue;
                if (faction.leader == null || !faction.leader.RaceProps.Humanlike) continue;

                var tracker = stWorldComp.GetOrCreateStoryTracker(faction.GetUniqueLoadID());

                if (!tracker.customNaturalGoodwill.HasValue)
                {
                    int vanillaBaseline = faction.NaturalGoodwill;
                    int randomOffset = Rand.RangeInclusive(-20, 20);
                    int finalTarget = UnityEngine.Mathf.Clamp(vanillaBaseline + randomOffset, -100, 100);

                    tracker.customNaturalGoodwill = finalTarget;
                    tracker.customNaturalGoodwillReason = "Organic baseline (World Generation)";
                }
            }
        }
    }
}

