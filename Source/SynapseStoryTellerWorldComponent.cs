using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using RimSynapse.Models;

namespace RimSynapse
{
    public class SynapseStoryTellerWorldComponent : WorldComponent
    {
        public Dictionary<string, float> categoryMultipliers = new Dictionary<string, float>();
        public Dictionary<string, float> incidentMultipliers = new Dictionary<string, float>();
        public float GlobalPacingMultiplier = 1.0f;
        public float TensionModifier = 1.0f;
        
        public SynapseStoryTellerWorldComponent(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Collections.Look(ref categoryMultipliers, "categoryMultipliers", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref incidentMultipliers, "incidentMultipliers", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref GlobalPacingMultiplier, "globalPacingMultiplier", 1.0f);
            Scribe_Values.Look(ref TensionModifier, "tensionModifier", 1.0f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (categoryMultipliers == null) categoryMultipliers = new Dictionary<string, float>();
                if (incidentMultipliers == null) incidentMultipliers = new Dictionary<string, float>();
            }
        }

        public float GetCategoryMultiplier(string categoryDefName)
        {
            if (categoryMultipliers.TryGetValue(categoryDefName, out float mult))
            {
                return mult;
            }
            return 1.0f;
        }

        public float GetIncidentMultiplier(string incidentDefName)
        {
            if (incidentMultipliers.TryGetValue(incidentDefName, out float mult))
            {
                return mult;
            }
            return 1.0f;
        }

        public float CalculateDynamicThreatPoints(IIncidentTarget target, float vanillaPoints)
        {
            Map map = target as Map;
            if (map == null) return vanillaPoints * TensionModifier;

            float combatCompetence = 0f;
            int freeColonists = 0;

            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.Downed || pawn.Dead) continue;
                
                freeColonists++;
                
                // Add points for combat skills
                combatCompetence += (pawn.skills?.GetSkill(RimWorld.SkillDefOf.Shooting)?.Level ?? 0) * 5f;
                combatCompetence += (pawn.skills?.GetSkill(RimWorld.SkillDefOf.Melee)?.Level ?? 0) * 5f;

                // Add points for equipped weapons
                if (pawn.equipment?.Primary != null)
                {
                    // A simple heuristic: ranged weapons give more threat points, higher market value weapons mean better tech
                    combatCompetence += pawn.equipment.Primary.MarketValue / 10f;
                }
                
                // Add points for apparel (armor)
                if (pawn.apparel != null)
                {
                    foreach (var app in pawn.apparel.WornApparel)
                    {
                        combatCompetence += app.MarketValue / 20f;
                    }
                }
            }

            // Also factor in installed security structures (turrets)
            float securityPower = 0f;
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (t.def.building != null && t.def.building.IsTurret)
                {
                    securityPower += t.MarketValue / 5f;
                }
            }

            // The final threat point calculation completely ignores statues, floors, and raw silver.
            // It relies exclusively on the colony's actual combat capacity and the LLM's Tension factor.
            // We scale it slightly to match RimWorld's general point distribution (base 35 points per colonist roughly).
            float baseColonistPoints = freeColonists * 35f;
            float actualThreat = (baseColonistPoints + combatCompetence + securityPower) * TensionModifier;

            // Ensure it doesn't drop to 0 or go to extreme integer overflows
            return UnityEngine.Mathf.Clamp(actualThreat, 35f, 10000f);
        }
    }
}
