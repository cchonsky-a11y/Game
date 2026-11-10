# P1 outcome spread: baseline before the Stakes and Impact pass

**Automated, scripted runs of the actual build. Not human playtests.** 30 seeds × 6 profiles = 180 runs; odd seeds open with the workshop, even seeds with the fountain; every profile leaves at the first chance from AD 163. Idle repairs the machine and does odd jobs, and takes no commission, challenge or invitation. Regenerate: `dotnet run --project src/Butterfly.Batch -- --p1-spread 1-30`.

## Can a run be lost?

- Runs that jumped: 180 of 180. Runs that ended any other way: 0 (the machine not ready by the end of the era).

## The world, by profile (mean, min–max)

| Profile | Left (AD) | Index at departure | Index on first arrival | Commissions done | Challenges done | Capabilities moved | Highest capability level | Attention unused / month |
|---|---|---|---|---|---|---|---|---|
| Idle | 163.0 (163.0–163.0) | 102.4 (99.7–108.1) | 99.2 (96.0–105.0) | 0.0 (0.0–0.0) | 0.0 (0.0–0.0) | 0.0 (0.0–0.0) | None | 2.2 (2.0–2.4) |
| Cooperative | 163.0 (163.0–163.0) | 103.4 (100.4–108.7) | 102.5 (98.9–108.5) | 12.0 (9.0–14.0) | 1.9 (1.0–2.0) | 9.6 (8.0–10.0) | Reproducible | 1.7 (1.4–1.9) |
| Negotiator | 163.0 (163.0–163.0) | 103.3 (100.7–108.5) | 102.1 (97.1–107.1) | 8.4 (0.0–12.0) | 1.8 (0.0–2.0) | 9.2 (0.0–10.0) | Reproducible | 1.7 (1.4–2.1) |
| Selective | 163.0 (163.0–163.0) | 103.5 (100.6–109.4) | 102.7 (97.7–108.5) | 7.7 (5.0–11.0) | 1.5 (0.0–2.0) | 7.3 (4.0–9.0) | Reproducible | 1.8 (1.5–2.1) |
| Engineering | 163.0 (163.0–163.0) | 103.4 (100.4–108.9) | 102.8 (99.0–108.5) | 7.0 (6.0–7.0) | 1.8 (1.0–2.0) | 7.7 (5.0–8.0) | Reproducible | 1.4 (1.0–1.9) |
| Relationship | 163.0 (163.0–163.0) | 106.9 (104.4–109.3) | 106.2 (104.1–108.1) | 11.7 (9.0–14.0) | 1.8 (1.0–2.0) | 9.1 (7.0–10.0) | Reproducible | 1.7 (1.5–1.9) |

- **What playing is worth to the Index:** active profiles arrive at 103.3 on average, Idle at 99.2 (a gap of 4.1 points; 100 = history).
- **Spread among active players on arrival:** 97.1–108.5.

## Capabilities at departure (runs reaching each level, active profiles)

| Capability | None | Theory | Demonstrated | Prototype | Reproducible | Manufacturable | Economical | Adopted | Institutionalized |
|---|---|---|---|---|---|---|---|---|---|
| alloys | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| bearings | 5 | 0 | 0 | 27 | 118 | 0 | 0 | 0 | 0 |
| casenotes | 57 | 0 | 12 | 81 | 0 | 0 | 0 | 0 | 0 |
| copying | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| drawings | 32 | 0 | 0 | 1 | 117 | 0 | 0 | 0 | 0 |
| electricity | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| gauges | 3 | 0 | 0 | 0 | 147 | 0 | 0 | 0 | 0 |
| governor | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| hydraulicpress | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| interchange | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| lathework | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| lineshaft | 6 | 2 | 0 | 4 | 138 | 0 | 0 | 0 | 0 |
| metrology | 1 | 0 | 0 | 0 | 149 | 0 | 0 | 0 | 0 |
| poweredboring | 27 | 0 | 0 | 123 | 0 | 0 | 0 | 0 | 0 |
| records | 56 | 0 | 13 | 0 | 81 | 0 | 0 | 0 | 0 |
| steam | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| steelgrades | 150 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| transmission | 22 | 0 | 0 | 10 | 118 | 0 | 0 | 0 | 0 |
| valveseats | 3 | 0 | 0 | 0 | 147 | 0 | 0 | 0 | 0 |

## How different are two players' returns?

Overlap = shared return places with the same variant ÷ all places either saw (1 = the same return; 0 = nothing in common), same seed, mean over seeds.

| | Idle | Cooperative | Negotiator | Selective | Engineering | Relationship |
|---|---|---|---|---|---|---|
| Idle | 1.00 | 0.00 | 0.04 | 0.01 | 0.00 | 0.00 |
| Cooperative | 0.00 | 1.00 | 0.67 | 0.73 | 0.86 | 0.85 |
| Negotiator | 0.04 | 0.67 | 1.00 | 0.59 | 0.68 | 0.67 |
| Selective | 0.01 | 0.73 | 0.59 | 1.00 | 0.75 | 0.73 |
| Engineering | 0.00 | 0.86 | 0.68 | 0.75 | 1.00 | 0.90 |
| Relationship | 0.00 | 0.85 | 0.67 | 0.73 | 0.90 | 1.00 |

Distinct return places (place:variant) seen across all runs: 29. Most common: guildhall:rule (148), fittings:guildfoot (146), journal-foot:guild (146), copyseats:outsold (128), felix:founder (116), aulus:shafts (115), landing:lintel (36), felix:yard (31).
