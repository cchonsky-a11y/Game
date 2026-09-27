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
Rome, AD 155. One region. The player makes 8–12 meaningful decisions over roughly 20 in-game years, faces the plague, repairs the time machine, then jumps 25–60 years forward and receives a four-beat arrival, walks around the Rome they find, and may jump once more to see how it aged.

## In scope

| Area | P0 version |
|---|---|
| Determinism | Seeded random generator; same seed + inputs = same result |
| Event log | Immediate causes, actors, effects (SYSTEMS.md §1); a `why <thing>` console command for current conditions only |
| Time | Turns in months or years; per-year simulation; one coarse-mode jump in decade steps |
| P0 pacing (**P0-only**, not in SYSTEMS.md) | **2-month turns, 120 per era** (AD 155–175; each era is 20 years; 2-month turns decided 2026-09-28, were 3). The player can change the turn length at any time with `turns <1|2|3>` (SYSTEMS §2; Attention stays 4 a turn and multi-turn work keeps its turn counts; the era ends by the calendar). **End turn** advances exactly one turn; **Wait** advances until something needs the player (an open prompt, news that turn, an affordable new investment, or the end of the era). Only turns where no action is possible (no free Attention and no open prompt) pass on their own; in the console, a choice that uses the last Attention ends the turn by itself unless debt can still be paid down (decided 2026-09-28). At game start the console says: "This prototype covers one era, a jump and your arrival, and then, if you like, one more jump to see how it all aged. The test ends there." Attention costs are tuned so total demand is at least 1.4× supply (120 × 4 = 480; was 1.5×, loosened 2026-09-28 with 2-month turns). Demand counts every project once, every institution step (found, charter, audit, endow), a full mentoring commitment per institution, the oversight needed to hold loyalty, active membership (two meetings a year) in one institution per domain, one plague response, and one personal action per turn |
| Care domains | **3 only:** Medicine, Governance, Economy |
| Priorities | Protect / Maintain / Accept Risk, available only for domains where the player has a voice (25% stake) in an institution that maintains it. Other domains follow Rome's real history until the player can influence them; priorities act relative to that history, scaled by the player's sway (SYSTEMS §7) |
| Projects | Public projects (magistrates, census, night watch, market, mint audit, road) need 10% of an institution in their domain; quarantine rules need 10% of a Medicine or Governance institution, or only membership once the first plague warning has come (decided 2026-09-28). 3–4 templated projects per domain (Economy 5, with the mint audit; one more each added 2026-09-28; gold ×1.5 except the hour-one choice; an institution you have a voice in pays a quarter of a project in its domain) (e.g., repair the fountain, fund a physician, free prices at the market, patronage for a senator) |
| Economic policy | Set through a Governance institution the player has a voice in, scaled by sway: coinage, prices, property, taxes, each Austrian / as history / interventionist (SYSTEMS §9). Austrian stances are better but provoke backlash; interventionist stances build malinvestment that ends in a **boom-bust crisis** with three visible warnings (SYSTEMS §6). Coinage sets inflation: prices rise each year under Rome's own slow debasement, faster with debasement, not at all with sound coin (SYSTEMS §9). Near half of Rome's economic decline, and some of Governance's, is the coin's: sound coin defended after departure spares it, debasement hastens it, and the arrival shows the coin (decided 2026-09-28) |
| Expectations and debt | SYSTEMS.md §6: expectation rule, 5% compounding, tiers, 1.5× paydown |
| Crisis | The plague, with **3 visible warning stages** and branching outcomes (directing a response needs 10% of a Medicine or Governance institution; otherwise Rome responds as history did), on its historical dates (warnings AD 165–166, outbreak late 166; the player changes how hard it hits, not when; decided 2026-09-28); the economic bust (from intervention), also with 3 visible warnings |
| Institutions | **9, three per domain:** two established rivals the player buys into (Medicine: the Physicians' Circle, the Tiber Island sanctuary; Governance: the Caecilian and Junian factions; Economy: the Merchants' Guild of Ostia, the banking house of Octavius) and one the player can found (the School of the Fountain, the Club of the Aventine, the trading house of Menodora). Influence grows with time and gold: seniority adds 1% a year of paid-up membership (up to 25%), and newcomers pay a premium on stake (×3, fading to ×1 over 5 years). Each established one has a requirement for joining, an entry fee, and annual dues that grow with the player's stake (SYSTEMS §7). Stakes from 1%: 10% influence, 25% voice, 50% control; founding costs ~65% of buying control and may fail; rivals push back as an institution takes more of its domain: the player's own from 20% of the domain, an established one past its starting share (SYSTEMS §7). Each with a named leader, loyalty, decay per SYSTEMS.md §7, and **2 pre-authored drift paths** (no general identity engine) |
| Money (P0) | Two currencies (decided 2026-09-28): everyday **denarii** (1 aureus = 25 denarii in AD 155) and **gold aurei**, whose price in denarii rises with the price level (history unless coinage policy changes it). Exchange at the money changers is an action (1 Attention, a fee each way). You arrive with a purse of scavenged aurei; the machine takes aurei back. Across the jump, the machine carries a small purse; more gold can be deposited with the banking house (interest, risk of failure or embezzlement) or buried (risk of discovery); the rest is lost; the arrival reveals the outcome |
| Gold | No passive income: work (odd / craft / consult: more gold for more Attention), owned property (workshop, warehouses) scaled by Economy, the player's share of institution surpluses, and memberships (each established institution the player belongs to raises work pay 10%). Work income is taxed (10%). Institutions pay their part of the player's priority upkeep plus their own running cost; the player never pays domain upkeep and covers their stake's share of institution shortfalls |
| Institution gold | Institutions hold gold given as endowments. **Only in the 30 years after departure:** holdings grow at the region's economic growth rate (0–1.5%/yr by Economy level; no fixed-rate compounding); institutions pay down debt in their own domain at the 1.5× premium (loyal: in full; drifted: partially; rogue: nothing). Actions: **endow** an institution with any amount of gold (the minimum endowment makes it endowed) and found an **audit charter** (costs gold). Jump preparation offers paying down debt directly, endowing an institution, and founding an audit charter |
| Institutional corruption | Checked each decade in the 30-year window. Chance = base hazard × exposure (small ×1, large ×2) × (1 − audit charter 0.5) × (1 − leader integrity: honest 0.3, average 0, venal −0.3). Minor / Major / Total (lose 25% / 50% / 100% of holdings; payments reduced / stop / stop; small / moderate / large Governance debt; Total makes the institution Captured or Rogue). Weights: unprotected 40/40/20, audited 70/25/5, shifted by integrity. Separate from loyalty. Each leader has a pre-authored integrity trait. The pre-jump briefing shows corruption risk (Low / Medium / High) and potential severity, never the outcome; the arrival's Discovery beat reveals any corruption and its level |
| Attention | 4 per turn; spent on projects, buying into or directing institutions, attending their meetings, overseeing an institution, economic policy, or one personal action. Also test **multi-turn commitments** as an option |
| Seeded choice | The hour-one fountain-or-workshop choice |
| Promise | One promise from an institution leader that conflicts with jump timing |
| Inventions (P0) | An **invention tree** of **12 inventions** in four branches of three tiers (decided 2026-09-28): mechanics (wheelbarrow → padded horse collar → water-driven trip hammer), accounts (double-entry bookkeeping → bills of exchange → a public accounts audit), hygiene (soap and boiled linen → distilled wine for wounds → a fever ward with case records), workshop (treadle lathe → water-powered bellows → blast furnace; each raises the workshop's income modestly, +10% / +15% / +20%). Every invention names the institution(s) it wins standing in, with stake and loyalty that vary by invention, covering all six established institutions; the list shows each payoff. Each after the first in its branch needs the one before it; the whole tree is visible, locked ones marked. Not the Knowledge Web (no diffusion or absorption). Each needs something from Rome (the workshop, a membership, a finished project) and pays off in income, standing (leader loyalty) and influence (stake). Made once each. No visibility or anachronism risk (decided 2026-09-28; a first version, to be fleshed out later) |
| Time machine (P0) | A full **assessment** of the machine comes first and reveals what is wrong (decided 2026-09-28). Then a repair track of **9 small steps** in three systems (coil housing, coolant, chronometer); each step costs a little gold and Attention and needs something from Rome (a membership or a finished project), or more gold instead. **All 9 are required to jump** (decided 2026-09-28; to be built out later), and all the gold the inventor scavenged from the machine at the start must go back in (decided 2026-09-28). Plus **3 optional upgrades** (gild the coil contacts, grind a sighting lens, balance the flywheel) that lengthen the jump. No fuel puzzle, repair tiers or malfunctions |
| Jump | Player chooses when to leave once the machine is repaired. **Two jumps (decided 2026-09-28, option A):** after the first arrival the player may jump once more at once, with no second era to play; the machine stays repaired and the range starts over from the repairs and upgrades. The distance is drawn at departure (seeded, 5-year steps) within a range set by the repairs (25–40 years), time in the era (+5 per 5 years beyond the first 5, up to +10) and upgrades (+5 each), capped at 60; the briefing shows the range, not the result (decided 2026-09-28). Simulated in decade steps, with a final half-decade step when needed |
| Echoes | **3 specific elements** (e.g., the fountain, the physicians' circle, the broken or kept promise). The seeded choice is always one of them |
| Arrival | Text version of the four beats: recognition, wrongness, personal echo, discovery. **Walk around Rome** at every arrival, including the first (decided 2026-09-28): `visit market | changers | forges | curia | subura` shows what is there now and, after a jump, what changed since you left, in words and numbers (prices and wages, the aureus and the coin's silver, the workshop and your inventions in use, the Curia, the fountain and the population); present conditions only. Optional `learn more` shows the Index and institution outcomes. **No causal chains after the jump** |
| Index | Geometric mean over the 3 domains, before and after the jump |
| News (P0) | A free `news` command (decided 2026-09-28): the talk of the Forum (history's dated news of Rome, AD 155–175: the emperors, Galen, the Parthian and Danube wars, Marcus's auction; hand-written, text only, changes nothing), local talk on your street (invented, one item every 4 months plus the festivals in their month; talk about the player's own world first: the hour-one choice, their institutions and leaders, the plague and prices reaching the street; each heard once), what befell Rome in the past year (world events from the log, never the player's own), and the market (the aureus, prices, the coin's silver). A new item shows as a headline on the turn that covers its date. Historical figures appear only in the news, never as people to talk to. History's news stops after a jump |
| Text | Hand-written templates only |

## Out of scope (do not build)
- Unity, graphics, UI beyond the console, audio
- Sim Mode scenes, historical figure dialogue, nudge verbs
- Fuel puzzles, repair tiers, malfunctions (the small P0 repair track above is in scope)
- The Knowledge Web, absorption bars, emergent advancements, diffusion
- Multiple regions, spillover, AI civilizations, diplomacy, warfare
- Agriculture, Knowledge, Military, Infrastructure, Faith and Culture domains
- Life budget, aging, mortality, succession, the Journal
- Fortune shares (the regional-economy share model of SYSTEMS §9), loss events other than the plague, the economic bust and institutional corruption
- Visibility, anachronism risk, Legend
- Stages of control (use a fixed Stage 3 setup)
- A general institution identity engine
- Post-jump causal chains, the Chronicle, signature systems, the tutorial engine
- Saves, iCloud, purchases, analytics
- Any language-model integration

## Order of work
1. Build the simulation and console game.
2. **Run the batch runner before any human test:** 100 seeded runs × 7 strategies (Balanced/Pay-down, Specialized, Neglectful, Endow, Split, FreeMarket, Interventionist), each with early and late jump timing. Fix any dominant strategy first.
3. Run 5 human testers.

## Pass criteria

| Test | Pass |
|---|---|
| Determinism | Identical results for identical seed and inputs |
| Balance vs. specialization | Both viable; within **each** jump timing (early, late), no strategy wins more than 65% of automated runs |
| Jump timing | **Not applicable to P0; deferred to P3.** Staying longer has no cost until aging and machine-discovery risk exist. Reported in the batch report for information only |
| Debt at departure | Within each timing, none of Pay-down (Balanced), Endow, and Split wins more than 65% of runs; the batch report flags it if more than ~80% of the best runs bought an audit charter |
| Debt pacing | 2–3 years of accepted risk → Strained; plague warning stages visible before the outbreak |
| Institution decay | Formula check (the P0 jump is now 25–60 years): after 250 years from strength 80: bare 80 × 0.9^25 ≈ 6; chartered 80 × 0.97^25 ≈ 37; strong 80 × 0.99^25 ≈ 62 |
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
