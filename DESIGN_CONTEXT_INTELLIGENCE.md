# RimSynapse-Storyteller — Context Intelligence Addendum

> **Source:** Ideas extracted from Core context-planning-1 review.
> These features were originally designed in Core's plan but belong to the Storyteller
> domain — they require LLM analysis, create persistent narrative data, and drive
> story-level decision making.
>
> **Status:** Design notes for future planning iteration.

---

## 1. Backstory Resonance Engine

When events occur in the colony, the Storyteller should detect **correlations between a pawn's backstory and current events** — making stories feel dynamic and personally relevant.

### How It Works

The Storyteller (not Core) uses the LLM to analyze backstory relevance:

1. When a significant event fires (raid, plague, trade, quest), Storyteller sends a lightweight LLM query:
   - Input: pawn backstory text + event summary
   - Output: relevance score (0–10) + keyword tags
2. If relevance is high (≥ 6), Storyteller:
   - Boosts the event's context weight for that pawn's future queries
   - Creates a "resonance memory" in Psychology (via its API) linking the pawn's past to the present
   - Injects the resonance into narrative thread context

### Why Storyteller, Not Core

Core reads vanilla game state — it doesn't make LLM calls to *interpret* that state. Backstory resonance requires semantic understanding (is "child soldier" related to "raid"?), which is an LLM analysis task. Core just provides the raw backstory text; Storyteller decides what it means.

### Pawn Arrival: Robust Backstory Generation

When a new pawn joins the colony (recruitment, wanderer, pod crash), Storyteller should:

1. Read the pawn's vanilla backstory (childhood title + adulthood title + `baseDesc`)
2. Read their traits, skills, and any existing relationships
3. Send a **one-time LLM query** to generate a rich, narrative backstory
4. Store the result as:
   - A **defining memory** in Psychology (`memoryType: "backstory"`, high `baseWeight`, very low `decayRate`)
   - Tagged with 2–5 keywords extracted by the LLM (e.g., `["soldier", "trauma", "loyalty"]`)
5. These keywords are then used by the resonance engine for future event correlation

```csharp
// Storyteller calls Psychology's API to plant the backstory memory
public void OnPawnJoinedColony(Pawn pawn)
{
    var backstoryText = BuildBackstoryPrompt(pawn);

    SynapseClient.PromptAsync(
        _storyHandle,
        "Generate a detailed backstory paragraph for this colonist. " +
        "Also extract 3-5 keyword tags that summarize their defining experiences. " +
        "Return JSON: { \"backstory\": \"...\", \"tags\": [\"...\"] }",
        backstoryText,
        result =>
        {
            if (!result.success) return;
            var parsed = ParseBackstoryResult(result.content);

            // Plant as a defining memory via Psychology API
            SynapsePsychology.AddMemory(pawn, new WeightedMemory
            {
                summary = parsed.backstory,
                memoryType = "backstory",
                tags = parsed.tags,
                weight = 1.0f,
                baseWeight = 1.0f,
                decayRate = 0.001f,  // almost never decays
                timesReferenced = 0,
            });
        });
}
```

---

## 2. Faction Goodwill Integral Tracking

The **area-under-the-curve** approach for faction relationships lives in Storyteller, not Core.

### Rationale

Core provides raw access to `faction.PlayerGoodwill` (an int from RimWorld's API). Storyteller is the mod that:
- Samples goodwill periodically (every 2–3 in-game days)
- Computes the moving average (integral)
- Derives trajectory (improving vs deteriorating)
- Calculates power, ideology, and opportunity dimensions

### Data Model (extends SynapseWorldComponent)

```csharp
// Added to Storyteller's existing SynapseWorldComponent
public class SynapseWorldComponent : WorldComponent
{
    // ... existing fields ...
    public List<FactionRelationshipTracker> factionTrackers = new();

    public override void ExposeData()
    {
        // ... existing ...
        Scribe_Collections.Look(ref factionTrackers, "factionTrackers", LookMode.Deep);
    }
}

public class FactionRelationshipTracker : IExposable
{
    public string factionId;
    public List<GoodwillSample> goodwillHistory = new();
    public float goodwillIntegral;           // computed moving average
    public int lastEventTick;                // last faction-related event tick

    public void ExposeData() { /* Scribe fields */ }
}

public class GoodwillSample : IExposable
{
    public int gameTick;
    public int goodwill;     // -100 to +100
    public void ExposeData() { /* ... */ }
}
```

### Sampling Schedule

- **Default:** Every 2 in-game days (120,000 ticks)
- **Immediate resample** when a faction event concludes:
  - Raid from/against faction
  - Trade caravan arrives/departs
  - Quest involving faction completes
  - Goodwill change event (gift, demand, etc.)
  - Alliance or hostility status change

### Power / Ideology / Opportunity Dimensions

Storyteller enriches the raw goodwill with strategic dimensions:

| Dimension | Source | Values | Use |
|---|---|---|---|
| **Power** | `faction.def.techLevel` + settlement count | `weak`, `moderate`, `strong`, `dominant` | LLM gauges threat/opportunity |
| **Ideology** | `faction.ideos?.PrimaryIdeo?.name` or archetype | Ideology name string | Cultural conflict/alignment |
| **Opportunity** | Nearest settlement distance + trade goods overlap | `high`, `moderate`, `low`, `none` | Strategic potential |

### What Storyteller Sends to Core's Context

When Storyteller builds its context packet, it provides faction data in a format Core can include:

```json
{
  "factionName": "Rough Outlanders",
  "factionType": "Outlander Rough",
  "currentGoodwill": -45,
  "goodwillIntegral": -12,
  "trajectory": "deteriorating",
  "recentEvent": "Raided us 2 days ago",
  "power": "strong",
  "ideology": "Individualist",
  "opportunity": "trade routes available but trust is low"
}
```

---

## 3. Dynamic Weight Boosting

The Storyteller is responsible for **adjusting context weights** based on narrative relevance. Core provides the default weights; Storyteller overrides them.

### Boost Rules (Storyteller-owned)

| Condition | Slot Affected | Boost | Example |
|---|---|---|---|
| Backstory resonance detected | Backstory slot | +3 | "Child soldier" backstory + raid event |
| Active narrative thread matches event | Thread slot | +2 | `food_shortage` thread + harvest failure |
| Faction just had an event | Faction slot | +3 | Trade caravan arrived yesterday |
| Pawn memory tags overlap with event | Memory slot | +2 | Memory tagged "betrayal" + social conflict |

### How Storyteller Overrides Weights

Storyteller uses the `ContextTierMask` + per-slot weight overrides when calling Core's context API:

```csharp
// Storyteller builds its context request with boosted weights
var options = new ChatOptions
{
    eventType = "event",
    contextTiers = ContextTierMask.Full,
    weightOverrides = new Dictionary<string, float>
    {
        { "backstory", 9f },       // boosted from 6
        { "factionTrackers", 7f }, // boosted from 4
    },
};
```

---

## 4. Memory Export / Import

The ability to export and import Synapse data (memories, personality, threads) for debug and mod-removal compatibility is a **Storyteller + DevTools** function.

### Export

Storyteller provides the export logic since it owns the WorldComponent with the richest data:

```csharp
public static class SynapseDataExport
{
    /// <summary>
    /// Export all Synapse data from the current save as a standalone XML file.
    /// Includes pawn memories (Psychology), narrative threads (Storyteller),
    /// and faction trackers (Storyteller).
    /// </summary>
    public static void ExportToFile(string outputPath)
    {
        // Collects data from:
        // - SynapsePawnComp on each pawn (Psychology mod data)
        // - SynapseWorldComponent (Storyteller mod data)
        // - SynapseContextWorldComponent (Core context settings)
        // Writes standalone XML that can be re-imported
    }
}
```

### UI Access

- **DevTools mod:** Full export + import buttons in the dashboard
- **Storyteller settings UI:** "Export Narrative Data" button (export only)
- **Core:** No export UI — Core stays vanilla+

---

## 5. Relationship to Core

```
Core (vanilla+)                      Storyteller (intelligence layer)
────────────────                     ──────────────────────────────────
Reads raw game state                 Interprets game state via LLM
Default weight table                 Dynamic weight boosters
Basic ContextTierMask defaults       Narrative-aware tier overrides
Provides faction.PlayerGoodwill      Computes goodwill integral + trajectory
Provides pawn.story.backstory        Detects backstory ↔ event resonance
Assembles + trims context            Enriches context with synthetic data
No LLM calls for context             Uses LLM to analyze correlations
```

Core is the **data plumbing**. Storyteller is the **narrative intelligence**.
