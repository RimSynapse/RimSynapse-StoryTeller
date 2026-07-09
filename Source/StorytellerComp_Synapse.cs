using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller
{
    public class StorytellerComp_Synapse : StorytellerComp
    {
        protected StorytellerCompProperties_Synapse Props => (StorytellerCompProperties_Synapse)props;

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            var coreComp = Find.World.GetComponent<SynapseCoreWorldComponent>();
            var stComp = Find.World.GetComponent<SynapseStoryTellerWorldComponent>();
            if (stComp == null || coreComp == null) yield break;

            float pacingMultiplier = stComp.GlobalPacingMultiplier;
            
            // Adjust the target days (higher multiplier means fewer days between incidents)
            float actualTargetDays = Props.incidentsTargetDays / Math.Max(0.1f, pacingMultiplier);

            float probPerTick = 1f / (actualTargetDays * 60000f);
            float probPerCheck = probPerTick * 1000f; // Storyteller usually checks every 1000 ticks

            if (Rand.Chance(probPerCheck))
            {
                // PERCEPTION CHECK: Does a hostile faction see us as an easy target?
                Faction highlyMotivatedFaction = GetMotivatedFaction(coreComp);
                
                if (highlyMotivatedFaction != null)
                {
                    IncidentParms raidParms = GenerateParms(IncidentCategoryDefOf.ThreatBig, target);
                    raidParms.faction = highlyMotivatedFaction;
                    
                    // Force drop pods if they are rich and far away
                    if (highlyMotivatedFaction.def.techLevel >= TechLevel.Industrial && Rand.Chance(0.5f))
                    {
                        raidParms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                    }
                    else
                    {
                        raidParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    }

                    if (IncidentDefOf.RaidEnemy.Worker.CanFireNow(raidParms))
                    {
                        yield return new FiringIncident(IncidentDefOf.RaidEnemy, this, raidParms);
                    }
                }
                else
                {
                    IncidentCategoryDef category = ChooseCategory(target, stComp);
                    if (category != null)
                    {
                        IncidentParms parms = GenerateParms(category, target);
                        
                        // Allow the LLM to selectively boost specific incidents within the category
                        IncidentDef incidentDef = ChooseIncidentByLLMWeights(category, parms, stComp);
                        
                        if (incidentDef != null && incidentDef.Worker.CanFireNow(parms))
                        {
                            yield return new FiringIncident(incidentDef, this, parms);
                        }
                    }
                }
            }
        }

        private Faction GetMotivatedFaction(SynapseCoreWorldComponent coreComp)
        {
            if (coreComp == null) return null;

            foreach (var tracker in coreComp.factionTrackers)
            {
                Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.GetUniqueLoadID() == tracker.factionId);
                if (faction != null && faction.HostileTo(Faction.OfPlayer))
                {
                    // If perceived wealth is vastly higher than perceived strength (e.g., 10x ratio based on points)
                    // Note: Wealth is usually around 50k-500k, while Strength points are 500-10000. 
                    // Let's normalize it roughly: 1 strength point ~ 50 wealth.
                    float normalizedStrength = (tracker.perceivedStrength * 50f) + 1f;
                    float greedRatio = tracker.perceivedWealth / normalizedStrength;

                    // If greed ratio is > 3, they are very tempted. Roll a chance to invade.
                    if (greedRatio > 3f && Rand.Chance(0.2f))
                    {
                        // Reset their perception slightly so they don't chain-raid infinitely
                        // We assume the raid is a "scouting in force" which updates their knowledge
                        tracker.perceivedStrength += 500f; 
                        return faction;
                    }
                }
            }
            return null;
        }

        private IncidentCategoryDef ChooseCategory(IIncidentTarget target, SynapseStoryTellerWorldComponent worldComp)
        {
            var weights = new Dictionary<IncidentCategoryDef, float>();
            
            // Default weights
            weights[IncidentCategoryDefOf.ThreatBig] = 2f;
            weights[IncidentCategoryDefOf.ThreatSmall] = 1f;
            weights[IncidentCategoryDefOf.DiseaseHuman] = 0.5f;
            weights[IncidentCategoryDefOf.Misc] = 3f;
            
            var diseaseAnimal = DefDatabase<IncidentCategoryDef>.GetNamedSilentFail("DiseaseAnimal");
            if (diseaseAnimal != null) weights[diseaseAnimal] = 0.2f;

            var orbitalVisitor = DefDatabase<IncidentCategoryDef>.GetNamedSilentFail("OrbitalVisitor");
            if (orbitalVisitor != null) weights[orbitalVisitor] = 1f;

            var factionArrival = DefDatabase<IncidentCategoryDef>.GetNamedSilentFail("FactionArrival");
            if (factionArrival != null) weights[factionArrival] = 1f;

            // Apply LLM category modifiers
            if (worldComp != null)
            {
                foreach (var category in weights.Keys.ToList())
                {
                    weights[category] *= worldComp.GetCategoryMultiplier(category.defName);
                }
            }

            return weights.RandomElementByWeightWithFallback(kvp => kvp.Value, default).Key;
        }

        private IncidentDef ChooseIncidentByLLMWeights(IncidentCategoryDef category, IncidentParms parms, SynapseStoryTellerWorldComponent worldComp)
        {
            var validIncidents = DefDatabase<IncidentDef>.AllDefs
                .Where(d => d.category == category && d.Worker.CanFireNow(parms))
                .ToList();

            if (validIncidents.Count == 0) return null;

            return validIncidents.RandomElementByWeightWithFallback(d => 
            {
                float baseChance = d.Worker.BaseChanceThisGame;
                if (worldComp != null)
                {
                    baseChance *= worldComp.GetIncidentMultiplier(d.defName);
                }
                return baseChance;
            }, null);
        }
    }
}
