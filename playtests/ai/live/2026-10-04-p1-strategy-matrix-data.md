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
| Cooperative | fountain | 30 | 5.7 (5.0–11.0) | 17.3 (6.0–33.0) / 96.0 | 13.6 / 13.6 / 0.0 / 0.0 / 13.6 | 8.3 / 2.0 | 2.0 | 57.1 (11.6–167.2) | 0.0 | 32.0 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Cooperative | workshop | 30 | 5.4 (5.0–7.0) | 15.5 (11.0–23.0) / 96.0 | 10.5 / 10.5 / 0.0 / 0.0 / 10.4 | 8.8 / 2.0 | 1.0 | 187.2 (75.3–341.7) | 0.0 | 32.5 (25.0–40.0) | 0.1 (0.0–2.0) | 0 |
| Negotiator | fountain | 30 | 5.7 (5.0–11.0) | 28.8 (12.0–49.0) / 96.0 | 12.6 / 9.4 / 3.1 / 0.0 / 9.4 | 8.4 / 1.9 | 1.9 | 48.0 (7.4–165.5) | 0.0 | 33.3 (25.0–40.0) | 0.1 (0.0–2.0) | 0 |
| Negotiator | workshop | 30 | 5.4 (5.0–7.0) | 21.2 (8.0–32.0) / 96.0 | 10.3 / 7.7 / 2.6 / 0.0 / 7.7 | 8.2 / 1.9 | 1.0 | 185.7 (40.0–359.3) | 0.1 | 30.5 (25.0–40.0) | 0.8 (0.0–20.0) | 0 |
| Selective | fountain | 30 | 5.7 (5.0–11.0) | 28.4 (13.0–45.0) / 96.0 | 12.0 / 8.1 / 0.0 / 3.9 / 8.1 | 5.5 / 1.3 | 2.0 | 44.7 (18.8–111.2) | 0.0 | 31.0 (25.0–40.0) | 0.1 (0.0–1.0) | 0 |
| Selective | workshop | 30 | 5.4 (5.0–7.0) | 19.4 (10.0–38.0) / 96.0 | 10.5 / 7.4 / 0.0 / 3.1 / 7.4 | 7.9 / 2.0 | 1.0 | 159.5 (41.3–306.5) | 0.1 | 32.3 (25.0–40.0) | 0.2 (0.0–3.0) | 0 |
| Engineering | fountain | 30 | 5.7 (5.0–11.0) | 38.5 (22.0–49.0) / 96.0 | 11.5 / 7.0 / 0.0 / 4.5 / 7.0 | 8.4 / 2.0 | 2.0 | 31.9 (6.4–56.8) | 0.0 | 34.7 (25.0–45.0) | 0.0 (0.0–1.0) | 0 |
| Engineering | workshop | 30 | 5.4 (5.0–7.0) | 22.5 (13.0–36.0) / 96.0 | 10.1 / 7.0 / 0.0 / 3.1 / 7.0 | 8.2 / 2.0 | 1.0 | 81.2 (40.7–169.6) | 0.0 | 42.7 (35.0–55.0) | 0.3 (0.0–3.0) | 0 |
| Relationship | fountain | 30 | 5.7 (5.0–11.0) | 21.4 (11.0–33.0) / 96.0 | 13.4 / 13.4 / 0.0 / 0.0 / 13.4 | 8.1 / 1.9 | 2.0 | 30.4 (9.9–49.2) | 0.0 | 32.7 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Relationship | workshop | 30 | 5.5 (5.0–9.0) | 15.4 (7.0–20.0) / 96.0 | 10.6 / 10.6 / 0.0 / 0.0 / 10.4 | 8.1 / 1.9 | 1.0 | 106.0 (44.1–204.8) | 0.2 | 30.2 (25.0–40.0) | 0.3 (0.0–2.0) | 0 |

## Income by source (era totals, aurei at the prices of the day; mean per run)

| Profile | Opening | Commissions (incl. shares) | Workshop | Odd jobs | Profit-share payments |
|---|---|---|---|---|---|
| Cooperative | fountain | 350.5 | 0.0 | 87.6 | 2.0 |
| Cooperative | workshop | 283.7 | 145.9 | 72.7 | 2.0 |
| Negotiator | fountain | 270.3 | 0.0 | 146.3 | 1.8 |
| Negotiator | workshop | 232.5 | 155.9 | 99.4 | 1.8 |
| Selective | fountain | 209.8 | 0.0 | 144.2 | 0.0 |
| Selective | workshop | 206.7 | 145.5 | 92.7 | 0.0 |
| Engineering | fountain | 228.2 | 0.0 | 196.0 | 2.0 |
| Engineering | workshop | 227.0 | 130.4 | 107.9 | 2.0 |
| Relationship | fountain | 347.6 | 0.0 | 114.0 | 1.9 |
| Relationship | workshop | 282.9 | 148.5 | 72.2 | 2.0 |

## Scene routing (all runs)

| Profile | Engineering | Personal | RomanLife | WorkEconomy | MachineMystery | CityHistory | Exploration | InstitutionsPolitics | Routed / run | Longest routed run (max) | Longest all-scene run (max) | Progression waiting (months / era) | Longest wait for one progression scene (months, max) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Cooperative | 1.8% (13.5%) | 13.1% (13.5%) | 49.0% (31.5%) | 7.2% (16.6%) | 5.0% (4.3%) | 7.0% (6.5%) | 5.0% (3.8%) | 11.8% (10.3%) | 96.0 | 9 | 6 (RomanLife) | 66.6 (40.0–84.0) | 53 |
| Negotiator | 1.9% (11.8%) | 12.2% (12.0%) | 50.4% (30.8%) | 7.1% (21.7%) | 5.4% (4.6%) | 7.5% (6.3%) | 4.5% (3.2%) | 11.0% (9.6%) | 95.5 | 9 | 9 (RomanLife) | 62.2 (2.0–87.0) | 60 |
| Selective | 1.8% (11.5%) | 12.0% (12.0%) | 51.0% (33.6%) | 6.5% (17.3%) | 5.2% (4.8%) | 7.4% (7.1%) | 4.2% (3.4%) | 11.8% (10.5%) | 95.9 | 9 | 6 (RomanLife) | 59.3 (37.0–80.0) | 33 |
| Engineering | 1.9% (13.0%) | 11.2% (10.8%) | 50.5% (33.0%) | 7.5% (17.4%) | 5.4% (4.9%) | 7.3% (6.5%) | 5.0% (4.1%) | 11.2% (10.3%) | 95.8 | 8 | 7 (RomanLife) | 61.1 (28.0–81.0) | 37 |
| Relationship | 1.9% (13.4%) | 12.2% (13.1%) | 50.5% (32.5%) | 6.8% (16.1%) | 5.2% (4.5%) | 6.8% (6.4%) | 4.7% (3.6%) | 11.9% (10.4%) | 95.9 | 12 | 8 (RomanLife) | 69.7 (41.0–88.0) | 55 |

Cells: share of routed optional scenes (share of all meaningful scenes, including the player's own work and interruptions, in brackets). "Progression waiting" counts months in which a commission, invitation, challenge or life development was a router candidate as the month ended; "longest wait" is the most months any one such candidate waited before it was picked.

Longest waits by candidate (max months as a candidate before it was picked; mean over runs where it was picked; runs where it was still waiting at departure):

| Candidate | Max wait | Mean wait | Runs picked | Still waiting at departure (runs; mean months waited) |
|---|---|---|---|---|
| invitation:circle | 60 | 11.8 | 150 | 1; 8.0 |
| commission:crane | 55 | 7.3 | 242 | 0 |
| invitation:guild | 53 | 9.4 | 294 | 0 |
| commission:drawings | 53 | 7.5 | 265 | 0 |
| life:felix-son | 46 | 6.7 | 211 | 0 |
| commission:fountainworks | 42 | 7.1 | 283 | 0 |
| commission:jars | 42 | 7.1 | 161 | 1; 14.0 |
| commission:fevers | 40 | 6.8 | 238 | 0 |
| challenge:power | 37 | 5.8 | 287 | 2; 7.0 |
| life:felix-quaestor | 37 | 6.5 | 231 | 0 |
| life:serenus-rival | 37 | 7.2 | 156 | 3; 5.0 |
| life:pollio-copy | 36 | 6.0 | 218 | 0 |
| commission:bilges | 35 | 4.4 | 297 | 0 |
| commission:millbearing | 35 | 6.1 | 288 | 1; 1.0 |
| life:diodoros-deliveries | 34 | 6.8 | 134 | 0 |
| life:cassianus-betrothal | 33 | 6.4 | 189 | 0 |
| commission:drains | 33 | 7.5 | 255 | 0 |
| commission:households | 33 | 6.6 | 168 | 3; 7.3 |
| commission:argiletum | 31 | 6.7 | 76 | 0 |
| commission:baths | 29 | 5.9 | 259 | 0 |

## Attention (era months)

| Profile | Opening | Months ending with 0 free | 1 free | 2+ free | Idle Attention / month | Overbooking refusals (future months) | Challenge stages refused for Attention |
|---|---|---|---|---|---|---|---|
| Cooperative | fountain | 33.4 | 14.5 | 48.1 | 1.8 | 0.0 | 4.8 |
| Cooperative | workshop | 36.4 | 16.9 | 42.7 | 1.6 | 0.0 | 2.9 |
| Negotiator | fountain | 30.6 | 17.2 | 48.2 | 1.8 | 0.0 | 3.0 |
| Negotiator | workshop | 35.7 | 17.7 | 42.6 | 1.6 | 0.0 | 2.2 |
| Selective | fountain | 29.8 | 13.0 | 53.1 | 2.0 | 0.0 | 1.2 |
| Selective | workshop | 33.7 | 17.2 | 45.1 | 1.7 | 0.0 | 1.1 |
| Engineering | fountain | 31.6 | 16.1 | 48.3 | 1.7 | 0.0 | 2.6 |
| Engineering | workshop | 41.0 | 18.9 | 36.1 | 1.2 | 0.0 | 2.5 |
| Relationship | fountain | 30.7 | 16.0 | 49.3 | 1.8 | 0.0 | 1.8 |
| Relationship | workshop | 33.8 | 19.3 | 42.9 | 1.6 | 0.0 | 1.0 |

## Jumps (measured, not changed)

- Runs that jumped: 300 of 300; jumped twice: 300.
- Departure years: AD 163 × 300.
- First jump distances: 25 yrs × 84, 30 yrs × 54, 35 yrs × 74, 40 yrs × 71, 45 yrs × 11, 50 yrs × 4, 55 yrs × 2; ranges offered at departure: 25–40 × 250, 30–45 × 19, 35–50 × 21, 40–55 × 10.
- Second jump distances: 25 yrs × 66, 30 yrs × 69, 35 yrs × 74, 40 yrs × 64, 45 yrs × 11, 50 yrs × 13, 55 yrs × 3; ranges offered: 25–40 × 250, 30–45 × 19, 35–50 × 21, 40–55 × 10.
- First arrival years: 196.2 (188.0–218.0); second arrival years: 230.1 (213.0–268.0).
- Machine upgrades bought (Engineering profile): 1.5 (0.0–3.0).

## Integrity

- Measurement and standards opened by: allotment × 3, hoist × 8, never × 1, pump × 288; by opening: workshop hoist × 4, never × 1, pump × 145; fountain allotment × 3, hoist × 4, pump × 143.
- Grand Challenges completed: both in 272 runs, one in 19, none in 9.
- Ledger reconciles at departure: 300 of 300.
- Lowest gold in any run: 0.0.
- Duplicate scene texts in an era: 0; repeated sentences between arrivals: 0.
- R-17: reached the warning in 296 of 300 runs; furthest scene: r17-acknowledged × 1, r17-active × 9, r17-again × 36, r17-dark × 53, r17-link × 2, r17-notebook × 199.
- Echo kinds on the first arrival: access 4, person 598, technical 1418, unintended 263; second: person 598, technical 1252, unintended 263.
- People remembered on the first arrival: Felix 275, Serenus 176, Gaius 65, Aulus 60, Diodoros 21, Cassianus 1; second: Gaius 188, Aulus 167, Diodoros 80, Cassianus 61, Serenus 49, Livia 27, Felix 22, Lucan 2, Marcus 2.
