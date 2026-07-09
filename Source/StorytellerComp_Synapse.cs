using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller
{
    /// <summary>
    /// Main Synapse storyteller component. Handles the interval tick loop
    /// that decides which events fire, factoring in LLM pacing and faction perceptions.
    /// </summary>
    public partial class StorytellerComp_Synapse : StorytellerComp
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
                        
                        IncidentDef incidentDef = ChooseIncidentByLLMWeights(category, parms, stComp);
                        
                        if (incidentDef != null && incidentDef.Worker.CanFireNow(parms))
                        {
                            yield return new FiringIncident(incidentDef, this, parms);
                        }
                    }
                }
            }
        }
    }
}
