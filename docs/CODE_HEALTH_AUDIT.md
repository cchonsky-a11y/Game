# CODE_HEALTH_AUDIT.md: code health and dead-code review (2026-10-04)

> A production-code review of `src/`, `tests/`, `data/` and the batch and playtest tooling, done at head `74b8cf8` before any change.
>
> **Rule for this pass:** nothing changes the game. That means no change to gameplay, narrative, pacing, economy, Attention, RNG call order, event order, or the seeded results.

## Baseline (before any change)

| Reference | Value |
|---|---|
| `dotnet test` | 482 total, 482 passed, 0 failed, 0 skipped |
| Snapshot reference hash | `b108741158288cd2e91ee8ab50fbf5acca2174b23d64cd5f3d40159efa6961f0` (arrives AD 247, Index 127.7) |
| `--p1-validate 1,2,3` output | md5 `cba8ce9c0b8f7621c03e321e0b92507d` |
| `--p1-matrix 1-60` output | md5 `ba2dff929d48b7ffd5195eb02d5aa277` |
| `--runs 20` (P0 batch gate) output | md5 `39a4804452c076fa4bc62eb667118655` |
| `--explore 200` output | md5 `704e44651897ef3c939778cd91448613` |

Every cleanup commit is checked against all six. The expectation is identical output.

## Method

1. **Roslyn analyzers** (IDE0051, IDE0052, IDE0059, IDE0060, CS0169, CS0414) were enabled for one throwaway build, through a temporary `.editorconfig` that is not committed. They find unused private members, write-only members, unused parameters and dead assignments.
2. **Cross-project reference counts.** Every public or internal member declared in `src/` was counted in `src/` and `tests/` separately, with comments stripped. A member with no production reference is either dead or a test-only hook.
3. **Property reads.** Every content-definition and state property was checked for a read outside its own declaration, which catches write-only state and parsed-but-unused content fields.
4. **Tuning keys.** All 395 keys in `data/tuning.json` were checked against literal and prefix-built (`"stakes.costPerPercent." + domain`) references in code.
5. **Content cross-check.** A new referential validator (below) was run over all content: duplicate ids, unknown ids and malformed requirements. **It found 0 problems.** The content is clean today, but nothing guaranteed it.
6. **Manual review** of the Attention, scene routing, ledger, commission, challenge, people, echo and access code; the console commands; the batch modes; the test helpers; and the playtest assets.

## Findings

| # | Area | Finding | Classification | Evidence | Risk | Recommended action |
|---|---|---|---|---|---|---|
| 1 | `P1Economy.ProjectAccounting` | A second ledger-writing path from P1 Sprint 1. Production builds the ledger from logged gold effects (`Simulation.Record`). This class is called only by one test. Used in play, it would double-record. | SAFE REMOVE | 0 production callers; 1 test (`P1IntegrationTests.ProjectAccountingMakesPaymentAndMaterialsVisible`); no reflection or data use | None | Remove it and its test. Ledger behavior is covered by `P1EconomyStressTests`. |
| 2 | `ProjectTerms.PlayerMaterialCost`, `.ProfitShare`, `.NonCashConsideration` | Read only by `ProjectAccounting`; never set by production | SAFE REMOVE | 0 production reads; set only in that test | None | Remove with #1. |
| 3 | `ProjectState.Dependencies`, `.CanContinueWithoutPlayer` | Never written or read | SAFE REMOVE | 0 references outside the declaration | None | Remove. |
| 4 | `InstitutionAccessState.LastInviterId`; `.PromoteToOfficer()` | Write-only field; method never called (offices run on the P0 office model) | SAFE REMOVE | 0 reads; 0 callers | None | Remove. Keep the `Officer` enum value, which the console names. |
| 5 | `SceneCandidate.IsAvailable` / `IsInterrupt` and `SceneRouter`'s handling of them | Production always builds available, non-interrupt candidates. Interrupts are played outside the router (`RouteScenes`), so the router's interrupt rule is a second, dead implementation of the same design rule. | SAFE REMOVE | Production constructs `SceneCandidate(id, category, weight)` only; tests pass the flags | None (no RNG change: the weights computed are identical) | Remove. The interrupt rule is tested through the real path (`P1RoutingTests`, the R-17 interrupt tests). |
| 6 | `Simulation.NoActionPossible()` | The P0 auto-end predicate. P1 removed auto-end. Only one test asserts it is false next to `Assert.NotNull(PendingEvent)`. | SAFE REMOVE | 0 production callers | None | Remove it and that one redundant assertion. |
| 7 | `Simulation.AvailableInventions()`; `BatchRunner.PolicyStrategies`; `Rng.State`; console `Signed` (shadowing core's `Signed`); empty `OnProjectStarted` hook | Unused | SAFE REMOVE | 0 callers (analyzer IDE0051 and reference count) | None | Remove. |
| 8 | Unused parameters: `PayDebtFromHoldings(…, arrival)`; `Json.Error(s, …)` | Unused arguments | SAFE SIMPLIFY | IDE0060 | None | Drop `arrival`. Use `s` to give JSON errors a line and column (robustness). |
| 9 | `ReservedAttention()` and `ReservedAttentionParts()` | The same reservation computed two ways; they must agree (the header shows parts, the month uses the total) | SAFE CONSOLIDATE | Identical filters term by term (projects, mentoring, machine, inventions, commissions, challenges, office duties) | Low (integer sums, no RNG) | Define the total as the sum of the parts, and remove the four `Reserved*Attention` helpers. Add a test that they agree. |
| 10 | `stakes.firstBuyPercent` tuning key | Never read. `Buy` hard-codes the `1` it stands for (CLAUDE.md: no hard-coded numbers). | SAFE CONSOLIDATE | 0 references to the key; literal `1` in `Simulation.Stakes.Buy` | None (value 1 = literal 1) | Read the key. |
| 11 | Content validation | No load-time referential checks for P1 content. A typo (`commission:pumpp:Done`, `knows:Feliks`) silently never holds. A bad person id throws mid-campaign. | VALIDATION IMPROVEMENT | `Holds` returns false for unknown ids; `ContentChecks` covers only the §14 hard constraints and the invention tree | Medium today (content is clean), growing with content | Add `ContentValidation` (duplicate ids, references, requirement grammar, levels, stages, months, chances, costs) and run it in `Content.Load`, failing with every problem listed. Move the `CapabilityDef.Leaps` check from tests into it. |
| 12 | P1 test helpers | 7 copies of "play the cellar pump", 3 of `FollowFelix`, 3 near-identical `Terms` / `Finish` pairs | TEST CLEANUP | Identical bodies (diffed); differences only in the gold set afterwards | None | Share them in one `P1Play` helper; each test keeps its own gold value. |
| 13 | Stale comments | "Empty means the turn can pass on its own" and "next turn … passes on its own" describe P0 auto-end | DOCUMENTATION CLEANUP | P1: only End Month moves time | None | Reword. |
| 14 | `ProjectState` in `World.Projects` | Parallel state beside `CommissionState` and `ChallengeState` (its own stage and month counters). Production reads only its id (ledger project ids) and `Terms.CompletionGold`. | POSSIBLY OBSOLETE: NEEDS DESIGN DECISION | `Stage`, `MonthsRemaining`, `Purpose`, `OwnerOrClient` and `Collaborators` are written but never read in production | Low now; could drift | Keep. It is the planned view-model source for the Projects section. Either have the views read it, or fold it into the commission and challenge states later. |
| 15 | Life-event state | `PersonState.Happened` and `World.LifeEventLog` record the same fact (always written together in `Happen`) | SAFE CONSOLIDATE (deferred) | Both written on adjacent lines; tests set both by hand | Low; inconsistency only through tests | Report only. Changing state shape touches tests and is out of a strict cleanup pass. |
| 16 | Two condition grammars | P1 `Holds` (`prefix:id:arg`, `!x`, `a\|b`) versus P0 event conditions (`flag`, `not:flag`, `own:<id>`, workshop `member:a\|b`, `influence:<domain>`) | POSSIBLY OBSOLETE: NEEDS DESIGN DECISION | `Simulation.Events.cs` and `Simulation.Workshop.cs` versus `Simulation.People.cs` | Medium (two syntaxes for authors) | Unify only with a content migration, which is out of scope for cleanup. |
| 17 | `InstitutionDef.LeaderRole` | Parsed, never shown | POSSIBLY OBSOLETE: NEEDS DESIGN DECISION | 0 reads | None | Show it in the Institutions view, or drop it from content (an authored field, so Corey's call). |
| 18 | Person profile fields (`Vulnerability`, `Distrusts`, `Opinion`, `Interest`, `Voice`) | Not read by the engine; checked for completeness by a test | DO NOT TOUCH | The design requires these for every recurring person (PROTOTYPE_SCOPE "People") | — | They are the authoring contract and future UI content. |
| 19 | P0 stake model (`Stake`, `Backed`, offices, voice, policy, `Buy`, `GrantStake`) | Old model beside P1 access | LEGACY: STILL REQUIRED | Offices, voice, policy, the bank, the batch gate, the explorer and the snapshot all read it; a guild member holds 10% standing for P0 systems | High if removed | Keep until each system migrates (SYSTEMS §7). |
| 20 | `MonthsPerTurn` and multi-month horizons | Turn length is fixed at 1 month in P1 | LEGACY: STILL REQUIRED | SYSTEMS §2 keeps the stage table for later eras; the jump uses decade steps | — | Keep. |
| 21 | Console `@autoend`, `turns`, `craft` / `consult` | P0 compatibility | LEGACY: STILL REQUIRED | `playtests/ai/live/play.py` sends `@autoend`; SYSTEMS §9 keeps craft and consult as typed commands | — | Keep. |
| 22 | Test hooks in production (`JumpForTests`, `MarkAssessedForTests`, `GrantStake`, `PeekRoutedScene`, `ScenePacingState.Reset`) | Internal and used only by tests | LEGACY: STILL REQUIRED | `InternalsVisibleTo` tests; `PeekRoutedScene` consumes RNG, so it must never be called in play | Low | Keep. They are documented as test-only. |
| 23 | Scene router, `Holds`, capability ladder, invitation gate, commission and challenge state machines | Large, but each is the game's own rules | DO NOT TOUCH | Mirrors SYSTEMS §2, §7, §9, §10 and §13 | — | — |
| 24 | `Simulation.Scenes` property | Returns the router, but reads like a scene list (`Content.Scenes` is the list) | SAFE SIMPLIFY (deferred) | Naming | None | A rename touches tests for no behavior gain; noted only. |
| 25 | `playtests/ai/transcripts` (8.6 MB), `explore-report-*.md` (about 1 MB) | Generated output kept in git | Archival playtest evidence or obsolete generated output | Regenerated by `playtests/ai/run.sh` and `--explore` | Repository size only | Corey to decide: move to an archive branch or a release artifact, then regenerate on demand. Not deleted here. |
| 26 | `--p1-validate` versus `--p1-matrix` | Overlapping P1 runners | DO NOT TOUCH | Validate gives per-seed detail (the documented check); the matrix gives aggregates across profiles; they share `P1Campaign.Play` | — | Keep both. |
| 27 | Explorer `Do(sim, …)` unused parameter | 37 call sites | SAFE SIMPLIFY (deferred) | IDE0060 | None | Churn in the batch tool for no gain; noted only. |
| 28 | Performance | Monthly loops do linear `First(…)` content lookups and re-split requirement strings in `Holds` | Report only | 300 matrix runs take about 8 s; a single run about 2 s with build | None | Not worth changing: no hot spot, and caching would add state. |

**Deletion evidence standard.** Each SAFE REMOVE above has no production caller (reference scan plus the compiler after removal), no content or tuning reference, no reflection or data-loader use (the loader reads JSON into explicit constructors; nothing binds by name), and no batch or playtest use.

## Results

The results are filled in after implementation; see "Final results" below.

## Final results

_(updated at the end of the pass)_
