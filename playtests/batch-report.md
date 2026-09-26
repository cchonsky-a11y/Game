# P0 batch balance report

100 seeded runs × 3 strategies × 2 jump timings (Early: leave at the start of AD 160, before the outbreak; Late: leave at the start of AD 165, when the era's 20 turns end).
A strategy **wins** a seed when it has the highest arrival Index among the three strategies for that seed and timing (ties split).

| Timing | Strategy | Win rate | Index at departure | Index at arrival (mean) | min–max | Plague severity | Outbreak while away | Crises in absence | Institution debt paid | Corruption (runs) | Promise kept / broken | Player actions |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Early | Balanced | 34% | 97 | 63 | 14–103 | 25.8 | 100% | 3.8 | 0.0 | 0 | 0 / 100 | 19.0 |
| Early | Specialized | 61% | 104 | 87 | 17–103 | 13.2 | 100% | 3.6 | 0.4 | 4 | 0 / 100 | 18.0 |
| Early | Neglectful | 5% | 33 | 33 | 13–98 | 136.4 | 100% | 15.9 | 0.0 | 0 | 0 / 0 | 9.0 |
| Late | Balanced | 58% | 99 | 98 | 62–114 | 9.5 | 0% | 2.6 | 0.3 | 6 | 100 / 0 | 36.0 |
| Late | Specialized | 42% | 99 | 96 | 57–103 | 10.0 | 0% | 3.3 | 0.3 | 4 | 100 / 0 | 30.1 |
| Late | Neglectful | 0% | 12 | 32 | 13–97 | 150.2 | 0% | 16.6 | 0.0 | 0 | 0 / 0 | 10.0 |

## Institution outcomes on arrival

| Timing | Strategy | Institution | Thriving | Drifted | Captured | Dissolved | Rogue | Not founded |
|---|---|---|---|---|---|---|---|---|
| Early | Balanced | circle | 0 | 0 | 0 | 100 | 0 | 0 |
| Early | Balanced | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Early | Specialized | circle | 0 | 0 | 0 | 100 | 0 | 0 |
| Early | Specialized | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Early | Neglectful | circle | 0 | 0 | 0 | 0 | 0 | 100 |
| Early | Neglectful | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Balanced | circle | 0 | 3 | 0 | 97 | 0 | 0 |
| Late | Balanced | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Specialized | circle | 0 | 0 | 0 | 100 | 0 | 0 |
| Late | Specialized | faction | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Neglectful | circle | 0 | 0 | 0 | 0 | 0 | 100 |
| Late | Neglectful | faction | 0 | 0 | 0 | 0 | 0 | 100 |

## Pacing

- First plague warning: AD 158–159; outbreak: AD 161–164 (mean 162.7).
- Balanced: first debt tier change on average 7.0–7.5 years in (earliest–mean).
- Specialized: first debt tier change on average 5.0–5.5 years in (earliest–mean).
- Neglectful: first debt tier change on average 2.0–2.3 years in (earliest–mean).

## Pass criteria (PROTOTYPE_SCOPE)

Gate 1, per timing: within each timing, no strategy wins more than 65% of seeds, and Balanced and Specialized both win some.
- Early: Balanced 34%, Specialized 61%, Neglectful 5% → **PASS**
- Late: Balanced 58%, Specialized 42%, Neglectful 0% → **PASS**

Gate 2, timing: for each strategy and seed, the timing with the higher arrival Index wins; neither timing may win more than 65% overall.
- Early 37%, Late 63% → **PASS**
  - Balanced: Early 11%, Late 89%
  - Specialized: Early 30%, Late 70%
  - Neglectful: Early 69%, Late 31%

**Overall balance gate: PASS**
