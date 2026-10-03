# P1 Implementation Status & Next Steps
**Phase:** P1 — Playable Game Structure

## Current state

P1 has been approved and started, but the prepared P1 code has **not been committed**.

Reason: the environment used to prepare it did not have a .NET SDK. The project rule requires `dotnet test` before every commit.

## Current GitHub branch

`cchonsky-a11y/Game`  
`claude/app-building-prompts-tl4ymh`  
HEAD verified: `53f850d04334577be26175d3bc714c0e07c06b38`

The branch still contains legacy P0 player-facing behavior:
- old/variable turn length,
- auto-end / auto-skip logic,
- direct institution stake buying,
- old money framing.

## Prepared P1 files

In `artifacts/P1_SPRINT1_INTEGRATION.zip`:

### Core
- `P1Architecture.cs`
- `P1SceneRouter.cs`
- `P1Economy.cs`
- `P1InstitutionAccess.cs`

### Tests
- `P1ArchitectureTests.cs`
- `P1IntegrationTests.cs`

### Docs
- `P1_SCOPE.md`
- `P1_SPRINT1_INTEGRATION.md`

## Foundation details

### Menu architecture
Eight sections:
- Now
- Projects
- People
- Institutions
- Knowledge
- Civilization
- Machine
- Journal

### Scene router
- deterministic project RNG only,
- eight scene categories,
- after 2 repeated meaningful scenes, a third gets a strong weight penalty,
- explicit player focus removes the repetition penalty,
- world interruptions are not suppressed.

### Projects / commercial terms
Tracks:
- funding model,
- payer,
- materials payer,
- upfront/completion gold,
- player material cost,
- profit share,
- non-cash consideration,
- purpose/client/duration/stage/collaborators/dependencies/NPC continuation.

### Economy ledger
Records:
- payments,
- costs,
- materials,
- profit share,
- dues,
- favors,
- adjustments.

Initially additive; does not silently mutate legacy `World.Gold`.

### Institution access
State progression:
Aware → KnowsMember → Guest → InvitedBack → SponsoredCandidate → Member → Officer

Gate requires:
- matching institution,
- inviter,
- relationship,
- relevant work,
- usefulness,
- sponsor risk.

## First Claude execution plan

### Commit 1 — additive P1 foundation
1. Apply prepared files.
2. Run `dotnet test`.
3. Resolve compile/test issues without redesign.
4. Commit.

### Commit 2 — calendar migration
1. `MonthsPerTurn = 1` fixed.
2. Remove normal `SetTurnLength` from player-facing flow.
3. Remove normal auto-end.
4. Explicit End Month.
5. Keep optional fast-forward with interruption stop conditions.
6. Update tuning/docs/tests.

### Commit 3 — World integration
Add:
- `ScenePacingState`,
- `EconomyLedger`,
- P1 project collections,
- institution access collections.

Preserve compatibility long enough to migrate systems incrementally.

### Commit 4 — first real paid-work vertical thread
Suggested model: pump / workshop commission:
- encounter,
- diagnosis,
- agreement,
- explicit payer/material terms,
- project stages,
- scene-category routing,
- ledger entries,
- completion,
- propagation/referral.

### Commit 5 — institution migration
- add relationship/invitation system to actual content,
- remove player-facing direct stake purchase for migrated institution,
- retain legacy stake code only if required temporarily for old saves/tests,
- begin deprecation plan.

## Next architectural work

- hidden technology/capability graph,
- Roman-baseline metadata/check,
- NPC autonomy,
- persistent character state,
- jump echo graph,
- view models for eight menu sections,
- resource bottlenecks,
- content tagging by scene category,
- payment visibility,
- month/Attention header.

## Documentation synchronization

For any rule changed:
1. `docs/SYSTEMS.md`
2. GDD / Appendix A
3. `data/tuning.json`
4. `docs/DECISIONS.md`
5. content files
6. tests

Do not leave "new code, old docs" as a permanent state.

## P1 success target

A fresh player can:
- arrive,
- live through early Rome,
- meet recurring people,
- take explicit paid/self-funded work,
- begin a Grand Challenge,
- earn institution access,
- use organized menus,
- prepare machine,
- jump,
- see persistent echoes,
- without critical blockers or hidden state confusion.

Five-real-human testing is later, when presentation is representative.
