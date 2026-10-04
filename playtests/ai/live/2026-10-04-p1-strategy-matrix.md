# P1 strategy matrix and stress pass (2026-10-04)

**These are automated, scripted runs of the actual build. They are not human playtests and not persona feedback.** Profiles are scripts written against the simulation's API; no person played. The full generated tables are in `2026-10-04-p1-strategy-matrix-data.md`. Regenerate them with:

```
dotnet run --project src/Butterfly.Batch -- --p1-matrix 1-60 [--out file]
dotnet run --project src/Butterfly.Batch -- --p1-matrix 1-60 --weight 4    # counterfactual, in memory only
```

**Method.** 60 seeds × 5 profiles = 300 runs. Odd seeds open with the workshop and even seeds with the fountain (150 each). Each run covers an era from AD 155 to departure, then two jumps. All runs use the build at the end of this pass. The profiles:

- **A Cooperative:** accepts everything.
- **B Negotiator:** asks for more on every offer.
- **C Selective:** refuses favors, profit shares and shared development, and any job when next month is already 3 Attention deep. It starts a challenge stage only with twice its cost in hand.
- **D Engineering-focused:** focuses on Engineering scenes, turns down Roman-life, city and personal jobs, and buys machine upgrades.
- **E Relationship / Roman-life:** focuses on Roman-life scenes, answers choices with their first option, and starts a challenge stage only when no paid job is under way.

Every profile does odd jobs only when no commission is under way and it holds under 30 aurei (AD 155 prices). Every profile jumps at the first chance from AD 163.

## Headline

- **Every run jumped twice.** The ledger reconciles at departure in 300 of 300 runs. Gold never goes negative (lowest 0.0).
- **No future-month overbooking was ever accepted or attempted** (0 refusals). No duplicate scene texts in any era, and no sentence repeated between first and second arrivals (after the fixes below).
- **Both Grand Challenges were completed in 267 of 300 runs.** One was completed in 26 runs and none in 7; the Selective fountain profile averages 1.3, because it waits until it can afford stages twice over.
- **Measurement and standards opened by the pump in 288 runs**, the hoist in 8 and the allotment in 3. It never opened in 1 workshop run, a Negotiator who lost the pump, the hoist and the baths.
  - Before this pass, every one of those 12 non-pump runs would have stayed closed.
  - The baths route didn't fire in these runs, because an earlier route always held first. It is covered by tests.
- **R-17:** the DO NOT JUMP warning was reached in 293 of 300 runs. The rest left before `listen` came up.
- **Echoes:** 298 of 300 first arrivals show all four kinds (personal, technical, institutional, unintended), after the fixes below. Before them it was 263.

## Fountain against workshop: odd-job months (of 96)

| Profile | Fountain | Workshop |
|---|---|---|
| A Cooperative | 18.6 (4–34) | 14.5 (10–25) |
| B Negotiator | 29.4 (13–48) | 21.3 (8–33) |
| C Selective | 28.4 (14–45) | 19.4 (10–38) |
| D Engineering | 38.9 (21–52) | 21.1 (12–34) |
| E Relationship | 21.5 (10–34) | 15.9 (9–23) |

Before this pass, the 8-seed legacy validation showed fountain seeds at 15–47 and workshop seeds at 15–16.

- **The fountain is no longer routinely in the 40–50 band.** Its means run 19–29 for profiles that take the work offered.
- **Runs over 40 come from profiles that refuse or lose work.** The Engineering profile turns down the fountain's own neighborhood jobs, which open as city scenes. The Negotiator loses about three jobs a run.

The asymmetry is kept:

- The workshop earns about 145 aurei a run from craft orders the fountain never gets, and leaves Rome with three to four times the gold (Cooperative: 198 vs 56).
- The fountain earns more from commissions (345 vs 284) through its neighborhood and medicine work, and joins two institutions (the guild and the Circle) where the workshop usually joins one.

## Results by strategy

- **A Cooperative** is the reference: about 14 (fountain) or 10 (workshop) commissions done. Two challenges done. 0 conflicts.
- **B Negotiator** wins more per job but loses 2.6–3.1 jobs a run, and walks into the thinnest worlds. Seed 5 workshop lost the pump, the hoist and the baths, then met no one new for six years. That exposed a gate on Felix's life, now fixed (below). It pays in odd jobs: +7 to +11 months over A.
- **C Selective** declines 3–4 jobs a run and takes no profit-share payments. It is not richer for it: odd jobs rise, while challenge completion stays close to A.
- **D Engineering** reaches the longest jumps (upgrades push the range to 30–45, 35–50 and 40–55). It is the poorest at departure and does the most odd jobs, because it refuses Roman-life and city work. Engineering alone is not a monopoly strategy.
- **E Relationship** completes as many commissions as A. It holds back challenge stages while paid work runs, yet still completes both challenges in almost every run. It leaves with less gold than A, since stages start later and cost more after prices rise.

## Scene routing at 3:1 (all 300 runs)

- **Routed optional scenes:** Roman life about 49%, Personal 12%, Institutions and politics 11%, Work 8%, City 7%, Machine mystery 5%, Exploration 5%, Engineering 2%.
- **All meaningful scenes, including the player's own work:** Roman life 30–33%, Work 17–22%, Personal 11–14%, Engineering 12–13%, Institutions 10%. Engineering stays well under a third of what the player experiences.
- **Longest runs.** All-scene runs of one category (always Roman life) reached 7–9. Those happen when nothing but Roman life can happen: no progression candidate waits, and most authored texture is Roman life. In an earlier build of this pass, a Negotiator who had lost three jobs ran 11 in a row. The 12-long routed run belongs to the Relationship profile, which stays focused on Roman life by choice. Neither breaks the rule ("a third in a row only when nothing else can happen, or the player stays focused").
- **Progression waits:** a commission, invitation or life development waits for the router about 7 months on average. The longest single wait was 49 months (the Circle's invitation averages 12).
  - The long tail comes from same-category exclusion, not the weight. Roman life fills half the routed slots, so a RomanLife-category progression scene (such as the baths job) is often barred by the two-in-a-row rule.

### Is 3:1 right? Counterfactual weights (in memory only; the game still uses 3)

| Progression weight | Roman life, routed | Engineering, all scenes | Months with progression waiting | Mean wait (top 20 candidates) | Max wait | Still waiting at departure (top 20) | Odd-job months | Commissions done |
|---|---|---|---|---|---|---|---|---|
| 1 | 51.7% | 12.4% | 85.3 | 13.5 | 86 | 133 | 27.9 | 8.6 |
| 2 | 49.7% | 12.6% | 75.2 | 9.4 | 69 | 52 | 24.3 | 9.2 |
| **3 (current)** | 49.3% | 12.6% | 66.0 | 7.3 | 49 | 21 | 22.9 | 9.4 |
| 4 | 49.2% | 12.7% | 59.8 | 6.0 | 55 | 20 | 22.4 | 9.4 |
| 6 | 49.1% | 12.7% | 49.1 | 4.5 | 49 | 9 | 21.2 | 9.3 |

**Recommendation: lock 3:1.**

- **Below 3 the plot starves.** At weight 1, 133 progression scenes are still unplayed at departure, there are 5 more odd-job months, and commissions done drop. Going from 2 to 3 still more than halves the unplayed progression scenes (52 to 21).
- **Above 3 nothing the player keeps changes.** Waits shorten, but commissions done, odd jobs and the Roman-life share stay the same.
- **The long-tail waits come from category exclusion, not the weight.** If Corey wants them shorter, the targeted options (not implemented) are:
  - give a waiting progression candidate an age bonus;
  - let a progression candidate count as its own category for the two-in-a-row rule.

## Attention (measured, not retuned)

| | Months ending with 0 free | 1 free | 2+ free | Idle Attention a month | Future-overbooking refusals | Challenge stages deferred for Attention |
|---|---|---|---|---|---|---|
| Range across profiles | 30–40 | 13–20 | 36–53 | 1.2–1.9 of 4 | 0 | 1.0–4.6 a run |

**Does Attention force prioritization? Partly, and in bursts.**

- About a third of months end with nothing left. Challenge stages are deferred 1–5 times a run because the month's Attention is gone.
- But 40–55% of months end with 2+ unused, and no profile ever tried to overbook a future month. The scripts start multi-month work only at the start of a month with the Attention free.
- The idle Attention partly reflects the scripts: they never attend meetings, give gifts, start P0 projects or spend on optional verbs, which a human may do.
- **Conclusion:** the 4-a-month budget binds when work overlaps, not every month. Retuning (for example to 1.4×) is out of scope and not recommended on this evidence alone.

## Economy and ledger stress

Each of these is covered by tests (`P1EconomyStressTests`, 10 tests):

- Every payment and material cost is in the ledger, and the ledger reconciles for a rich workshop and a poor fountain path.
- Favors pay nothing.
- A walked deal pays nothing, and the job can't be taken back up.
- Nothing pays twice.
- Terms are priced when agreed and held to completion.
- You can't spend gold you don't have, and gold never goes negative.
- Nothing stays "working" after departure, and no money appears in transit.

**Found and fixed:**

1. **Profit shares were never paid.** Vettius's terms promised "a tenth of what I get for selling copies", and the referral text said he paid it twice, but no share ever reached the ledger. Each share is now paid once, on schedule (3 aurei every 6 months, twice; PROPOSED P1-21), as a ProfitShare ledger entry. A share still owing lapses, logged, at departure.
2. **Coin in hand crossed the jump.** SYSTEMS §9 and the departure briefing say denarii left in hand stay behind, but `World.Gold` survived the jump (123 in, about 90 out). It is now left behind at departure as a logged, ledgered event. This is the approved rule; no rule changed.
3. **Duplicate text after a refused vote.** Felix's (and Serenus's) sponsorship played word for word twice. A second sponsorship now has its own words. The guild's sponsor scene no longer assumes the pump.
4. **A drift description repeated on both arrivals.** "A handful of houses that decide who may land grain at Ostia..." appeared on both arrivals when a drifted guild was later captured. A path already described now says it has gone further down the same road.
5. **A thin world after three lost jobs.** Felix's fever, and so the vow and Serenus, waited on a guild referral that only his two jobs gave. His fever now needs only that you know him.
6. **Unintended echoes had one source.** Only Pollio's copy of the pump produced them, so 37 of 300 first arrivals had none. Three more bad copies now exist (Vettius's drawings, Serenus's tables, brass bearings).
7. **Half the cast had no echo.** Arrivals always led with Felix and Cassianus. Livia, Marcus, Lucan and Aulus now have echoes, the fountain's Gaius is remembered through the allotment, and arrivals prefer the people the inventor was closest to. First arrivals now feature Felix, Serenus, Gaius, Aulus and Diodoros; second arrivals Gaius, Aulus, Diodoros, Cassianus, Serenus, Livia and others.

## Jump range (measured; no change)

- **How it is chosen.** At departure the distance is drawn uniformly, in 5-year steps, from a range. The range is 25–40 years from the required repairs, +5 per optional upgrade (three exist), plus +5 per whole 5 years in the era beyond the first 5 (up to +10), capped at 60 (`jump.range.*`, PROPOSED P0-23).
  - The player controls only the inputs: upgrades and when to leave. The draw itself is out of the player's hands.
  - The briefing shows the range, never the draw.
- **Second jump.** It is available at once on arrival. The time bonus restarts from the arrival year (so 0 if you leave at once), upgrades still count, and no further repair is needed.
- **Observed (300 runs, all leaving in AD 163):**
  - First jump: 25 yrs × 58, 30 × 60, 35 × 80, 40 × 75, 45 × 15, 50 × 8, 55 × 4.
  - Second jump: 25 × 64, 30 × 64, 35 × 79, 40 × 70, 45 × 9, 50 × 7, 55 × 7.
  - Ranges offered: 25–40 (251 runs), 30–45 (17), 35–50 (19), 40–55 (13).
  - First arrivals AD 188–218; second arrivals AD 213–273.
- **What content supports.**
  - History curves and the coin run to AD 420, and the historical plagues of 189 and 251 are on their dates.
  - People echoes are keyed by **arrival number, not elapsed years**. A first-arrival line written for 25–40 years (Felix's son running the yard) would read the same after 60 years or more.
  - Carried capabilities climb one rung per 20 years.
  - Institutions decay per decade.
- **What would break at 80–100 years:**
  - Every person the inventor knew would be dead, but first-arrival echo lines assume some are alive.
  - Capabilities would climb 4–5 rungs, to institutionalized, so every technical echo would read as complete.
  - The 30-year compounding and debt-payment windows would close well before arrival, so most of the absence would be pure drift.
  - Content for the AD 255–275 crisis years (Gallienus, the coin's collapse) is thin.

**Models for Corey (not implemented; left OPEN):**

- **A. Constrained random window (today's model, refined).** Keep the seeded draw, but key echoes to elapsed years and cap the window by what content supports (for example 25–60 until era 2 exists). Simple, keeps uncertainty, and the player can't aim.
- **B. Player-chosen band with uncertainty.** The player picks a band (near, middle or far); the machine lands within it with a spread that narrows as repairs improve. More agency and a clearer jump decision, but the player can aim at known history, so content must cover every band.
- **C. Capability-improved targeting.** The range starts wide and uncontrolled, and real capabilities (the chronometer system, metrology, instrument steel) narrow it, so the Grand Challenges pay into the jump. This ties Knowing-Is-Not-Making to the machine, but needs the jump-range growth rule SYSTEMS §11 leaves undefined.
