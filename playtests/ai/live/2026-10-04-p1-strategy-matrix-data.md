# P1 strategy matrix: 60 seeds × 5 scripted profiles (300 runs)

Progression routing weight: 3.0 (the game's tuning).

**Automated, scripted runs of the actual build (`dotnet run --project src/Butterfly.Batch -- --p1-matrix ...`). Not human playtests, not persona feedback.** Odd seeds open with the workshop, even seeds with the fountain. Every profile assesses and repairs the machine, restores its gold, opens the R-17 channel when it can, takes workshop orders when it has a workshop, does odd jobs only when no commission is under way and money is short (below 30 aurei at AD 155 prices), and jumps once the machine is ready and the year is at least AD 163, then jumps again.

- **A Cooperative:** looks at and accepts every job; never asks for more; accepts every invitation; starts every challenge stage it can.
- **B Negotiator:** as A, but asks for more on every offer.
- **C Selective:** turns down favors, profit shares and shared development, and any job when next month is already 3 Attention deep; starts a challenge stage only with twice its cost in hand.
- **D Engineering-focused:** stays focused on Engineering scenes; turns down jobs that open as Roman life, city or personal scenes; buys machine upgrades when it has 60+ aurei.
- **E Relationship / Roman-life:** stays focused on Roman-life scenes; answers Rome's and people's choices with their first option; starts a challenge stage only when no paid job is under way.

## By profile and opening (mean, min–max)

| Profile | Opening | Runs | First commission (month) | Odd-job months / era | Commissions offered / accepted / walked / declined / done | Challenge stages / done | Members | Gold at departure | Min gold | First jump (yrs) | Quiet months | Attention conflicts |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Cooperative | fountain | 30 | 5.7 (5.0–11.0) | 18.4 (6.0–34.0) / 96.0 | 13.5 / 13.5 / 0.0 / 0.0 / 13.4 | 8.1 / 1.9 | 1.9 | 59.2 (6.7–159.8) | 0.0 | 32.5 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Cooperative | workshop | 30 | 5.4 (5.0–7.0) | 14.6 (10.0–25.0) / 96.0 | 10.5 / 10.5 / 0.0 / 0.0 / 10.4 | 8.3 / 2.0 | 1.0 | 193.7 (80.3–330.5) | 0.0 | 34.3 (25.0–40.0) | 0.1 (0.0–1.0) | 0 |
| Negotiator | fountain | 30 | 5.7 (5.0–11.0) | 30.4 (12.0–55.0) / 96.0 | 12.5 / 9.4 / 3.1 / 0.0 / 9.3 | 9.3 / 1.9 | 1.9 | 40.9 (9.7–103.1) | 0.0 | 31.8 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Negotiator | workshop | 30 | 5.4 (5.0–7.0) | 20.9 (8.0–33.0) / 96.0 | 10.3 / 7.6 / 2.7 / 0.0 / 7.6 | 8.6 / 1.8 | 1.0 | 163.4 (22.5–306.4) | 0.1 | 33.5 (25.0–40.0) | 0.7 (0.0–20.0) | 0 |
| Selective | fountain | 30 | 5.7 (5.0–11.0) | 28.4 (12.0–46.0) / 96.0 | 12.3 / 8.3 / 0.0 / 3.9 / 8.3 | 5.8 / 1.4 | 2.0 | 41.7 (10.1–127.7) | 0.0 | 34.3 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Selective | workshop | 30 | 5.4 (5.0–7.0) | 19.4 (10.0–38.0) / 96.0 | 10.3 / 7.3 / 0.0 / 3.0 / 7.2 | 7.6 / 1.9 | 1.0 | 154.8 (41.3–306.6) | 0.1 | 33.0 (25.0–40.0) | 0.2 (0.0–3.0) | 0 |
| Engineering | fountain | 30 | 5.7 (5.0–11.0) | 38.1 (21.0–59.0) / 96.0 | 11.5 / 6.9 / 0.0 / 4.6 / 6.9 | 7.7 / 1.8 | 2.0 | 32.6 (8.5–60.3) | 0.0 | 36.5 (25.0–50.0) | 0.0 (0.0–0.0) | 0 |
| Engineering | workshop | 30 | 5.4 (5.0–7.0) | 20.9 (12.0–34.0) / 96.0 | 10.0 / 7.0 / 0.0 / 3.1 / 7.0 | 7.6 / 2.0 | 1.0 | 85.7 (40.3–164.7) | 0.0 | 45.5 (30.0–55.0) | 0.3 (0.0–3.0) | 0 |
| Relationship | fountain | 30 | 5.7 (5.0–11.0) | 23.0 (14.0–37.0) / 96.0 | 13.3 / 13.3 / 0.0 / 0.0 / 13.3 | 8.2 / 2.0 | 1.9 | 27.5 (7.0–39.7) | 0.0 | 33.2 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Relationship | workshop | 30 | 5.5 (5.0–9.0) | 15.7 (9.0–23.0) / 96.0 | 10.2 / 10.2 / 0.0 / 0.0 / 10.1 | 7.6 / 2.0 | 1.0 | 107.7 (37.4–208.7) | 0.0 | 30.3 (25.0–40.0) | 0.1 (0.0–1.0) | 0 |

## Income by source (era totals, aurei at the prices of the day; mean per run)

| Profile | Opening | Commissions (incl. shares) | Workshop | Odd jobs | Profit-share payments |
|---|---|---|---|---|---|
| Cooperative | fountain | 346.1 | 0.0 | 92.8 | 2.0 |
| Cooperative | workshop | 284.7 | 147.2 | 68.4 | 2.0 |
| Negotiator | fountain | 269.2 | 0.0 | 153.2 | 1.7 |
| Negotiator | workshop | 221.5 | 154.5 | 98.2 | 1.7 |
| Selective | fountain | 214.4 | 0.0 | 144.1 | 0.0 |
| Selective | workshop | 200.8 | 144.4 | 92.5 | 0.0 |
| Engineering | fountain | 225.7 | 0.0 | 193.3 | 2.0 |
| Engineering | workshop | 226.8 | 131.8 | 99.6 | 2.0 |
| Relationship | fountain | 343.5 | 0.0 | 122.5 | 2.0 |
| Relationship | workshop | 277.4 | 144.4 | 73.8 | 2.0 |

## Scene routing (all runs)

| Profile | Engineering | Personal | RomanLife | WorkEconomy | MachineMystery | CityHistory | Exploration | InstitutionsPolitics | Routed / run | Longest routed run (max) | Longest all-scene run (max) | Progression waiting (months / era) | Longest wait for one progression scene (months, max) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Cooperative | 1.8% (13.4%) | 13.1% (13.5%) | 47.1% (30.6%) | 7.7% (16.6%) | 5.0% (4.3%) | 7.0% (6.4%) | 5.2% (3.9%) | 13.1% (11.3%) | 96.0 | 8 | 7 (RomanLife) | 69.2 (42.0–88.0) | 43 |
| Negotiator | 2.0% (11.8%) | 13.1% (12.4%) | 48.1% (29.4%) | 7.8% (22.3%) | 5.3% (4.4%) | 6.7% (5.9%) | 4.7% (3.3%) | 12.4% (10.5%) | 95.6 | 9 | 9 (RomanLife) | 68.3 (2.0–87.0) | 60 |
| Selective | 1.8% (11.5%) | 12.3% (12.0%) | 49.6% (32.6%) | 6.5% (17.1%) | 5.2% (4.8%) | 7.1% (6.9%) | 4.4% (3.5%) | 13.1% (11.5%) | 95.9 | 8 | 6 (RomanLife) | 63.1 (37.0–87.0) | 41 |
| Engineering | 1.9% (12.7%) | 10.6% (10.4%) | 49.4% (32.4%) | 8.4% (17.8%) | 5.1% (4.8%) | 7.4% (6.6%) | 5.0% (4.1%) | 12.2% (11.3%) | 95.8 | 8 | 7 (RomanLife) | 63.3 (39.0–89.0) | 40 |
| Relationship | 1.8% (13.4%) | 12.1% (12.9%) | 48.3% (31.4%) | 7.7% (16.3%) | 5.3% (4.4%) | 6.6% (6.3%) | 5.0% (3.8%) | 13.2% (11.5%) | 96.0 | 12 | 8 (RomanLife) | 71.5 (40.0–89.0) | 53 |

Cells: share of routed optional scenes (share of all meaningful scenes, including the player's own work and interruptions, in brackets). "Progression waiting" counts months in which a commission, invitation, challenge or life development was a router candidate as the month ended; "longest wait" is the most months any one such candidate waited before it was picked.

Longest waits by candidate (max months as a candidate before it was picked; mean over runs where it was picked; runs where it was still waiting at departure):

| Candidate | Max wait | Mean wait | Runs picked | Still waiting at departure (runs; mean months waited) |
|---|---|---|---|---|
| commission:crane | 60 | 8.1 | 246 | 0 |
| life:cassianus-betrothal | 53 | 7.2 | 194 | 0 |
| life:felix-son | 49 | 6.9 | 209 | 0 |
| commission:drawings | 43 | 8.0 | 266 | 0 |
| invitation:guild | 41 | 9.7 | 294 | 0 |
| invitation:circle | 41 | 12.3 | 149 | 4; 9.0 |
| commission:drains | 40 | 7.8 | 259 | 0 |
| commission:fountainworks | 39 | 7.4 | 285 | 0 |
| life:vettius-copies | 39 | 6.8 | 171 | 1; 15.0 |
| commission:sluice | 39 | 6.1 | 200 | 10; 5.6 |
| life:pollio-copy | 38 | 6.1 | 219 | 0 |
| commission:jars | 38 | 7.3 | 165 | 1; 14.0 |
| commission:households | 36 | 7.9 | 160 | 7; 11.1 |
| commission:argiletum | 36 | 8.0 | 81 | 0 |
| commission:millbearing | 32 | 6.6 | 291 | 2; 2.0 |
| life:serenus-tables-copied | 32 | 7.1 | 124 | 3; 2.0 |
| commission:bilges | 31 | 4.4 | 297 | 0 |
| life:diodoros-deliveries | 30 | 6.1 | 140 | 0 |
| life:felix-quaestor | 30 | 6.9 | 222 | 2; 11.5 |
| commission:fevers | 30 | 6.2 | 232 | 1; 2.0 |

## Attention (era months)

| Profile | Opening | Months ending with 0 free | 1 free | 2+ free | Idle Attention / month | Overbooking refusals (future months) | Challenge stages refused for Attention |
|---|---|---|---|---|---|---|---|
| Cooperative | fountain | 32.5 | 15.4 | 48.1 | 1.8 | 0.0 | 4.6 |
| Cooperative | workshop | 36.3 | 16.2 | 43.5 | 1.6 | 0.0 | 2.7 |
| Negotiator | fountain | 30.4 | 17.3 | 48.3 | 1.8 | 0.0 | 2.8 |
| Negotiator | workshop | 35.5 | 17.6 | 43.0 | 1.6 | 0.0 | 2.3 |
| Selective | fountain | 30.4 | 12.8 | 52.9 | 1.9 | 0.0 | 1.1 |
| Selective | workshop | 33.6 | 16.9 | 45.5 | 1.7 | 0.0 | 1.1 |
| Engineering | fountain | 30.8 | 16.6 | 48.6 | 1.7 | 0.0 | 2.7 |
| Engineering | workshop | 39.6 | 20.0 | 36.4 | 1.3 | 0.0 | 2.2 |
| Relationship | fountain | 31.1 | 15.0 | 49.9 | 1.8 | 0.0 | 1.9 |
| Relationship | workshop | 33.6 | 18.7 | 43.7 | 1.7 | 0.0 | 1.0 |

## Jumps (measured, not changed)

- Runs that jumped: 300 of 300; jumped twice: 300.
- Departure years: AD 163 × 300.
- First jump distances: 25 yrs × 57, 30 yrs × 58, 35 yrs × 84, 40 yrs × 77, 45 yrs × 11, 50 yrs × 9, 55 yrs × 4; ranges offered at departure: 25–40 × 250, 30–45 × 19, 35–50 × 17, 40–55 × 14.
- Second jump distances: 25 yrs × 66, 30 yrs × 74, 35 yrs × 66, 40 yrs × 68, 45 yrs × 13, 50 yrs × 8, 55 yrs × 5; ranges offered: 25–40 × 250, 30–45 × 19, 35–50 × 17, 40–55 × 14.
- First arrival years: 197.5 (188.0–218.0); second arrival years: 231.4 (213.0–273.0).
- Machine upgrades bought (Engineering profile): 1.6 (0.0–3.0).

## Integrity

- Measurement and standards opened by: allotment × 3, baths × 1, hoist × 8, never × 1, pump × 287; by opening: workshop hoist × 4, never × 1, pump × 145; fountain allotment × 3, baths × 1, hoist × 4, pump × 142.
- Machine archive step: a senator's note in 0 runs, a bribed clerk in 300.
- Grand Challenges completed: both in 266 runs, one in 28, none in 6.
- Ledger reconciles at departure: 300 of 300.
- Lowest gold in any run: 0.0.
- Duplicate scene texts in an era: 0; repeated sentences between arrivals: 0.
- R-17: reached the warning in 291 of 300 runs; furthest scene: r17-acknowledged × 1, r17-active × 6, r17-again × 35, r17-dark × 59, r17-link × 5, r17-notebook × 192, r17-request × 2.
- Echo kinds on the first arrival: access 8, person 598, technical 1398, unintended 682; second: person 598, technical 1240, unintended 266.
- First arrivals showing all four echo kinds (personal, technical, institutional, unintended): 297 of 300; missing unintended: 3; missing institutional: 1; second arrivals with all four: 266.
- People remembered on the first arrival: Felix 274, Serenus 176, Aulus 63, Gaius 55, Diodoros 23, Cassianus 7; second: Gaius 174, Aulus 151, Diodoros 81, Cassianus 66, Serenus 49, Hermogenes 24, Felix 23, Livia 22, Marcus 7, Lucan 1.
