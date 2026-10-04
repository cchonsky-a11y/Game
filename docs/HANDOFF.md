# HANDOFF.md: picking up this project cold

> For any AI assistant (or person) continuing work on The Butterfly Effect without the earlier conversation.
> Read this first, then `CLAUDE.md`, which is binding. Last updated 2026-09-28; P1 note 2026-10-03.

> **The project is now in P1 (decided 2026-10-02).** The current authority is the owner's handoff in `docs/handoff/2026-10-02/` (start with `00_READ_THIS_FIRST.md` and `01_MASTER_HANDOFF_CURRENT.md`) and the P1 `docs/PROTOTYPE_SCOPE.md`. The five-human test is deferred to a graphical milestone. The P0 notes below remain accurate for the systems still running from P0; where they conflict with the P1 handoff, the handoff wins.

## 1. What this is
A turn-based historical strategy game about a stranded time traveler: Rome, AD 155, then jumps forward to see what their choices did. We are building **P0 only**: a deterministic, text-based C# simulation with a console game, an automated batch runner and xUnit tests. There are no graphics, networking or language-model features.

- Repository: `github.com/cchonsky-a11y/Game`, working branch `claude/app-building-prompts-tl4ymh`.
- Owner and decision-maker: **Corey**. Every rule, number and scope decision is his.

## 2. Ground rules (short version of CLAUDE.md; CLAUDE.md wins)
1. **Scope:** build only what `docs/PROTOTYPE_SCOPE.md` lists as in scope. The GDD is background, not a build list.
2. **Numbers:** `docs/SYSTEMS.md` is the source of truth. Every tuning value lives in `data/tuning.json` as `{ "value", "ref" }`, never in code.
3. **Conflicts:** if documents conflict or a rule is missing, **stop and ask Corey**. Don't guess.
4. **Determinism:** the seeded RNG in Butterfly.Core only. No `DateTime.Now`, no unseeded `Random`, no order-dependent iteration over unordered collections.
5. **Tests:** every formula has a unit test. `dotnet test` must pass, including the determinism and snapshot tests, before any commit.
6. **Event log:** every state change is logged with causes, actors and effects.
7. **Doc sync:** a change to a rule or number updates, in the same commit, SYSTEMS.md, then the GDD (Appendix A), then tuning.json, then `docs/DECISIONS.md` (dated), and `docs/P0_PROPOSALS.md` or PROTOTYPE_SCOPE if they are affected. Every task summary lists the documents touched.
8. **Balance gates:** don't tune just to pass the batch gates (Corey). Report the results; he decides.
9. **Commits:** small slices with clear messages. No model or vendor identifiers in commits or files. Push to the working branch only. No PR unless asked.

## 3. Commands
```
dotnet build
dotnet test                                                                 # ~330 tests incl. determinism, snapshot, console, validator
dotnet run --project src/Butterfly.Console -- --seed 42                     # play; numbered menu; 'help'
dotnet run --project src/Butterfly.Console -- --seed 42 --inputs f.txt --checks c.txt   # scripted run + harness checks
dotnet run --project src/Butterfly.Batch -- --runs 100 --out playtests/batch-report.md # 7 strategies × early/late gate
dotnet run --project src/Butterfly.Batch -c Release -- --explore 10000 --out playtests/explore-report-10000.md  # random personas, 2 jumps
dotnet run --project src/Butterfly.Batch -c Release -- --idle 1000          # a player who does nothing (should track history)
dotnet run --project src/Butterfly.Batch -- --p1-validate 1,2,3             # P1 scripted two-jump validation (not human)
dotnet run --project src/Butterfly.Batch -- --p1-matrix 1-60 [--weight W]  # P1 strategy matrix, 5 scripted profiles (not human)
playtests/ai/run.sh                                                         # 24 scripted playthroughs + harness checks
python3 playtests/ai/live/play.py setup                                     # live blind AI testers (see §6)
```

## 4. Code map
- `src/Butterfly.Core/` (.NET Standard 2.1, must stay Unity-compatible): a partial class `Simulation` split by system:
  - `Simulation.Economy`, `Policy`, `Stakes`, `Plague`, `Recurrences`, `Jump`, `Events`, `Workshop`, among others;
  - `World.cs` (state), `Content.cs` (content types), `GameData.cs` (finds `data/` by walking up from the working directory).
- `src/Butterfly.Console/`: `Program.cs` (commands, `ProcessLine`) and `Menu.cs` (the numbered menu).
- `src/Butterfly.Batch/`: `BatchRunner` (the gate; FreeMarket is exempt via `GateExempt`), `Strategies`, `Explorer` (random personas, cross-tabs).
- `data/tuning.json`: all numbers. `data/content/`: events, workshop, inventions, institutions, text templates.
- `tests/Butterfly.Core.Tests/`: formula tests, determinism, snapshot (reference hash in `SnapshotTests`; a deliberate rule change updates it, and the commit says so).

## 5. Where things stand

### P1 status (2026-10-04, freeze pass)
- **APPROVED / IMPLEMENTED:**
  - Hermogenes and the political-client foundation, as built;
  - the archives machine step takes a senator's note (the salutatio), with a clerk's bribe as fallback;
  - Grand Challenge credit goes to the first route that became eligible;
  - P1-20, P1-21 and P1-22 approved provisionally.
- **Unchanged by decision:**
  - the age bonus is built but OFF (P1-23 PROPOSED);
  - Attention is not retuned (NEEDS HUMAN VALIDATION);
  - the jump range is unchanged (redesign deferred).
- **OPEN / DEFERRED:**
  - the following, its membership and offices;
  - the Junian path;
  - client exclusivity;
  - recurring political obligations;
  - P1-23;
  - elapsed-time echoes and the long-jump model;
  - what R-17 is;
  - human usability and Attention validation.
- **Next:** P1 systems are frozen pending Corey. P2 is not started.

### P1 status (2026-10-04, polish pass)
- **IMPLEMENTED:**
  - event, effect, workshop, invention and machine content validated at load;
  - the departure briefing groups every kind of money, with tests for each;
  - the router remembers waiting progression scenes, with an age bonus off by default (PROPOSED P1-23);
  - odd jobs vary their words, with pay unchanged;
  - a relationship-first introduction to a senator's house (Hermogenes, the salutatio, a favor), as a client only;
  - no repeated discovery lines across arrivals.
- **Report:** `docs/P1_POLISH_2026-10-04.md`, covering wealth, validation, the long waits, fallback work, political access, the elapsed-time echo design, route causality, Attention and jump range.
- **OPEN:** turning on the age bonus (about 0.10); the factions' following; the jump-range model and the elapsed-time echoes before longer jumps; Attention (needs people). (The machine archive step, open at the time, is APPROVED and IMPLEMENTED in the freeze pass; see above.)

### P1 status (2026-10-04, hardening pass)
- **IMPLEMENTED:**
  - Grand Challenges open by any of several routes (Measurement and standards: pump, hoist, Subura allotment, baths).
  - The fountain opening's own paid neighborhood chain (the allotment introduces Gaius; the Argiletum follows).
  - The shared-foot stage can be vouched for by Gaius.
  - Profit shares are paid; coin in hand stays behind at a jump.
  - Second sponsorships and drift descriptions no longer repeat; Felix's life no longer waits on the guild.
  - Three more bad copies give unintended echoes several sources; half the cast gained echoes; arrivals remember the people you were closest to.
  - A scripted strategy matrix (`--p1-matrix 1-60`, with `--weight` counterfactuals).
- **Reports:**
  - the gate map, `docs/P1_PROGRESSION_MAP.md`;
  - the 300-run matrix, `playtests/ai/live/2026-10-04-p1-strategy-matrix.md`;
  - the completion audit, `docs/P1_COMPLETION_AUDIT_2026-10-04.md` (12 PASS, 5 PARTIAL, 0 FAIL; not a declaration of completion).
- **OPEN DESIGN QUESTIONS:** what R-17 is; the patron path into the factions; the jump-range model (A/B/C in the matrix report); whether Attention should bind more often; locking 3:1 (recommended).

### P1 status (2026-10-04, second pass)
- **IMPLEMENTED (this pass):** the R-17 warning (DO NOT JUMP after `listen`; notebook "reciprocal lock"); 13 commissions in all, across domains, with a second chance after losing the first; deeper recurring lives (13 more life events, private scenes, choices people can refuse); ~45 Roman-life scenes, many reusable with new words each time; progression candidates outweigh texture in the router; access kinds (factions closed pending a patron path, sanctuary by gifts, bank by shares; Rome's choices give standing); the P0 smith renamed Successus; an executable two-jump validation runner (`--p1-validate`) and its report in `playtests/ai/live/`.
- **OPEN DESIGN QUESTIONS:** what R-17 is; the patron path into the Senate factions; Measurement and standards for a player who lost the pump; the fountain choice's heavier reliance on odd jobs; the jump range (25–60 kept, an 83-year live jump exists); the Attention ratio (1.25×, not retuned).
- **LEGACY COMPATIBILITY:** the internal stake still drives offices, voice and policy; batch strategies, explorer and snapshot use the old purchase; craft/consult remain typed commands.

### P1 status (2026-10-04, first pass)
- **IMPLEMENTED:** 1-month turns, End Month, fast-forward that stops on interruptions (and no longer for stake purchases); the hard future-Attention limit; one numbered choice at a time; the scene router over a monthly candidate pool (commissions, invitations, Grand Challenges, people's lives, Roman life, the machine mystery), with `focus` and unrouted interruptions; the ledger; two commissions (the cellar pump, Lollius's bilges); the guild and the Physicians' Circle by invitation (separate evidence, a vote that can refuse); the capability network (19 nodes, edge levels, maturity apart from spread, the full ladder); recurring people with bounded, branching lives; jump echoes on every arrival without repeats; the eight section views (`view`); the Roman-baseline check; old inventions as practical projects (regard, capability links); Grand Challenges: Measurement and standards, Powered workshops (with Sextus Nerius's consequence).
- **PROPOSED (numbers and readings awaiting Corey):** P1-01 to P1-15 in `docs/P1_PROPOSALS.md`.
- **NOT YET IMPLEMENTED:** the sanctuary and the two Senate factions still sell seats (P0 legacy; open question); craft/consult remain typed fallback commands; the batch strategies, explorer and snapshot still use the legacy stake purchase; enough commissions and Roman-life scenes to fill a whole era (the scripted session runs thin after about AD 158); a P1 batch strategy set.

### P0 status (2026-09-28)
- **Built:** everything in PROTOTYPE_SCOPE's in-scope table, including:
  - two jumps with a walk around Rome on each arrival;
  - the workshop's ladder of sizes (smithy → yard → works → foundry);
  - Rome's choices, which stack and leave marks;
  - offices and camps; advocacy; founding small, with a power for each founded institution;
  - the historical plague recurrences (AD 189, about AD 251);
  - the numbered menu.
- **Every PROPOSED amount is approved for human testing** (P0_PROPOSALS note), to be revisited with the testers' results.
- **Batch gate** (`playtests/batch-report.md`):
  - Late jumps pass.
  - Early jumps fail: founding the school wins about 94% of runs. **Corey chose to leave this to the human testers.** Don't tune it.
  - FreeMarket is meant to be better and is exempt from the gate.
- **Jump timing:** deferred to P3 (staying longer is always better in P0). Don't try to fix it.
- **Free markets:** only the player's `policy` or `advocate` moves Rome off its historical economic policy. Left alone, Rome follows history. Keep it that way.
- **Human testing:** deferred to a graphical milestone (2026-10-02). `playtests/kit/` stays for then.

## 6. Live blind AI playtesters
Advisory only: they find bugs and unclear text; they are not gate evidence. The setup, the prompt and the ten personas are in `playtests/ai/live/tester-prompt.md`. Reports go in `playtests/ai/live/reports/<date>/`.

**2026-09-28 round (seeds 611–620):** three testers finished: t2 (history buff, 8/10), t6 (profit-seeker, 7/10) and t7 (early jumper, 5/10). Their reports are in `reports/2026-09-28/`. The other seven were stopped by usage limits partway through the era; rerun them with new seeds.

**Next round (proposed):** 50 testers, seeds 701–750, in five cohorts of 10: optimization, casual/onboarding, roleplay, systems specialists, and adversarial/edge cases. Run it only after the open bugs below are fixed and CI is green. Report:
- completion rate and first-jump year;
- how many met the plague and Demetria;
- participation in institutions, policy, workshop and inventions;
- common confusion, and unsupported commands players tried;
- remembered Echoes, and what players believe they caused;
- ratings, and whether they want another jump.

Keep bugs apart from design notes.

## 7. Open work queue (ordered; check DECISIONS.md and `playtests/ai/bugs.md` for anything newer)
**Done 2026-09-28:** A1–A10 from the first live round, plus a crash on `jump` `jump` with an unready machine (see `playtests/ai/bugs.md`, "Live blind testers").

**Bugs and text** (fix freely, one per commit, each with a test): as of 2026-09-29, T1–T3 and L1–L11 (and L15) are done. **L12, L13, L14, L16, L17, L18** are open; see the status in `playtests/ai/bugs.md`. After them: full regression (tests, `playtests/ai/run.sh`, a batch run, 50 scripted seeds 701–750 with `--checks`), then the 50-tester blind round.


**Design observations** (report to Corey; don't change without his decision): listed at the end of the "Live blind testers" section of `playtests/ai/bugs.md`. They cover leaving early, consulting's dominance, the middle-years grind, repeated office offers, the fountain's value, the Curia text, and a question about deposits after an arrival.

**P1 order of work:** see `docs/PROTOTYPE_SCOPE.md` (P1) and `docs/handoff/2026-10-02/04_P1_IMPLEMENTATION_STATUS.md`. Open questions for Corey: `docs/P1_PROPOSALS.md`.

## 8. How to report back to Corey
End every task with:
- what changed;
- tests run, with their result;
- the documents that need updating (SYSTEMS / GDD Appendix A / tuning.json / DECISIONS);
- any question that needs his decision, one line each, with a recommendation.

Keep it short. He decides; you propose.
