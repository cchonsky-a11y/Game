# HANDOFF.md: picking up this project cold

> For any AI assistant (or person) continuing work on The Butterfly Effect without the earlier conversation.
> Read this first, then `CLAUDE.md`, which is binding. Last updated 2026-09-28.

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

## 5. Where things stand (2026-09-28)
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
- **Next gate:** 5 human testers with `playtests/kit/` (seeds 101–505). That is the only P0 pass/fail evidence.

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

**Waiting on Corey:** the human playtest results. After them come the gate review, DECISIONS entries and a rewrite of PROTOTYPE_SCOPE for P2.

## 8. How to report back to Corey
End every task with:
- what changed;
- tests run, with their result;
- the documents that need updating (SYSTEMS / GDD Appendix A / tuning.json / DECISIONS);
- any question that needs his decision, one line each, with a recommendation.

Keep it short. He decides; you propose.
