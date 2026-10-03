# The Butterfly Effect — Current Master Handoff
**Cutoff:** October 2, 2026  
**Phase:** P1 — Playable Game Structure  
**Owner / final design authority:** Corey

---

## 1. What this project is

**The Butterfly Effect** is a turn-based historical strategy / time-travel game for iPhone, iPad, and Mac.

The protagonist is a 21st-century inventor whose time machine fails during a **non-travel systems test** and strands him in **Rome, AD 155**. He must survive, repair the machine's support systems using what the Roman world can actually supply, and decide how much to accelerate civilization before each jump.

The game is not primarily "invent modern gadgets in Rome." Its strongest current design is:

> **Roman baseline → identify bottleneck → apply future theory → build/formalize capability → propagate through people and institutions → jump → observe altered history.**

The player is one person in a large world. Rome remains alive and autonomous. Institutions, relationships, craftsmen, successors, markets, households, disease, politics, culture, and unintended consequences determine whether an idea survives.

### Core pillars

1. **Every Choice Echoes** — actions produce delayed consequences, sometimes decades later.
2. **Knowing Is Not Making** — knowing future science does not create materials, tools, labor, capital, social legitimacy, or manufacturing capability.
3. **One Person in a Large World** — the player cannot personally control civilization; influence spreads through people and institutions.
4. **The jump is the payoff** — jumps reveal technical, personal, institutional, physical, and unintended echoes.

---

## 2. Repository and implementation reality

Repository: `cchonsky-a11y/Game`  
Branch: `claude/app-building-prompts-tl4ymh`  
Verified branch HEAD: `53f850d04334577be26175d3bc714c0e07c06b38`

The repository is a deterministic C# simulation core intended to remain engine-independent / Unity-compatible.

### Project rules that remain binding

- Corey is the final decision-maker.
- `docs/SYSTEMS.md` is the intended system source of truth after synchronization.
- Tuning values belong in `data/tuning.json`.
- Deterministic seeded RNG only.
- Formula changes require tests.
- Every state-changing event should be logged / causally traceable.
- Rule/number changes require documentation synchronization.
- Do not tune solely to make a gate pass.
- Use small commits.
- Work on the existing branch unless Corey says otherwise.
- No PR unless requested.
- **Run `dotnet test` before every commit.**

### Current repo is behind current design

Examples verified from the live branch:

- `Simulation.Pacing.cs` still documents old P0 pacing and supports auto-advance / auto-end behavior.
- `Simulation.cs` still supports variable `MonthsPerTurn`.
- `data/tuning.json` still has a 2-month default and a 3-month cap.
- `World.cs` still contains the old institution `Stake` model and old "aurei in your purse" state/comments.
- Existing institution code still assumes direct investment/stake thresholds in places.
- Current approved design is **1 month per turn, no normal auto-end, invitation/sponsorship institutions, explicit commercial terms, and a much broader capability model**.

Do not treat the current executable as proof the new live design is implemented.

---

## 3. Milestone decision: P0 → P1

Corey explicitly moved the project into **P1**.

The previous "five real human testers" gate is deferred until a later graphical/UI milestone, because feedback on a text/console presentation would not adequately represent the intended game.

### P0 is no longer blocked by human testing

Human testing should be scheduled once there is:

- representative navigation/UI,
- graphical presentation,
- mature menu organization,
- more representative pacing,
- a build that resembles the intended player experience.

### P1 definition

**P1 = playable vertical-slice architecture.**

A fresh player should be able to:

**arrive in AD 155 → establish themselves → form recurring relationships → complete explicit paid work → begin a Grand Challenge → earn institutional access through people → prepare the machine → make a meaningful jump → see persistent consequences**

without relying on ad hoc console flow.

---

# 4. Locked player-facing information architecture

The mature menu should organize depth instead of deleting it.

Primary sections:

1. **Now** — current month, immediate decisions, interruptions.
2. **Projects** — commissions, experiments, inventions, Grand Challenges.
3. **People** — relationships, households, obligations, NPC status.
4. **Institutions** — guest/member status, sponsors, invitations, obligations.
5. **Knowledge** — theories, discoveries, principles, experimental evidence.
6. **Civilization** — capabilities, adoption, propagation, bottlenecks.
7. **Machine** — time-machine state, mystery, readiness, jump controls.
8. **Journal** — history, prior choices, causal echoes, prior jumps.

Within technology/project menus, use organization such as:

- Active
- Available now
- Blocked
- Emerging
- Archived / completed

The 10×1000 design stress test flagged "too many active systems," but Corey's response was important: **the answer is better information architecture, not flattening the game.** Too many systems shown at the same level is the problem.

---

# 5. Calendar, turns, Attention

## Locked calendar

- **1 turn = 1 calendar month.**
- Task durations are authored in `durationMonths`.
- Machine assessment = **2 months**.
- No player-facing variable turn-length selector in normal play.
- Prose does not need to say "month 1 of 2" unless natural; UI can show progress.

## Attention

- Standard player Attention remains **4 total per month**.
- Example header:
  `Attention: 2 free / 4 total · 2 reserved — Machine Assessment`
- Information-only actions: normally 0 Attention.
- Normal meetings: 0 normal Attention unless a specific commitment says otherwise.
- Future commitments must be visible.
- No silent overbooking of future Attention.
- Spending the last point of Attention **does not automatically end the month**.
- Explicit **End Month** is the normal calendar advance.
- Optional fast-forward may exist, but it must stop on meaningful interruptions/events.

---

# 6. Opening and time-machine premise

## Locked opening tone / facts

Opening line:

> **"The machine stops screaming before you do."**

Key sequence:

- protagonist remains sweating/shaking after the test failure,
- this was a **non-travel systems test**; no time travel was intended,
- controls are dead,
- hatch opens,
- he expects a modern environment / concrete,
- gets dirt,
- mule-cart driver shouts in Latin,
- protagonist: **"Thank God I took that in college."**
- realization: **"Ancient Rome. Not ruins. Alive."**

Do not front-load:
- plague,
- jump economics,
- range explanations,
- Echo terminology,
- tutorial-system exposition.

Gold is scavenged from **machine components**, not from a convenient pouch.

## Machine architecture

Current conceptual systems:

- Temporal Field Core
- Geometry / Containment
- Temporal Reference
- Spatial Reference
- Navigation Computer
- Pulse Storage
- Thermal Management
- possible Catalyst

The **Temporal Field Core is intact**. Roman capability can repair/support surrounding systems over time.

### Central mystery

Rome AD 155 appears to have been **actively selected by a damaged reference system**.

The machine later reveals `155-03-ROMA` and appears to have **received Rome**, not simply chosen it through normal navigation.

An unresolved identifier / reference state, **R-17**, appears repeatedly.

Later live events:

- `REFERENCE REQUEST ACCEPTED — R-17`
- `LINK QUALITY: DEGRADED`
- `REMOTE REFERENCE ACKNOWLEDGED`
- `R-17 ACTIVE`
- later deliberate contact returned:
  - `REQUEST RECEIVED`
  - `SOURCE: R-17`
  - **`DO NOT JUMP`**

The original AD 155 accident contains a similar handshake pattern immediately before the Rome lock.

The protagonist later reconstructs that `R17` appeared in an obscure pre-accident mathematical paper about temporal-reference stability, including the phrase **"reciprocal lock."**

Do not explain R-17 too soon.

---

# 7. Narrative and dialogue rules — hard constraints

This is one of the most important areas of feedback.

## Global rule

> **Say it once.**

Live scene structure:

> **scene → necessary mechanical state change → choices**

Do not narrate the significance of dialogue/action after the player can already see it.

### Banned narrator patterns

Avoid patterns like:

- "That lands."
- "That gets his attention."
- "That's a shift."
- "There it is."
- "That changes things."
- "That changes the value."
- "This isn't just..."
- "That matters."
- "This matters."
- "That tells you something."
- "The real issue is..."
- "Not a question."
- "No flourish."
- "Just yes."
- "That is enough."
- "He understands."
- "You can tell he means it."
- "The room changes."
- "That sits between them."
- "Something in his voice tells you..."
- "He looks at you differently now."

Also avoid clipped screenwriter/title-card fragments such as:

> Not a contract.  
> Not yet.  
> Just a lead.

The user specifically flagged this presentation as weird. Write it as normal prose.

## Dialogue realism

Conversations must not exist only to convey systems.

Characters may:

- interrupt,
- misunderstand,
- joke,
- hesitate,
- deflect,
- change subject,
- refuse,
- disagree personally,
- leave things unsaid,
- walk away,
- answer a different question.

NPCs should not simply explain the player's idea back to him.

Natural examples:

- "That won't work."
- "Who pays?"
- "My apprentice will ruin it."
- "Come back after trying it."
- "My father did that."
- "Why teach my competitor?"
- "Fine. You buy the iron."

## Historical voice

Romans speak **concretely**, not in modern managerial abstraction.

Avoid Roman NPC phrasing like:

- rollout
- implementation
- performance
- process
- capacity
- metrics
- success criteria
- workflow
- scalable
- shutdown procedure
- inspection standard

A Roman millwright can say:

- "Move that lever."
- "That bearing runs hot."
- "Tie your sleeve."
- "Grease it before dawn."
- "Or buy another bearing."

The protagonist may internally recognize:
- preventive maintenance,
- reliability engineering,
- stress concentration,
- process control,
- feedback,
- closed-loop control,
- etc.

The future inventor performs the abstraction; Roman craftsmen supply real practical knowledge.

---

# 8. Roman culture is a primary pillar

Historical life is not flavor sprinkled around engineering. It is one of the central game experiences.

Integrate through routine, conflict, relationships and schedules:

- baths,
- shrines, vows, sacrifice, omens, festivals, funerals, temples,
- meals, markets, taverns, patron dinners,
- status, seating, clothing, client ties,
- freedmen, slaves, apprentices,
- spouses, children, inheritance, marriage,
- credit, bargaining, imports, shortages,
- courts, contracts, witnesses, property, debt,
- military/veteran/procurement life,
- fountains, graffiti, vendors, smells/noise,
- education, scribes, tutors, rhetoric,
- citizenship/class,
- races, gladiators, venationes, theater,
- dice/board games, music, poetry,
- medicine, temple healing, home remedies, empirical observation.

Use Latin terms sparingly.

### Culture warning

Do not turn this into a checklist of "Roman content drops." It should happen because people live there.

---

# 9. Games, spectacles and betting

Games/spectacles are civic-life events first, betting second.

Possible activities:

- Circus races,
- games/festivals,
- theater,
- ceremonies,
- baths,
- banquet/dice gambling.

Optional wagering uses imperfect information from:

- stable workers,
- associates,
- bookmakers,
- merchants,
- faction fans,
- patrons,
- observation.

Rules:

- source reliability/bias should be hidden,
- odds move,
- favorites can lose,
- mishaps occur,
- no exposed true probabilities,
- no risk-free betting income,
- event remains socially valuable without a wager.

Good prior model: early Circus scene with Livia supporting Greens; player bet 25 on Blues at 5:2; crash; Greens won; player lost 25.

---

# 10. Early reputation / intrigue

For roughly the first six months:

- protagonist remains socially minor,
- foreignness is not instantly world-famous,
- months 1–2: stranger / useful / paying / troublesome,
- months 3–4: a few locals know him,
- months 5–6: adjacent circles know "foreign craftsman / Gaius associate",
- after six months: visible results can generate referrals/invitations,
- year+: broader institutional/patron access if earned.

Early intrigue is local/personal/commercial/cultural.

Do not jump immediately to senators, assassinations, imperial conspiracies, or grand political plots.

The machine mystery remains a quiet background thread.

---

# 11. Recurring characters and NPC autonomy

Every recurring character should have:

- personal goal,
- vulnerability,
- people they care about,
- someone they distrust/resent/compete with,
- household/relationships,
- status and obligations,
- opinions/biases,
- outside interests,
- distinct voice,
- offscreen development.

Their lives continue without the player.

NPCs can:
- cancel,
- get sick,
- marry,
- lose money,
- quarrel,
- change jobs,
- copy an invention,
- leave,
- fail,
- independently improve an idea,
- derail the player's optimal plan.

### Important current characters

**Gaius Fabius Crispus**  
Craftsman collaborator. Proud, practical. Eventually reduces bench work. Does not want expansion for its own sake.

**Livia**  
Gaius's wife. Green-faction supporter. Sharp/teasing. Family tradition and household perspective. Important human counterweight to engineering seriousness.

**Marcus Fabius Tertius**  
Gaius's sister's son. Father was a clerk who wanted to own a shop. Marcus initially tries to become the man his father wanted. Leaves Gaius for Priscus's shop for better pay/own apprentice; later develops as a supervisor/manager; in the future becomes founder of the Fabian Works.

**Diodoros**  
Porter who wanted responsibility/pay. In one simulated timeline injured hand in warehouse fire; later moved toward repair/delivery coordination.

**Titus Aelius Serenus**  
Physician. Blunt, evidence-oriented but period-grounded. Fever-case records develop toward comparative medical observation.

**Lucan**  
Older smith. Practical collaborator. Quote: **"Do not become a man who only has ideas."**

**Felix**  
Commercial/workshop connector. In future history remembered as the person who connected shops rather than as a grand inventor.

**Aulus Septimius Crispus**  
Millwright. Practical maintenance/reliability instincts. His craft lineage later becomes highly influential.

**Decimus Vettius Priscus**  
Bronze worker / workshop owner; takes Marcus.

**Sextus Nerius**  
Finishing-shop owner affected by powered production and labor transitions.

Other recurring names have appeared; avoid accidental duplicate surnames (too many Varros existed in exploratory play).

---

# 12. Institution system — current locked model

The old model of buying a percentage stake in an institution is no longer the desired player-facing system.

Consequential access normally follows:

> **Aware → knows member → guest → invited back → sponsored candidate → member → office/leadership**

### Invitation gate

A forward invitation needs all of:

1. **specific inviter**
2. **existing relationship**
3. **relevant work in that domain**
4. **demonstrated usefulness**
5. **inviter willing to accept social/reputational risk**

General fame does not qualify.

A famous engineer does not automatically get recruited by physicians, politicians, religious bodies, or scholars.

### Historical nuance

Some associations can support direct application/open participation when historically appropriate. But important/prestigious/consequential groups should usually be relationship-driven.

### Current live example

The Ostia craftsmen/merchant association is warranted because the protagonist:

- worked for years around Ostia,
- built/deployed powered workshops,
- worked with Felix/Aulus and others,
- solved actual commercial/technical problems,
- affected neighboring shops,
- repeatedly attended meals/gatherings,
- had Felix willing to sponsor him.

The medical "circle" through Serenus was intentionally downgraded: Serenus can bring the player to a professional supper because the fever records are relevant, but that does **not** automatically become a second formal institutional recruitment track.

### Institutions should create obligations

Examples:

- dues,
- meals,
- funeral contributions,
- mutual aid,
- dispute support,
- referrals,
- reputation,
- apprenticeship/training,
- technical standards,
- inspections,
- arbitration.

They are not buff dispensers.

---

# 13. Work, economy and negotiation

Remove generic consulting grind as the primary loop.

Preferred work progression:

> **encounter → help/diagnosis → prototype → paid commission → repeat work → scaled opportunity**

Initial work can be unpaid if clearly stated:

> `Pay: none initially · may lead to paid commission`

### Explicit money rule

Before substantial technical work begins, state:

- who pays,
- approximate/agreed payment,
- who pays materials,
- whether it is self-funded R&D,
- whether there is profit sharing,
- whether it is a favor/non-cash exchange.

No invisible free labor.

### Negotiation

Negotiation is not free upside.

Counterparties may:

- stand firm,
- reduce scope,
- walk,
- remember repeated pressure,
- offer non-cash value,
- give one-shot offers,
- resent the player.

Prior useful example: wealthy merchant wanted five-year exclusivity; player negotiated priority/training; merchant walked.

### Ledger

Current long live simulations did not maintain a reliable exact cash balance. **Do not invent a current cash number.**

Known older state: around June AD 156 there was **1,076.5 denarii + earlier unreconciled cash**, with machine gold/materials held separately/unliquidated.

P1 must replace this ambiguity with a clean ledger.

---

# 14. Invention philosophy

Core rule:

> **Small problems prove principles. Big projects change capability.**

Two layers:

1. **Practical projects**
   - prove principles,
   - build trust,
   - create income,
   - train craftsmen,
   - establish repeatability.

2. **Grand Challenges**
   - create durable civilizational capability.

### Once bottleneck understood

> **Once a Grand Challenge bottleneck is understood, stop repetitive micro-scenes; advance to project/institution/capability change.**

This rule is reinforced strongly by simulated critic feedback.

### Invention process

> **Problem → Observation → Principle → Prototype → Failure → Refinement → Craft adaptation → Repeatability → Adoption → Spread**

One working prototype is not automatically a civilizational invention.

---

# 15. The protagonist: future polymath, not magic

Mental model:

> **Leonardo da Vinci with the unfair advantage of having actually seen the future.**

He knows broad future frameworks and technology ladders.

He understands:

- mechanics,
- materials,
- stress concentration,
- load paths,
- fatigue/fracture,
- hardness/toughness,
- heat treatment,
- thermal expansion,
- heat transfer,
- energy losses,
- standardization,
- controlled experiment,
- failure analysis,
- physics,
- electricity,
- chemistry,
- medicine,
- manufacturing,
- systems engineering,
- control theory,
- modern mathematics.

But knowing is not making.

His constraints are:

- Roman materials,
- purity,
- metallurgy,
- supply,
- available tools,
- machinability,
- craftsmen,
- labor,
- money,
- transport,
- institutions,
- maintenance,
- cultural acceptance.

Roman craftsmen often know practical local material/tool realities better than he does.

NPCs can be wrong. The protagonist can also be wrong about what is economically sensible in Roman conditions.

---

# 16. Mandatory Roman-baseline technology check

This was repeatedly corrected in live play and is now a hard design rule:

> **Do not "invent" something Rome already had. Start from the best historically plausible Roman/Hellenistic practice and push it forward.**

For every invention/technology scene ask:

1. What already exists in AD 155/163?
2. What do top practitioners already know?
3. What does the protagonist know that they do not?
4. What manufacturing/social/resource bottleneck prevents the next step?
5. Is the leap an improvement, formalization, combination, scaling, standardization, or new application?

Examples:

### Metallurgy
Do **not** invent heat treatment, bronze, brass, or practical material selection.

Push toward:

- process records,
- controlled composition,
- repeatable alloy recipes,
- grading,
- purpose-specific material selection,
- controlled heat treatment,
- spring/tool materials,
- pressure-safe materials,
- cross-workshop reproducibility.

### Hydraulics
Do **not** invent pumps, valves, siphons, float control or water wheels.

Push toward:

- pressure-rated sealing/fittings,
- hydraulic actuation,
- accumulators,
- presses,
- calibrated pressure regulation,
- relief systems,
- multi-actuator control,
- servo-like feedback.

### Measurement
Do **not** portray Romans as unable to measure precisely.

Push from:

> local skilled standards → shared standards → tolerance thinking → traceability → inspection → process control → improved tooling → cross-workshop interchangeability → machine tools

---

# 17. Full technology/capability architecture

The technology system should be a **network**, not a flat or strictly linear tech tree.

Every major technology should separately track ideas such as:

- Theory understood
- Demonstrated
- Prototype built
- Reproducible
- Manufacturable
- Economical
- Adopted
- Institutionalized / survives the inventor

The player can know a theory while civilization lacks the capability to exploit it.

Example:

> Electromagnetic induction: known by protagonist  
> Generator capability: unavailable because wire, insulation, magnets, machining, rotational power or instruments are insufficient

### Highest-leverage capability multipliers

- Measurement & metrology
- Mathematical language
- Scientific/experimental method
- Knowledge reproduction
- Materials engineering
- Precision manufacturing / machine tools
- Controlled power
- Feedback/control
- Industrial chemistry
- Instrumentation

See `03_TECHNOLOGY_CAPABILITY_LADDER.md` for the full ladder.

---

# 18. Scene pacing

Current strongest rule:

> **Never allow the same activity type to dominate more than two consecutive meaningful scenes unless the player explicitly chooses to remain focused. Even then, the world may intrude.**

Scene categories:

- Engineering
- Personal/relationships
- Roman life
- Work/economy/reputation
- Machine mystery
- City/historical events
- Exploration
- Institutions/politics

After two consecutive scenes of a category, strongly deprioritize a third.

This must become an **implemented deterministic router**, not just a writing reminder.

Natural interruptions include:

- illness,
- festival,
- family obligations,
- bad copy,
- accident,
- fire,
- plague,
- patron invitation,
- betrayal,
- funeral,
- marriage,
- birth,
- disappearance,
- stolen credit,
- unintended spread,
- machine event.

---

# 19. Jump design

The jump is the game's emotional center.

### Readiness language

If the machine is ready:

> **"The machine is ready. You can leave now, or remain in Rome and continue your work."**

Do not discourage early jumping with a hidden gate.

Early jumps are valid:
- thinner history,
- more unresolved threads,
- more baseline outcomes,
- more upside left unrealized.

Later jumps usually have richer interconnected consequences because more history exists — not because they must be "better."

### Echo mixture

Major jumps should combine:

- physical/technical echoes,
- human/personal echoes,
- institutional echoes,
- unintended echoes,
- distortions/misattribution,
- failures/disappearances.

The player reconstructs causal links; `why` can reveal details.

---

# 20. Live-play historical timeline / design evidence

This is conceptual live-play evidence, not exact executable state.

## AD 155–156 early period

- protagonist arrives in Rome during non-travel systems test failure,
- local/low-status relationships form,
- machine support systems are diagnosed,
- practical workshop threads emerge,
- early invention examples include gauges/standards, wear surfaces, hoist lock, pump fittings, heat wrap, cart axle fitting, foreman coordination,
- machine destination/reference anomalies begin.

### Heat-wrap correction
Inner packed fiber directly takes heat / fills gaps / insulates. Outer clay/mineral holds/protects and may add insulation; it does not magically prevent heat from reaching fiber. Thin outer shell performed better than thick shell. Field trial limited to high-use rooms before scaling.

### Foreman principle
Core lesson: **authority and responsibility move together.**

### Hoist
Geometry should carry load rather than friction. Prototype wear issues led toward deeper seating. Do not micro-tune forever.

---

## November 156 → June 158 auto-simulated design timeline

Highlights:

- Marcus's mother recovers.
- boring rig delegated.
- festival/family scenes.
- Decimus Vettius Priscus enters network.
- machine destination-trace issue.
- three-cylinder repeatability challenge.
- 2/3 interchangeable.
- Marcus/Gaius family debt argument.
- Felix production work delegated.
- Ostia exploration suggests water/animal-power leverage.
- machine finds `155-03-ROMA`.
- warehouse fire; Cassianus loses goods; Diodoros hurts hand.
- bad imitations appear.
- craft/merchant gathering begins institutional access naturally.
- machine appears to have **received Rome**.
- Marcus father-death anniversary/private history.
- production improves to 5/6 pass with process catching failure.
- Diodoros shifts from porter toward coordination.
- water mill points toward powered workshops.
- Marcus leaves Gaius for Priscus but remains in network.
- temporal reference locks; R-17 remains unresolved.

---

## July 158 → September 159

- `REFERENCE REQUEST ACCEPTED — R-17`
- Ludi Romani / Gaius-Marcus partial reconciliation.
- replaceable bearing block at Ostia.
- bad competing "foreign-style" fittings.
- Serenus begins fever notes.
- structured notes push Scientific Method/Public Health.
- `LINK QUALITY: DEGRADED`
- exclusive-rights deal lost when merchant walks.
- fountain/court issue uses fever patterns cautiously.
- bearing block propagates: one bad mutation, one better mutation.
- Gaius/Livia leave for family estate death.
- line-shaft test proves one source can drive multiple tools.
- `REMOTE REFERENCE ACKNOWLEDGED / R-17 ACTIVE`
- Marcus starts failure rack at Priscus.
- machine has forward displacement available; player stays.

---

## Powered-workshop arc, late 159–160

Goal: one power source driving multiple real production tasks.

Work included:

- water wheel/main shaft,
- replaceable bearings,
- belt/gearing,
- boring,
- grinding,
- disengagement,
- maintenance/serviceability.

Aulus Septimius Crispus joins as millwright.

Key lesson: stop turning Aulus into a modern safety/operations manager. He should contribute practical craft observations; protagonist recognizes the systems abstraction.

By January AD 160:
- powered boring + grinding are productive,
- labor/social consequences begin,
- Sextus Nerius asks what powered machinery means for his 12-man finishing shop,
- younger workers' paths into skilled work become a social issue.

---

## Institution redesign live arc, AD 160–161

The initial post-redesign simulation generated too many invitations. Corey corrected it.

Final rule:

> Invitation must be causally earned in-domain.

The Ostia association remains the legitimate formal track because of years of work and relationships.

Medical gatherings through Serenus remain informal/professional unless later work earns more.

September AD 161:
- Felix puts his name behind the protagonist,
- association admits him,
- obligations include dues, meals, mutual aid, funeral contributions, reputation,
- benefits include referrals, introductions, dispute support,
- no automatic leadership or generalized influence.

---

## Association dispute, AD 162

A copied powered finishing rig creates two separate disputes:

1. a worker leaves Sextus for Lucius before an alleged one-year obligation is complete,
2. Lucius copied features of Sextus's paid adaptation.

The worker obligation is confirmed by witnesses; remaining tools/training value is settled; worker remains with Lucius.

The machine-copy dispute reveals existing informal custom:

- common craft knowledge belongs to the trade,
- direct copying of another man's paid work can create an obligation,
- independent improvements are different,
- exclusivity must be agreed rather than assumed.

Resolution: Lucius pays limited compensation for the copied arrangement; does not grant Sextus perpetual exclusive ownership.

This is good institution gameplay: custom, reputation, precedent and practical dispute resolution rather than modern IP law pasted onto Rome.

---

## Technology expansion, AD 162–164

Corey explicitly pushed the invention ladder beyond small workshop optimization.

### Metallurgy
Corey corrected the design when heat treatment was treated as if newly discovered.

Locked interpretation:
- Roman smiths already know practical heat treatment/material behavior,
- protagonist formalizes, measures, reproduces, combines and applies.

Alloy direction:
- catalog existing alloys,
- controlled composition,
- purpose-specific recipes,
- bearing bronze,
- valve/fitting alloys,
- repeatability.

### Hydraulics
Corey asked whether Roman hydraulics/valves already existed. Answer: yes; the game must build on them.

Development:
- press/force multiplication,
- accumulator,
- metered control valve,
- pressure safety,
- feedback/control concepts.

Spring relief valve was reconsidered: weighted/calibrated approaches may be more appropriate before highly repeatable spring metallurgy.

### Mathematical/physics formalization
Player pauses hardware work to connect practical problems into a shared engineering language:
- force,
- moment/torque,
- work,
- power,
- piston area/pressure,
- pulley ratios,
- shaft speed,
- eventually energy/conservation/control.

Do not portray geometry/mechanics as absent in Rome. Connect and formalize.

### By March AD 164 conceptual state
Self-reinforcing chains exist in:
- measurement,
- materials,
- machine tools,
- powered workshops,
- hydraulics,
- mathematics,
- documentation,
- early chemistry.

Promising future tracks:
- mechanical governor,
- industrial chemistry,
- optics/instrumentation,
- electrical experiments,
- steam,
- higher mathematics,
- mass knowledge reproduction,
- medicine/public health.

---

# 21. Major jump: March AD 164 → Rome AD 247

The player chooses to jump despite the earlier `DO NOT JUMP` warning.

Arrival is approximately **AD 247**, under Philip the Arab.

Immediate signs:
- Rome is still Rome but industrially altered,
- steam-driven workshop machinery exists,
- widespread shaft power,
- more iron construction,
- technical Latin has evolved,
- city is larger, dirtier, louder,
- coal/smoke/industrial noise,
- plaque names **MARCUS FABIUS TERTIUS — Founder**.

### Personal echoes discovered

**Marcus Fabius Tertius**
- becomes a master,
- organizes workshops around shared specifications and powered machinery,
- under Commodus the network expands into the **Fabian Works**,
- dies in his seventies.

**Gaius Fabius Crispus**
- stays in Rome,
- respected maker,
- deliberately refuses expansion beyond household scale,
- workshop passes to a younger apprentice rather than Marcus.

**Livia**
- outlives Gaius,
- administers household property,
- supports two apprentices from family funds,
- not famous, but meaningful human legacy.

**Lucan**
- remembered through "Lucan's comparisons" for tool metal and bearing bronze,
- becomes a method/tradition rather than a public hero.

**Titus Aelius Serenus**
- fever records are copied, expanded and argued over,
- later physician cites him for comparative household records while disagreeing with some conclusions,
- becomes an argument/tradition rather than a sainted founder.

**Felix**
- repeated in contracts/workshop partnerships/disputes,
- remembered as **"Felix, who connected the shops."**

**Sextus**
- family business expands then splits,
- one branch wealthy, another collapses after debt expansion,
- later quote attributed to a son:
  > "My father bought speed. He did not buy wisdom."

### Association echo

Old craftsmen/merchant association evolves into a larger body handling:

- contracts,
- mutual aid,
- inspection,
- training,
- dispute settlement,
- written standards,
- approved reference measures,
- apprenticeship records,
- accident funds,
- trusted-workshop lists,
- eventually inspection seals for pressure equipment/powered machinery.

No single "founding law"; it evolves through accumulated precedents.

The protagonist is not mythologized as universal founder. His name appears only occasionally, often as "the foreign craftsman Felix once sponsored," with later identity uncertainty.

### Aulus millwright tradition

Aulus's habits survive in workshop manuals and craft sayings:

- brace long shafts close to joints,
- keep bearings accessible,
- allow sections to disconnect for repair,
- avoid hidden single-point load,
- leave adjustment room,
- build for ordinary craftsmen to service,
- test under real load.

Later writers systematize these as rules for machinery that must keep working.

By AD 247:
- standardized bearing blocks,
- sectional shafts,
- inspection routines,
- maintenance intervals.

A later millwright, **Aulus Septimius Varro**, is credited with formalizing governor mechanisms; records say his grandfather learned under "Celer's men." It may be a lineage of practice, not necessarily direct blood descent.

### Current live-play location

Meta testing interrupted the live game here.

**Current conceptual live state: Rome, AD 247.**

The last offered directions were:
1. trace governor tradition / automatic control,
2. inspect steam machinery,
3. inspect Fabian Works training,
4. learn whether original Aulus understood his later influence,
5. return to the machine / R-17.

Do not confuse this conceptual live session with executable save state.

---

# 22. Testing evidence — headline

Detailed index is in `05_TEST_EVIDENCE_INDEX.md`.

### Old Claude blind agents, seeds 611–620
Six finished, mean ~7.3; five ≥7; all would jump again. Main feedback: strong arrival/jump/causal payoff, weak mid-era grind, hidden plague cost, menu friction, institution/control wording, early-jump thinness.

### 50 actual executable runs, seeds 701–750
- 50/50 first + second arrival
- 0 crashes
- 0 unknown commands
- 0 dead ends
- first departure AD 159–172, median 166
- second arrival AD 209–257, median 236

Harness had an incorrect 250-year-jump validation assumption.

### 100 actual executable blind-style sessions, 10×10
- 100/100 valid
- 0 harness failures
- 63/100 reached two jumps
- heuristic personas, not humans or independent LLM agents

### Follow-up executable 100×10
- 100/100 deliberately driven to two jumps
- **coverage only; not comparable improvement over 63/100**
- confirmed old executable still had:
  - pouch gold,
  - generic work,
  - direct institution buying,
  - menu batching,
  - no true betting loop,
  - old turn-centric presentation.

### 10×10 humanized current-design review
- mean 8.45
- 95/100 would continue
- 96/100 ≥8
- institution access 8.94
- Roman culture 8.68
- concerns: modern business language, early intrigue escalation, state visibility, bargaining needing real "no", more private life.

### 100 simulated video-game critics through two jumps
- mean 8.12
- median 8.15
- 67/100 ≥8
- 78/100 continue
- first-jump payoff 7.94
- second-jump payoff 8.62
- top criticism: dialogue/exposition, technical-scene monotony, pacing must be systemic, micro-invention steps too long, NPC autonomy.

### 10×1000 current humanized design simulation
10,000 modeled testers; **not humans / not independent executable agents**.

- overall 8.39
- median 8.45
- 70.2% ≥8
- 62.5% continue after two jumps
- jump payoff 8.71
- historical authenticity 8.58
- technology ladder 8.41
- invention system 8.36
- player agency 8.34
- character depth 8.24
- institution system 8.17
- pacing 7.82

Top risks:
- engineering dominates 19.0%
- too many active systems 17.8%
- functional dialogue 15.0%
- Roman baseline protection 13.7%
- exposition 11.9%
- tech acceleration too fast 10.9%
- institution invitation gates 10.5%
- economy ledger 10.1%

Interpretation from Corey: do **not** flatten the system depth simply because the UI can feel overloaded. Organize it better.

---

# 23. Old open L-items / compatibility cleanup

Known open items from the prior P0 queue:

- **L12** — plague costs visible before choice
- **L13** — distinguish personal votes / institution balance
- **L14** — grammar/menu title truncation
- **L16** — forge provenance wording
- **L17** — second-arrival Subura population baseline
- **L18** — seniority-cap explanation

A1–A10 were previously fixed; do not regress them.

When P1 systems supersede an old system, explicitly decide whether an old L-item is still relevant rather than mechanically fixing obsolete behavior.

---

# 24. P1 Sprint 1 code already prepared but not committed

Prepared additive files:

- `P1Architecture.cs`
- `P1SceneRouter.cs`
- `P1Economy.cs`
- `P1InstitutionAccess.cs`
- `P1ArchitectureTests.cs`
- `P1IntegrationTests.cs`
- `P1_SCOPE.md`
- `P1_SPRINT1_INTEGRATION.md`

They are packaged in:

`artifacts/P1_SPRINT1_INTEGRATION.zip`

### What they contain

- 8-section menu enum
- scene categories and pacing state
- deterministic scene router using project `Rng`
- repeated-category soft penalty after two scenes
- explicit focus override
- world interruption exemption
- project stages and funding terms
- economy ledger and project-accounting entries
- relationship-first institution access state
- invitation causal-gate validation
- unit tests

### Validation status

Not committed because the preparation environment did not have a .NET SDK.

**First Claude task: apply/review → `dotnet test` → fix only as needed → small commit.**

---

# 25. Immediate P1 implementation order

Recommended order:

1. **Apply and validate P1 Sprint 1 additive foundation.**
2. Fixed **1-month turns**.
3. Remove normal auto-end.
4. Add explicit **End Month**.
5. Add fast-forward that stops on meaningful interruptions.
6. Integrate `ScenePacingState` into `World`/simulation.
7. Integrate explicit `EconomyLedger`.
8. Integrate `ProjectState`/commercial terms.
9. Migrate one real paid commission end-to-end.
10. Add institution access collections and begin retiring direct stake/buy-in UI.
11. Build hidden technology/capability graph.
12. Add Roman-baseline metadata/checks to technology content.
13. Add NPC autonomous project progression.
14. Add jump-echo model covering personal + technical + institutional + unintended effects.
15. Build player-facing menu/view models around Now/Projects/People/Institutions/Knowledge/Civilization/Machine/Journal.
16. Continue narrative/dialogue cleanup.
17. Add resource/economic bottlenecks to major Grand Challenges.
18. Synchronize SYSTEMS → GDD Appendix A → tuning → DECISIONS → content/tests.

---

# 26. What not to do

Do not:

- treat old stake-buy-in institutions as current intended design,
- reintroduce variable normal turn lengths,
- auto-end a month because Attention hits zero,
- invent exact current cash after unreconciled live simulations,
- turn Romans into tutorial narrators,
- make Roman craftsmen ignorant of technologies they historically knew,
- turn every relationship into a contract ladder,
- make every institution start inviting the protagonist,
- make the protagonist famous in the first months,
- overexplain emotional meaning after dialogue,
- let engineering monopolize scene flow indefinitely,
- treat a one-off prototype as durable civilization capability,
- make future technology appear simply because the protagonist knows the theory,
- call simulated persona testing "real human testing."

---

# 27. One-sentence north star

> **The player is a future polymath trying to accelerate a civilization that is already sophisticated, while people, institutions, materials, culture, money, history and time itself determine what survives him.**
