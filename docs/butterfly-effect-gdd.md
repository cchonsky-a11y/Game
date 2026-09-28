# The Butterfly Effect — Game Design Document

**Status:** Design complete, pre-prototype · **Version:** 1.3 (P0 review: debt compounding cap, Medicine-only plague odds, institution gold and corruption, P0 pacing) · **Date:** September 26, 2026 · **Purpose:** Personal design exploration

---

## 0. Executive Summary

An iOS strategy game for iPhone, iPad, and Mac. A 21st-century inventor's time machine malfunctions and strands them in Ancient Rome in AD 155. To get home, they must drag civilization forward far enough to repair the machine and produce its missing fuel, one era at a time, across 8 eras. Every change ripples forward. The core tension is allocation: finite attention and gold spread across the needs of a civilization, where needs the player chose to put at risk compound and surface later as plague, famine, revolution, or collapse.

**The core emotional promise:** the player leaves an era, returns centuries later, and finds a world their actions changed. The game shows the change; it doesn't have to prove the link.

| Element | Decision |
|---|---|
| Genre | Turn-based historical strategy, civilization scale plus individual scale |
| Target player | Civilization players who want more depth on mobile; secondary appeal to history and story fans |
| Structure | One continuous campaign, 8 playable eras, Rome (AD 155) to the present day |
| Modes | Sim Mode (the inventor, directly controlled; everyone else nudged) and Influence Mode (civilization-scale direction, exercised only through institutions) |
| Core mechanic | Allocation, neglect debt, and delayed consequences |
| Session length | 30–60 minutes; campaign about 22 hours |
| Monetization | Free through the first jump; one-time campaign unlock; optional expansions, cosmetics, and small capped perks |
| Engine | Unity 6 with an engine-independent C# simulation core |
| Platforms | iPhone (13 and later), iPad, Mac; iCloud cross-save |

---

## 1. Vision, Target Player, and Pillars

**Target player.** Civilization players who want real depth that is clear and approachable, with complexity layered in over time. They reject aggressive free-to-play mechanics, dense spreadsheet interfaces, and shallow progression.

**Platform.** iPhone is the performance baseline; iPad and Mac are higher-fidelity targets. Cross-save through iCloud.

**Tone.** Similar to the Civilization series: serious and historically grounded, but accessible, with room for lighter moments.

### Design pillars (ranked)

| Rank | Pillar | Player experience | Test for any feature |
|---|---|---|---|
| 1 | **Every Choice Echoes** | "The world I came back to is different because of what I did." | Does it visibly change the world the player later returns to? |
| 2 | **Knowing Is Not Making** | "I know exactly what I need, and I have to figure out how to get there with what this era has." | Does it make the player bridge future knowledge and past means? |
| 3 | **One Person in a Large World** | "I'm vulnerable and limited, and the world has its own will." | Does it keep the inventor personally present, limited, and at risk, and the world autonomous? |

**Guiding rule:** the player has the fun, not the simulation. Every hidden system must surface as a decision or a visible result.

**Conflict resolution:** Pillar 1 beats Pillar 2, and Pillar 2 beats Pillar 3.

**Presentation rule:** simulation underneath, human dilemma on top.

### Design constitution
1. Player actions must visibly change the world.
2. The inventor never directly controls civilizations.
3. Personal Attention never scales with power.
4. Knowledge never bypasses Means and Carriers.
5. Neglect must arise from understandable trade-offs.
6. Simulation complexity must surface as decisions, not bookkeeping.
7. Every jump must materially transform the world.
8. Failure should create history, not merely subtract progress.
9. Major institutions are represented through people.
10. No feature exists merely because other historical strategy games have it.

---

## 2. Premise, Narrative, and the Inventor

### Premise
The player is the inventor of a time machine. They launched the prototype early, with an incomplete fuel formula, after dismissing a warning. The machine malfunctioned, threw them back far further than thought possible, and was damaged. To get home, the inventor must advance civilization enough to understand what happened, repair the machine, and produce the missing fuel components.

### Timeline model
Anchored and simple. The inventor and the machine are fixed points; everything else can change. "Home" is the inventor's departure year. There are no paradox mechanics: the inventor's own existence is never at risk.

### The inventor
- **Backstory:** a brilliant, somewhat isolated physicist-engineer, privately funded and working alone, which explains why no rescue ever comes.
- **Customization:** name, appearance, and one starting specialty.

| Specialty | Advantage | Key system |
|---|---|---|
| Chemist | Experiments cost less and fail more informatively; precise blueprints for chemistry and materials | Fuel puzzles |
| Engineer | Repairs and tool-making advance faster; better jump distance; precise engineering blueprints | Machine and infrastructure |
| Physician | Earns trust quickly, resists disease, survives longer early on | Survival and influence (never a higher lifespan cap) |

### Narrative thread
- **The malfunction mystery:** the machine's damaged internal log can be read one layer per era as instruments improve (lenses, chemistry, electricity, computing). **Final revelation:** the fuel formula was flawed, and the inventor was warned and launched anyway. The 8 components are the complete formula.
- **Motive tension:** at least one moment per era forces a choice between rushing the next jump and protecting what the player built (a component ready just as a crisis hits; a protégé asking you to stay; the machine discovered).
- **Endings:** see Section 14.

### Literary precedents
*Lest Darkness Fall* (L. Sprague de Camp) shows the opening loop (a stranded modern man survives through knowledge) and the gap between knowing and making (cannons that cause more trouble than they're worth; not enough parchment for a newspaper). Its main weakness, changes without side effects or surprising chains, is exactly what this game's Pillar 1 fixes. Also: *A Connecticut Yankee in King Arthur's Court* (Mark Twain).

---

## 3. Anachronistic Knowledge

The inventor knows what is needed and how things work, but not how to produce them with the era's materials, tools, and people.

**Four ways to introduce an idea:** teach an individual, demonstrate an invention, write a text, or found an institution. They differ in speed, accuracy, and how much attention they draw.

**Absorption capacity** (three bars shown for every idea):

| Bar | Question | Example: printing in Rome |
|---|---|---|
| Knowledge | Do the prerequisite ideas exist? | Presses exist (olive and wine presses) |
| Means | Are materials and tools available? | Paper is scarce |
| Carriers | Is there a group or institution to use and spread it? | Literacy is limited |

- Adoption **speed** is set by the weakest bar; **accuracy** by the average of all three.
- Low accuracy produces **distorted adoption**: the idea takes hold in altered form, with partial benefits and side effects (e.g., germ theory without microscopes becomes ritual washing in temples).
- Raising a weak bar is usually its own project.
- **Absorption applies to every kind of idea**, not only technology: political institutions, administration, finance, and social reforms all need Knowledge, Means, and Carriers. The defining question is always: can this society actually receive and sustain this idea?

---

## 4. Life Budget, Mortality, and Succession

### Life budget
Aging happens only within eras, never during jumps. The inventor starts at age 28.

**Healthy years** is the expected maximum age for a healthy adult with access to the best care available in that civilization. It is not historical life expectancy, which was far lower mostly because of infant mortality. Health declines and mortality risk rise as the inventor nears the ceiling, but the ceiling itself stays predictable so the life budget can be planned.

| Medicine level of the civilization | Healthy years (expected maximum age) |
|---|---|
| Historical baseline | ~70 |
| Well advanced | ~85 |
| Heavily advanced | ~95 |

| Performance | Years needed per era |
|---|---|
| Standard | ~10 |
| Good (lands partway into the next era) | ~7 |
| Excellent (lands late, or skips an era) | ~5, or 0 if skipped |

| Player | Healthy years | Years in eras | Ending age | Result |
|---|---|---|---|---|
| Excellent | 95 | 7 eras × 6 = 42 | 28 + 42 = 70 | Home, with 25 spare years |
| Good | 85 | 8 × 7 = 56 | 28 + 56 = 84 | Home, with 1 year of margin |
| Standard | 85 | 8 × 10 = 80 | 108 | Successor needed around era 7 |
| Neglectful | 70 | 8 × 10 = 80 | Dies at 70, during era 5 | Successor needed, or the run ends |

Neglecting medicine literally shortens the inventor's life. Making it home as the original inventor is the hardest achievement in the game.

### Mortality
- The inventor faces the same **anachronism risk** as the people they influence: persecution, assassination, heresy charges, exile. They can also die at the front, or in transit from a jump malfunction.
- **Hybrid succession:** a successor (apprentice, child, or sworn order) inherits the machine and the mission only if deliberately prepared. The **Journal** carries the inventor's knowledge; how much survives depends on how much was written. The Journal can be discovered or stolen.
- **Early hint:** in the first era, a patron asks who will continue the inventor's work. The system activates later, but the question is planted early so succession never feels like a late add-on.
- **No successor:** in Standard mode, restart from the latest era checkpoint (start or midpoint); in Ironman, the run ends.
- **Legend:** visibility builds a Legend that persists across jumps. It can help (a sect preserving your teachings) or hurt (an inquisition hunting "the Returning One").

---

## 5. Time Machine, Eras, Fuel, Repairs, and Jumps

### The 8 eras

| # | Era | Landing window | Fuel component | Mechanical repair | Signature system |
|---|---|---|---|---|---|
| 1 | Rome | AD 155 start | Refined conductor (metallurgy) | Structural frame and housing (Roman smithing) | Patronage |
| 2 | Late Antiquity and the Islamic Golden Age | c. 500–900 | Concentrated solvent (distillation) | Containment seals (high-temperature ceramics) | Translation and preservation |
| 3 | High Middle Ages | c. 1100–1300 | Optical-grade crystal (glassmaking) | Gearing and linkages (millwrights) | Guilds and pilgrim routes |
| 4 | Renaissance | c. 1450–1550 | Precision regulator (clockwork) | Optical alignment (lens grinding) | Printing and propaganda |
| 5 | Scientific Revolution | c. 1650–1750 | Pure reactive compound (chemistry) | Vacuum chamber and pump | Scientific societies and contact |
| 6 | Industrial | c. 1800–1880 | High-pressure vessel (industrial steel) | Machined precision parts | Factories and labor |
| 7 | Electrical | c. 1890–1945 | Stable power source (electricity) | Coils and insulated wiring | Mass media and ideologies |
| 8 | Information Age | c. 1950–2000 | Control circuit (semiconductors) | Diagnostic and navigation computer; decodes the final log layer | Networks |

The 8th jump brings the inventor home. If the player has pushed history ahead, later components become easier. Clue geography deliberately pulls the player beyond Rome.

### Two tracks per era
- **Fuel: a discovery puzzle.** The player always knows *which* component is needed, but not *how* to make it with the era's means.
- **Repair: a capability project.** The inventor knows the exact blueprint; the challenge is finding craftspeople, tools, and materials. Building repairs openly leaves lasting benefits (a gear workshop later serves mills and clocks); secret workarounds carry more anachronism risk.
- Repairs are permanent. After an era skip, both the skipped era's fuel and repair must be completed in the landing era.

### Fuel puzzle rules
- **Recipe structure:** Material + Process + Condition, each drawn from a plausible pool.
- **Randomization tied to world state:** which option works depends on the player's altered world (trade, workshops, techniques), so recipes can't be computed from a seed and posted online.
- **Clues** come from people in Sim Mode, texts, trade goods, and the malfunction log. The **Journal** records clues and hypotheses.
- **Experiments** cost resources, turn time (so life budget), and sometimes risk. Results appear as **physical observations** (a vessel cracks, a residue forms, the mixture holds under heat, conductivity changes) over a **deducible core**: underneath, a result tells the player *how many* parts are correct, not which (see Section 18: per-part feedback makes brute force trivial). Occasional telling byproducts act as bonus clues.
- **Specialty interpretation:** a specialist gets at most **one extra inference** per experiment within their own domain (e.g., a Chemist may conclude the solvent is right). The cap keeps the Chemist from dominating a fuel-heavy game.
- **Fallbacks** guarantee a path: buy the material through trade (high cost), reverse-engineer an AI civilization's discovery, or commission a specialist.

### Repair tiers and jump outcomes

| Tier | Requirement | Malfunction chance | Lethal share | Landing |
|---|---|---|---|---|
| Partial | Fuel plus a makeshift repair | 15% | 5% | Start of era |
| Standard | Fuel plus the era's required repair | 5% | 1% | Start of era |
| Full | Plus calibration with era tools | 0% | 0% | Partway in (Sphere Index ≥ 110) |
| Superior | Plus optional upgrades | 0% | 0% | Late in the era, or skip it (Sphere Index ≥ 130) |

Non-lethal malfunctions: wrong region, earlier landing, machine damage, or injury that costs life budget.

### Jump sequence
Prepare → Decide → Jump → Off-screen simulation → Arrival → Re-establish.

- **Player-controlled timing:** the player is never forced to jump. Staying builds stronger institutions and reduces off-screen debt, at the cost of aging, rising anachronism risk, and a greater chance the machine is discovered, stolen, or damaged. Jumping early speeds progress home while problems compound in your absence.
- **Machine security:** each year carries a discovery risk, reduced by hiding the machine inside an institution you control.
- **Promises:** people can ask the inventor to stay until something is done (e.g., a protégé asks you not to leave before the hospital opens). If the machine is ready first, the player chooses between staying and aging, or breaking the promise. Consequences reach relationships, descendants, institution loyalty, Legend, and the Chronicle. Limit: 2 active promises at a time.
- **Echoes:** before each jump, the game identifies 3–5 emotionally important elements of the era (a person, an institution, an idea, an object, a mistake). Later eras deliberately surface what became of them. **The Rome era always includes the hour-one seeded choice (fountain or workshop) as an Echo**, so the free slice ends with a visible consequence of the player's first real decision.
- **Arrival sequence (four beats), emotion before statistics:**
  1. **Recognition:** the time-lapse ends on something familiar.
  2. **Wrongness:** architecture, borders, language, clothing, or symbols reveal that history changed.
  3. **Personal Echo:** someone recognizes the inventor's symbol, institution, Journal, family line, or legend.
  4. **Discovery:** the player learns what survived, drifted, or collapsed (institutions, fortune, Echoes).

  **Optional "Learn more":** the Chronicle and Era Report are available for curious players. The game does not require or present causal chains linking arrival outcomes to specific earlier decisions; the world simply shows what changed.

---

## 6. Progression from Individual to Civilization

Influence Mode is exercised only through **institutions** the inventor founds or controls. The inventor never commands a civilization directly; Rome itself remains an AI actor for the whole game.

### Stages of control

| Stage | Name | What the player can do | Unlock |
|---|---|---|---|
| 1 | Stranger | Personal actions only (Sim Mode) | Start |
| 2 | Local Figure | Nudge people in a district; run a household or workshop; first followers | A livelihood plus a patron |
| 3 | Founder | Influence Mode opens: map of institutional reach; directives to 1–2 institutions | First institution founded |
| 4 | Power Broker | Civilization-level levers through several institutions and positions | Institutions on 2+ paths, plus Standing |
| 5 | World Shaper | Institutional branches in other civilizations | Institutions on 3+ paths, one abroad |

**Losing influence:** a patron dies, a scandal breaks, a regime changes, or a rival captures an institution. At most one stage is lost per event; institutions survive unless destroyed separately.

**Arrival after a jump:** nothing survived → Stage 1; a Legend or weak institution → Stage 2; a strong, loyal institution → Stage 3 (maximum). Starting at Stage 3 saves about 2–4 years of the era.

### Three currencies

| Currency | Role | Source |
|---|---|---|
| Standing | Sets the stage | Actions and reputation across all paths |
| Gold | Sets **how much** an action can do | Livelihood, patrons, institutions |
| Institutional capacity | Sets **how many** directives per turn | Founding and growing institutions |

### Institutions

| Attribute | Meaning |
|---|---|
| Type | Its path |
| Reach | Geographic and social area it affects |
| Capacity | Directives per turn |
| Loyalty | How faithfully it carries out your intent (low loyalty distorts results) |
| Leader | A named notable with ambitions, fears, and a successor: the institution's face |
| Identity | What the institution has become (see ideological drift) |
| Drift | Goals slide toward its own interests and a new identity over time, especially during jumps |

Directive effectiveness = capacity × loyalty, within reach and limited by type. Very disloyal institutions can **go rogue** and become AI rivals.

**Ideological drift.** Institutions don't only lose loyalty; they evolve toward a recognizable identity: charitable, commercial, bureaucratic, militant, ritualistic, isolationist, reformist, or politically powerful. Example: a medical order founded to spread sanitation later preserves the inventor's texts so rigidly that experimentation becomes heresy. Identity changes which directives an institution accepts and how it behaves while you're away.

### The five paths

| Path | Early → mature form | Key levers | Link to fuel and repairs | Main risks |
|---|---|---|---|---|
| Political | Patron network → senate faction or bureaucracy | Laws, public works, taxes | Funding | Purges, assassination, civil war |
| Religious | Circle of followers → order or church | Norms, charity, hospitals, mass reach | Safe hiding places for the machine | Heresy, schism, fanaticism |
| Commercial | Workshop → guild or trading company | Trade, materials, investment | Fuel materials; repair workshops | Rivals, debt, monopoly backlash |
| Scholarly | Pupils → academy or university | Knowledge, the Carriers bar | Fuel clues; trained craftspeople | Persecution of ideas |
| Military | Bodyguards → officer corps or knightly order | Security, conquest, commanders | Protecting the machine | Coups, war costs, fear |

### Gold
- **Sources:** the inventor's own work (Stage 1+), patrons (Stage 2+), commercial institutions (strongest), taxes, tithes, and tribute (Stage 4+), scholarly grants (weak).
- **Uses:** experiments, repairs, craftspeople, founding and maintaining institutions, directives, domain investment, gifts to historical figures, hiding the machine, training a successor.
- **Limits:** absorption caps how much a domain can use per turn; Standing can be bought only up to a small cap (beyond it, scandal risk); conspicuous wealth attracts theft and suspicion; every path provides something gold can't buy; **gold is never sold for real money.**
- **Across jumps:** a small reserve carried in the machine; holdings kept in trust by institutions; buried caches that may not survive.

### Fortune across jumps
Institutions hold a **share of their region's economy**, not a growing balance (a fixed 2% interest over 400 years would turn 100 gold into about 275,000).

- Value on arrival = share × the regional economy at arrival. Thriving regions produce fortunes; neglected regions take your fortune down with them.
- The share changes with institution strength, drift, shocks, and the region's growth.
- **Access depends on loyalty:** high = regular draws; medium = negotiation; low = token gifts; rogue = nothing, possibly hostility.

### Loss events: hazards are random, exposure is chosen

**Loss = Hazard × Exposure × (1 − Resilience)**

| Event | Player's role | Mitigation |
|---|---|---|
| Revolution | Neglected stability; heavy extraction; suppression | Governance, education, charity, fair laws |
| Confiscation | Institution too large or visible; weak allies; ruler in debt to it | Allies, legitimacy, spreading holdings |
| Institutional collapse | Low loyalty; overexpansion; no succession rules | Charters, succession rules, audits |
| Natural disaster | Neglected infrastructure or agriculture; exposed concentration | Defenses, reserves, geographic spread |
| Epidemic | Neglected medicine, or your own trade networks spread it | Medicine, sanitation, quarantine |
| War | Neglected military; provocation; expansion into contested regions | Defense, diplomacy |
| Financial crash | Banking or joint-stock companies introduced before society could absorb them | Wait for absorption; regulatory institutions |

**Anti-cheat rules:** (1) wealth attracts risk: larger economic shares face higher confiscation and revolution risk; (2) aggressive extraction creates stability debt; (3) longer jumps mean more exposure to shocks.

**Diversification:** concentrated holdings grow faster but carry more risk; diversified holdings are safer but need more institutions to manage.

**Legibility:** a fortune briefing before each jump shows exposure and resilience per holding (never outcomes); "Your fortune" on arrival shows what each institution now holds.

---

## 7. Core Loop and Time Model

### Three loops
- **Turn (1–5 minutes):** review alerts, debt, clues, and life budget → spend Attention → issue directives → allocate gold → end turn → resolution with causes.
- **Era (about 2.5–3 hours):** arrive → re-establish → build institutions → pursue fuel and repair → manage neglect and crises → prepare the jump → decide to stay or jump.
- **Campaign (about 22 hours):** 8 eras → home, carried by the mystery, the life budget, and the Chronicle.

### Attention
- **4 per turn**, −1 when old or unwell, +1 with an apprentice or secretary. **Never grows with stage** (Pillar 3). Attention represents significant personal commitments, not routine clicks; institutions handle routine operation. (Multi-turn commitments, such as mentoring someone for a season, will be tested in P0 before adoption.)
- Spent on: Sim Mode scenes, experiments, supervising repairs, relationships, overseeing an institution in person (loyalty boost), travel, rest.
- **Emergency valve:** borrow Attention from the next turn during a crisis, at a health cost.
- Because turns lengthen, personal Attention per year falls from 48 (Stage 1) to 8 (Stage 4). **From Stage 3 up, scholarly and commercial institutions can run experiments and repair work through directives**, slower and less accurately than the inventor personally.

### Turn length

| Stage | Turn length | Years (standard era) | Turns |
|---|---|---|---|
| 1 | 1 month | 1.5 | 18 |
| 2 | 2 months | 2.5 | 15 |
| 3 | 3 months | 3 | 12 |
| 4 | 6 months | 3 | 6 |
| 5 | 1 year | Later eras | — |
| **Total** | | **10** | **51** |

- **Everything simulated scales per year, not per turn** (aging, debt, drift, AI actions).
- **Temporal zoom:** turns can always be shortened (e.g., monthly during a plague), never lengthened beyond the stage cap.
- **Undo** is allowed within a turn, but **any action that reveals information** (experiment results, conversations, investigations) is final.

### Playtime estimate (assumed)
Turns: 18 × 1.5 + 15 × 2 + 12 × 3 + 6 × 5 = 123 minutes, plus 30–60 minutes of arrival, jump, and scenes ≈ **2.5–3 hours per era**, about **22 hours per campaign**. Each era needs its signature system (Section 11) so eras don't feel repetitive.

---

## 8. Allocation, Neglect, Consequences, and Continuity

### The 8 care domains

| Domain | Crisis when neglected |
|---|---|
| Agriculture | Famine |
| Medicine | Plague; also caps the inventor's lifespan |
| Knowledge | Stagnation; absorption falls |
| Governance | Unrest → revolution |
| Military | Invasion |
| Economy | Poverty, crashes |
| Infrastructure | Disasters do more damage |
| Faith and Culture | Schism, loss of cohesion |

Each region you influence has a level per domain; levels decay without upkeep. **The player never fills bars directly.**

- **Priorities:** for each region, the player sets each domain to **Protect**, **Maintain**, or **Accept Risk**. Neglect is a chosen, understood vulnerability, not a forgotten meter. Upkeep gold is split automatically by priority.
- **Projects:** investment happens through concrete, historically grounded projects (repair the sewers, fund Galen, train physicians, set quarantine rules, found a hospital) that change domain levels underneath. Projects are **templates**: a small set per domain, re-dressed for each era, so content stays manageable.

### Expectations and debt
- **Expectation = the higher of the historical benchmark for the era and the region's own recent peak (fading over time).** Decline after success hurts more than never investing. *(Tested in P0: if players avoid improving domains to keep expectations low, split this into separate social-expectation and physical-capacity values.)*
- **Accrual per year** = (expectation − level) × rate; **existing debt compounds 5% per year.**
- **Tiers:** Stable → Strained → Fragile → Critical, each raising the yearly crisis chance. Severity scales with debt.
- **Warning stages:** major crises have visible antecedents (rising food prices → unrest → institutional defiance → riots). Randomness affects timing and severity, never whether the trajectory was visible.
- **Failure creates history:** crises open new paths as well as causing damage. A plague can bring labor scarcity, migration, orphaned talent, religious growth, medical experimentation, and wage changes; a destroyed academy scatters its scholars and their knowledge to other regions; a rogue institution can become a future rival.
- **Example (Medicine, Rome):** expectation 60, level 40 → 20 debt per year. With compounding: 20, then 20 × 1.05 + 20 = 41, then 41 × 1.05 + 20 ≈ 63 after 3 years → Fragile.
- **Paying down debt costs 1.5×** what prevention would have. A crisis also releases debt, violently.
- **Spillover** travels along trade routes (plague, economic shocks), borders (refugees, war), and shared institutions (schisms). Your own trade networks increase spillover.
- **Legibility:** every gauge shows the expectation line, a tier color, and a cause breakdown; risk forecasts show a window, never a date; crises show their immediate causes. (Explanations are for in-era decisions; after a jump, the world simply shows what changed.)

### Continuity across absence
During a jump the world is simulated in decade steps. Domains without an institution drift toward the historical baseline and accumulate debt.

| Institution type | Domains it maintains |
|---|---|
| Monastery or religious order | Medicine, Knowledge, Faith and Culture |
| Guild or trading house | Economy, Infrastructure |
| Academy | Knowledge |
| Officer corps | Military |
| Political faction | Governance |

| Institution quality | Decay per decade | Strength after 300 years (from 80) |
|---|---|---|
| Bare | 10% | 80 × 0.9^30 ≈ 3 → dissolved |
| Chartered and endowed | 3% | 80 × 0.97^30 ≈ 32 → weak |
| Chartered, endowed, thriving region, keeps your Legend alive | 1% | 80 × 0.99^30 ≈ 59 → strong |

- **Drift** is slowed by charters, founding principles, and reverence for the founder. Gratitude fades generation by generation unless an institution keeps the memory alive.
- **Outcomes on arrival:** Thriving, Drifted, Captured, Dissolved, or Rogue.
- **Difficulty curve:** early jumps (400+ years) are harsh; later jumps (under 100 years) are forgiving.

---

## 9. Feasibility Principles

**The simulation decides; the language model only describes.**

| Content | Method |
|---|---|
| Fuel recipes | Procedural only, with an automatic solvability check |
| Emergent advancements | Procedural rule tables; the model writes names and descriptions |
| Historical figure divergence | Procedural probability |
| Echo and talent figures | Procedural traits; the model writes biography and dialogue within limits |
| Authored historical figures | Hand-written; no model |
| In-era "Why?" explanations | Templates from the event log (immediate causes) |
| Chronicle entries, arrival summaries | The model summarizes the structured event log |

- **Apple's Foundation Models framework** provides a free, offline, on-device model with structured output. It is not a source of historical facts.
- **Deterministic simulation** (seed + inputs = same result); generated text is cached in the save; **offline first**; template fallbacks on devices without Apple Intelligence.
- **Scale (assumed):** about 100–300 regions × 8 domains per turn; a 400-year jump ≈ 40 steps × 300 × 8 ≈ 96,000 domain updates, hidden in the time-lapse; **populations simulated as aggregate groups**; about 200 active named notables.

---

## 10. AI Civilizations and Conflict

### AI civilizations
- About 10–12 major civilizations plus minor peoples at the start; **Rome is one of them.**
- **Historical agenda** (always visible, from the culture's character) and **emergent agenda** (generated from its current conditions, uncovered through investigation, always traceable, can change between eras).
- AI civilizations adopt, resist, copy, steal, or weaponize your ideas; produce their own advancements; and run their own care domains and debt, whose crises spill over to you.
- **Attitude** responds to your institutions, your Legend, and your home civilization, in graded steps (Wary → Cool → Hostile; Warm → Friendly → Allied), each with a visible breakdown of reasons.
- **Diplomacy runs through institutions:** political factions steer foreign policy; commercial institutions negotiate trade; religious institutions open missions. At Stage 4+, directives can push the home state toward treaties or war; the state decides, weighted by your influence.

### Conflict: abstracted, with key decisions
- **Strength** (size × quality) and **Advantage** (commander, terrain, doctrine, supply, morale), with **bounded randomness** and a **forecast** shown before battle.

| Moment | Options |
|---|---|
| Before battle | Engage, delay, withdraw |
| Turning point | Hold, flank, commit reserves |
| Aftermath | Pursue, spare, negotiate |

- **Stages 1–3:** wars happen to you. **Stage 4+:** influence war and peace; appoint commanders, including historical figures.
- **The inventor at the front:** a military camp scene to assess supply, raise morale, or introduce field medicine, with a risk of injury or death.
- **Anachronistic weapons** give a large advantage but spread to rivals, escalate future wars, and can be turned against regions you built.
- **Aftermath** feeds population, plague risk, rival commanders (coups), refugees, the number of regions to manage, and the Chronicle. Naval warfare uses the same model.

---

## 11. Advancement System: the Knowledge Web

### Structure
**18 advancement domains** under the 8 care domains:

| Care domain | Advancement domains |
|---|---|
| Agriculture | Agronomy; food preservation and husbandry |
| Medicine | Anatomy and surgery; public health and pharmacology |
| Knowledge | Language and writing; mathematics; natural philosophy and science; education |
| Governance | Law; administration and statecraft |
| Military | Doctrine; arms and fortification |
| Economy | Trade and finance; crafts and manufacturing; navigation and exploration |
| Infrastructure | Engineering and construction; materials and energy |
| Faith and Culture | Religion and ethics; arts and architecture |

**Size target (assumed):** 18 × about 4 per era × 8 eras ≈ 575 core nodes, plus about 290 refinements (only ones that change how you play) ≈ **865 nodes**, plus **30–50 frontier nodes** beyond 2026.

**Capability layers:** behind the visible web, reusable capabilities (e.g., Precision Manufacturing, High-Temperature Workshop, Formal Medical Training, Long-Distance Accounting, Large-Scale Printing) are shared by many nodes. The visible web stays large; capability layers reduce production cost and keep rules consistent.

**Each node has:** prerequisites (**alternative paths allowed**), Means, Carriers, effects, **shadow effects** (delayed side effects), a historical window (used to measure anachronism), and any link to fuel or repairs.

### Acquisition
1. The world's own momentum (regions and AI civilizations discover nodes on their own)
2. Player introduction (the four channels, subject to absorption)
3. Diffusion through trade, conquest, missions, and scholars (knowledge lives in regions, not a global pool)
4. Emergence (below)

### The inventor's view
The inventor sees the **entire future web**, up to 2026. **Specialty sets blueprint detail**: precise in the specialty's domains, partial elsewhere. Fuel and repair chains are highlighted. The web opens on the local problem and relevant path, never the entire future at once; filters cover domain, "ready now," and fuel and repair paths.

### Emergent advancements
- **Triggers:** distorted adoption; the first meeting of two tagged advancements in a region; a nudged talent or historical figure with the right traits; adaptation forced by a crisis.
- **Formula:** base node × modifier (carrier type, cultural context, degree of distortion) → rule tables → a new variant with bounded effects and at least one trade-off.
- **Example:** germ theory × religious carrier × weak Means → **Temple Purity Rites** (+Medicine, +Faith, spreads through temples; later secular medicine resisted until a reform).
- Emergent nodes can serve as prerequisites on alternative paths; each is recorded in the Chronicle. **Target: 2–4 per era.**

### Signature systems (one per era)

| Era | Signature system | What plays differently | Signature crisis |
|---|---|---|---|
| Rome | Patronage | Patron-client networks drive influence | Proscriptions |
| Late Antiquity / Islamic Golden Age | Translation and preservation | Knowledge can be lost; texts must be saved | Loss of libraries |
| High Middle Ages | Guilds and pilgrim routes | Ideas travel with pilgrims; guilds guard techniques | Guild monopolies |
| Renaissance | Printing and propaganda | Fast information; public opinion becomes a resource | Religious wars |
| Scientific Revolution | Scientific societies and contact | Better experiment methods; intercontinental contact | Colonial exploitation |
| Industrial | Factories and labor | Labor becomes political; pollution appears | Labor uprisings |
| Electrical | Mass media and ideologies | Mass politics; alliance blocs | World war |
| Information Age | Networks | Near-instant diffusion; misinformation | Information collapse |

### Worlds more advanced than the present day
- **Frontier nodes** (about 30–50), grounded in current research directions and labeled speculative in the Codex. Only very advanced worlds reach them, through their own research.
- **The inventor's advantage reverses:** past the 2026 level in a domain, the inventor has no blueprints and learns from the world instead.
- Advanced worlds make fuel and repairs easier, enable Superior repairs and skips, and decode the malfunction log faster.
- **Soft ceiling:** absorption and limited frontier tiers keep worlds roughly a few decades ahead at most (to be tuned).
- **Higher stakes:** advanced physics in immature eras raises catastrophic risk; weapons of mass destruction appear only abstractly, as consequences, never as player tools.
- **Art and ending:** one near-future architecture kit, plus an "Advanced Present" ending variant.

---

## 12. Sim Mode, Historical Figures, Anachronism Risk, Legacy, and the Chronicle

### Start date: AD 155 (Antonine era)
The player arrives in a peaceful Rome at its height, about ten years before the **Antonine Plague** (AD 165–180, probably smallpox, an estimated 5–10 million deaths), which arrived with troops returning from the siege of Seleucia and coincided with the Marcomannic Wars. Galen, the physician who described it, and the co-emperors Marcus Aurelius and Lucius Verus are central figures. The first era contains a catastrophe the inventor knows is coming, the ideal first test of Pillars 1 and 2.

### Scene templates
About 8–10 scene types reused in every era and re-dressed with era-specific art: market, workshop or lab, residence, seat of power, temple, port, tavern, library or school, military camp, street. Each era has 5–8 active scenes that unlock with influence, plus a few unique landmark scenes (e.g., the Roman Forum).

### Sim Mode actions
A scene visit costs **1 Attention** and allows **up to 3 actions**: Observe (reveal clues, causes, agendas), Converse (relationships and intelligence), or Nudge.

| Nudge verb | Effect | Risk |
|---|---|---|
| Suggest | Introduce an idea | Absorption applies |
| Encourage | Strengthen ambition in a domain | Low |
| Warn | Use knowledge of the future | High visibility; can build a prophet's Legend |
| Connect | Introduce two notables | Can trigger emergent advancements |
| Fund | Spend gold on a person's work | Wealth visibility |
| Attribute | Credit an idea to someone else or to a "rediscovered ancient text" | Lowers your visibility; raises theirs; less Legend for you |

Nudge success = Trust × receptivity (how the idea fits the person's traits).

**Trust has memories.** Trust is a number, but tapping Why? shows the few significant remembered interactions behind it ("You predicted the outbreak," "You funded my work," "You humiliated me publicly"). Memories come directly from the causal event log.

### Historical figures and notables

| Tier | Content | Target quantity |
|---|---|---|
| A: Authored | Full dialogue; historically grounded personality | Rome ~12–15; anchor eras ~10 each |
| B: Semi-authored | Short biography, traits, templated dialogue | ~20 per era |
| C: Procedural | Echo and talent figures; model-written biographies | As needed, within ~200 active notables |

- Figures have their own goals and can accept, resist, misuse, or distort nudges. They can serve as commanders and hold keys to fuel and repair puzzles.
- **Divergence rule:** chance of appearing as documented = 1 − (regional divergence × sensitivity). Example: divergence 0.4 × sensitivity 1.5 → 40% chance. Otherwise the figure appears altered or is lost, and an echo figure may fill the role. **Your changes to Rome could erase Shakespeare.**
- **Talent discovery:** unknown individuals of exceptional ability who never made the historical record.

### Anachronism risk
- **Visibility meters** for the inventor and each nudged notable rise with how anachronistic, how public, and how threatening to power an act is, and with displays of wealth.
- **Threat events** (investigation, accusation, assassination attempt) occur with chance based on Visibility × threat to power.
- **Protection:** patrons, sanctuary institutions, secrecy (slower spread), and the Attribute verb.

### Legacy transfer

| Factor | Legacy retained |
|---|---|
| Base | 30% |
| Each trained student (max 2) | +20% each |
| Work written down | +20% |
| Work held by an institution | +10% |

Example: one student and written notes, no institution = 30 + 20 + 20 = 70%.

### The Chronicle
Three tabs: **People** (documented, altered, lost, echoes), **Events** (averted, changed, new), **Knowledge** (earlier or later than history). Each region shows a divergence meter. Losing a major figure is a notable moment on arrival. The Chronicle feeds the ending.

### Religions
The **existence of major world religions is a fixed point.** Founders never appear and can't be nudged or erased. Their spread, institutions, reforms, and schisms can change; the player interacts with communities and institutions only.

---

## 13. First Hour, Onboarding, and the Tutorial System

### The first hour

| Time | Beat | Target feeling |
|---|---|---|
| 0–2 | Cold open: the lab, a dismissed warning, launch, malfunction | Intrigue |
| 2–4 | Arrival near Ostia; choose name, appearance, specialty | "Where am I?" |
| 4–7 | Hide the machine (quarry, ruined shrine, or warehouse, each with its own risk) | Vulnerability |
| 7–10 | Ostia market: Observe, Converse; earn first gold with your specialty | **"A stranger with knowledge but no means"** |
| 10–15 | Machine diagnostic: Component 1 and Repair 1; a smith holds the first clue | Purpose |
| 15–20 | First Suggest; absorption bars appear; **seeded choice:** fund the smith's workshop or repair the district fountain | Agency |
| 20–30 | First patron; first Connect; first Trust bars; someone refuses | **"People have their own will"** |
| ~27 | Stage 2 | Growth |
| 30–40 | Two-month turns; first domain priorities and first project; first experiment | Competence |
| 40–50 | **Seeded choice pays off:** a fever from the broken fountain, or a delayed component; a Journal line connects it to the earlier choice | "It's all connected" |
| 50–60 | First log layer decoded; rumors from Parthia; the Journal connects 155 to the coming plague | **"My choices echo, and something is coming"** |

### Onboarding principles
- **The Journal is the tutorial's voice, and it teaches strategy, not rules.** Instead of explaining a debt formula, the inventor notes: "Treating fevers one by one won't fix a contaminated well. And someone here has to keep it clean after I'm gone." One line teaches Medicine, Attention, institutions, and the game's philosophy.
- **Succession is hinted early:** the first patron asks who will continue your work.
- One system at a time; Influence Mode, AI civilizations, and battles stay hidden until they matter.
- **2-year grace period:** no death, reduced crises, no machine theft.
- A meaningful moment every 3–5 minutes; short, tap-first text; tips can be turned off.
- **Targets:** day-one retention ≥ 35%; day-7 to day-1 ratio ≥ 0.45.

### Tutorial system (programmed)

| Layer | What it is |
|---|---|
| Guided prologue | Scripted first hour with highlighted controls and temporary control locks; skippable for returning players |
| Just-in-time tutorials | A short guided tutorial the first time each mechanic becomes relevant |
| Codex | Searchable "How it works" reference with animated diagrams; replays any tutorial |
| Practice scenarios | Replayable mini-scenarios for complex systems |

**Just-in-time rules:**
1. Every new mechanic gets a tutorial the first time it becomes relevant: core systems, each era's signature system, new verbs, institution types, and domain effects, emergent advancements that change play, and all future updates and expansions.
2. 3 steps or fewer; details in the Codex.
3. One at a time; others queue with a "New" badge.
4. Never mid-crisis (only the essential one plays).
5. A 60-second refresher after 2+ weeks away.
6. Every tutorial played is replayable from the Codex.

**First-time tutorial triggers include:** Sim Mode, Attention, and gold (prologue); fuel and repair (first diagnostic); absorption and nudges (first Suggest); care domains and debt (first allocation); experiments and the Journal; stages and institutions (Stage 2); Influence Mode and directives (Stage 3); AI civilizations (first contact); battles (first war); Visibility (first significant rise); succession (age 50 or poor health); jump preparation; arrival and the Chronicle; each era's signature system; fortune and loss risk.

**Engine:** steps stored as data (trigger, highlighted target, control lock, Journal line, completion condition); each mechanic carries a tutorial ID and unlock event; completion synced via iCloud; settings for Guided, Standard, or Off; works in every mode; opt-in anonymous analytics on where players drop off.

---

## 14. Civilization Index, Stakes, and Endings

### The Index
Sub-score per care domain = your world's value ÷ the historical value at the same date × 100 (100 = matches real history).

| Domain | Metric | Benchmark source |
|---|---|---|
| Agriculture | Population and food security | Maddison Project 2023 population; HYDE (to verify) |
| Medicine | Life expectancy | Historical estimates (e.g., Our World in Data compilations) |
| Knowledge | Advancements vs. historical dates | Computed from the Knowledge Web |
| Governance | Stability | Authored curves (assumed) |
| Military | Security | Authored curves (assumed) |
| Economy | GDP per capita | Maddison Project 2023 |
| Infrastructure | Urbanization | Historical estimates (to verify) |
| Faith and Culture | Cohesion, cultural output, cultural diversity preserved | Authored curves (assumed) |

Early-era data is sparse; smoothed regional curves and authored curves are labeled as estimates in the Codex.

**Overall Index = geometric mean**, which rewards balance:

| Civilization | Sub-scores | Arithmetic mean | Geometric mean |
|---|---|---|---|
| Balanced | 150, 140, 120, 110, 100, 90, 80, 60 | 106 | ≈ 102 |
| Lopsided | 250, 200, 100, 100, 60, 40, 30, 20 | 100 | ≈ 72 |

Two views: **Sphere Index** (your regions) and **World Index** (the globe).

**In play:** highlights domains below 100; trend arrows and forecasts; **Sphere Index ≥ 110 enables Full-repair landing, ≥ 130 enables Superior** (late landing or skip); an Era Report on each arrival.

### Stakes
- Regions can collapse and cultures can disappear permanently, recorded in the Chronicle.
- **The only outright failure:** the inventor dies without a successor (Standard: restart from checkpoint; Ironman: run ends).
- Losing your home region or the machine is not a failure (the machine can be rebuilt from the Journal at great cost in years).
- The interface always states what ends the game and what determines the ending.

### Endings
**Evaluation:** World Index at arrival vs. the real present; the Chronicle (people, events, cultures gained or lost); the personal outcome (who arrives, relationships, key choices).

**Presentation: descriptive, not a verdict.** The ending describes the resulting civilization across dimensions (health, knowledge, stability, prosperity, cultural diversity, peace, equality, environment) and how history remembers the inventor, under a one-line **descriptive headline** generated from that profile (e.g., "A healthy, prosperous, but fractured world"). The player decides whether it's better. **The World Index stays visible** as a clear score, avoiding the confusion Humankind players reported about what decides the outcome.

**Internal tiers** (used for achievements and the headline's tone; never shown as a moral label):

| Tier | World Index |
|---|---|
| 1 | ≥ 130, few cultures lost (includes the Advanced Present variant) |
| 2 | 110–129 |
| 3 | 90–109 |
| 4 | 70–89 |
| 5 | < 70 |

**Variants:** The Inventor Returns vs. The Heir Returns; the **Stay** ending (dismantle the machine after the first jump, live out your life, and the simulation fast-forwards to the present for the same evaluation); how the inventor's Legend is remembered.

**The reveal:** arrival in the lab in the departure year; a montage of the altered present; the final log layer completes the mystery.

**Replay:** shareable Chronicle comparison with real history; Game Center achievements; randomized recipes and divergence.

### Sensitive history
- **Slavery** exists in AI societies as historical reality. **The player's institutions can never use enslaved labor.** The player can pursue manumission and reform, facing elite backlash. Exploitation may raise short-term output but always carries Governance and Faith and Culture debt plus a human cost in the Index: **always a net loss.**
- **Conquest and colonialism** appear through AI actions and events. The player can wage abstracted war, but there are **no atrocity actions**; aftermath always shows the human cost, and destroyed cultures lower the Index.
- **Enforcement:** these rules are hard simulation constraints backed by automated content checks, not just writing guidelines. No project, advancement, or verb may let the player use enslaved labor, commit atrocities, depict or erase a religious founder, or wield a weapon of mass destruction as a tool.
- Historical context cards; acknowledged honestly, never gratuitous.

---

## 15. Monetization

**Model:** free through the first jump and the arrival in Era 2, then a one-time purchase unlocks the full campaign. **To be tested:** ending the free slice at the arrival vs. extending it about 20 minutes into Era 2 so players can act on what they saw. No commitment until tested. Optional expansions, cosmetics, and small capped perks.

| Tier | Contents | Proposed price |
|---|---|---|
| Free | The full Rome era, the first jump, the arrival and Era Report in Era 2 | $0 |
| Complete Campaign | All 8 eras, endings, Sandbox, Ironman | $19.99 (launch $14.99) |
| Expansions | Alternate start points (Han China, Gupta India, Aksum, the Maya), new regions or civilizations, each with its own historical figures | $4.99–$9.99 |
| Scenario packs | "Prevent the Fall of Rome," "Survive the Black Death," "Save the Library" | $2.99–$4.99 |
| Cosmetics | Time machine designs, inventor outfits, architectural styles, jump effects | $0.99–$4.99 |
| Chronicle extras | Expanded biographies, alternate-history narratives, shareable timeline art | Low |
| Heirloom perks | Small, capped performance improvements (below) | Low |
| Complete Edition | Everything, bundled | Discount |

Family Sharing enabled where Apple allows it (to verify).

### Heirloom perks
**Rules:** each effect ≤ 10%; buy once, keep forever; 2 slots per campaign chosen at the start, non-stacking; **every perk also earnable through achievements**; the base game is balanced without perks; offered only at campaign setup or in the Workshop menu.

| Perk | Effect |
|---|---|
| Letter of Introduction | +10 Trust with one patron at the start of each era |
| Traveler's Purse | +50 gold on each arrival |
| Silver Tongue | +5% nudge success |
| Keen Eye | Observe reveals one extra detail per scene |
| Artisan's Mark | Repairs build 5% faster |
| Guild Seal | +5% institution capacity at founding |
| Ledger of Accounts | One additional fortune risk factor shown before a jump |

### Never sold
Time skips; gold, resources, influence, or advancement bypasses; fuel puzzle solutions or hints; protection from neglect debt, anachronism risk, Visibility, or death; extra Attention or life budget; guaranteed successors; premium currency, loot boxes, gacha, energy, or timers; store prompts during a crisis; limited-time pressure offers; ads; standalone figure packs; difficulty modes; save slots.

**Honest trade-off:** a lower revenue ceiling than whale-driven free-to-play, chosen deliberately to protect the pillars. Illustrative (assumed): 5% conversion at $19.99 ≈ $1.00 per install before Apple's commission and taxes, plus expansions. Apple Arcade would also suit the design but conflicts with the free-to-play choice.

---

## 16. Art, UI, and Session Design

### Art
- **Realistic isometric 3D** in the style of modern mobile strategy games like Rise of Kingdoms.

| Scale | Technique |
|---|---|
| World map | Isometric terrain, simplified detail, overlays |
| City and region | Continuous zoom from the map; detailed isometric 3D |
| Sim Mode scenes | Stylized transition into an authored scene template |

- **Feasibility:** fixed camera angle with limited rotation; baked lighting with an era profile; MetalFX upscaling; 30 fps on iPhone, 60 on iPad and Mac.
- **Divergent architecture:** buildings reflect the altered history (Roman forms persisting into the Renaissance; washing basins at temples after Temple Purity Rites). Built from modular parts, starting with about 10 key building types. One near-future kit for worlds beyond 2026.
- **Historical figures:** dignified portraits based on historical sources; procedural faces for generated figures; religious founders never depicted.
- **Battles:** a skippable 10–20 second vignette, then the outcome card.
- **Jumps:** a time-lapse of the world map (under 10 seconds) that hides the off-screen simulation. The machine shows visible repairs each era.

### UI
| Problem (Civilization VI on iPhone) | Rule |
|---|---|
| Small text | Dynamic Type; minimum readable size; no critical information in a cramped top bar |
| Mis-taps | Large tap targets; confirmation for irreversible actions; undo within a turn |
| Late-game slowdowns | Background simulation during the player's turn; progress shown; under 3 seconds to process a turn |

- **Landscape** is primary for the map on iPhone; card screens also support portrait.
- **Home screen:** the map stays home (it carries the divergent architecture and arrival payoffs), with a **"Matters that need you" tray** of cards (e.g., "3 matters require you") leading into scenes, crisis decisions, experiments, and projects.
- **The interface always answers three questions:** What needs my attention? What happens if I ignore it? What can I realistically do?
- **Main screens:** Map, Scene, Knowledge Web, Journal, Chronicle, Institutions, Index, Jump Preparation, Codex, Workshop. The turn dashboard (date, age and lifeline, Attention, gold, capacity, alerts, End Turn) is always visible. **Every number has a "Why?" link.**
- **iPad:** persistent side panels. **Mac:** pointer, keyboard shortcuts, hover tooltips.
- **Accessibility:** color plus shape for debt tiers; VoiceOver for menus and cards; reduced motion; haptics.

### Sessions
| Area | Design |
|---|---|
| Saves | Autosave every turn; checkpoints at era start and midpoint; single slot in Ironman |
| Interruptions | Instant save on backgrounding; exact resume |
| Sync | iCloud across iPhone, iPad, Mac; latest turn wins after a prompt |
| Offline progress | None; the world moves only when you play |
| Notifications | None by default; optional reminder |
| Returning | 60-second refresher after 2+ weeks |
| End of session | Summary of what's unresolved |
| Battery | 30 fps cap on iPhone; low-power mode |
| Download | First era in the initial download; later eras delivered in the background |

---

## 17. Technical Architecture

**Engine:** Unity 6 (C#). Unity Personal is free up to $200,000 in annual revenue or funding (Pro: $2,200 per seat per year). The simulation core is engine-independent, so switching engines would only mean rewriting presentation. Plain-text C# suits building with Claude Code.

### Layers
1. **Simulation core** (engine-independent C#, deterministic, seeded): time; regions, domains, expectations, debt, spillover; institutions, gold, economic shares, loss events; the Knowledge Web, diffusion, absorption, emergence, frontier nodes; AI civilizations and battle resolution; notables, trust, Visibility, legacy, divergence; the inventor's life budget, Attention, Journal, succession; the machine (recipe generator with automatic solvability check, repairs, jumps); the Index; and the **event log**, which records the immediate causes of state changes and powers in-era "Why?" explanations, Trust memories, the Chronicle, and summaries. Multi-century causal ancestry is not required.
2. **Presentation (Unity):** isometric renderer; era asset kits streamed on demand; MetalFX (Unity support or native plugin, to verify).
3. **Native Apple bridge (Swift plugin):** Foundation Models, StoreKit 2, iCloud, Game Center.
4. **Text and narrative:** authored dialogue as data; templates; cached model text with fallbacks.
5. **Tutorial engine:** data-driven steps; tutorial IDs tied to unlock events; iCloud-synced completion.
6. **Saves:** snapshot plus event log since checkpoint; compressed; versioned with migration.
7. **Purchases:** StoreKit 2 one-time purchases verified on device; no server required.
8. **Analytics:** opt-in, anonymous, mainly tutorial drop-off.
9. **Tools:** content in text data files (spreadsheet → JSON) for nodes, figures, and events; a balance simulator running thousands of headless AI-played campaigns; determinism tests; a replay tool.

### Performance budgets (baseline device)
| Task | Target |
|---|---|
| End-of-turn processing | < 3 seconds (background during the turn) |
| Jump simulation | < 10 seconds |
| Memory | < 1.5 GB (assumed) |

### Devices
| Tier | Devices | Settings |
|---|---|---|
| Low | iPhone 13–15 (A15, A16) | 30 fps, reduced detail, shortened time-lapse, template text |
| Standard | iPhone 15 Pro and later; M-series iPads | Full detail; on-device generated text |
| High | Mac; iPad Pro | 60 fps, higher resolution |

**Minimum OS:** iOS 26 (required for Foundation Models). Apple Intelligence runs on iPhone 15 Pro and later.

**Scope note:** the full game (22-hour campaign, ~865 nodes, ~200 authored figures, 8 era art kits, realistic 3D) is studio-scale production. The architecture lets one person build and test the simulation core, where the design's real risks lie; art can use placeholders or asset stores, or be commissioned later.

---

## 18. Prototype Plan

| # | Prototype | Tool | Time (solo, assumed) |
|---|---|---|---|
| P0 | **Butterfly Test** (includes the neglect and continuity model) | Text-based C# console app, built with Claude Code and reusable in the simulation core | 1–3 weekends |
| P2 | Paper fuel puzzle | Cards or a spreadsheet; 3–5 playtesters | 1–2 weekends |
| P3 | Headless simulation core | C#, built with Claude Code | 3–6 weeks |
| P4 | Playable vertical slice: the first hour in Rome | Unity 6, placeholder art | 2–3 months |
| P5 | The first jump (optional) | Unity 6 | 1–2 months |
| | **Total to a playable first jump** | | **About 4–8 months part-time** (see BUILD_GUIDE.md) |

### P0: Butterfly Test
**The key question:** do players feel their actions changed the world they return to, and want to see what happens next?

Scope: one region; 3 care domains (Medicine, Governance, Economy); 2 institutions with leaders and 2 pre-authored drift paths each; 8–12 meaningful decisions; one crisis (the plague) with 3 visible warning stages; one promise that conflicts with jump timing; 3 specific Echoes; one 250-year absence; a four-beat arrival. The neglect math (expectations, 5% compounding debt, crisis tiers, institution decay, geometric Index) runs underneath. Full scope and out-of-scope list: PROTOTYPE_SCOPE.md.

| Test | Pass criteria |
|---|---|
| Balance vs. specialization | Both viable; neither wins more than 65% of 100 automated runs per strategy (3 strategies), run **before** human testing |
| Debt pacing | 2–3 years of neglect → Strained; crisis likely within about 5–8 years |
| Prevention vs. cure | 1.5× premium meaningful but recoverable |
| Institution decay | After 250 years from strength 80: bare 80 × 0.9^25 ≈ 6; chartered 80 × 0.97^25 ≈ 37; strong 80 × 0.99^25 ≈ 62 |
| Index | Clearly separates balanced from lopsided |
| Impact | At least 3 of 5 testers say, unprompted, that their actions changed the returned world, pointing to at least one specific Echo |
| Real choices | Testers can describe what they were choosing *between*, not just what they clicked |
| Desire to continue | Testers want to see what happens after the next jump |
| Expectations | Players don't avoid improvements to keep expectations low (if they do, split expectation into social and physical values) |

**Kill criteria:** if one strategy always dominates, rework debt or expectations. **If the jump and arrival aren't compelling, redesign before expanding the game.**

### P2: Fuel puzzle
Model: 6 materials × 4 processes × 4 conditions = 96 combinations; clue cards; time cost per experiment.

**Flaw found on paper:** feedback that says *which* parts are wrong lets brute force finish in at most max(6, 4, 4) = 6 attempts. **Fix:** feedback says only *how many* parts are right, with occasional telling byproducts.

At 3–6 months per experiment, brute force (10+ attempts) costs about 2.5–5 years; deduction (3–5 attempts) about 1–2.5 years.

| Test | Pass criteria |
|---|---|
| Solvability | Deduction in 3–5 experiments |
| Guessing penalty | Guessing takes at least twice as many |
| Satisfaction | At least 3 of 5 testers describe an "aha" |
| Fallbacks | Needed in under 20% of attempts |
| Physical observations | Players can interpret results without the underlying rule being spelled out |

**Kill criterion:** if it isn't fun, simplify fuel into a capability project like the repairs.

### P3: Headless simulation core
Scope: time steps, regions and domains, debt, institutions, coarse-mode jumps, the event log with in-era "Why?" output, and a balance simulator running 1,000 campaigns (balanced, specialized, neglectful).

| Test | Pass criteria |
|---|---|
| Determinism | Same seed and inputs → same outcome |
| Match with P0 | Reproduces P0 findings at full scale |
| Performance | 400-year jump, 300 regions × 8 domains, < 10 s on laptop, then on iPhone |
| "Why?" explanations | Readable to someone who didn't write them |

### P4: Vertical slice, the first hour
Scope: the Section 13 first hour: Ostia market, a simple map, Attention, gold, Suggest and Connect, the Component 1 puzzle, the seeded choice and fever payoff, the tutorial engine, the Journal.

| Test | Pass criteria |
|---|---|
| Target feelings | Reported at minutes 10, 30, and 60 |
| "It's all connected" | At least 3 of 5 testers connect the fever to their earlier choice |
| Tutorial | At least 80% complete it without help |
| Performance | < 3 s turn processing on iPhone 13 |

### P5: The first jump
Scope: end of Rome → time-lapse → Era 2 arrival → Era Report, institution outcomes, fortune, Chronicle. **Must prove:** the arrival feels like the payoff, and institution decay feels fair.

---

## Appendix A: Key Numbers

| Parameter | Value |
|---|---|
| Start | AD 155, inventor age 28 |
| Eras | 8 playable; the 8th jump returns home |
| Years per era | ~10 standard; ~7 good; ~5 or skip excellent |
| Healthy years by medicine | ~70 / ~85 / ~95 |
| Attention | 4 per turn |
| Turns per standard era | 51 (P0 prototype only: 2-month turns, 120 per 20-year era; the player may shorten or restore turns up to 3 months) |
| Economic policy (P0) | Austrian stance +1 Economy/yr each, backlash −8 faction loyalty and +8 Governance debt on adoption; interventionist +1.5/yr each and 8 malinvestment/yr; bust after 3 warnings from malinvestment 20 |
| Playtime | ~2.5–3 hours per era; ~22 hours per campaign |
| Debt compounding | 5% per year; during a jump, only for the first 30 years after departure |
| Authority (P0) | Public projects need 10% of an institution in their domain; plague measures 10% of a Medicine or Governance institution, or membership once the first warning has come; a plague response needs 10% (SYSTEMS §7 voice, lowered by the warnings) |
| Money (P0) | Denarii for everyday use (1 aureus = 25 denarii in AD 155); gold aurei hold their value as the denarius is debased; exchange 1 Attention, 2.5% fee each way; start with 60 scavenged aurei; the machine takes aurei back |
| Gold across the jump (P0) | Carry 10 aurei; deposit with the bank (2%/yr in gold; 8%/decade × integrity loss risk, halved at 10% of the bank) or bury (6%/decade found); the rest is lost; revealed on arrival |
| Walking around (P0) | At every arrival, including AD 155: market (wheat, wages), money changers (aureus rate, silver), forges (workshop, inventions in use), Curia, Subura (fountain, population); present conditions only |
| Tradeoffs (P0) | Faction lock-out at 10% applies to every purchase, seniority and reward, and the rival faction marks up your Governance projects 50%; consulting −2 standing with the Circle; quarantine rules Economy −3 and guild −10; wheelbarrow, collar, trip hammer: Governance debt +3 and guild −5; endowed institutions drift ×1.5; grievances before joining lower your starting loyalty |
| Savings limits (P0) | Bank takes up to 100 aurei per depositor, a jar holds 50; loss or discovery chance rises with the sum |
| Camps and offices (P0) | Two camps per institution; a meeting is a vote (weight 1/2/3/5 by office); offices with period titles offered at the new year (officer 10%/2 yrs, deputy 25%/5 yrs and your camp leading, head 50% or the guild's 5-yearly election); duties 1/1/2 Attention a turn; last orders force = office 0.1/0.35/0.6/1 × loyalty × voting record, slowing drift up to 90%; the head names a successor; the leading camp scales upkeep while away ×(1 ± 0.5); an office keeps its domain up 0.4/0.8/1.2 a year and pays 15/25/40% of your projects there; founding failure 7% a year |
| Decision events (P0) | 6 dated historical choices + 4 leaders' requests to members; 2–3 options each; gold −50…+60 aurei (AD 155), levels ±1.5–4.5, loyalty ±4–12, lean ±0.15–0.3, income +4–5 a year; unanswered lapse to the last option after 3 turns or at departure; a choice you made leaves a mark shown in the Personal echo, up to 2 an arrival |
| Workshop (P0) | Orders every 4 months, 2 offered, take 1 a season (2 with 2+ apprentices): fittings 5, tools 4, instruments 4, ironwork 12 aurei (AD 155) × output × prices, taxed; apprentices max 4, 3 aurei a year, +10% output each; the smith's regard 0–100 from 50; after you leave apprentices add 0.25 × (1 + 0.5 × techniques) each to the Economy target for 50 years; fate score ≥4 street of forges, ≥1 working, else gone |
| Second jump (P0) | After the first arrival, one more jump at once (no second era); range from repairs and upgrades only |
| Plague (P0) | On its historical dates (warnings AD 165–166, outbreak October 166); left alone it kills 10% of Rome and costs each domain its historical drop; player actions change only severity (Medicine debt raises hazard; Governance and Economy debt tiers raise severity); recurrence odds from Medicine debt |
| History as baseline | Everything follows history unless the player directly or indirectly changes it |
| Institution gold (30-year window after departure) | Grows 0–1.5% per year by Economy level; pays own-domain debt at 1.5× (loyal full, drifted partial, rogue none); frozen afterward in P0 |
| Corruption (30-year window) | Chance per decade = 0.05 × exposure (×1 small, ×2 large) × (1 − audit 0.5) × (1 − integrity: honest 0.3, average 0, venal −0.3); Minor / Major / Total lose 25% / 50% / 100%; weights 40/40/20 unprotected, 70/25/5 audited |
| Paying down debt | 1.5× the cost of prevention |
| Joining institutions (P0) | One requirement for the first purchase: Circle a Medicine project or the promise; sanctuary none; Caecilians patronage; Junians property; guild a business; bank a 5% first deposit; the two factions exclusive at 10% |
| Attention costs (P0) | Buy 2, invest 2, found 3, charter 3, audit 3, endow 2, policy 2, attend a meeting 1, oversee 1; stake base price Medicine 2.4 / Governance 3.6 / Economy 3 per 1% |
| Seniority and newcomer premium (P0) | +1% stake a year of paid-up membership, up to 25%; newcomer price ×3 fading to ×1 over 5 years |
| Era length (P0) | 20 years (80 three-month turns, AD 155–175) |
| Entry fee and dues (P0) | Entry fee 5–30 gold by institution; annual dues = base 1–5 gold + 0.1 per percent held |
| Inventions (P0) | An invention tree: 12 inventions in four branches of three tiers (mechanics: wheelbarrow → horse collar → trip hammer; accounts; hygiene; workshop: lathe → water bellows → blast furnace, +10/15/20% workshop income); varied stake and loyalty for all six established institutions; each needs its branch predecessor and something from Rome; pays income, leader loyalty, stake and some domain levels |
| Time machine (P0) | A full assessment first (1 Attention a turn for 4 turns) reveals what is wrong; 9 repair steps in three systems, all required to jump; all 60 gold scavenged from the machine must go back in (fixed; the machine's gold is not debased); 3 optional upgrades; the jump is 25–60 years, drawn within a range set by repairs, time in the era and upgrades |
| Joining benefits (P0) | +10% work pay per membership; a voice (25%) makes the institution pay a quarter of projects in its domain; project gold ×1.5 |
| Prices (P0) | Price level +1.5%/yr as history, +4% debased, 0% sound (by sway); gold costs scale with it; pay catches up half as fast |
| The coin (P0) | Near half of Rome's historical Economy decline (155–268) and a quarter of Governance's come from debasement, timed by the silver content (78% → 50% by 200 → 3% by 268); sound coin defended after departure spares that share, debasement adds half again |
| Jump range | Starts short (first jump about 25–50 years) and grows as technology and knowledge advance (rule to be defined) |
| Institution decay per decade | 10% bare; 3% chartered and endowed; 1% strong |
| Lasting mark (P0) | During an absence Rome follows history's swings and only the gap from history moves, 25% a decade toward 0.6 × the departure lead or deficit, from the day you leave; random plague recurrences off; surviving institutions add their maintenance (0.6 per strength point, scaled); drift shows at 30 points; Austrian stances pull the Economy target up 3 a decade more for each decade their institution stands; an interventionist policy busts at most once per absence |
| Institution stakes (P0) | First buy 1%; 10% influence, 25% voice, 50% control; each 1% costs domain base × (1 + stake%/4); founding your own ≈ 40% of the cost of 50% control, starts weak and may fail; sway = min(1, 2 × influence) |
| Jump landing thresholds | Sphere Index ≥ 110 (Full), ≥ 130 (Superior) |
| Malfunction chance | 15% Partial (5% of those lethal); 5% Standard (1% lethal) |
| Sim Mode | 1 Attention per scene; up to 3 actions |
| Legacy | 30% base; +20% per student (max 2); +20% written; +10% institution |
| Active notables | ~200 |
| Knowledge Web | ~575 core + ~290 refinements + 30–50 frontier nodes |
| Emergent advancements | 2–4 per era |
| Grace period | First 2 in-game years |
| Active promises | 2 maximum |
| Echoes | 3–5 per era |
| Arrival sequence | 4 beats (recognition, wrongness, personal echo, discovery), plus optional Learn more |
| Campaign unlock | $19.99 (launch $14.99) |
| Heirloom perks | ≤ 10% each; 2 slots |
| Performance | < 3 s per turn; < 10 s per jump |
| Retention targets | Day 1 ≥ 35%; day-7/day-1 ≥ 0.45 |

## Appendix B: Open Items to Verify

- Unreal Engine royalty terms (for the engine comparison)
- Unity support for MetalFX, or the need for a native plugin
- Family Sharing for non-consumable in-app purchases
- Maddison Project terms for commercial use; HYDE and urbanization data sources
- Free-slice length: arrival only vs. arrival plus about 20 minutes of Era 2 (test before committing)
- The historical roster at AD 155 (e.g., Galen's whereabouts, Ptolemy's dates) for authored Rome figures
- Knights Templar as the confiscation example
- App Store review expectations for on-device generated text
- Memory budget and frame rate on iPhone 13 (to validate in P4)

## Appendix C: Key Sources

- Apple Foundation Models: machinelearning.apple.com/research/apple-foundation-models-2025-updates; developer.apple.com/videos/play/wwdc2026/339/
- Apple Intelligence devices: support.apple.com/en-gb/guide/iphone/aside/iph275f4d617/26/ios/26
- MetalFX: developer.apple.com/videos/play/wwdc2025/211/
- Unity pricing: unity.com/blog/unity-is-canceling-the-runtime-fee
- Antonine Plague: en.wikipedia.org/wiki/Antonine_Plague; worldhistory.org/Antonine_Plague/
- Maddison Project 2023: Bolt & van Zanden (2024), Journal of Economic Surveys, DOI 10.1111/joes.12618
- Old World (Orders, postmortem): pcgamesn.com/old-world/review; gamespress.com/Soren-Johnson-GDC-2022-Talk
- Sid Meier on decisions: antoinebuteau.com/lessons-from-sid-meier/; gamedeveloper.com/game-platforms/analysis-sid-meier-s-key-design-lessons
- Civilization VI iOS: apps.apple.com/app/id1235863443; macworld.com/article/231955/civilization-vi-iphone-review.html
- Civilization VI agendas: gamerevolution.com/?p=12915; pcgamesn.com/civilization-vi/leaders-agendas
- Civilization VII ages: forums.civfanatics.com/threads/overview-of-mechanics.691596
- Crusader Kings III warfare and succession: Steam community discussions (app 1158310)
- Victoria 3 radicals: ggrecon.com/guides/victoria-3-radicals; Paradox dev diary 14
- RimWorld wealth and raids: rimworldwiki.com/wiki/Wealth_management
- Noita alchemy: noita.fandom.com/wiki/Lively_Concoction
- Return of the Obra Dinn: en.wikipedia.org/wiki/Return_of_the_Obra_Dinn
- Spore: macworld.com/article/1136116/spore.html
- Mount & Blade II renown: mountandblade.fandom.com/wiki/Renown
- Humankind Fame: gamesradar.com/humankind-review
- Polytopia: macworld.com/article/3186485/…/the-battle-of-polytopia.html
- Rise of Kingdoms criticism: riseofkingdomsguides.com; sommerspc.com/?p=13449
- Mobile retention benchmarks: businessofapps.com/?p=96421; gamedevreports.substack.com/p/gameanalytics-mobile-gaming-benchmarks
- Lest Darkness Fall: tor.com/2008/08/14/lest-darkness-fall
- Machinations: cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf
