using Verse;

namespace RimSynapse.Models
{
    public class PastEvent : IExposable
    {
        public int gameTick;
        public string eventDescription;
        public string category;

        public void ExposeData()
        {
            Scribe_Values.Look(ref gameTick, "gameTick");
            Scribe_Values.Look(ref eventDescription, "eventDescription");
            Scribe_Values.Look(ref category, "category");
        }
    }
}
