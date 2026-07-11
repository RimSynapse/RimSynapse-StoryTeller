using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller
{
    /// <summary>
    /// Intercepts major threat letters to rewrite their text via LLM,
    /// adding Synapse's spunky personality and commenting on the difficulty.
    /// </summary>
    [HarmonyPatch(typeof(LetterStack), "ReceiveLetter", new Type[] { typeof(Letter), typeof(string), typeof(int), typeof(bool) })]
    public static class Patch_LetterStack_ReceiveLetter
    {
        private static System.Collections.Generic.HashSet<Letter> _processedLetters = new System.Collections.Generic.HashSet<Letter>();

        public static bool Prefix(LetterStack __instance, Letter let, string debugInfo, int delayTicks, bool playSound)
        {
            // Temporarily disable letter rewrites to prevent breaking dynamic grammar resolution
            return true;
        }
    }
}
