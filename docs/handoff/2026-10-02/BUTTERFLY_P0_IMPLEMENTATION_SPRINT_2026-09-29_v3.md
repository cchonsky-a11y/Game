# The Butterfly Effect — P0 Implementation Sprint Package
**Prepared:** 2026-09-29  
**Target branch:** `claude/app-building-prompts-tl4ymh`  
**Baseline commit:** `cde50e06e5a1017f7f14e6ad3c5af453c49474d7`

## Purpose
Implement the approved P0 redesign decisions made during the live design playtest, while preserving deterministic simulation, event logging, test coverage, and the documented source-of-truth chain.

This package is implementation guidance, not permission to retune unrelated systems.

---

# 1. Approved P0 Rules — Authoritative

## 1.1 Calendar and time
- **1 turn = 1 calendar month.**
- All player-facing timed tasks should be defined in **real game time, preferably months**, not in turns.
- Replace or migrate `durationTurns` / `Turns` concepts toward `durationMonths`.
- Engine converts real-time duration to turn count only if needed internally.
- Do **not** change intended real-world durations merely because turn length changed.
- **Machine assessment = 2 months.**
- Normal month transition: brief.
- January / start of a new year: short atmospheric year-opening summary.
- Major events: full narrative treatment.

### Required invariant
Changing `time.monthsPerTurn` in the future must not silently alter the intended real-time duration of jobs, projects, repairs, commitments, plague phases, training, or office terms.

---

# 2. Attention and scheduling

## 2.1 Main menu
Every main-menu action shows its Attention cost inline.

Examples:
- `Work for income — varies`
- `Walk around Rome — 0 Attention`
- `Begin repair — 2 Attention · 4 months`

Information-only actions are 0 Attention.

## 2.2 Multi-month commitments
Jobs and other commitments may last multiple months.

A commitment has at minimum:
- id
- display name
- start date
- durationMonths
- attentionPerMonth
- gold/pay terms if applicable
- current progress
- completion/failure state
- optional client/character id
- optional discovery hooks

### Required invariant
The player may **never silently reserve more Attention in a future month than the monthly maximum**.

If accepting a job would create:
`futureCommittedAttention > attention.perTurn`
the game must reject it or explicitly require the player to cancel/renegotiate another commitment first.

### Player-facing schedule
Status should be able to show:
- This month: 2/4 committed
- Next month: 4/4 committed
- Following month: 2/4 committed

## 2.3 Work costs
Work is not a flat-cost action.

Illustrative structure only — exact pay/balance numbers remain tunable:
- odd job: low Attention, short
- skilled job: moderate
- consulting: higher
- major commission: high
- some jobs span multiple months

No balance number in this package should be treated as approved unless separately recorded in tuning/docs.

---

# 3. Work system redesign

Work should become a narrative and discovery system, not only a money source.

## 3.1 Job definition
A job may include:
- difficulty
- durationMonths
- attentionPerMonth
- compensation
- minimum reputation
- client
- related domain(s)
- problem tags
- discovery table / event hooks
- interruption consequences
- completion consequences
- reputation effect

## 3.2 Reputation ladder
Player access should grow naturally:
small practical jobs → successful work → referrals → technical consulting → major commissions.

A stranger in AD 155 should not immediately receive major Roman infrastructure commissions.

## 3.3 Jobs as scenes
Harder jobs should create character and technical scenes:
- client disagreement
- craftsman limitations
- material quality
- worker practice
- unexpected failure
- political/institutional interference
- economic constraints

## 3.4 Work can spark ideas
Difficult or unusual jobs can reveal invention ideas.

Important:
- discovery is **not guaranteed**
- discovery should emerge from a concrete problem
- completing a job should not automatically grant a finished invention
- deterministic seeded RNG only

Examples:
- inconsistent rigging → standard lifting practice
- worn pump seals → maintenance standards
- inconsistent fittings → gauges / repeatable dimensions
- rope failure → rope quality standards

---

# 4. Institutions redesign

## 4.1 Scheduled meetings
Institution meetings become **regularly scheduled events**.

Membership/standing determines access.

Examples of cadence:
- monthly
- every 2 months
- quarterly
- annual

Cadence must be represented in real game time, not hard-coded turn counts.

## 4.2 Attention
**Attending institution meetings and normal actions inside them consume 0 normal Attention.**

They may still cost:
- gold
- reputation
- loyalty
- influence
- promises
- political capital
- relationship consequences

## 4.3 Advocacy
Remove free-floating advocacy from the normal main menu.

Advocacy appears only where it makes narrative sense:
- guild meeting
- physicians' circle
- Curia/political meeting
- religious body
- commercial association
- other explicit forum

Player must have appropriate access/standing.

Narrative must identify **who is being persuaded**.

## 4.4 Meetings should feel like episodes
Do not replace the main menu with a second spreadsheet.

A meeting should usually center on a small number of live disputes:
- named people
- competing agendas
- proposals
- votes
- office nominations
- institutional projects
- internal rivalries

---

# 5. Exploration redesign

## 5.1 Narrative-first
`Walk around Rome` starts with a scene, not a POI list.

The scene should:
1. establish place
2. deliver sensory/social texture
3. show Romans behaving like people
4. surface one or more situations
5. then offer choices

## 5.2 Historical authenticity
Historical accuracy is a hard constraint.

Three content layers:
1. **documented historical background**
2. **historically plausible fiction**
3. **intentional alternate-history consequences created by the player**

Do not invent an anachronism because it "feels Roman."

## 5.3 Neighborhood identity
Different parts of Rome should feel distinct:
- Subura
- Forum
- river/warehouse districts
- Aventine
- Palatine
- etc.

## 5.4 Exploration can spark inventions
Walks are a primary invention-discovery path.

Loop:
**walk → observe real problem → idea → feasibility → experiment → deployment → consequences**

Do not turn walks into a giant hidden tech tree.

---

# 6. Invention system refinement

## 6.1 Idea is not invention
An Idea should be a separate state from a completed invention.

A discovered idea may still require:
- materials
- manufacturing capability
- precision
- specialist
- institution
- customer / use case
- prerequisite method
- experiment/prototype

This directly supports the pillar:
**Knowing Is Not Making.**

## 6.2 Avoid idea spam
The 100k model produced many discovery opportunities; do not copy test-scaffolding probabilities into production.

Ideas should be meaningful enough that players remember why they discovered them.

## 6.3 Failure can progress sideways
Failed experiments can create:
- partial technical insight
- relationship change
- reputation loss/gain
- injury/damage
- new problem
- alternative invention path

Failure should not always be "nothing happened."

---

# 7. Recurring Roman characters

Important systems should be embodied by named Romans.

Core recurring-character direction:
- recognizable voice
- occupation/class
- family ties
- ambitions
- fears
- affiliations
- opinion of player
- relationship history
- descendants/apprentices/successors where appropriate

## 7.1 Workshop partner
The workshop partner is a **major persistent character**, not UI furniture.

Current approved concept:
**Gaius Fabius Crispus** — fictional, historically plausible workshop partner/craftsman.

His role:
- practical technical counterweight
- embodies Roman manufacturing constraints
- challenges modern assumptions
- owns/prioritizes his workshop
- workers/apprentices/family matter
- persists through the Rome-era narrative
- workshop fate should matter emotionally after a jump

Exact name may be changed later, but the role is approved.

## 7.2 Other recurring-character prototypes
Examples developed in playtest:
- Titus Aelius Serenus — physician
- Lucius Marcius Varro — merchant
- Marcus Tullius Naso — grain trader/associate

Treat as fictional characters; historical plausibility should be reviewed before locking names/biographies.

---

# 8. News redesign

## 8.1 Tone
Use the energy of a theatrical Roman civic herald/newsreader.

Reference inspiration: HBO *Rome* public-newsreader energy only.
Do not copy protected dialogue, characters, or scenes.

## 8.2 Editorial logic
News people want attention.

Do not report stable/minimal inflation or normal coin quality as a headline merely because the simulation has the number.

Prioritize:
- unusual market movement
- food/supply problems
- political change
- fires/disasters
- crime/scandal
- public works problems
- wars/decrees
- disease reports
- elite gossip
- neighborhood stories

## 8.3 News → world opportunity
Some news items can seed actionable leads.

Important:
- no `NEW QUEST` banner
- do not label which stories matter
- some stories are pure flavor
- some are misleading/incomplete
- some unlock world actions naturally

Example:
news reports fever → a Serenus conversation / Subura investigation becomes available.

---

# 9. Narrative pacing

## Routine month
Use compact state transition.

Example:
`March, AD 155`
`Attention: 4/4`
`Workshop expansion complete.`
`Machine assessment complete.`

Then menu.

## New year
Use a short atmospheric summary of:
- Rome
- important characters
- economy/institutions
- player position

Then menu.

## Major narrative moments
Use full scene:
- arrival
- jump
- major crisis
- institution conflict
- discovery
- major project completion
- significant relationship event
- important consequence

---

# 10. Opening rewrite

Story first, mechanics second.

Approved opening direction:
- “The machine stops screaming before you do.”
- darkness / hot metal / burned insulation
- “Then light pours in when you crack open the hatch.”
- inventor climbs out **expecting the lab**
- encounters living Rome
- controls are dead
- no plague mention in opening
- do not front-load 25–60-year jump explanation
- no magical pouch of Roman coins

## Gold source
Starting gold is **scavenged from the machine's electronics/components**:
- contacts
- connectors
- circuit traces
- shielding / durable components where appropriate

Initially it is raw valuable material, not Roman currency.

---

# 11. Time-machine canon rewrite

## 11.1 Accident premise
The inventor was **not attempting to travel**.

They were running a non-travel systems test:
- partial temporal-field energization
- brief hold
- data collection
- shutdown

No destination was entered.
No jump command was intended.
The inventor was not prepared as a passenger.

The machine unexpectedly performed a complete spacetime displacement and arrived in **Rome, AD 155**, which was not the inventor's starting city.

## 11.2 Central mystery
Why did a test with no destination resolve to Rome, AD 155?

Do not answer immediately.

## 11.3 Machine architecture
Use a coherent fictional architecture rather than generic "spaceship parts."

Canonical conceptual systems:
- Temporal Field Core
- Field Geometry / Containment
- Temporal Reference
- Spatial Reference
- Spacetime Navigation Computer
- Pulse Energy Storage
- Thermal Management
- Temporal Catalyst / consumable medium

The player does **not** need a physics lecture.

Internal voice should use technical shorthand because the inventor already understands their own machine.

## 11.4 Damage model
The irreplaceable field core survives.
Supporting systems are damaged.

The early goal is not:
"build a time machine in Rome."

It is:
"repair enough supporting capability to force a crude controlled forward jump."

This is what lets the campaign span eras.

## 11.5 Internal voice
Machine-related narration should be:
- technically competent
- dry
- frustrated
- concise
- aware that every "simple" repair depends on civilization-scale capabilities

---

# 12. Jump payoff

Before expanding eras, strengthen jump consequences.

Each major jump should aim to reveal:
1. **physical echo** — building/object/infrastructure
2. **human echo** — character, family, descendant, apprentice
3. **institutional echo** — guild, school, state, church, company
4. **unintended echo** — something the player did not expect

The jump should be one of the game's primary rewards.

---

# 13. Imperfect information

Characters may:
- be biased
- exaggerate
- misunderstand
- repeat rumor
- disagree
- have incomplete information

The herald may report an official account that conflicts with what the player personally observed.

Do not make every NPC statement a direct simulation truth dump.

---

# 14. Existing open bug queue

At baseline commit `cde50e0`:
- T1–T3: done
- L1–L11: done
- L12–L14: open
- L15: appears done based on status omission; verify
- L16–L18: open

Do not regress prior A1–A10 fixes.

Remaining definitions:
- L12: `why plague` should show response gold/Attention cost before choice
- L13: distinguish personal votes from institution balance; do not change math without proof
- L14: grammar + menu-title truncation
- L16: forge provenance text should not claim player financed it if guild did
- L17: second-arrival Subura population baseline wording
- L18: seniority cap should get a one-time explanation

---

# 15. Current code audit — likely implementation surfaces

The repository already has useful separation. Primary likely touch points:

## Time/calendar
- `src/Butterfly.Core/SimTime.cs`
- `src/Butterfly.Core/Simulation.cs`
- `src/Butterfly.Core/Simulation.Pacing.cs`
- `src/Butterfly.Core/Simulation.Recurrences.cs`
- `data/tuning.json`

Current audit notes:
- `SimTime` already stores whole months — good foundation.
- `Simulation.Pacing.cs` still contains stale P0 commentary referring to **3-month turns / 40 per era**.
- `AttentionBudget.cs` calculates supply/demand using `time.monthsPerTurn` and many `Turns` fields.

## Attention/scheduling
- `src/Butterfly.Core/AttentionBudget.cs`
- `src/Butterfly.Core/World.cs`
- `src/Butterfly.Core/Simulation.Pacing.cs`
- likely new commitment/schedule model or extension of existing commitment structures

## Institutions
- `src/Butterfly.Core/Simulation.Institutions.cs`
- `src/Butterfly.Core/Simulation.Policy.cs`
- `src/Butterfly.Core/Simulation.Stakes.cs`
- `src/Butterfly.Core/Simulation.Offices.cs`
- `data/content/institutions.json`

## Work/jobs
- inspect `Simulation.Personal.cs`, `Simulation.Economy.cs`, workshop logic, console command/menu routing
- likely introduce explicit job definitions/state rather than treating work as isolated actions

## Exploration
- `src/Butterfly.Core/Simulation.Walk.cs`
- `data/content/events.json`
- `data/content/text.json`
- likely add character-aware encounter content

## Inventions
- `src/Butterfly.Core/Simulation.Inventions.cs`
- `data/content/inventions.json`
- `Content.cs` definitions/state models

## News
- `src/Butterfly.Core/Simulation.News.cs`
- `data/content/news.json`

## Time machine
- `src/Butterfly.Core/Simulation.Machine.cs`
- `src/Butterfly.Core/Simulation.Jump.cs`
- `data/content/machine.json`
- opening text in `data/content/text.json` / console presentation

## Docs
Per repo rules, sync:
1. `docs/SYSTEMS.md`
2. `docs/butterfly-effect-gdd.md` / Appendix A as applicable
3. `data/tuning.json`
4. `docs/DECISIONS.md`
5. `docs/PROTOTYPE_SCOPE.md` if P0 scope/gate changes
6. `docs/HANDOFF.md`

---

# 16. Migration strategy

Do not perform a blind global rename from Turns → Months.

Recommended sequence:

## Phase A — time abstraction
1. Define a canonical helper for duration conversion.
2. New content uses `DurationMonths`.
3. Old content can temporarily deserialize legacy `Turns` if needed.
4. Convert legacy duration to intended months using the **old authored real-time duration**, not the new 1-month turn assumption.
5. Add validation preventing both ambiguous fields from being set.

Example:
If legacy content says:
`Turns = 3`
and it was authored when:
`monthsPerTurn = 2`
then intended duration is:
`DurationMonths = 6`
not 3.

## Phase B — commitments
Introduce a calendar commitment model and future Attention validation.

## Phase C — institutions/work
Move institution meetings and multi-month jobs onto calendar commitments/events.

## Phase D — content/narrative
Apply news, walk, characters, opening, machine rewrite.

This reduces the chance of breaking every timed system in one commit.

---

# 17. Automated acceptance tests

These are required behavior tests, not tuning targets.

## Calendar
- One `EndTurn()` advances exactly 1 month under new tuning.
- December → January increments year correctly.
- Era end still occurs by calendar date.

## Real-time duration
- machine assessment started in January completes after February is processed / exactly 2 months of elapsed duration according to chosen start/completion convention
- a 6-month project remains 6 months even if `monthsPerTurn` changes in a dedicated conversion test
- legacy-duration migration preserves old real-time duration

## Attention
- total monthly Attention never < 0
- total monthly committed Attention never > maximum
- accepting a commitment that would exceed a future month's cap is rejected
- cancellation/renegotiation frees future capacity
- info actions cost 0

## Institutions
- attending a meeting costs 0 normal Attention
- advocacy command unavailable outside a meeting
- advocacy available inside correct meeting when access requirements met
- meeting schedule is calendar-based

## Work
- multi-month job reserves correct Attention each month
- pay timing deterministic
- completion logged
- abandoned job consequence logged
- job discovery hooks deterministic under seed
- harder jobs can be reputation gated

## Exploration/inventions
- walk can reveal idea without completing invention
- idea cannot be directly treated as completed invention
- prerequisites correctly block development
- discovery events are logged

## News
- normal/stable economic state does not force a "coinage is sound" headline
- actionable lead can be unlocked by news
- lead is not presented as a quest banner

## Narrative
- routine month does not produce long opening scene
- January can produce year-opening summary
- major event can produce full scene

## Time machine
- assessment text reflects non-travel test premise
- no destination was entered
- machine origin city is not assumed to be Rome
- early machine repair does not imply building temporal core from Roman technology

## Determinism
For same seed + same command sequence:
- event log hash identical
- discoveries identical
- work events identical
- meeting events identical

---

# 18. 100k stress-test invariants to preserve

The conceptual 100k model was useful for architecture, not tuning.

Preserve these invariants in the actual executable stress runner:
- no negative Attention
- no >4 future committed Attention
- no institution meeting consuming normal Attention
- exact 2-month machine assessment
- multi-month work progression correct
- advocacy only in meeting context
- ideas can arise from walks/jobs
- actionable news leads may occur
- zero crash / impossible / dead-end conditions attributable to scheduling

Do not reproduce conceptual random probabilities as final balance.

---

# 19. P0 exit criterion update — proposed for Corey approval/documentation

Old gate: five real human testers.

Practical replacement:
- approved redesign implemented
- unit/integration/regression tests green
- large automated stress test clean
- deep blind AI playthroughs show coherent first-era loop
- whatever real-human blind testing is reasonably available, preferably 1–2 people
- Corey approves the Rome → people → work/exploration → inventions → institutions → jump → consequences loop as compelling enough to continue

Core question:
**Does the first jump make a player want to know what happened next?**

Do not change this gate in docs until Corey explicitly says to replace the old five-human requirement.

---

# 20. Suggested implementation order / commit plan

1. `time: make one turn one month and introduce real-time duration model`
2. `schedule: add future Attention commitments and validation`
3. `work: add multi-month jobs and reputation gating`
4. `institutions: move advocacy into scheduled zero-Attention meetings`
5. `ideas: separate discovery from completed inventions`
6. `walk: narrative-first exploration and idea hooks`
7. `news: herald presentation and lead hooks`
8. `characters: recurring Roman identity/state foundation`
9. `machine: rewrite test-failure premise and architecture`
10. `narrative: compact months, year-openings, major scenes`
11. `bugs: resolve L12-L14 and L16-L18`
12. `docs/tests: full synchronization and stress pass`

Every commit:
- small enough to inspect
- `dotnet test`
- deterministic test coverage
- event logging for state changes
- no unrelated retuning

---

# 21. Definition of done for this sprint

The sprint is done when a fresh executable can demonstrate this sequence coherently:

1. inventor accidentally arrives in Rome from a non-travel machine test
2. starting gold is scavenged from machine components
3. turn/month UI shows inline Attention
4. machine assessment takes 2 months
5. player meets named Romans, including workshop partner
6. walk scene feels like Ancient Rome and can spark an idea
7. idea is not automatically an invention
8. player accepts a multi-month job
9. future Attention reservation is visible and safe
10. news can seed a natural lead
11. institution meeting occurs on schedule
12. advocacy exists only in that meeting context
13. meeting costs no normal Attention
14. routine month transitions remain compact
15. jump/arrival consequences preserve physical + human + institutional + unintended echoes
16. all tests/stress checks pass deterministically


---

# 22. Post-playtest recommendations — approved after 100-run mining

These recommendations are now part of the approved redesign direction unless explicitly marked otherwise.

## 22.1 Protect the jump payoff; reduce grind between meaningful beats

Repeated blind-style testing and the earlier Claude reports point to the same problem: the middle of an era can collapse into repetitive work / attendance / end-turn loops.

Approved direction:
- Do **not** solve this by merely shortening the calendar.
- Make intervening months denser with meaningful content.
- Work should become short character/problem stories rather than a repeatable cash button.
- Institution meetings should become episodes with actual disputes and named people.
- Exploration should surface problems, relationships, rumors, and opportunities.
- Invention progress should come through experiments, prototypes, collaborators, failures, and deployment.
- Routine scenes stay concise; major scenes get the long-form treatment.

The player should regularly feel that something changed even when a major historical event did not fire.

## 22.2 Make Attention immediately legible

Attention remains the clearest recurring UX friction.

Approved UI direction:
- Always show total Attention, free Attention, and reserved Attention distinctly.
- Example presentation:
  `Attention: 2 free / 4 total · 2 reserved — Machine Assessment`
- Show upcoming commitments before the player accepts a multi-month action.
- Never silently reserve future Attention.
- If overcommitment is ever allowed, it must be deliberate and explicitly acknowledged by the player.
- Otherwise reject the action before state changes.

## 22.3 Remove surprise month advancement

Approved rule:
- The month ends when the player explicitly chooses **End Month**.
- Spending the final Attention point does not automatically advance time.
- Multi-month commitments do not silently skip months.
- If all normal Attention is occupied, the player may still use appropriate 0-Attention actions.
- 0-Attention actions should be naturally bounded so the player cannot create absurd amounts of activity while time is frozen.
- Optional fast-forward can exist, but only as an explicit player action with interruption on decisions/events.

## 22.4 Early jumps remain fully valid

**Important correction to earlier recommendation: do not gate, heavily discourage, or over-warn early jumps.**

Approved rule:
> Once the machine is capable of jumping, the player may leave.

The departure screen should stay neutral and concise. It should not scold the player or list all the content they will miss.

Example tone:
> **The machine is ready.**
> You can leave now, or remain in Rome and continue your work.

The consequences after the jump should teach the player what their level of involvement produced.

An early departure may result in:
- few surviving relationships,
- weak or short-lived inventions,
- institutions developing without the player,
- historical events proceeding close to baseline,
- unresolved problems worsening,
- or unexpected positive outcomes caused by the player's absence.

Early jumping is not the "wrong" path. It should produce a thinner, stranger, or less personally connected history because the player changed less—not because the game punishes them for leaving.

Core question at departure:
**"Have I done enough here?"**
not
**"Has the game finally given me permission to leave?"**

## 22.5 Make the first jump teach the game's central promise

The first jump should arrive soon enough that players learn why the game is special.

Do not force a long pre-jump tutorial era.

The first return should clearly demonstrate:
- something physical changed,
- someone or a relationship left an echo,
- an institution or practice changed,
- and at least one consequence was not what the player intended.

After players understand this loop, later eras can support longer arcs.

## 22.6 Downstream invention propagation is a core system

Approved rule:
> Player inventions may cause other people to invent things the player never directly designed.

Propagation can happen through:
- apprentices,
- rival workshops,
- descendants,
- institutions,
- procurement,
- customers,
- accidental combinations,
- copied techniques,
- shared standards,
- and economic incentives.

Possible outcomes:
- direct improvement,
- adjacent invention,
- combination invention,
- institutional standardization,
- misuse,
- monopoly,
- flawed copying,
- disappearance and later rediscovery.

This should be probabilistic and path-dependent.

Avoid presenting downstream development as a conventional tech-tree unlock. The player should discover it in the world after time has passed.

## 22.7 Jump reveal structure

Each major jump should try to reveal four distinct kinds of echo:

1. **Physical echo** — something visible/tangible changed.
2. **Human echo** — a person, apprentice, family, rival, or remembered relationship persisted.
3. **Institutional/systemic echo** — a practice, institution, market, standard, or political structure changed.
4. **Unintended echo** — something happened the player did not plan.

Downstream inventions can layer across any of these.

The jump should remain discovery-first, not a giant report screen.

## 22.8 Character lineages

Important relationships should be able to persist through:
- children,
- apprentices,
- students,
- business partners,
- rivals,
- successors,
- institutions,
- traditions,
- stories,
- reputations.

Biological descendants are only one path.

Recurring Romans should have goals, fears, obligations, relationships, and opinions unrelated to the player. Major characters should not exist solely as system interfaces.

## 22.9 Preserve causal legibility

The existing `why` concept is validated and should be preserved.

Preferred sequence:
1. Player experiences a consequence naturally.
2. World/narrative gives visible evidence.
3. Optional `why` detail exposes the contributing mechanical causes.

Do not replace narrative consequence with raw modifiers.

Major consequences should provide enough evidence that the player can reconstruct:
**"I did X, which influenced Y, and years later this became Z."**

## 22.10 Work economy / progression redesign

Consulting should not dominate every rational income strategy.

Approved direction:
- High-paying work requires reputation, referrals, relationships, or specialized capability.
- Consulting should be limited by availability and access rather than functioning as an always-on optimal button.
- Workshop jobs provide non-cash value:
  - technical knowledge,
  - relationships,
  - referrals,
  - invention clues,
  - reputation,
  - access to better commissions.
- Progression should roughly feel like:
  small repair → referral → skilled commission → consulting / major contract → historically consequential project.

Exact pay tuning remains a tuning task, not a locked design number.

## 22.11 Institutions should be human episodes

Replace spreadsheet-like meeting loops with scenes involving a small number of disputes and named people.

A meeting can include:
- advocacy,
- votes,
- office offers,
- factions,
- funding,
- audits,
- succession,
- rivalries,
- institutional projects.

If the player's preferred faction loses, the game should make the cause legible:
- insufficient influence,
- stronger opposing bloc,
- officeholder power,
- patronage,
- absenteeism,
- prior promises,
- etc.

Never show results like "your votes 9–0, but the other camp leads" without an explanation.

## 22.12 Plague climax needs clearer choices

Approved improvements:
- Show costs before a plague response is chosen.
- Keep response choices available long enough to act on.
- If preparation already reduced mortality, response options should still differ in another meaningful dimension:
  - deaths,
  - economic damage,
  - trust,
  - institutional standing,
  - long-term precedent,
  - political backlash.
- Do not let the climax look mechanically pointless.

## 22.13 Avoid narrator moral verdicts that overstate evidence

Replace statements like:
> "The Subura has seen you profit from its bad years twice."

when the trigger does not match the player's own understanding.

Prefer concrete social perception:
> "Some in the Subura say you grew richer while the district suffered."

Then let `why` or a character explain which actions created that perception.

The world can judge the player. The narrator should not misstate what the player actually did.

---

# 23. Revised implementation priority after playtest evidence

Recommended order for the next major implementation pass:

1. one turn = one month
2. explicit free/reserved/future Attention UI
3. remove surprise auto-end / silent time skipping
4. multi-month work + reputation/referral progression
5. concise routine narrative pacing
6. institution meetings as named-character episodes
7. first-jump payoff and neutral early-jump handling
8. downstream invention propagation foundation
9. character lineage / successor persistence
10. causal `why` support for major consequences
11. plague response clarity and differentiation
12. economic rebalance so consulting is not universally dominant
13. remaining L12-L14, L16-L18 cleanup
14. deterministic regression + stress + blind-style replay pass

---

# 24. New jump acceptance checks

A jump implementation is not considered complete unless automated and blind-style tests can demonstrate:

- Early departure is allowed once the machine is capable.
- Departure text is neutral rather than punitive or over-warning.
- A low-involvement player can return to a relatively baseline world without the game breaking.
- A high-involvement player receives richer, more numerous echoes.
- At least one jump path can produce a downstream invention the player never directly created.
- Human/apprentice/successor legacy can persist independently of physical artifacts.
- Institutional outcomes can diverge from the player's preference while remaining causally explainable.
- Unintended consequences can occur.
- The player can inspect why a major result happened without requiring that explanation to understand the narrative.


---

# 25. Approved HBO *Rome*-inspired social-world additions

These are reference inspirations only. Do not copy characters, dialogue, plots, or distinctive scenes. The goal is to capture the lived-in social texture: hierarchy, patronage, households, indirect history, rumor, and personal consequences.

## 25.1 Social hierarchy should shape access and interpretation

Rome should not treat reputation as one generic number.

NPCs should evaluate the player differently based on:
- citizenship/status assumptions,
- occupation,
- wealth,
- patronage,
- institutional membership,
- demonstrated technical competence,
- household ties,
- foreignness,
- public reputation.

A craftsman may respect demonstrated skill while a magistrate ignores abstract expertise.
A wealthy freedman may have money without elite social standing.
A merchant may tolerate eccentricity if the player reliably solves problems.

The player should gradually learn how Rome categorizes them.

## 25.2 Politics should appear through people before abstractions

Institutional conflict should usually be introduced through named Romans with motives.

Preferred sequence:
1. A person explains what they want.
2. Another person disagrees or has a competing interest.
3. The player sees the practical stakes.
4. The institution UI summarizes the balance of power.

Avoid presenting politics first as:
`Guild faction A: 43% / faction B: 57%`

Instead show:
- Sabinus pushing a policy because his warehouses are full,
- Varro opposing him because delays hurt his trade,
- an officeholder worried about grain unrest,
- a porter leader angry about workload,
then reveal the institutional totals.

## 25.3 Add a patronage / favor network

Relationships should generate obligations, introductions, and future requests.

Possible favor types:
- introduction to a magistrate or contractor,
- recommendation for a commission,
- access to a restricted workshop or archive,
- loan or material supply,
- legal protection,
- political support,
- medical help,
- sponsorship,
- employment for an apprentice or relative.

Favors can be:
- owed by the player,
- owed to the player,
- transferable through households or patrons,
- remembered after time jumps.

A favor should not become a simple currency. It should retain a specific social context.

## 25.4 Households are persistent social units

Important NPCs should exist within households rather than as isolated system interfaces.

A household can include:
- spouse,
- children,
- relatives,
- apprentices,
- slaves,
- freedmen,
- clients,
- business partners.

Households can persist even when the original character dies.

Example:
Gaius may die, but his workshop household survives through:
- an apprentice,
- a widow managing accounts,
- a son who hates the player's standards,
- a freedman who expands the business,
- a rival who marries into the family.

This gives jumps human continuity without relying only on biological descendants.

## 25.5 Add a hidden social-network layer

Important NPCs should connect to other people through:
- patron/client relationships,
- family,
- trade,
- religion,
- military service,
- workshop ties,
- institutional membership,
- neighborhood ties.

This network should generate opportunities organically.

Example chain:
Gaius → supplier → bathhouse contractor → municipal official → public works commission.

The player should discover networks through people, not through quest markers.

## 25.6 Religion should be part of ordinary life

Religion should appear as everyday practice, not a lore lecture.

Possible manifestations:
- household shrines,
- vows,
- sacrifices,
- funerals,
- festivals,
- omens,
- temple obligations,
- religious interpretation of illness,
- political use of ritual.

The modern protagonist may be skeptical, but surrounding Romans treat these practices as normal.

Avoid framing Romans as foolish for religious beliefs.

## 25.7 Public ritual can deliver information

Historical and civic information does not need to come only from herald/news systems.

Events can reach the player through:
- funerals,
- games,
- triumphs,
- public sacrifices,
- court proceedings,
- processions,
- executions,
- market closures,
- military departures,
- temple observances.

These events should communicate social mood as well as facts.

## 25.8 Street-to-palace contrast

The same problem should be able to travel through multiple layers of Roman society.

Example:
- Gaius struggles with a bronze fitting.
- Varro needs reliable replacement parts.
- A bathhouse contractor adopts the practice.
- A military supplier notices it.
- An official procurement office begins specifying dimensions.

This allows the player to see how a small technical idea becomes a larger system change.

## 25.9 Major history should usually arrive indirectly

Do not default to putting the player beside famous historical figures whenever an important event occurs.

A war can appear as:
- metal shortages,
- military orders,
- interrupted trade,
- wounded veterans,
- apprentices drafted away,
- tax pressure,
- panic buying.

A political crisis can appear as:
- altered contracts,
- patronage changes,
- arrests,
- rumor,
- disappearing clients,
- new opportunities.

Major history should first affect people and systems the player already cares about.

## 25.10 Rumor and conflicting accounts

NPCs should disagree.

Possible cases:
- a merchant exaggerates a shortage,
- a physician recognizes symptoms but misidentifies the cause,
- an official downplays unrest,
- a herald repeats the official version,
- a neighborhood rumor contradicts elite reporting.

No NPC should function as an omniscient truth terminal.

The player may need to infer what is happening from conflicting evidence.

## 25.11 The player can become mythologized

After time jumps, later Romans may remember the protagonist incorrectly.

Possible distortions:
- described as Greek or Egyptian,
- remembered as a physician rather than an inventor,
- credited with inventions actually made by apprentices,
- omitted entirely while Gaius receives the credit,
- turned into a religious figure,
- split into two legendary people,
- dismissed as folklore,
- confused with an actual historical person.

This should depend on:
- who witnessed events,
- written records,
- institutional survival,
- prestige,
- propaganda,
- time elapsed,
- rival claims.

The player knows what actually happened; history does not necessarily preserve it accurately.

## 25.12 History-memory system

Major player actions should create a hidden historical-memory record with fields such as:
- witnesses,
- written evidence,
- physical evidence,
- institutional custody,
- competing claimants,
- prestige,
- oral transmission,
- distortion risk.

Across jumps, memory can:
- remain accurate,
- simplify,
- merge with another story,
- transfer credit,
- become myth,
- disappear,
- later be rediscovered.

This should be deterministic under seeded RNG and logged for testing.

## 25.13 Dark humor should come from people

Humor should emerge naturally from:
- personality clashes,
- workplace frustration,
- social awkwardness,
- bureaucracy,
- gossip,
- understatement.

Do not make Rome itself the joke.

Gaius-style dry exchanges are the preferred model.

## 25.14 Status signaling should be visible in scenes

The game should communicate status through behavior and environment:
- who moves aside,
- who gets interrupted,
- who is accompanied,
- who sits,
- who stands,
- who can enter a room,
- who speaks first,
- clothing,
- escorts,
- household size,
- architectural space.

Avoid redundant UI labels when the scene can show status.

## 25.15 Major historical events must become personal

Design rule:
> Never introduce a major historical event only as a historical fact. Show who it hurts, who profits, who is afraid, who lies about it, and who asks the player to act.

Example:
Instead of:
> "The Marcomannic Wars have begun."

Show:
- Gaius cannot get enough iron,
- Varro's caravan is delayed,
- an apprentice's brother is called up,
- a contractor wants fifty identical fittings,
- prices shift,
then allow the player to learn the larger event.

---

# 26. Priority implementation from the social-world additions

Highest-value additions:

1. Patronage / favor network
2. Household persistence for major NPCs
3. Named-person politics before institution summaries
4. Major history delivered through personal/economic consequences
5. Player mythologization / historical-memory distortion

These should integrate with:
- jump echoes,
- downstream inventions,
- institution meetings,
- recurring characters,
- news,
- exploration,
- work progression.

They should not become five separate menu systems. The player experiences them primarily through scenes and consequences.
