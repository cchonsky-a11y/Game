# READ THIS FIRST — The Butterfly Effect handoff to Claude

**Owner / final decision-maker:** Corey  
**Repository:** `cchonsky-a11y/Game`  
**Working branch:** `claude/app-building-prompts-tl4ymh`  
**Branch HEAD verified 2026-10-02:** `53f850d04334577be26175d3bc714c0e07c06b38`  
**Current phase:** **P1 — Playable Game Structure**

This package is the current authoritative handoff assembled from the project conversations, live design/play sessions, prior implementation packages, and test evidence through October 2, 2026.

## Start here

1. Read `01_MASTER_HANDOFF_CURRENT.md` in full.
2. Read `02_CONVERSATION_DERIVED_DECISION_LOG.md`.
3. Read `03_TECHNOLOGY_CAPABILITY_LADDER.md`.
4. Read `04_P1_IMPLEMENTATION_STATUS.md`.
5. Read `05_TEST_EVIDENCE_INDEX.md`.
6. Read the repo's `CLAUDE.md`, `docs/SYSTEMS.md`, `docs/DECISIONS.md`, `docs/HANDOFF.md`, and `docs/butterfly-effect-gdd.md`.
7. Where the old repo docs conflict with this handoff, **do not silently choose the old behavior**. The newer owner-approved decisions in this package supersede them and must be synchronized back into the repo docs/code/tuning/tests.

## Immediate implementation task

The P1 additive code prepared in `artifacts/P1_SPRINT1_INTEGRATION.zip` has **not been committed** because the environment that prepared it did not have the .NET SDK. Preserve the project rule:

> Run `dotnet test` before every commit.

Recommended first sequence:

1. Apply/review the P1 Sprint 1 integration files.
2. Run the complete test suite.
3. Fix compile/test issues without changing owner-approved design.
4. Commit the additive P1 foundation in a small commit.
5. Implement fixed **1-month turns** and explicit **End Month**.
6. Remove normal auto-end behavior.
7. Add opt-in fast-forward that stops on meaningful interruptions.
8. Add P1 state collections to `World`.
9. Migrate one real paid-work thread end-to-end.
10. Begin retiring direct institution stake/buy-in behavior from the player-facing game.

## Phase decision

The prior requirement for **five real-human testers is no longer a P0 gate**. Corey explicitly moved that validation to a later phase when the game has representative graphics/UI. The project has moved into P1.

## Do not use as current authority

These older handoffs are stale and are intentionally **not** included as implementation authority:

- `CLAUDE_MASTER_HANDOFF_2026-09-28.md`
- `claude_consolidated_change_package_2026-09-28.md`
- `claude_consolidated_change_package_v2_2026-09-28.md`

`BUTTERFLY_P0_IMPLEMENTATION_SPRINT_2026-09-29_v3.md` is included as the best prior P0 implementation package, but this new handoff supersedes it wherever later decisions differ.

## Important evidence distinction

Several test suites in this package are simulated/persona-based design stress tests. They are useful evidence but are **not real human playtests** and, unless explicitly described as executable runs, are **not executable validation**. Never report them as humans or independent agents.
