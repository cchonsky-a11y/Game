# CLAUDE.md — The Butterfly Effect

## What this project is
A turn-based historical strategy game for Apple platforms. We are currently building **prototypes only**, starting with P0: a text-based simulation in C#.

## Read these first
- Current scope (what to build now): @docs/PROTOTYPE_SCOPE.md
- Canonical rules and numbers: @docs/SYSTEMS.md
- Principles and constraints: @docs/VISION.md
- Process and sync rules: @docs/BUILD_GUIDE.md

The full design document (docs/butterfly-effect-gdd.md) is background reference only. **It is not a build list.**

## Rules
1. Build **only** what PROTOTYPE_SCOPE.md lists as in scope. Never build anything on its out-of-scope list, even if another document describes it.
2. SYSTEMS.md is the source of truth for rules and numbers. Put every tuning value in `data/tuning.json`; never hard-code numbers.
3. If documents conflict, or a rule you need is missing, **stop and ask**. Do not guess or invent rules.
4. The simulation is deterministic. All randomness goes through the seeded generator in Butterfly.Core. No `DateTime.Now`, no unseeded `Random`, no iteration over unordered collections where order affects results.
5. Every formula gets a unit test using the reference values in BUILD_GUIDE.md §8. Run all tests before finishing any task.
6. Record every meaningful state change in the event log with its immediate causes, actors, and effects.
7. Never add language-model integration, graphics, networking, or analytics.
8. The hard constraints in SYSTEMS.md §14 must never be violated by any code or content.
9. Work in small slices: one slice per task, ending with passing tests.
10. If you change a number or rule, list the documents that must be updated (SYSTEMS.md, GDD Appendix A, tuning.json, DECISIONS.md) in your summary.

## Project layout
- `src/Butterfly.Core/` — simulation library, .NET Standard 2.1 (must stay Unity-compatible: no APIs beyond .NET Standard 2.1)
- `src/Butterfly.Console/` — text game for human players
- `src/Butterfly.Batch/` — automated strategy runner
- `tests/Butterfly.Core.Tests/` — unit, determinism, and snapshot tests
- `data/tuning.json` — all tuning values; each leaf is `{ "value", "ref" }`, where `ref` names its SYSTEMS.md section or a `PROPOSED P0-xx` entry in `docs/P0_PROPOSALS.md` (placeholders awaiting approval)
- `data/content/` — projects, institutions, Echoes, text templates

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Play: `dotnet run --project src/Butterfly.Console -- --seed 42`
- Batch: `dotnet run --project src/Butterfly.Batch -- --runs 100`

## Definition of done for a task
- Tests pass, including the determinism test.
- No out-of-scope code added.
- Summary lists any document updates needed.
