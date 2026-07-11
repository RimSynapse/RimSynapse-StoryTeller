using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSynapse.StoryTeller
{
    /// <summary>
    /// Intercepts quest letter delivery to rewrite quest titles and descriptions via LLM,
    /// giving each quest a narrative flavor that fits your colony's current story.
    /// </summary>
    [HarmonyPatch(typeof(QuestUtility), "SendLetterQuestAvailable")]
    public static class Patch_QuestUtility_SendLetterQuestAvailable
    {
        private static System.Collections.Generic.HashSet<Quest> _processedQuests = new System.Collections.Generic.HashSet<Quest>();

        public static bool Prefix(Quest quest)
        {
            // Temporarily disable quest rewrites because it intercepts quests BEFORE 
            // their grammar (e.g. [asker_nameFull]) is resolved, which breaks quests.
            return true;
        }


    }
}

