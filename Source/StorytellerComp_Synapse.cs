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
            var coreComp = Find.World.GetComponent<RimSynapse.SynapseCoreWorldComponent>();
            var stComp = Find.World.GetComponent<SynapseStoryTellerWorldComponent>();
            if (stComp == null || coreComp == null) yield break;

            if (Find.CurrentMap != null)
            {
                int currentHour = GenLocalDate.HourOfDay(Find.CurrentMap);
                int currentDay = GenLocalDate.DayOfYear(Find.CurrentMap);

                // Check every 6 hours
                if (currentHour % 6 == 0 && stComp.lastInvestigationHour != currentHour)
                {
                    stComp.lastInvestigationHour = currentHour;
                    RimSynapse.StoryTeller.SynapseStorytellerOpportunistic.TriggerPacingAdjustment();
                }
            }

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
                        // Temporarily reduce pacing to prevent further events while LLM is thinking
                        stComp.GlobalPacingMultiplier = 0.001f;
                        RimSynapse.StoryTeller.SynapseStorytellerOpportunistic.TriggerEventSelection(category, target);
                    }
                }
            }
        }
    }
}
