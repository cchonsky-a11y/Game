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
| Cooperative | fountain | 30 | 5.7 (5.0–11.0) | 18.6 (4.0–34.0) / 96.0 | 13.5 / 13.5 / 0.0 / 0.0 / 13.4 | 8.3 / 2.0 | 1.9 | 55.7 (8.6–161.0) | 0.0 | 32.7 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Cooperative | workshop | 30 | 5.4 (5.0–7.0) | 14.5 (10.0–25.0) / 96.0 | 10.5 / 10.5 / 0.0 / 0.0 / 10.4 | 8.0 / 2.0 | 1.0 | 197.6 (80.3–367.0) | 0.0 | 34.2 (25.0–40.0) | 0.1 (0.0–1.0) | 0 |
| Negotiator | fountain | 30 | 5.7 (5.0–11.0) | 29.4 (13.0–48.0) / 96.0 | 12.4 / 9.3 / 3.1 / 0.0 / 9.2 | 8.5 / 1.8 | 1.9 | 43.3 (0.0–147.2) | 0.0 | 33.3 (25.0–40.0) | 0.0 (0.0–1.0) | 0 |
| Negotiator | workshop | 30 | 5.4 (5.0–7.0) | 21.3 (8.0–33.0) / 96.0 | 10.4 / 7.6 / 2.8 / 0.0 / 7.6 | 8.8 / 1.8 | 1.0 | 159.8 (30.6–306.4) | 0.1 | 32.7 (25.0–40.0) | 0.7 (0.0–20.0) | 0 |
| Selective | fountain | 30 | 5.7 (5.0–11.0) | 28.4 (14.0–45.0) / 96.0 | 12.3 / 8.2 / 0.0 / 4.0 / 8.2 | 5.9 / 1.3 | 2.0 | 39.0 (15.3–119.7) | 0.0 | 31.5 (25.0–40.0) | 0.1 (0.0–1.0) | 0 |
| Selective | workshop | 30 | 5.4 (5.0–7.0) | 19.4 (10.0–38.0) / 96.0 | 10.3 / 7.3 / 0.0 / 3.0 / 7.3 | 7.6 / 2.0 | 1.0 | 161.6 (41.3–306.4) | 0.1 | 32.0 (25.0–40.0) | 0.2 (0.0–3.0) | 0 |
| Engineering | fountain | 30 | 5.7 (5.0–11.0) | 38.9 (21.0–52.0) / 96.0 | 11.5 / 6.9 / 0.0 / 4.6 / 6.9 | 8.2 / 1.9 | 2.0 | 33.8 (10.8–60.1) | 0.0 | 37.7 (25.0–50.0) | 0.0 (0.0–0.0) | 0 |
| Engineering | workshop | 30 | 5.4 (5.0–7.0) | 21.1 (12.0–34.0) / 96.0 | 10.1 / 7.0 / 0.0 / 3.1 / 7.0 | 7.8 / 2.0 | 1.0 | 82.2 (40.3–161.8) | 0.0 | 45.0 (30.0–55.0) | 0.3 (0.0–3.0) | 0 |
| Relationship | fountain | 30 | 5.7 (5.0–11.0) | 21.5 (10.0–34.0) / 96.0 | 13.5 / 13.5 / 0.0 / 0.0 / 13.5 | 7.9 / 2.0 | 2.0 | 29.7 (13.3–39.8) | 0.0 | 33.0 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Relationship | workshop | 30 | 5.5 (5.0–9.0) | 15.9 (9.0–23.0) / 96.0 | 10.2 / 10.2 / 0.0 / 0.0 / 10.1 | 7.5 / 2.0 | 1.0 | 116.5 (41.6–209.2) | 0.0 | 32.8 (25.0–40.0) | 0.1 (0.0–1.0) | 0 |

## Income by source (era totals, aurei at the prices of the day; mean per run)

| Profile | Opening | Commissions (incl. shares) | Workshop | Odd jobs | Profit-share payments |
|---|---|---|---|---|---|
| Cooperative | fountain | 345.4 | 0.0 | 94.2 | 1.9 |
| Cooperative | workshop | 284.1 | 147.3 | 67.9 | 2.0 |
| Negotiator | fountain | 268.6 | 0.0 | 147.3 | 1.8 |
| Negotiator | workshop | 220.2 | 153.8 | 100.0 | 1.8 |
| Selective | fountain | 211.8 | 0.0 | 144.0 | 0.0 |
| Selective | workshop | 203.1 | 146.9 | 92.5 | 0.0 |
| Engineering | fountain | 225.9 | 0.0 | 199.2 | 2.0 |
| Engineering | workshop | 226.9 | 130.7 | 100.4 | 2.0 |
| Relationship | fountain | 348.3 | 0.0 | 114.0 | 1.9 |
| Relationship | workshop | 280.3 | 147.7 | 74.5 | 2.0 |

## Scene routing (all runs)

| Profile | Engineering | Personal | RomanLife | WorkEconomy | MachineMystery | CityHistory | Exploration | InstitutionsPolitics | Routed / run | Longest routed run (max) | Longest all-scene run (max) | Progression waiting (months / era) | Longest wait for one progression scene (months, max) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Cooperative | 1.8% (13.4%) | 13.2% (13.6%) | 48.0% (31.1%) | 7.9% (16.7%) | 4.9% (4.3%) | 7.1% (6.5%) | 5.1% (3.9%) | 11.9% (10.5%) | 96.0 | 8 | 7 (RomanLife) | 67.1 (42.0–89.0) | 49 |
| Negotiator | 2.0% (11.9%) | 13.1% (12.4%) | 48.8% (29.9%) | 8.0% (22.4%) | 5.3% (4.5%) | 7.0% (6.2%) | 4.6% (3.2%) | 11.2% (9.6%) | 95.6 | 9 | 9 (RomanLife) | 67.6 (2.0–87.0) | 48 |
| Selective | 1.8% (11.5%) | 12.5% (12.2%) | 50.3% (33.2%) | 6.4% (17.2%) | 5.3% (4.8%) | 7.4% (7.1%) | 4.4% (3.5%) | 11.8% (10.4%) | 95.9 | 9 | 7 (RomanLife) | 63.0 (37.0–87.0) | 42 |
| Engineering | 1.8% (12.8%) | 10.8% (10.5%) | 49.8% (32.6%) | 8.5% (18.0%) | 5.2% (4.8%) | 7.5% (6.7%) | 5.2% (4.2%) | 11.1% (10.3%) | 95.8 | 8 | 7 (RomanLife) | 62.6 (39.0–81.0) | 40 |
| Relationship | 1.8% (13.3%) | 12.3% (13.0%) | 49.7% (32.2%) | 7.7% (16.5%) | 5.1% (4.4%) | 6.9% (6.5%) | 5.0% (3.8%) | 11.4% (10.2%) | 95.9 | 12 | 8 (RomanLife) | 69.9 (40.0–88.0) | 48 |

Cells: share of routed optional scenes (share of all meaningful scenes, including the player's own work and interruptions, in brackets). "Progression waiting" counts months in which a commission, invitation, challenge or life development was a router candidate as the month ended; "longest wait" is the most months any one such candidate waited before it was picked.

Longest waits by candidate (max months as a candidate before it was picked; mean over runs where it was picked; runs where it was still waiting at departure):

| Candidate | Max wait | Mean wait | Runs picked | Still waiting at departure (runs; mean months waited) |
|---|---|---|---|---|
| invitation:circle | 49 | 12.1 | 148 | 1; 17.0 |
| commission:crane | 48 | 7.7 | 245 | 0 |
| life:cassianus-betrothal | 48 | 6.5 | 196 | 0 |
| commission:drains | 46 | 8.3 | 259 | 0 |
| commission:drawings | 45 | 7.9 | 268 | 0 |
| invitation:guild | 43 | 9.5 | 294 | 0 |
| commission:fountainworks | 43 | 7.2 | 285 | 0 |
| life:serenus-tables-copied | 42 | 7.9 | 119 | 4; 9.3 |
| life:felix-son | 40 | 7.1 | 210 | 0 |
| commission:jars | 38 | 6.6 | 167 | 0 |
| commission:fevers | 38 | 6.5 | 231 | 1; 1.0 |
| life:pollio-copy | 37 | 6.0 | 220 | 0 |
| commission:households | 36 | 7.4 | 158 | 3; 12.0 |
| commission:argiletum | 36 | 7.9 | 80 | 0 |
| life:vettius-copies | 34 | 6.5 | 173 | 0 |
| commission:bilges | 33 | 4.5 | 297 | 0 |
| life:serenus-rival | 33 | 6.1 | 157 | 4; 4.8 |
| life:diodoros-deliveries | 32 | 6.5 | 141 | 1; 11.0 |
| commission:sluice | 32 | 5.9 | 204 | 6; 6.0 |
| life:diodoros-antioch | 32 | 7.5 | 59 | 1; 31.0 |

## Attention (era months)

| Profile | Opening | Months ending with 0 free | 1 free | 2+ free | Idle Attention / month | Overbooking refusals (future months) | Challenge stages refused for Attention |
|---|---|---|---|---|---|---|---|
| Cooperative | fountain | 33.1 | 15.2 | 47.8 | 1.8 | 0.0 | 4.9 |
| Cooperative | workshop | 36.1 | 16.1 | 43.8 | 1.7 | 0.0 | 2.7 |
| Negotiator | fountain | 30.9 | 16.4 | 48.7 | 1.8 | 0.0 | 2.8 |
| Negotiator | workshop | 35.5 | 17.8 | 42.6 | 1.6 | 0.0 | 2.3 |
| Selective | fountain | 30.2 | 12.9 | 52.9 | 1.9 | 0.0 | 1.1 |
| Selective | workshop | 33.4 | 17.5 | 45.2 | 1.7 | 0.0 | 1.1 |
| Engineering | fountain | 31.2 | 16.4 | 48.4 | 1.7 | 0.0 | 2.6 |
| Engineering | workshop | 39.8 | 20.1 | 36.1 | 1.2 | 0.0 | 2.2 |
| Relationship | fountain | 31.1 | 14.9 | 50.0 | 1.8 | 0.0 | 1.9 |
| Relationship | workshop | 33.5 | 19.3 | 43.2 | 1.7 | 0.0 | 1.0 |

## Jumps (measured, not changed)

- Runs that jumped: 300 of 300; jumped twice: 300.
- Departure years: AD 163 × 300.
- First jump distances: 25 yrs × 58, 30 yrs × 60, 35 yrs × 80, 40 yrs × 75, 45 yrs × 15, 50 yrs × 8, 55 yrs × 4; ranges offered at departure: 25–40 × 251, 30–45 × 17, 35–50 × 19, 40–55 × 13.
- Second jump distances: 25 yrs × 64, 30 yrs × 64, 35 yrs × 79, 40 yrs × 70, 45 yrs × 9, 50 yrs × 7, 55 yrs × 7; ranges offered: 25–40 × 251, 30–45 × 17, 35–50 × 19, 40–55 × 13.
- First arrival years: 197.5 (188.0–218.0); second arrival years: 231.6 (213.0–273.0).
- Machine upgrades bought (Engineering profile): 1.6 (0.0–3.0).

## Integrity

- Measurement and standards opened by: allotment × 3, hoist × 8, never × 1, pump × 288; by opening: workshop hoist × 4, never × 1, pump × 145; fountain allotment × 3, hoist × 4, pump × 143.
- Grand Challenges completed: both in 267 runs, one in 26, none in 7.
- Ledger reconciles at departure: 300 of 300.
- Lowest gold in any run: 0.0.
- Duplicate scene texts in an era: 0; repeated sentences between arrivals: 0.
- R-17: reached the warning in 293 of 300 runs; furthest scene: r17-acknowledged × 1, r17-active × 13, r17-again × 35, r17-dark × 60, r17-link × 4, r17-notebook × 186, r17-request × 1.
- Echo kinds on the first arrival: access 4, person 598, technical 1392, unintended 692; second: person 598, technical 1232, unintended 266.
- First arrivals showing all four echo kinds (personal, technical, institutional, unintended): 298 of 300; missing unintended: 2; missing institutional: 1; second arrivals with all four: 266.
- People remembered on the first arrival: Felix 277, Serenus 168, Gaius 64, Aulus 59, Diodoros 26, Cassianus 4; second: Gaius 186, Aulus 155, Diodoros 79, Cassianus 72, Serenus 54, Livia 27, Felix 18, Marcus 6, Lucan 1.
