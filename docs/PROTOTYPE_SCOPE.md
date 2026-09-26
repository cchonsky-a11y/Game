# PROTOTYPE_SCOPE.md — Current Milestone: P0 Butterfly Test

> This file defines the **only** work in scope right now.
> VISION.md and SYSTEMS.md describe the long-term game; they are **not** a build list.
> If something is not listed under "In scope," do not build it, even if other documents describe it.

## The question P0 must answer
Do players feel their actions changed the world they return to, and want to see what happens next?

Target tester sentence (unprompted): *"The plague hit harder because I left before the fountain was fixed, and the order I founded turned into something I didn't want."*

## Deliverable
- A **text-based** prototype: no graphics, no Unity.
- A **C# class library targeting .NET Standard 2.1** (usable later inside Unity) containing the simulation.
- A **console app** that runs the library for a human player.
- A **small batch runner** that plays the scenario automatically with scripted strategies.

## Scenario
Rome, AD 155. One region. The player makes 8–12 meaningful decisions over roughly 10 in-game years, faces the plague, then jumps 250 years forward and receives a four-beat arrival.

## In scope

| Area | P0 version |
|---|---|
| Determinism | Seeded random generator; same seed + inputs = same result |
| Event log | Immediate causes, actors, effects (SYSTEMS.md §1); a `why <thing>` console command for current conditions only |
| Time | Turns in months or years; per-year simulation; one coarse-mode jump in decade steps |
| P0 pacing (**P0-only**, not in SYSTEMS.md) | 6-month turns, 20 per era (AD 155–165). Turns with no pending decision advance on their own (a pending decision is an open prompt, news that turn, an affordable new investment, or the end of the era). Attention costs are tuned so total demand is at least 1.5× supply (20 × 4 = 80). Demand counts every project once, every institution step (found, charter, audit, endow), a full mentoring commitment per institution, the oversight needed to hold loyalty, one plague response, and one personal action per turn |
| Care domains | **3 only:** Medicine, Governance, Economy |
| Priorities | Protect / Maintain / Accept Risk per domain |
| Projects | 2–3 templated projects per domain (e.g., repair the fountain, fund a physician, market regulation, patronage for a senator) |
| Expectations and debt | SYSTEMS.md §6: expectation rule, 5% compounding, tiers, 1.5× paydown |
| Crisis | The plague, with **3 visible warning stages** and branching outcomes |
| Institutions | **2:** a physicians' circle and a senate faction. Each with a named leader, loyalty, decay per SYSTEMS.md §7, and **2 pre-authored drift paths** (no general identity engine) |
| Gold | Simple income and spending on projects and upkeep |
| Institution gold | Institutions hold gold given as endowments. **Only in the 30 years after departure:** holdings grow at the region's economic growth rate (0–1.5%/yr by Economy level; no fixed-rate compounding); institutions pay down debt in their own domain at the 1.5× premium (loyal: in full; drifted: partially; rogue: nothing). Actions: **endow** an institution with any amount of gold (the minimum endowment makes it endowed) and found an **audit charter** (costs gold). Jump preparation offers paying down debt directly, endowing an institution, and founding an audit charter |
| Institutional corruption | Checked each decade in the 30-year window. Chance = base hazard × exposure (small ×1, large ×2) × (1 − audit charter 0.5) × (1 − leader integrity: honest 0.3, average 0, venal −0.3). Minor / Major / Total (lose 25% / 50% / 100% of holdings; payments reduced / stop / stop; small / moderate / large Governance debt; Total makes the institution Captured or Rogue). Weights: unprotected 40/40/20, audited 70/25/5, shifted by integrity. Separate from loyalty. Each leader has a pre-authored integrity trait. The pre-jump briefing shows corruption risk (Low / Medium / High) and potential severity, never the outcome; the arrival's Discovery beat reveals any corruption and its level |
| Attention | 4 per turn; spent on projects, overseeing an institution, or one personal action. Also test **multi-turn commitments** as an option |
| Seeded choice | The hour-one fountain-or-workshop choice |
| Promise | One promise from an institution leader that conflicts with jump timing |
| Jump | Player chooses when to leave; 250-year absence simulated in decade steps |
| Echoes | **3 specific elements** (e.g., the fountain, the physicians' circle, the broken or kept promise). The seeded choice is always one of them |
| Arrival | Text version of the four beats: recognition, wrongness, personal echo, discovery. Optional `learn more` shows the Index and institution outcomes. **No causal chains after the jump** |
| Index | Geometric mean over the 3 domains, before and after the jump |
| Text | Hand-written templates only |

## Out of scope (do not build)
- Unity, graphics, UI beyond the console, audio
- Sim Mode scenes, historical figure dialogue, nudge verbs
- Fuel puzzles, repairs, repair tiers, malfunctions
- The Knowledge Web, absorption bars, emergent advancements, diffusion
- Multiple regions, spillover, AI civilizations, diplomacy, warfare
- Agriculture, Knowledge, Military, Infrastructure, Faith and Culture domains
- Life budget, aging, mortality, succession, the Journal
- Fortune shares (the regional-economy share model of SYSTEMS §9), loss events other than the plague and institutional corruption
- Visibility, anachronism risk, Legend
- Stages of control (use a fixed Stage 3 setup)
- A general institution identity engine
- Post-jump causal chains, the Chronicle, signature systems, the tutorial engine
- Saves, iCloud, purchases, analytics
- Any language-model integration

## Order of work
1. Build the simulation and console game.
2. **Run the batch runner before any human test:** 100 seeded runs × 5 strategies (Balanced/Pay-down, Specialized, Neglectful, Endow, Split), each with early and late jump timing. Fix any dominant strategy first.
3. Run 5 human testers.

## Pass criteria

| Test | Pass |
|---|---|
| Determinism | Identical results for identical seed and inputs |
| Balance vs. specialization | Both viable; within **each** jump timing (early, late), no strategy wins more than 65% of automated runs |
| Jump timing | Neither early nor late timing wins more than 65% of runs across all strategies |
| Debt at departure | Within each timing, none of Pay-down (Balanced), Endow, and Split wins more than 65% of runs; the batch report flags it if more than ~80% of the best runs bought an audit charter |
| Debt pacing | 2–3 years of accepted risk → Strained; plague warning stages visible before the outbreak |
| Institution decay | After 250 years from strength 80: bare 80 × 0.9^25 ≈ 6; chartered 80 × 0.97^25 ≈ 37; strong 80 × 0.99^25 ≈ 62 |
| Impact | At least 3 of 5 testers say, unprompted, that their actions changed the returned world, pointing to at least one Echo |
| Real choices | Testers can describe what they were choosing *between*, not just what they clicked |
| Desire to continue | Testers want to see what happens after another jump |
| Expectations | Testers don't avoid improvements to keep expectations low |
| "Why?" | In-era explanations readable to someone who didn't write the code |

## Kill criteria
- One strategy always dominates → rework debt or expectations before human testing.
- **The jump and arrival aren't compelling → redesign before any further prototype.**
- Testers describe only what they clicked, not what they chose between → cut decisions before adding systems.

## After P0
Next milestones, in order: P2 fuel puzzle (paper), P3 headless simulation core, P4 first-hour vertical slice in Unity, P5 first jump. Each will get its own version of this file.
