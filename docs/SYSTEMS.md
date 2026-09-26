# SYSTEMS.md — Canonical Rules

> The authoritative rules and numbers. When this file and the GDD disagree, this file wins; update both.
> Describing a system here does **not** mean it is in current scope. See PROTOTYPE_SCOPE.md.
> Values marked (tune) are starting points to be validated in prototypes.

## 1. Determinism and causality
- The simulation is deterministic: the same seed and the same player inputs always produce the same result.
- All randomness comes from a seeded generator owned by the simulation.
- **Event log.** Every meaningful state change is recorded as an event with:
  - `id`, `time`, `type`, `target`
  - `immediateCauses` (event ids)
  - `actors` (who acted, including the player)
  - `effects` (state deltas)
- Current conditions during an era must be explainable from the log ("Why?"). Multi-century causal ancestry and post-jump causal chains are **not** required.
- The language model never decides outcomes; it only describes logged events. Generated text is cached in the save.

## 2. Time
- Game time is measured in years. All simulated change (aging, debt, decay, drift, AI actions) scales per year, never per turn.
- Turn length by stage: 1 = 1 month; 2 = 2 months; 3 = 3 months; 4 = 6 months; 5 = 1 year.
- The player may shorten turn length at any time, never lengthen it beyond the stage cap.
- Jumps are simulated in decade steps (coarse mode) using the same rules.
- Undo is allowed within a turn, except for actions that reveal information (experiments, conversations, investigations).

## 3. The inventor
- **Attention:** 4 per turn; −1 when old or unwell; +1 with an apprentice or secretary. Never scales with stage. Can be borrowed from the next turn during a crisis at a health cost.
- **Healthy years** (expected maximum age with the best available care): baseline ~70; well-advanced medicine ~85; heavily advanced ~95. Mortality risk rises near the ceiling; the ceiling is predictable.
- Starts at age 28. Ages only within eras, never during jumps.
- **Succession:** a prepared successor inherits the machine; knowledge transfers through the Journal. No successor: Standard mode restarts from the latest checkpoint; Ironman ends the run.

## 4. Absorption
Every idea has three bars: **Knowledge**, **Means**, **Carriers** (0–100).
- Adoption speed is driven by the minimum bar.
- Adoption accuracy is driven by the mean of the bars.
- Low accuracy produces **distorted adoption**: an altered variant with partial benefits and side effects.
- Applies to technology, institutions, administration, finance, and social reforms.

## 5. Care domains, priorities, and projects
- Domains: Agriculture, Medicine, Knowledge, Governance, Military, Economy, Infrastructure, Faith and Culture.
- Each region has a level per domain. Levels decay without upkeep.
- The player sets each domain per region to **Protect**, **Maintain**, or **Accept Risk**. Upkeep gold is split by priority.
- Investment happens through **projects** (templated per domain, re-dressed per era) that change domain levels. The player never sets a domain level directly.

## 6. Expectations, debt, and crises
- **Expectation** = max(historical benchmark for the era, the region's recent peak fading over time). (tune; split into social and physical values if P0 shows players avoiding improvement)
- **Debt accrual per year** = max(0, expectation − level) × rate (rate = 1, tune).
- **Compounding:** existing debt grows 5% per year.
- **Tiers:** Stable → Strained → Fragile → Critical; each raises the yearly crisis chance. Severity scales with debt.
- **Warning stages:** every major crisis passes through visible antecedent stages before breaking out.
- **Paying down debt** costs 1.5× the gold that prevention would have cost.
- A crisis releases debt and damages levels, population, and wealth.
- **Crisis outcomes branch:** crises can also create openings (migration, orphaned talent, new movements, experimentation, scattered scholars).
- **Spillover:** debt spreads along trade routes, borders, and shared institutions in proportion to connection strength.

## 7. Institutions
- Attributes: `type` (political, religious, commercial, scholarly, military), `reach`, `capacity` (directives per turn), `loyalty`, `leader` (a named notable with a successor), `identity`, `drift`.
- Directive effectiveness = capacity × loyalty, within reach, limited by type.
- **Identities:** charitable, commercial, bureaucratic, militant, ritualistic, isolationist, reformist, politically powerful. Drift moves identity toward the institution's own interests; charters, founding principles, and reverence for the founder slow it.
- **Decay per decade during absence:** bare 10%; chartered and endowed 3%; chartered, endowed, in a thriving region, keeping the Legend alive 1%.
- **Outcomes on arrival:** Thriving, Drifted, Captured, Dissolved, Rogue. Rogue institutions become AI actors.
- Institutions maintain the domains matching their type while the inventor is away.

## 8. Progression
- Stages: 1 Stranger; 2 Local Figure (livelihood + patron); 3 Founder (first institution; Influence Mode opens); 4 Power Broker (institutions on 2+ paths); 5 World Shaper (3+ paths, one abroad).
- At most one stage lost per negative event.
- Starting stage on arrival: 1 by default; 2 with a Legend or weak surviving institution; 3 maximum with a strong, loyal surviving institution.

## 9. Gold and fortune
- Gold scales actions; institutional capacity limits how many.
- Institutions hold a **share of the regional economy**, never a balance that earns interest. Value = share × regional economy.
- Access on arrival by loyalty: high = regular draws; medium = negotiated; low = token; rogue = none.
- **Loss = Hazard × Exposure × (1 − Resilience).** Hazards may be random; exposure and resilience come from player choices.
- Larger shares raise confiscation and revolution risk; aggressive extraction adds stability debt; longer jumps increase exposure.

## 10. People
- Nudge success = Trust × receptivity.
- Trust is explained by remembered interactions drawn from the event log.
- **Visibility** rises with how anachronistic, public, and threatening an act is, and with displays of wealth. Threat chance ∝ Visibility × threat to power.
- **Legacy retained on death:** 30% base; +20% per trained student (max 2); +20% if written; +10% if held by an institution.
- **Divergence:** chance a later historical figure appears as documented = 1 − (regional divergence × sensitivity); otherwise altered or lost, possibly replaced by an echo figure.
- **Promises:** at most 2 active; breaking one affects relationships, descendants, institution loyalty, Legend, and the Chronicle.

## 11. Fuel, repairs, and jumps
- Each era requires one fuel component (discovery puzzle) and one mechanical repair (capability project).
- **Recipe** = Material + Process + Condition; valid options depend on world state; every generated recipe must pass an automatic solvability check.
- **Experiment feedback:** physical observations over a core that reveals **how many** parts are correct, never which. A specialist gets at most one extra inference per experiment in their domain.
- **Repair tiers:**

| Tier | Malfunction chance | Lethal share | Landing |
|---|---|---|---|
| Partial | 15% | 5% | Start of era |
| Standard | 5% | 1% | Start of era |
| Full | 0% | 0% | Partway in (Sphere Index ≥ 110) |
| Superior | 0% | 0% | Late in era or skip (Sphere Index ≥ 130) |

- The player is never forced to jump.
- **Echoes:** 3–5 specific elements (person, institution, idea, object, mistake) tagged per era before a jump and surfaced later. The Rome era always includes the hour-one seeded choice.
- **Arrival:** recognition → wrongness → personal echo → discovery. Chronicle and Era Report available under an optional Learn more.

## 12. Index
- Sub-score per domain = world value ÷ historical value at the same date × 100.
- Overall Index = geometric mean of sub-scores (rewards balance).
- Sphere Index (player's regions) and World Index (globe).

## 13. Knowledge
- Knowledge lives in regions and spreads by diffusion; the world advances on its own.
- Nodes: prerequisites (alternative paths allowed), Means, Carriers, effects, shadow effects, historical window, fuel or repair link.
- Reusable capability layers sit behind nodes.
- Emergent variants come from rule tables: base node × modifier (carrier, culture, distortion) → bounded variant with at least one trade-off.

## 14. Hard constraints (enforced in code and content checks)
- Player institutions can never use enslaved labor.
- No atrocity actions (massacre, ethnic cleansing) exist as player verbs.
- Religious founders are never depicted, nudged, or erased; the existence of major world religions is fixed.
- No project, advancement, or verb gives the player a weapon of mass destruction as a tool; such outcomes occur only as consequences.
- Exploitation always produces a net Index loss.
