# P0 batch balance report

100 seeded runs × 3 strategies × 2 jump timings (Early: leave at the start of AD 162, before the outbreak; Late: leave at the start of AD 169, after it).
A strategy **wins** a seed when it has the highest arrival Index among the three strategies for that seed and timing (ties split).

| Timing | Strategy | Win rate | Index at departure | Index at arrival (mean) | min–max | Plague severity | Outbreak while away | Crises in absence | Promise kept / broken | Player actions |
|---|---|---|---|---|---|---|---|---|---|---|
| Early | Balanced | 9% | 112 | 108 | 96–113 | 10.3 | 100% | 4.7 | 0 / 49 | 26.5 |
| Early | Specialized | 91% | 103 | 114 | 96–120 | 7.5 | 100% | 4.8 | 0 / 49 | 33.0 |
| Early | Neglectful | 0% | 30 | 74 | 18–103 | 88.4 | 100% | 7.9 | 0 / 0 | 9.0 |
| Late | Balanced | 96% | 118 | 131 | 114–135 | 8.2 | 0% | 4.0 | 100 / 0 | 45.2 |
| Late | Specialized | 4% | 106 | 119 | 112–120 | 7.5 | 0% | 5.0 | 100 / 0 | 51.5 |
| Late | Neglectful | 0% | 14 | 71 | 17–103 | 88.4 | 0% | 8.3 | 0 / 0 | 10.0 |

## Institution outcomes on arrival

| Timing | Strategy | Institution | Thriving | Drifted | Captured | Dissolved | Rogue | Not founded |
|---|---|---|---|---|---|---|---|---|
| Early | Balanced | circle | 0 | 51 | 0 | 0 | 49 | 0 |
| Early | Balanced | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Early | Specialized | circle | 21 | 79 | 0 | 0 | 0 | 0 |
| Early | Specialized | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Early | Neglectful | circle | 0 | 0 | 0 | 0 | 0 | 100 |
| Early | Neglectful | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Balanced | circle | 28 | 72 | 0 | 0 | 0 | 0 |
| Late | Balanced | faction | 0 | 100 | 0 | 0 | 0 | 0 |
| Late | Specialized | circle | 29 | 71 | 0 | 0 | 0 | 0 |
| Late | Specialized | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Neglectful | circle | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Neglectful | faction | 0 | 0 | 0 | 0 | 0 | 100 |

## Pacing

- First plague warning: AD 161–162; outbreak: AD 164–167 (mean 165.3).
- Balanced: first debt tier change on average never.
- Specialized: first debt tier change on average 5.0–5.5 years in (earliest–mean).
- Neglectful: first debt tier change on average 2.0–2.3 years in (earliest–mean).

## Pass criteria (PROTOTYPE_SCOPE)

- All runs pooled: Balanced 52%, Specialized 48%, Neglectful 0% → **PASS** (both viable, none above 65%)
- Early: Balanced 9%, Specialized 91%, Neglectful 0% → **FAIL** (a strategy dominates or one isn't viable)
- Late: Balanced 96%, Specialized 4%, Neglectful 0% → **FAIL** (a strategy dominates or one isn't viable)
