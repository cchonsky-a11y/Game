# The Butterfly Effect — 50 Actual Executed Playtests
**Date:** 2026-09-28  
**Seeds:** 701–750  
**Build:** self-contained Linux build produced by GitHub Actions from `claude/app-building-prompts-tl4ymh`

## What was actually run
These were **50 real executions of the compiled game**, not a static review. Each execution used the game’s own scripted-input mode with `@autoend on`, and each run also invoked the built-in `--checks` validator.

They are automated scenario testers, not 50 independent human testers or 50 independent fresh AI agents. The five real-human P0 gate remains separate.

The matrix deliberately varied:
- first choice: fountain / workshop / neither,
- early vs. late departure,
- departure years from AD 159 through AD 172,
- promise intent: yes / no / unanswered,
- plague-response intent: hospice / quarantine / none,
- unique RNG seeds 701–750,
- both first and second time jumps.

## Execution result
- **50/50** processes exited successfully.
- **50/50** reached the first arrival.
- **50/50** reached the second arrival.
- **0** crashes.
- **0** `Unknown command` responses in the final matrix.
- **0** built-in `DEAD END` findings.
- **0** built-in `IMPOSSIBLE` findings.
- **0** built-in `ECHO` findings.
- **0** built-in `TEXT` findings.
- Every run produced the four arrival beats and continued through the second jump.

First departure years ranged **AD 159–172** (median AD 166).  
Final second-arrival years ranged **AD 209–257** (median AD 236).

The first-arrival Index ranged **94–101** (median 97).  
The second-arrival Index ranged **85–101** (median 97).

## Scenario coverage
- Fountain first choice: **17** runs
- Workshop first choice: **17** runs
- Neither first choice: **16** runs
- Early departure: **17** runs
- Stayed into/through plague-era testing: **33** runs

Among the 33 late scenarios, the scripts attempted a spread of:
- promise yes: 12
- promise no: 12
- unanswered: 9
- hospice response: 12
- quarantine response: 11
- no response: 10

Important: hospice/quarantine require authority. The base machine-focused scenarios generally did not build enough institutional authority, so those attempted responses were often rejected and Rome fell back to its normal response. Treat those as command-path tests, not as 12 successful hospice and 11 successful quarantine branches.

---

# New confirmed test-infrastructure defects

## T1. `Harness.cs` still requires a 250-year jump
Every one of the 50 runs received:

`SYSTEMS | absence lasted <25–50> years, not 250`

This is a **stale validator**, not a failure of the current game.

Current sources say:
- `docs/PROTOTYPE_SCOPE.md`: P0 jumps **25–60 years**.
- `docs/SYSTEMS.md`: P0 jump carries the inventor **25–60 years**.
- GDD P0 time-machine row: **25–60 years**.

But `src/Butterfly.Console/Harness.cs` still has:

`if (a.ArrivalYear - a.DepartureYear != 250) ...`

### Fix
Update the Harness arrival-duration validation to check the current legal jump range / actual machine range rather than exactly 250 years.

Also sweep stale documentation. `P0_PROPOSALS.md`, parts of `DECISIONS.md`, `BUILD_GUIDE.md`, and `playtests/ai/README.md` still contain 250-year-era assumptions.

This is a documentation/test-sync issue, not a tuning change.

---

## T2. The plague-warning Harness rule contradicts the current historical dates
Every one of the 50 runs also received:

`SYSTEMS | outbreak without three earlier warnings`

The current Harness says the outbreak is invalid if **any warning is in the same year as the outbreak**:

`warnings.Any(w => w.Time.Year >= outbreak.Time.Year)`

But current tuning explicitly defines:
- Warning 1: AD 165
- Warning 2: AD 166
- Warning 3: AD 166
- Outbreak: AD 166

with months `[6, 0, 6, 9]`.

So Warning 3 correctly occurs earlier in AD 166 than the outbreak, but the Harness rejects it because it compares only the year.

### Fix
Validate the full `SimTime` / month ordering:
- exactly three warnings,
- warning 1 < warning 2 < warning 3 < outbreak,
rather than requiring every warning year to be less than the outbreak year.

For players who jump before AD 165, also make sure the validator distinguishes historical stages resolved during absence from player-facing warning requirements if that distinction is intended.

---

# Existing automated scripts are stale
Before creating the final matrix, I executed the repository’s existing 24 scripts against the current compiled build.

They no longer exercise the current machine loop correctly: they were written before the present assessment/9-step repair requirements and commonly attempt to jump with an unassessed/unrepaired machine. The built-in harness therefore reports dead ends.

Examples include `01-early-fountain-keep-paydown.txt` reaching its intended jump year with the machine still at 0/9 because it never assesses/repairs it.

### Fix
Refresh `playtests/ai/scripts/01`–`24` so every scenario either:
- intentionally tests an unready-machine failure, or
- performs the current assessment + 9 repair steps + 60-aureus restoration before attempting a successful jump.

Do this before relying on `playtests/ai/run.sh` as regression evidence.

---

# What the 50 runs do support
The current compiled game is robust across the machine-repair → departure → arrival → second-jump path:

- machine assessment and all nine repairs can complete across diverse seeds;
- gold restoration works;
- first departure succeeds at both early and later dates;
- arrival generation survived all 50 seeds;
- all 50 produced two complete arrivals;
- no impossible state was caught;
- no unfilled placeholders / doubled-word text was caught by the Harness;
- no Echo-structure failure was caught;
- deterministic scripted execution was stable.

That is meaningful evidence that the **core jump pipeline is functioning**.

# What this matrix does NOT prove
This matrix was machine/jump-path heavy. It does not clear the open L1–L18 issues involving institution leadership, office/camp semantics, workshop financing provenance, seniority caps, and other paths that require more institution-heavy play.

Do not mark those fixed because this matrix passed.

It also does not replace the five real-human P0 testers.

---

# Recommended Claude order after this test
1. Fix the two validator bugs above first, so future `--checks` output is trustworthy.
2. Refresh the 24 stale scripted playtests to the current machine mechanics.
3. Run the refreshed 24-script suite and require clean Harness output.
4. Continue fixing L1–L18.
5. Re-run this 50-seed matrix after L1–L18.
6. Then run fresh blind AI testers / real human testers.

## Bottom line
**The game itself completed 50/50 full two-jump executions without crashing or violating the Harness’s impossible-state, Echo, text, or dead-end checks.**

The only universal Harness failures are both caused by **outdated validation assumptions**:
- 250-year jump expectation,
- year-only plague-warning ordering.

Those should be treated as test-infrastructure bugs and fixed before the next large regression round.
