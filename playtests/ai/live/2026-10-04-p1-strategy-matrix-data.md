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
| Cooperative | fountain | 30 | 5.7 (5.0–11.0) | 20.6 (7.0–34.0) / 96.0 | 13.5 / 13.5 / 0.0 / 0.0 / 13.5 | 9.6 / 1.9 | 1.9 | 50.8 (6.7–144.6) | 0.0 | 31.7 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Cooperative | workshop | 30 | 5.4 (5.0–7.0) | 15.1 (10.0–25.0) / 96.0 | 10.5 / 10.5 / 0.0 / 0.0 / 10.4 | 9.9 / 1.9 | 1.0 | 163.4 (46.1–304.9) | 0.0 | 33.5 (25.0–40.0) | 0.0 (0.0–1.0) | 0 |
| Negotiator | fountain | 30 | 5.7 (5.0–11.0) | 31.5 (20.0–55.0) / 96.0 | 12.5 / 9.5 / 3.0 / 0.0 / 9.4 | 9.7 / 1.8 | 1.8 | 39.6 (9.7–103.1) | 0.0 | 31.2 (25.0–40.0) | 0.1 (0.0–2.0) | 0 |
| Negotiator | workshop | 30 | 5.4 (5.0–7.0) | 21.2 (8.0–33.0) / 96.0 | 10.3 / 7.6 / 2.7 / 0.0 / 7.6 | 9.3 / 1.8 | 1.0 | 148.7 (22.5–306.4) | 0.1 | 35.2 (25.0–40.0) | 0.7 (0.0–20.0) | 0 |
| Selective | fountain | 30 | 5.7 (5.0–11.0) | 29.4 (14.0–46.0) / 96.0 | 12.3 / 8.3 / 0.0 / 4.0 / 8.2 | 6.4 / 1.0 | 2.0 | 33.7 (10.1–81.7) | 0.0 | 32.7 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Selective | workshop | 30 | 5.4 (5.0–7.0) | 20.5 (10.0–40.0) / 96.0 | 10.2 / 7.2 / 0.0 / 3.1 / 7.0 | 9.7 / 1.8 | 1.0 | 122.3 (42.6–228.8) | 0.1 | 32.3 (25.0–40.0) | 0.0 (0.0–1.0) | 0 |
| Engineering | fountain | 30 | 5.7 (5.0–11.0) | 41.7 (29.0–60.0) / 96.0 | 11.5 / 6.9 / 0.0 / 4.6 / 6.9 | 9.1 / 1.7 | 2.0 | 29.9 (8.5–60.3) | 0.0 | 36.2 (25.0–50.0) | 0.0 (0.0–0.0) | 0 |
| Engineering | workshop | 30 | 5.4 (5.0–7.0) | 23.5 (12.0–34.0) / 96.0 | 10.0 / 7.0 / 0.0 / 3.0 / 7.0 | 9.8 / 2.0 | 1.0 | 72.6 (42.1–164.7) | 0.0 | 44.0 (30.0–55.0) | 0.0 (0.0–1.0) | 0 |
| Relationship | fountain | 30 | 5.7 (5.0–11.0) | 23.6 (14.0–37.0) / 96.0 | 13.3 / 13.3 / 0.0 / 0.0 / 13.2 | 9.4 / 1.8 | 1.9 | 22.1 (6.5–39.5) | 0.0 | 32.0 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |
| Relationship | workshop | 30 | 5.5 (5.0–9.0) | 16.2 (9.0–23.0) / 96.0 | 10.1 / 10.1 / 0.0 / 0.0 / 9.9 | 9.4 / 1.9 | 1.0 | 78.2 (37.4–180.0) | 0.0 | 31.2 (25.0–40.0) | 0.0 (0.0–0.0) | 0 |

## Income by source (era totals, aurei at the prices of the day; mean per run)

| Profile | Opening | Commissions (incl. shares) | Workshop | Odd jobs | Profit-share payments |
|---|---|---|---|---|---|
| Cooperative | fountain | 346.5 | 0.0 | 103.9 | 2.0 |
| Cooperative | workshop | 283.8 | 145.3 | 70.6 | 2.0 |
| Negotiator | fountain | 270.2 | 0.0 | 159.1 | 1.7 |
| Negotiator | workshop | 221.8 | 150.9 | 99.6 | 1.9 |
| Selective | fountain | 214.0 | 0.0 | 149.6 | 0.0 |
| Selective | workshop | 196.9 | 145.5 | 98.2 | 0.0 |
| Engineering | fountain | 225.7 | 0.0 | 212.7 | 2.0 |
| Engineering | workshop | 226.8 | 132.3 | 112.9 | 2.0 |
| Relationship | fountain | 343.1 | 0.0 | 124.9 | 2.0 |
| Relationship | workshop | 274.7 | 145.9 | 76.8 | 2.0 |

## Scene routing (all runs)

| Profile | Engineering | Personal | RomanLife | WorkEconomy | MachineMystery | CityHistory | Exploration | InstitutionsPolitics | Routed / run | Longest routed run (max) | Longest all-scene run (max) | Progression waiting (months / era) | Longest wait for one progression scene (months, max) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Cooperative | 1.8% (13.7%) | 13.8% (13.7%) | 46.0% (29.7%) | 8.1% (17.2%) | 5.1% (4.3%) | 6.7% (6.3%) | 5.7% (4.2%) | 12.8% (11.0%) | 96.0 | 7 | 6 (RomanLife) | 72.1 (38.0–88.0) | 45 |
| Negotiator | 2.0% (12.0%) | 13.3% (12.4%) | 47.7% (29.1%) | 8.0% (22.5%) | 5.3% (4.5%) | 6.6% (5.9%) | 4.9% (3.3%) | 12.3% (10.4%) | 95.6 | 9 | 9 (RomanLife) | 68.9 (2.0–87.0) | 60 |
| Selective | 1.8% (11.8%) | 12.5% (12.0%) | 49.0% (32.2%) | 6.9% (17.5%) | 5.3% (4.8%) | 6.9% (6.8%) | 4.9% (3.8%) | 12.7% (11.2%) | 96.0 | 8 | 6 (RomanLife) | 64.5 (37.0–87.0) | 41 |
| Engineering | 1.9% (13.1%) | 11.4% (10.8%) | 47.9% (31.2%) | 9.0% (18.5%) | 5.2% (4.7%) | 6.9% (6.3%) | 5.6% (4.4%) | 12.1% (11.1%) | 96.0 | 8 | 7 (RomanLife) | 66.1 (38.0–88.0) | 40 |
| Relationship | 1.8% (13.7%) | 12.3% (12.9%) | 47.2% (30.6%) | 8.2% (16.8%) | 5.5% (4.5%) | 6.5% (6.2%) | 5.3% (3.9%) | 13.2% (11.4%) | 96.0 | 12 | 8 (RomanLife) | 72.4 (44.0–91.0) | 53 |

Cells: share of routed optional scenes (share of all meaningful scenes, including the player's own work and interruptions, in brackets). "Progression waiting" counts months in which a commission, invitation, challenge or life development was a router candidate as the month ended; "longest wait" is the most months any one such candidate waited before it was picked.

Longest waits by candidate (max months as a candidate before it was picked; mean over runs where it was picked; runs where it was still waiting at departure):

| Candidate | Max wait | Mean wait | Runs picked | Still waiting at departure (runs; mean months waited) |
|---|---|---|---|---|
| commission:crane | 60 | 8.1 | 247 | 0 |
| life:cassianus-betrothal | 53 | 7.3 | 193 | 0 |
| life:felix-son | 49 | 7.1 | 210 | 0 |
| challenge:standards | 45 | 3.7 | 226 | 0 |
| life:marcus-priscus | 44 | 6.1 | 151 | 11; 5.5 |
| commission:drawings | 43 | 7.6 | 266 | 0 |
| invitation:guild | 41 | 9.7 | 294 | 0 |
| commission:fountainworks | 41 | 7.4 | 285 | 0 |
| invitation:circle | 41 | 12.5 | 150 | 5; 10.4 |
| commission:drains | 40 | 7.7 | 258 | 0 |
| life:vettius-copies | 39 | 6.6 | 171 | 1; 15.0 |
| life:livia-betrothal | 39 | 6.9 | 185 | 7; 9.4 |
| commission:sluice | 39 | 6.2 | 201 | 10; 5.1 |
| commission:jars | 38 | 7.4 | 167 | 1; 14.0 |
| life:pollio-copy | 37 | 6.1 | 219 | 0 |
| commission:households | 36 | 7.9 | 155 | 8; 15.0 |
| life:brass-bearings | 35 | 4.4 | 133 | 14; 4.6 |
| commission:argiletum | 34 | 7.7 | 81 | 0 |
| life:diodoros-mother | 33 | 6.7 | 104 | 2; 5.5 |
| commission:millbearing | 32 | 6.6 | 291 | 2; 2.0 |

## Attention (era months)

| Profile | Opening | Months ending with 0 free | 1 free | 2+ free | Idle Attention / month | Overbooking refusals (future months) | Challenge stages refused for Attention |
|---|---|---|---|---|---|---|---|
| Cooperative | fountain | 32.6 | 15.6 | 47.8 | 1.8 | 0.0 | 4.6 |
| Cooperative | workshop | 36.9 | 16.6 | 42.4 | 1.6 | 0.0 | 2.8 |
| Negotiator | fountain | 30.4 | 17.6 | 48.1 | 1.7 | 0.0 | 2.8 |
| Negotiator | workshop | 35.9 | 17.7 | 42.4 | 1.6 | 0.0 | 2.3 |
| Selective | fountain | 30.4 | 13.2 | 52.4 | 1.9 | 0.0 | 1.0 |
| Selective | workshop | 34.0 | 17.8 | 44.2 | 1.6 | 0.0 | 1.1 |
| Engineering | fountain | 31.6 | 17.6 | 46.7 | 1.6 | 0.0 | 2.7 |
| Engineering | workshop | 40.7 | 20.7 | 34.6 | 1.2 | 0.0 | 2.3 |
| Relationship | fountain | 31.0 | 15.4 | 49.6 | 1.7 | 0.0 | 1.9 |
| Relationship | workshop | 34.0 | 19.3 | 42.7 | 1.6 | 0.0 | 1.0 |

## Jumps (measured, not changed)

- Runs that jumped: 300 of 300; jumped twice: 300.
- Departure years: AD 163 × 300.
- First jump distances: 25 yrs × 60, 30 yrs × 65, 35 yrs × 84, 40 yrs × 71, 45 yrs × 8, 50 yrs × 11, 55 yrs × 1; ranges offered at departure: 25–40 × 250, 30–45 × 23, 35–50 × 21, 40–55 × 6.
- Second jump distances: 25 yrs × 62, 30 yrs × 72, 35 yrs × 68, 40 yrs × 77, 45 yrs × 13, 50 yrs × 6, 55 yrs × 2; ranges offered: 25–40 × 250, 30–45 × 23, 35–50 × 21, 40–55 × 6.
- First arrival years: 197.0 (188.0–218.0); second arrival years: 230.9 (213.0–263.0).
- Machine upgrades bought (Engineering profile): 1.4 (0.0–3.0).

## Integrity

- Measurement and standards opened by: allotment × 3, baths × 1, hoist × 8, never × 1, pump × 287; by opening: workshop hoist × 4, never × 1, pump × 145; fountain allotment × 3, baths × 1, hoist × 4, pump × 142.
- First return: started in 300 of 300 first jumps; completed in 300; second jump offered before the return was seen: 0.
- Return sites per arrival: 4 × 1, 6 × 299; below return.sitesMin (4): 0; visited by the protocol: 3.0 (3.0–3.0).
- Return site categories: Human 600, Institutional 300, Journal 299, Mystery 8, Technical 299, Unintended 292; runs with 3+ kinds: 300; runs with only one kind: 0.
- People found, by band: Aulus heirs 59, Aulus memory 29, Cassianus heirs 3, Felix heirs 264, Felix memory 12, Gaius elder 25, Gaius heirs 36, Serenus elder 80, Serenus heirs 92.
- Departure threads found: copies:outsold 266, foot:drift 5, foot:fabianfoot 2, foot:guildfoot 292, shaft:idle 5, shaft:shafts 83.
- Misattribution sites per return: 4.4 (1.0–5.0); journal lines written: 2.3 (0.0–3.0); journal comparisons among the sites: 299 runs.
- Machine archive step: a senator's note in 0 runs, a bribed clerk in 300.
- Grand Challenges completed: both in 234 runs, one in 60, none in 6.
- Ledger reconciles at departure: 300 of 300.
- Lowest gold in any run: 0.0.
- Duplicate scene texts in an era: 0; repeated sentences between arrivals: 0.
- R-17: reached the warning in 285 of 300 runs; furthest scene: r17-acknowledged × 1, r17-active × 9, r17-again × 34, r17-dark × 63, r17-link × 9, r17-notebook × 182, r17-request × 2.
- Echo kinds on the first arrival: access 11, person 587, technical 1629, unintended 471; second: person 598, technical 1315, unintended 292.
- First arrivals showing all four kinds (personal, technical, institutional, unintended) in the beats and the return together: 298 of 300; in the beats alone: 277; missing unintended (both): 2; missing institutional (both): 0; second arrivals with all four: 292.
- People remembered on the first arrival: Livia 196, Diodoros 185, Hermogenes 141, Lucan 65; second: Felix 275, Serenus 172, Aulus 89, Gaius 58, Cassianus 4.
