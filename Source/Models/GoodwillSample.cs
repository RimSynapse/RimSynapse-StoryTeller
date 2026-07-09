using Verse;

namespace RimSynapse.StoryTeller.Models
{
    /// <summary>
    /// A point-in-time goodwill sample for historical tracking.
    /// Used by the StoryTeller's faction relationship tracker.
    /// </summary>
    public class GoodwillSample : IExposable
    {
        public int gameTick;
        public int goodwill;

        public void ExposeData()
        {
            Scribe_Values.Look(ref gameTick, "gameTick");
            Scribe_Values.Look(ref goodwill, "goodwill");
        }
    }
}
