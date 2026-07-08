using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using RimSynapse.Models;

namespace RimSynapse
{
    public class SynapseStoryTellerWorldComponent : WorldComponent
    {
        public List<NarrativeThread> narrativeThreads = new List<NarrativeThread>();
        public List<FactionRelationshipTracker> factionTrackers = new List<FactionRelationshipTracker>();
        
        // Use a List for Scribe serialization since Queue is not natively supported by Scribe_Collections
        public List<PastEvent> backlogQueueList = new List<PastEvent>();
        private Queue<PastEvent> _backlogQueue = new Queue<PastEvent>();

        // Interaction history stub for future LLM conversation records
        // public List<InteractionRecord> interactionHistory = new List<InteractionRecord>();
        
        public SynapseStoryTellerWorldComponent(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Collections.Look(ref narrativeThreads, "narrativeThreads", LookMode.Deep);
            Scribe_Collections.Look(ref factionTrackers, "factionTrackers", LookMode.Deep);
            Scribe_Collections.Look(ref backlogQueueList, "backlogQueueList", LookMode.Deep);

            // Scribe doesn't support Queue natively, so we sync with a List
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                backlogQueueList.Clear();
                backlogQueueList.AddRange(_backlogQueue);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (narrativeThreads == null) narrativeThreads = new List<NarrativeThread>();
                if (factionTrackers == null) factionTrackers = new List<FactionRelationshipTracker>();
                if (backlogQueueList == null) backlogQueueList = new List<PastEvent>();
                
                _backlogQueue.Clear();
                foreach (var pastEvent in backlogQueueList)
                {
                    _backlogQueue.Enqueue(pastEvent);
                }
            }
        }

        public void EnqueuePastEvent(PastEvent pastEvent)
        {
            _backlogQueue.Enqueue(pastEvent);
        }

        public bool TryDequeuePastEvent(out PastEvent pastEvent)
        {
            if (_backlogQueue.Count > 0)
            {
                pastEvent = _backlogQueue.Dequeue();
                return true;
            }
            
            pastEvent = null;
            return false;
        }

        public int BacklogCount => _backlogQueue.Count;
    }
}
