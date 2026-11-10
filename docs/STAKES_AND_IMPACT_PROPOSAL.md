# Stakes and Impact pass: proposal

> **Status: direction approved and the five rulings of §10 given by Corey (2026-11-10: yes to all). Slice 0 is done (§9). Each later slice's rules and numbers are still to be proposed and approved.** This is a design proposal, not canon. Nothing here is in SYSTEMS.md, tuning.json or code yet. Each slice (§9) goes through the normal path when it is built: SYSTEMS → GDD Appendix A → tuning (as `PROPOSED` entries) → DECISIONS → content → tests.
> Historical details are from memory and must be checked before they become content.

## 1. Why

Corey's read after playing more than ten years of game time: the impact was minimal, outcomes don't vary enough, and there is no way to lose. The game's own data agrees.

| Finding | Evidence |
|---|---|
| Doing nothing scores about the same as playing well | Neglectful strategy: Index 94–102. Random players: middle 80% land at 97–130, median 112 (`playtests/explore-report-100000.md`). Corey's run: 121 at departure, 113 on arrival. |
| The limit is what an action buys, not how many there are | About 580 Attention over twelve years. Machine repair uses about 100; both Grand Challenges together use 28. Steps take 6–18 calendar months; later work unlocks at months 26–40. |
| The player's mark fades by rule | SYSTEMS §8: the long-run target keeps 0.6 of the gap from history. |
| Outcomes are decided early and stay put | Rank correlation departure → arrival 0.91. One strategy wins 94–97% of runs at each timing; the batch balance gate fails. |
| History is a fixed rail | The plague and the dated choices come on their dates in every game; exactly one crisis in every absence. |
| There is no way to lose | No death, arrest, bankruptcy or loss of the machine in code. Leaving late beats leaving early in 92% of runs: staying costs nothing. |
| The inventor's work is small | Ceiling in Rome: a shared unit of measure and one wheel driving two machines. Steam, electricity, the press, the governor and printing are in the capability graph but no work moves them. |

Those numbers are from the P0 strategy-level batch. **Slice 0 measured the P1 layer** (`playtests/ai/live/2026-11-10-p1-outcome-spread.md`; 30 seeds × 6 scripted profiles, leaving in AD 163; scripted, not human):

| P1 finding | Measured |
|---|---|
| No run can be lost | 180 of 180 runs jumped |
| Playing is worth about 4 Index points | Active profiles arrive at 103.3 on average; a player who only repairs the machine arrives at 99.2 |
| Active players end close together | 97.1–108.5 on arrival |
| Nothing gets past "reproducible" | No capability in any run reached manufacturable or beyond |
| Half the capability graph never moves | 9 of 19 nodes stay at None in every run (alloys, copying, electricity, governor, hydraulic press, interchangeable parts, lathe work, steam, graded steel) |
| A third to a half of Attention goes unused | 1.4–1.8 of 4 unused a month for active profiles |
| Returns do differ in what they show | An idle player's return shares nothing with an active one's; two active players share 59–90% of places and variants |

## 2. What this pass is for

A genius from the future should be able to change Rome a great deal, lose badly, and come back to a world that is different in kind, and not always better. Pillar 1 (Every Choice Echoes) outranks Pillar 2 (Knowing Is Not Making); today Pillar 2 is winning.

Kept as they are: 4 Attention a month (constitution 3), the inventor never controls civilizations directly (constitution 2), the Roman baseline rule for foundation work, the hard constraints (SYSTEMS §14), determinism, the first six quiet months.

## 3. Time costs something

Staying must be a real choice.

- **Aging.** Already canon and unbuilt (SYSTEMS §3: starts at 28, healthy years about 70, ages only within eras). Eight eras of ten-plus years each do not fit in one life. Show the life budget from the start, so every year in Rome is visibly spent.
- **Illness.** The inventor lives through a smallpox epidemic and cannot catch it. They should be able to. Their own medicine work (the fountain, soap, a fever ward, variolation) lowers their own risk, which makes that work personal.
- **Notice** (§4) grows with fame and time.

## 4. Notice: what Rome thinks you are

Replaces the unbuilt single "Visibility" meter (SYSTEMS §10, GDD §12). No Roman can conclude "you are from the future". They explain the inventor with the categories they have.

**Readings.** People settle on a reading of the inventor. Each opens doors and carries its own danger.

| Reading | Comes from | Opens | Danger |
|---|---|---|---|
| Craftsman | Work through shops and the guild | Guild, steady commissions | Low; little fame |
| Healer | Medicine work | Households, the Circle | A patient's death becomes a poisoning charge |
| Wonder-worker | Public wonders | Patrons, crowds, the palace | Sorcery accusation; being kept |
| Magus | Unexplained devices, secrecy, the machine seen | Fear, a following | Capital charge of magic |
| Prophet | Foreknowledge spoken aloud (the plague) | Belief once proved right | Expulsion as an astrologer; treason if it touches the emperor |
| Fraud | Failed wonders, broken promises | Nothing | Patrons leave; work dismissed |
| The senator's man | Clientage | Protection, the Curia | The patron's enemies; his fall is yours |

**Who acts on it:** rivals and lost clients (accusation), guilds and workers whose living is threatened (sabotage, a mob), the powerful (suppression, or keeping the inventor as a prize).

**Threat events**, each with warning stages like every crisis (SYSTEMS §6): an accusation and trial, a burned or seized workshop, exile, conscription, execution. A non-citizen has fewer protections.

**Protection:** a patron, an institution taking the credit (attribution: less fame, less danger), citizenship as a long goal, discretion.

**The machine's own risks:** stripped as scrap for its gold and bronze, lost to fire, or produced as evidence of sorcery.

## 5. Ways to lose

Three levels, in line with constitution 8 (failure creates history).

1. **Run-ending.** Death without a successor (already canon): of illness, age, execution, or a lethal jump. Until saves exist, a run-ending loss closes with what the world remembered.
2. **Setbacks.** Exile, a seized workshop, a lost fortune, a damaged machine, a bad landing, being kept in someone's service.
3. **A worse world.** A return below history that the player's own bold actions caused (§8), not only neglect.

The interface always says what can end the run (GDD §14).

## 6. The machine can fail during a jump

Corey, 2026-11-10: jump failure is a way to lose, and repairs need more decisions and their own risks.

**Today:** nine fixed errands, all required, no choices, no risk. **Proposed:**

- **Each repair has ways to do it.** *Proper:* slow, and needs what Rome can really make (a capability, a person, a membership). *Improvised:* fast and cheap, and adds jump risk. *Cannibalized:* take a part from elsewhere in the machine; instant, and costs something else (range, a later repair, an instrument).
- **Each system fails in its own way.**

| System | A poor repair risks |
|---|---|
| Coil (power) | Wrong distance: short or long of the target range |
| Coolant | A hard landing: machine damage to repair in the next era; injury (healthy years lost) |
| Chronometer | Wrong date: arriving off target, perhaps into a crisis |

- **Repairs meet the big work.** A proper repair should need a real achievement (drawn wire for the coil, glass and distilling for the coolant, a geared calculator for the chronometer), so machine work and world work are one activity.
- **Systems tests.** The opening was a non-travel systems test. The player can run more: each reveals the true state of one system, and costs wear, time and Notice (the machine screams).
- **No hidden gate.** Already canon ("no hidden gate against early jumps"; "the player is never forced to jump"). The machine may jump before every repair is done, at a stated risk. The briefing shows risk as bands, never outcomes (the convention already used for corruption and deposits).
- **Malfunction outcomes:** wrong distance, wrong date, hard landing, injury, cargo lost, and rarely death. GDD §5 already gives tiers (Partial 15% malfunction, 5% of those lethal; Standard 5% and 1%; Full 0%).

## 7. Impact: leverage, wonders, and what lasts

### 7.1 Leverage, not more actions
Other games keep the ruler's actions few and make each one direct other people:

| Game | How one action reaches far |
|---|---|
| Old World | A small pool of Orders a turn; governors and generals multiply what one order does |
| Crusader Kings | Council members hold standing tasks that run until changed |
| Civilization | Governors, policy cards and automated builders: set once, keep working; Great People are one-shot large effects |
| Victoria 3 | The player sets laws and production methods; the population and the economy do the rest |
| King of Dragon Pass / Six Ages | A few actions a season, each large, carried out by advisors with skills and opinions |

The shared pattern: standing instructions, carried out by named people with their own competence and agenda, with the size of one action growing as the player rises. For this game:

- **Hand work to people.** A commission or stage can be given to someone who has seen the method (Marcus, Gaius, Felix's men). It then costs no Attention and runs on their skill: they may improve it, botch it, or copy it for themselves. The people model already allows all three.
- **Institutions as levers again.** P1 made institutions obligations. A member with standing should be able to put a matter to the house: one Attention, the house's capacity does the work.
- **Cut the waiting.** Shorten the 6–18-month steps and the month-26–40 gates. Small plumbing jobs stop appearing after the first year or so.

### 7.2 Wonders and foundations
Two kinds of work, a real choice between them.

- **A wonder** is a one-off the inventor makes by hand with the best craftsmen: spectacular, quick, not reproducible. In the capability model it is a prototype with no ladder under it. It brings fame, patrons and Notice. Candidates: a geared astronomical calculator, spectacles and then a telescope or microscope, variolation before the plague, electro-gilding, a hot-air balloon, color-changing glass, movable type.
- **A foundation** is the slow ladder that exists now (standards, powered shops), plus the dormant nodes given real work.
- **Bets.** A wonder can fail in public (the Fraud reading), frighten people (Magus), or be taken (the prize).
- Weapons stay out (§14). Real mysteries only (Roman concrete, the dodecahedra, the Antikythera mechanism, the flexible-glass legend); nothing pseudo-historical.

### 7.3 What lasts: rooting
Corey, 2026-11-10: whether an achievement compounds depends on how deeply it is ingrained in society; take the rule from history's real leaps.

| Historical case | What happened | Why |
|---|---|---|
| Printing in Europe | Spread across the continent within decades, then compounded | Paper was cheap, many competing towns, it paid for itself, no one could stop it |
| Movable type in China and Korea | Invented centuries earlier; stayed marginal | Thousands of characters; state-run; woodblock was cheaper |
| Hero's steam and automata | Stayed temple toys | No use that paid; no one to carry them |
| The Antikythera mechanism | Nothing like it for over a thousand years | One maker or one shop; nothing written that survived |
| Roman concrete | Used for centuries, then the recipe was lost | Craft knowledge inside a state building program; the program ended |
| Greek fire | Lost entirely | A guarded state secret; the few holders died out |
| Damascus steel | Lost | Depended on one ore source; the makers didn't know which part mattered |
| The cure for scurvy | Found, adopted, then lost again | Adopted as a rule without the reason; a cheaper substitute broke it |
| Hindu-Arabic numerals | Took centuries; banned in places | Useful, but distrusted by authorities; won through merchants |
| Smallpox inoculation | Spread fast where rulers backed it; resisted elsewhere | State and church stance decided it |
| The heavy plough, the horse collar | Slow, then permanent | Paid for themselves on every farm; needed no institution |

**Proposed rule.** When the inventor leaves, each achievement has a **rooting** made of things the game already tracks:

1. **Carriers:** how many independent shops, houses or towns can do it without the inventor.
2. **Written and taught:** drawings, records, apprentices; and whether the *reason* was passed on, not only the rule.
3. **Pays for itself:** whether it is economical with no patron.
4. **Dependence:** on one secret, one supplier, one patron or the state.
5. **Fit with power:** backed, ignored, or opposed.

Rooting decides the fate during the absence, replacing the single "keep 0.6 of the gap" rule for these achievements:

| Rooting | Fate |
|---|---|
| None (a wonder alone) | A relic, a legend, or lost |
| Thin (one shop, one patron) | Lost when that shop or patron fails; or a guarded secret |
| Rule without reason | Survives distorted (the journal comparison already shows this) |
| Several carriers, pays for itself | Persists and spreads |
| Institutional and self-funding | **Compounds:** grows each decade and moves its domain's trajectory |
| Dependent on the state | Lasts while the state does, then falls with it (Rome's third century) |

This maps onto the existing maturity ladder (reproducible → institutionalized), the separate spread record, and Knowledge / Means / Carriers. The new parts are that rooting reaches the domains and the Index, and that well-rooted work grows.

## 8. History diverges, and not always for the better

Corey, 2026-11-10: this is the whole point. The model is the sports almanac in *Back to the Future Part II*: not the literal plot, but the idea that an advantage left behind is used by someone else for their own ends.

- **Everything powerful the inventor leaves has a holder.** A wonder, a method, a deposit of gold, an institution, a prediction that came true, the journal, a favor owed. While the inventor is away each holder uses it for their own interests. People already have goals, loyalties and rivals, and institutions already return Thriving, Drifted, Captured or Rogue. This extends that to everything else.
- **Outcomes run both ways.** The same press prints medical tables or an emperor's proscription lists. The same variolation saves a city or becomes a paid privilege of the rich. Shared measures serve honest trade or a tax office that misses nothing. Good outcomes need rooting, and holders whose interests the player understood before leaving.
- **Dated events can move.** History stays the baseline until the player has pushed far enough from it. Past a threshold in a domain, a dated event can be delayed, brought forward, changed or cancelled, and new ones can appear. Every divergence is still caused by the player's actions and logged, as the 2026-09-28 rule requires. Natural events (an epidemic arriving from the east) keep their dates; human ones (a succession, a debasement, a persecution) can move.
- **Returns differ in kind.** A Rome with presses, a Rome that banned machines, a Rome whose plague never took hold, a Rome ruled by the house that held the inventor's gold. Not 113 against 100.
- **The player reconstructs it.** After a jump the world shows what changed and does not prove the link (VISION). The departure briefing lists who holds what, as facts.

## 9. Order of work

Small slices, each ending with passing tests and proposed numbers for Corey.

| # | Slice | Gives |
|---|---|---|
| 0 | **Done 2026-11-10.** Measure the P1 layer: scripted, very different players; compare their first returns (`--p1-spread`) | A baseline, so later slices can be shown to widen outcomes |
| 1a | **Done 2026-11-10.** A life that can be spent and lost: age and healthy years shown, catching the plague, the fever, leaving the city, death ends the run (STAKES_PROPOSALS S-01 to S-08) | The first way to lose; a reason to leave early |
| 1b | Notice: readings, the first threat events | Tension from fame |
| 2 | The machine: repair choices, risk bands, systems tests, jumping early, malfunctions | Jump failure; repairs as decisions |
| 3 | Rooting: what lasts, what compounds, capability work reaching the domains | Real impact |
| 4 | Leverage and pacing: handing work to people, shorter waits, small jobs taper off | More done per action |
| 5 | One wonder end to end, with its fates at the return | The genius fantasy; bets |
| 6 | Holders and divergence: who has what while you're away; dated events that move | Variance; worse worlds |

## 10. Conflicts with current canon: ruled by Corey, 2026-11-10 (yes to all five asked: rows 1, 2, 3, 5, 7)

Rows 4, 6 and 8 were not put to Corey as questions; they stand as proposals and are confirmed when their slice is built.

| # | Conflict | Proposed ruling |
|---|---|---|
| 1 | P1 gameplay is frozen; the P2 slice excludes new invention work and balance changes; repair tiers and malfunctions are on the out-of-scope list | Lift the freeze for this pass; move the items in scope slice by slice |
| 2 | "A historical crisis comes on its historical dates; the player changes how hard, not when" (2026-09-28) | Keep for natural events; let human events move past a divergence threshold |
| 3 | Long-run rule keeps 0.6 of every gap; GDD soft ceiling of "a few decades ahead" | Keep as the default; rooted achievements follow §7.3 and may exceed it |
| 4 | "Roman baseline first: never invent what Rome had" | Keep for foundations; a wonder may be new to Rome, made once |
| 5 | The machine needs all nine steps to jump | Allow an early jump at stated risk |
| 6 | Death without a successor is "the only outright failure" (GDD §14); succession and saves are unbuilt | Add execution and a lethal jump as causes of death; until saves exist a loss ends the run |
| 7 | The five-person first-return test is designed for the current game | Run it after this pass, on the changed return; Corey's call |
| 8 | Institutions "bring obligations, not buffs" (P1) | Keep obligations; add putting a matter to the house as a lever, not a passive bonus |

## 11. Open questions

1. Does the inventor have someone to go home to? (Why leave at all.)
2. Is being *kept* (conscripted by a patron or the palace) a setback the player can escape, or an ending?
3. How far may a single era diverge: decades ahead of history, or centuries?
4. Which wonder first? (Suggested: the geared calculator, because it can also be the chronometer's proper repair.)
5. What does R-17 pay out at the first return?
