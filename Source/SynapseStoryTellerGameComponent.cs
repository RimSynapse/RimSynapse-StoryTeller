using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller
{
    public class SynapseStoryTellerGameComponent : GameComponent
    {
        public SynapseStoryTellerGameComponent(Game game) : base()
        {
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            SynapseFactionEvaluator.CheckAllFactions();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            SynapseFactionEvaluator.CheckAllFactions();
        }
    }
}
