# WellEdge — Training Repository

## What This Repo Is

This is a personal training repository for ramping up on the Full-Stack Developer role at the Well Intervention platform (iWiNG). The goal is to build real, working software while learning the target stack — not to produce production code.

The project built here is **WellEdge**: a mini real-time edge monitoring system for a single oil well. It simulates what the actual edge server does — ingesting sensor data, processing it through a pipeline, and streaming results to a live dashboard.

## Repo Structure

```
halliburton-training/
  CLAUDE.md                   ← you are here
  documents/                  ← all markdown files live here
    LEARNING_PLAN.md          ← 8-phase learning plan with knowledge checks
  src/                        ← code goes here (created in Phase 1)
    WellEdge.Domain/
    WellEdge.Application/
    WellEdge.Infrastructure/
    WellEdge.Worker/
    welldash/                 ← Angular frontend (created in Phase 7)
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 13 / .NET 9 |
| Runtime | ASP.NET Core, Worker Services |
| Pipeline | System.Threading.Channels |
| Events | MediatR |
| Real-time | SignalR |
| Database | MongoDB 7 (time-series collections) |
| Messaging | MQTT via MQTTnet + Mosquitto (local broker) |
| Frontend | Angular 18+, Signals, RxJS, SignalR JS client |
| Observability | Serilog |

## Learning Rules

These rules apply to all interactions inside this repo. They exist to protect the learning process.

**1. Never give away knowledge check answers.**
Each phase in `documents/LEARNING_PLAN.md` ends with a set of check questions. If the user asks you something that maps directly to one of those questions, do not answer it directly. Instead, ask a follow-up question that helps them reason through it themselves. Example: if they ask "what happens when a bounded channel is full?", ask "what do you think the producer's options are, and what are the trade-offs of each?"

**2. Guide, don't solve.**
When the user is stuck on a build task, give hints and point to the right concept or documentation section. Do not write the implementation for them unless they explicitly ask for it after demonstrating they understand the concept.

**3. Explain the why, not just the what.**
If you explain a concept, always include why it matters in the context of a real-time edge system. Abstract explanations without domain grounding are less useful here.

**4. Challenge assumptions.**
If the user proposes an approach, ask about the edge cases first. This mirrors the kind of thinking needed for high-frequency, reliability-critical systems.

**5. When the user explicitly asks for code, provide it.**
The rules above apply to learning-mode questions. If the user explicitly says "write this for me" or "show me the implementation", that is a clear signal to help directly.

## Conventions

- C# projects: `WellEdge.<Layer>` (e.g., `WellEdge.Domain`, `WellEdge.Infrastructure`)
- Angular app: `welldash`
- All markdown documents: stored in `documents/`
- Sensor simulation: 4 Hz (250 ms interval), single well ID `W-001`
- All timestamps: UTC

## Phase Tracker

Update this as phases are completed.

| Phase | Topic | Status |
|-------|-------|--------|
| 1 | C# Foundations | Not started |
| 2 | Worker Services + Clean Architecture | Not started |
| 3 | System.Threading.Channels | Not started |
| 4 | MediatR + Event-Driven | Not started |
| 5 | SignalR | Not started |
| 6 | MongoDB .NET | Not started |
| 7 | MQTT + Angular Signals | Not started |
| 8 | Reliability & Production Patterns | Not started |
