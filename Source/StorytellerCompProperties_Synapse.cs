using RimWorld;

namespace RimSynapse.StoryTeller
{
    public class StorytellerCompProperties_Synapse : StorytellerCompProperties
    {
        public float incidentsTargetDays = 10f;
        public float threatsTargetDays = 10f;

        public StorytellerCompProperties_Synapse()
        {
            this.compClass = typeof(StorytellerComp_Synapse);
        }
    }
}
