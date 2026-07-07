# RimSynapse-Storyteller Design Document

## Overview
RimSynapse-Storyteller introduces an AI-driven storyteller that generates narrative events and manages overarching story threads. It acts as an AI director, hooking into RimWorld's incident system to provide a cohesive and evolving narrative.

**Dependencies:** RimSynapse Core (for LLM connectivity and context). Soft dependency on RimSynapse-Psychology for enhanced pawn-level narrative hooks.

## Core Features

### 1. Narrative Thread System
*   **Concept:** Events are categorized and tagged to form ongoing "threads" (e.g., `food_shortage`, `mechanoid_war`, `internal_strife`).
*   **Lifecycle:** Threads have a weight that decays over time. Active threads are injected into the Context Assembly, influencing how the LLM generates future events and pawn dialogues.
*   **Resolution:** Threads can be explicitly resolved, resulting in a generated `resolutionSummary` that gets stored in the colony's history.
*   **Categories:** Threads are categorized into overarching themes like `crisis`, `social`, `political`, `economic`, and `military`.

### 2. AI Storyteller / Director
*   **Incident Hooks:** Integrates with RimWorld's core incident system. Instead of purely random events based on wealth, the AI evaluates the colony's current narrative threads and state to generate contextually appropriate events (raids, social gatherings, quests, etc.).

### 3. AI Advisor
*   **Colony Analysis:** Periodically analyzes the colony's state (using Core's Context Assembly) to provide actionable recommendations.
*   **Structured Output:** Generates advice in JSON format, targeting specific pawns or colony metrics:
    ```json
    {
        "advices": [
            {
                "target": "Engie",
                "reason": "Engie has been working non-stop and mood is dropping",
                "action": "schedule_recreation"
            }
        ]
    }
    ```

### 4. Colony Memory Summarizer
*   **Compression:** To maintain a long-term colony history without overflowing the LLM's context window, the system periodically summarizes older events and interactions into concise paragraphs.

## Data Model (Scribe-Persisted)

All colony-level data is serialized via a `WorldComponent` in the RimWorld save file.

```csharp
public class SynapseWorldComponent : WorldComponent
{
    public List<NarrativeThread> narrativeThreads = new();
    public List<InteractionRecord> interactionHistory = new();
    public List<PromptLogEntry> promptLog = new(); // Used alongside DevTools

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref narrativeThreads, "narrativeThreads", LookMode.Deep);
        Scribe_Collections.Look(ref interactionHistory, "interactionHistory", LookMode.Deep);
        Scribe_Collections.Look(ref promptLog, "promptLog", LookMode.Deep);
    }
}

public class NarrativeThread : IExposable
{
    public string keyword;
    public string category;
    public string description;
    public float weight;
    public float decayRate;
    public int timesReferenced;
    public bool isResolved;
    public string resolutionSummary;

    public void ExposeData() { /* Scribe_Values for fields */ }
}
```
