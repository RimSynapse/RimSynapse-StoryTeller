using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using RimSynapse.Models;
using RimSynapse.StoryTeller.Models;

namespace RimSynapse.StoryTeller
{
    public class SynapseStoryTellerWorldComponent : WorldComponent
    {
        public Dictionary<string, float> categoryMultipliers = new Dictionary<string, float>();
        public Dictionary<string, float> incidentMultipliers = new Dictionary<string, float>();
        public float GlobalPacingMultiplier = 1.0f;
        public float BasePacingMultiplier = 1.0f;
        public float TensionModifier = 1.0f;

        // ── Faction Story Trackers (extended fields for StoryTeller) ──
        public List<FactionStoryTracker> factionStoryTrackers = new List<FactionStoryTracker>();

        // ── Knowledge Propagation System ──
        public List<KnowledgePacket> inTransitKnowledge = new List<KnowledgePacket>();

        public SynapseStoryTellerWorldComponent(World world) : base(world)
        {
        }

        public int lastInvestigationHour = -1;
        public int lastFlavorEventDay = -1;

        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Collections.Look(ref categoryMultipliers, "categoryMultipliers", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref incidentMultipliers, "incidentMultipliers", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref GlobalPacingMultiplier, "globalPacingMultiplier", 1.0f);
            Scribe_Values.Look(ref BasePacingMultiplier, "basePacingMultiplier", 1.0f);
            Scribe_Values.Look(ref TensionModifier, "tensionModifier", 1.0f);
            Scribe_Collections.Look(ref factionStoryTrackers, "factionStoryTrackers", LookMode.Deep);
            Scribe_Collections.Look(ref inTransitKnowledge, "inTransitKnowledge", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (categoryMultipliers == null) categoryMultipliers = new Dictionary<string, float>();
                if (incidentMultipliers == null) incidentMultipliers = new Dictionary<string, float>();
                if (factionStoryTrackers == null) factionStoryTrackers = new List<FactionStoryTracker>();
                if (inTransitKnowledge == null) inTransitKnowledge = new List<KnowledgePacket>();
            }

            Scribe_Values.Look(ref lastInvestigationHour, "lastInvestigationHour", -1);
            Scribe_Values.Look(ref lastFlavorEventDay, "lastFlavorEventDay", -1);
        }



        // ── Faction Story Tracker Accessors ──

        public FactionStoryTracker GetOrCreateStoryTracker(string factionId)
        {
            var tracker = factionStoryTrackers.Find(f => f.factionId == factionId);
            if (tracker == null)
            {
                tracker = new FactionStoryTracker { factionId = factionId };
                factionStoryTrackers.Add(tracker);
            }
            return tracker;
        }

        // ── Pacing/Threat ──

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

            float baseColonistPoints = freeColonists * 35f;
            float actualThreat = (baseColonistPoints + combatCompetence + securityPower) * TensionModifier;

            return UnityEngine.Mathf.Clamp(actualThreat, 35f, 10000f);
        }

        // ── Knowledge Propagation ──

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (Find.TickManager.TicksGame % 1000 == 0 && inTransitKnowledge.Count > 0)
            {
                int currentTick = Find.TickManager.TicksGame;
                for (int i = inTransitKnowledge.Count - 1; i >= 0; i--)
                {
                    var packet = inTransitKnowledge[i];
                    if (currentTick >= packet.arrivalTick)
                    {
                        ProcessArrivingKnowledge(packet);
                        inTransitKnowledge.RemoveAt(i);
                    }
                }
            }
        }

        private void ProcessArrivingKnowledge(KnowledgePacket packet)
        {
            var coreWorldComp = Find.World.GetComponent<SynapseCoreWorldComponent>();
            if (coreWorldComp == null) return;

            var tracker = coreWorldComp.factionTrackers.Find(f => f.factionId == packet.targetFactionId);
            if (tracker == null)
            {
                tracker = new FactionRelationshipTracker { factionId = packet.targetFactionId };
                coreWorldComp.factionTrackers.Add(tracker);
            }

            tracker.perceivedWealth = UnityEngine.Mathf.Lerp(tracker.perceivedWealth, packet.payloadWealth, 0.2f);
            tracker.perceivedStrength = UnityEngine.Mathf.Lerp(tracker.perceivedStrength, packet.payloadStrength, 0.2f);
        }

        public void BroadcastKnowledge(Faction originFaction, float actualWealth, float actualStrength)
        {
            if (originFaction == null || originFaction.IsPlayer) return;

            foreach (Faction targetFaction in Find.FactionManager.AllFactionsVisible)
            {
                if (targetFaction == originFaction || targetFaction.IsPlayer || targetFaction.Hidden) continue;

                float relation = originFaction.GoodwillWith(targetFaction);
                float knowledgeTransferFactor = UnityEngine.Mathf.Clamp01((relation + 100f) / 200f * 0.9f + 0.1f);

                float payloadWealth = actualWealth * knowledgeTransferFactor;
                float payloadStrength = actualStrength * knowledgeTransferFactor;

                int distance = 50;
                if (originFaction.def.settlementGenerationWeight > 0 && targetFaction.def.settlementGenerationWeight > 0)
                {
                    var originBase = Find.WorldObjects.Settlements.Find(s => s.Faction == originFaction);
                    var targetBase = Find.WorldObjects.Settlements.Find(s => s.Faction == targetFaction);
                    if (originBase != null && targetBase != null)
                    {
                        distance = Find.WorldGrid.TraversalDistanceBetween(originBase.Tile, targetBase.Tile, true, 100);
                        if (distance > 100 || distance < 0) distance = 100;
                    }
                }

                int delayTicks = distance * 1000;

                inTransitKnowledge.Add(new KnowledgePacket
                {
                    sourceFactionId = originFaction.GetUniqueLoadID(),
                    targetFactionId = targetFaction.GetUniqueLoadID(),
                    payloadWealth = payloadWealth,
                    payloadStrength = payloadStrength,
                    arrivalTick = Find.TickManager.TicksGame + delayTicks
                });
            }

            // Update originator's own tracker directly
            var coreWorldComp = Find.World.GetComponent<SynapseCoreWorldComponent>();
            if (coreWorldComp != null)
            {
                var originTracker = coreWorldComp.factionTrackers.Find(f => f.factionId == originFaction.GetUniqueLoadID());
                if (originTracker == null)
                {
                    originTracker = new FactionRelationshipTracker { factionId = originFaction.GetUniqueLoadID() };
                    coreWorldComp.factionTrackers.Add(originTracker);
                }
                originTracker.perceivedWealth = actualWealth;
                originTracker.perceivedStrength = actualStrength;
            }
        }
    }
}
