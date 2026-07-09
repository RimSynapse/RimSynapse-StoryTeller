using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimSynapse.StoryTeller
{
    /// <summary>
    /// Bootstraps all Harmony patches for the StoryTeller module.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("RimSynapse.StoryTeller");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
