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
- The player sets each domain per region to **Protect**, **Maintain**, or **Accept Risk**, but **only for a domain where they have a voice (a 25% stake, §7) in an institution that maintains it**; their institutions there pay the upkeep, split by priority and scaled by their sway (§7). A domain they have no voice in **follows Rome's real history** (the historical curve), at no cost to them; only their projects and the consequences of their choices move it off that curve. With a voice, priorities act **relative to history**: Protect +2 a year, Maintain ±0, Accept Risk −8 (tune), multiplied by the player's **sway** over the domain (§7). The inventor starts with no responsibilities: they survive on work, and work income is taxed (P0: 10%, tune).
- Investment happens through **projects** (templated per domain, re-dressed per era) that change domain levels. The player never sets a domain level directly.

## 6. Expectations, debt, and crises
- **Expectation** = max(historical benchmark for the era, the region's recent peak fading over time). (tune; split into social and physical values if P0 shows players avoiding improvement)
- **Debt accrual per year** = max(0, expectation − level) × rate (rate = 1, tune).
- **Compounding:** existing debt grows 5% per year. During a jump, compounding stops **30 years after departure**. After those 30 years, a domain that no surviving institution maintains **stops accruing new debt** and drifts toward its long-run target (see §8); maintained domains keep the accrual rule.
- **Tiers:** Stable → Strained → Fragile → Critical; each raises the yearly crisis chance. Severity scales with debt.
- **Plague (the Medicine crisis):** its odds, both warning advancement and recurrence, come from **Medicine debt only**. Governance and Economy debt tiers instead raise plague **severity**: severity × (1 + Governance tier bonus + Economy tier bonus), with a bonus of 0 / 0.1 / 0.2 / 0.35 for Stable / Strained / Fragile / Critical (tune).
- **Warning stages:** every major crisis passes through visible antecedent stages before breaking out.
- **Paying down debt** costs 1.5× the gold that prevention would have cost.
- A crisis releases (clears) debt, resets expectations to current levels, and damages levels, population, and wealth. In the medium term (about 10 years) a plague must always leave the Index lower than no plague would; if it doesn't, crisis damage is raised until the levels lost outweigh the debt cleared. The outbreak ends with: "The sickness burns itself out. Nothing is fixed; there are simply fewer people left, and the survivors expect less."
- **Plague immunity:** no new plague outbreak within **30 years** of the last one.
- **Plague severity labels** follow the share of the region's population that dies: **Contained** (under 5%), **Severe** (5–15%), **Catastrophic** (over 15%). The death toll is always shown.
- **Crisis outcomes branch:** crises can also create openings (migration, orphaned talent, new movements, experimentation, scattered scholars).
- **Boom and bust (the Economy crisis):** interventionist economic policy builds **malinvestment** (P0: 8 a year per interventionist stance). Once it reaches 20 the first of **three visible warnings** appears (prices outrun wages; moneychangers refuse new coin; credit dries up), advancing at most one a year with chance malinvestment × 0.01 (max 0.9). The bust liquidates it: Economy damage 0.3 × malinvestment, Governance 0.1 ×, a quarter of the inventor's gold lost, Economy debt cleared and expectations reset. (tune)
- **Spillover:** debt spreads along trade routes, borders, and shared institutions in proportion to connection strength.

## 7. Institutions
- Attributes: `type` (political, religious, commercial, scholarly, military), `reach`, `capacity` (directives per turn), `loyalty`, `leader` (a named notable with a successor), `identity`, `drift`.
- Directive effectiveness = capacity × loyalty, within reach, limited by type.
- **Identities:** charitable, commercial, bureaucratic, militant, ritualistic, isolationist, reformist, politically powerful. Drift moves identity toward the institution's own interests; charters, founding principles, and reverence for the founder slow it.
- **Decay per decade during absence:** bare 10%; chartered and endowed 3%; chartered, endowed, in a thriving region, keeping the Legend alive 1%.
- **Outcomes on arrival:** Thriving, Drifted, Captured, Dissolved, Rogue. Rogue institutions become AI actors.
- Institutions maintain the domains matching their type while the inventor is away: strength × rate × control factor × min(1, 2 × domain share) for each the player steers.
- **Stakes (decided 2026-09-27).** Each domain has established institutions (rivals) and room for one the player founds. The player **buys into** an established institution: the first purchase makes them a member with **1%**; each further 1% costs the domain's base price × (1 + stake% / 10), and the base differs by domain (P0: Medicine 1.2, Governance 1.8, Economy 1.5 gold; tune), 1 Attention per purchase. **10%** counts toward influence but gives no oversight; **25%** gives a **voice** (priorities in its domain, economic policy through a Governance institution, a plague response); **50%** gives **oversight and control** (oversee, mentor, charter, endow, audit, invest). Dividends and shortfalls are shared by stake.
- **Founding** an institution of one's own costs **about 65%** of buying 0 → 50% of an established one in the same domain (P0: 2 Attention). The player controls it from the start, but it starts weak (strength 15, tune), holds little of its domain, and **may fail** (15% a year while strength is below 25, tune). Gold invested in an institution the player controls builds its strength (0.1 per gold, tune).
- **Influence and rivals.** An institution's **domain share** = its strength ÷ the strength of all institutions in the domain. **Control factor** = 0 below 10%, else min(1, stake ÷ 50%). The player's **influence** over a domain = Σ control factor × share; **sway** = min(1, 2 × influence) scales how much of a priority or policy takes effect. Rivals push back **from a 20% stake, harder the more the player holds** (decided 2026-09-28): each rival in the domain has a chance a year of 0.75% × the player's stake in percent (15% at 20%, 37.5% at 50%, max 75%) to cost that institution 3 strength (tune).
- **Leader integrity:** every institution leader has an integrity trait: honest, average, or venal.
- **Institution gold:** institutions hold gold given to them as endowments (see §9). An institution whose holdings reach the minimum endowment counts as endowed.
- **Debt payment during absence:** only in the **30 years after departure**, institutions pay down debt in the domains matching their type, at the 1.5× paydown premium, out of their holdings. Loyal institutions pay in full, drifted ones partially (50%, tune), rogue ones not at all. Payments reduce holdings.
- **Corruption:** checked each decade during the 30-year window only, separately from loyalty (loyalty decides whether an institution pays; corruption decides whether the gold survives).
  - Chance per decade = base hazard (0.05, tune) × exposure (×1 small holdings, ×2 large) × (1 − audit charter 0.5) × (1 − integrity: honest 0.3, average 0, venal −0.3).
  - Levels: **Minor** (lose 25% of holdings; debt payments continue at a reduced rate; small Governance debt); **Major** (lose 50%; payments stop; moderate Governance debt); **Total** (lose 100%; payments stop; large Governance debt; the institution becomes Captured or Rogue).
  - Level weights (Minor / Major / Total): unprotected 40 / 40 / 20; with an audit charter 70 / 25 / 5. A venal leader shifts weight toward Total, an honest leader toward Minor.
  - An **audit charter** (costs gold) can be founded as part of jump preparation. The pre-jump briefing shows corruption risk as Low / Medium / High and the potential severity, never the outcome; the arrival's Discovery beat reveals any corruption and its level.

## 8. Progression
- Stages: 1 Stranger; 2 Local Figure (livelihood + patron); 3 Founder (first institution; Influence Mode opens); 4 Power Broker (institutions on 2+ paths); 5 World Shaper (3+ paths, one abroad).
- At most one stage lost per negative event.
- **Long-run state during an absence** (after the 30-year window): each domain drifts **25% per decade** toward its long-run target = historical baseline + k × (departure level − baseline at departure), with **k = 0.3**. This applies to deviations both above and below the baseline, so the region you leave keeps part of its lead or its deficit for centuries. Surviving institutions that maintain a domain add their maintenance on top and can hold it above the target. During the first 30 years, domains move 50% per decade toward the baseline plus institutional maintenance. (tune)
- Starting stage on arrival: 1 by default; 2 with a Legend or weak surviving institution; 3 maximum with a strong, loyal surviving institution.

## 9. Gold and fortune
- Gold scales actions; institutional capacity limits how many.
- **Economic policy** (set through a political institution the player has a voice in, P0: the Caecilian or Junian faction or their own club; 1 Attention per change; every effect scales with sway over Governance, §7). Four issues, each with an **Austrian stance**, Rome's **historical practice**, or an **interventionist stance**: coinage (sound / debase), prices (free / controlled), property (secure / discretionary), taxes (light / heavy). Austrian stances are better for the economy: each grows the Economy +1 a year against history and keeps shaping its target during an absence while the institution survives. They provoke **backlash** from those who profit from intervention: when adopted, −8 loyalty in the institution carrying it and +8 Governance debt; free prices while the plague rages add +5 Governance debt a year. Interventionist stances give a quick boom (+1.5 a year each) but build malinvestment that ends in a bust (§6). Taxes set the tax on work: light 5%, history 10%, heavy 20% (heavy also adds Governance +1 a year). Commercial institutions (P0: the Merchants' Guild of Ostia, the banking house of Octavius, or the player's own trading house) maintain the Economy. (tune)
- **No passive income without leverage.** The inventor earns gold by working (better-paid work costs more Attention) and from property they funded and own, which grows with the Economy. Each institution earns its own rate × the Economy level × strength, so margins are thin until it is established. It pays its own running cost (different per institution; none once endowed) **and its part of the upkeep of the player's priority for its domain**. The inventor receives their stake's share of its surplus and covers their stake's share of any shortfall. The inventor never pays domain upkeep directly. (tune)
- Institutions hold a **share of the regional economy**. Value = share × regional economy.
- **During the 30-year window after departure**, holdings are a balance growing at the regional economic growth rate (0–1.5% per year by the Economy level; equivalent to a fixed share of a growing economy), never a fixed interest rate. **After the window, holdings freeze in P0.** The full share model arrives at P3.
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
