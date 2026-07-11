using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace RimSynapse.Expansions.Royalty
{
    public static class StoryTellerRoyaltyIntegration
    {
        public static string GetAskerTitle(Pawn asker)
        {
            if (!ModsConfig.RoyaltyActive) return null;
            return GetAskerTitleInternal(asker);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetAskerTitleInternal(Pawn asker)
        {
            return asker.royalty?.MostSeniorTitle?.def?.LabelCap;
        }
    }
}
