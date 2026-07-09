using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using RimSynapse.Models;

namespace RimSynapse.StoryTeller.UI
{
    public class MainTabWindow_SynapsePerceptions : MainTabWindow
    {
        private Vector2 scrollPosition;

        public override Vector2 RequestedTabSize => new Vector2(600f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 35f), "Faction Perceptions (Synapse)");
            Text.Font = GameFont.Small;

            var coreComp = Find.World.GetComponent<SynapseCoreWorldComponent>();
            if (coreComp == null) return;

            Rect outRect = new Rect(0, 45f, inRect.width, inRect.height - 45f);
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, coreComp.factionTrackers.Count * 65f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (var tracker in coreComp.factionTrackers)
            {
                Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.GetUniqueLoadID() == tracker.factionId);
                if (faction == null || faction.Hidden || faction.IsPlayer) continue;

                Rect rowRect = new Rect(0, y, viewRect.width, 60f);
                Widgets.DrawHighlightIfMouseover(rowRect);

                // Faction Name & Icon
                Rect iconRect = new Rect(5f, y + 5f, 30f, 30f);
                if (faction.def.FactionIcon != null)
                {
                    GUI.color = faction.Color;
                    GUI.DrawTexture(iconRect, faction.def.FactionIcon);
                    GUI.color = Color.white;
                }

                Rect nameRect = new Rect(40f, y + 5f, 200f, 30f);
                Widgets.Label(nameRect, faction.Name);

                // Perceived Wealth
                Rect wealthRect = new Rect(250f, y + 5f, 150f, 25f);
                Widgets.Label(wealthRect, $"Wealth: {tracker.perceivedWealth:N0}");

                // Perceived Strength
                Rect strengthRect = new Rect(250f, y + 30f, 150f, 25f);
                Widgets.Label(strengthRect, $"Strength: {tracker.perceivedStrength:N0}");

                // Analysis
                Rect analysisRect = new Rect(410f, y + 5f, viewRect.width - 410f, 50f);
                float normalizedStrength = (tracker.perceivedStrength * 50f) + 1f;
                float greedRatio = tracker.perceivedWealth / normalizedStrength;

                string analysis = "Neutral";
                if (faction.HostileTo(Faction.OfPlayer))
                {
                    if (greedRatio > 3f)
                    {
                        GUI.color = Color.red;
                        analysis = "TEMPTING TARGET";
                    }
                    else if (greedRatio < 0.5f)
                    {
                        GUI.color = Color.gray;
                        analysis = "Intimidated";
                    }
                    else
                    {
                        GUI.color = Color.yellow;
                        analysis = "Evaluating";
                    }
                }
                else
                {
                    GUI.color = Color.green;
                    analysis = "Friendly";
                }

                Widgets.Label(analysisRect, analysis);
                GUI.color = Color.white;

                Widgets.DrawLineHorizontal(0, y + 59f, viewRect.width);
                y += 65f;
            }

            Widgets.EndScrollView();
        }
    }
}
