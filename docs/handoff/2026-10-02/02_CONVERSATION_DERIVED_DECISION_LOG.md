# Conversation-Derived Decision Log
**Purpose:** chronological digest of material project decisions from the accessible ChatGPT project conversations through 2026-10-02.

This is **not a byte-for-byte export of the ChatGPT UI**. It is the complete project-conversation digest available in this handoff: owner decisions, corrections, live-play discoveries, test interpretations, implementation state, and handoff requirements. It intentionally excludes hidden chain-of-thought.

---

## September 25–26 — Original GDD review and market/design framing

The game begins as a complete pre-prototype GDD:
- iOS/iPadOS/Mac,
- Unity 6,
- deterministic engine-independent C# sim core,
- inventor stranded in AD 155 Rome,
- multiple historical eras,
- limited Attention,
- civilization-scale consequences,
- time machine / fuel puzzle,
- one-time unlock monetization concept.

Core pillars affirmed:
- Every Choice Echoes,
- Knowing Is Not Making,
- One Person in a Large World.

A P0 "Butterfly Test" is proposed before larger prototypes: prove that the first jump produces a strong emotional payoff and traceable consequences.

---

## September 27–29 — P0 executable, blind testing and bug/clarity queue

The repository and branch become the working implementation target.

Key project discipline:
- deterministic RNG,
- tests,
- event logs,
- formulas documented,
- tuning centralized,
- Corey approves design changes.

Blind and executable tests reveal:
- jump payoff strong,
- causal `why`/Echo direction strong,
- mid-era grind,
- hidden costs,
- menu friction,
- institution semantics,
- turn skipping/auto-end surprises,
- stale text/arrival contradictions.

A1–A10 fixed. L1–L18 tracked; later known open set includes L12–L14 and L16–L18.

`BUTTERFLY_P0_IMPLEMENTATION_SPRINT_2026-09-29_v3.md` becomes the best pre-latest implementation package.

---

## Late September — Major live redesign

### Opening
User rejects tutorial-heavy opening.

Locked:
- non-travel test,
- story first,
- "The machine stops screaming before you do.",
- dead controls / dirt / mule cart / Latin realization,
- no plague/jump-range/Echo front-loading,
- machine gold scavenged from components, not pouch.

### Calendar / Attention
User approves:
- 1 month per turn,
- explicit Attention visibility,
- machine assessment 2 months,
- no auto-end when Attention hits zero,
- explicit End Month.

### Institutions
Old direct "buy into institution" system rejected.

New direction:
- relationship first,
- guest,
- repeat invitation,
- sponsorship,
- membership,
- office.

Later refined further:
- invitations must be domain-relevant and causally earned,
- general fame is not enough.

### Work
Generic consulting grind replaced by:
encounter → help/diagnosis → prototype → paid commission → repeat work → scaled opportunity.

Negotiation must be able to fail.

### Roman life
Roman culture becomes a primary pillar rather than flavor:
households, baths, religion, patronage, markets, food, festivals, games, law, class, apprentices, slavery/freedmen, education, medicine, commerce.

### Narrative
User repeatedly flags robotic/explanatory phrasing.

Hard rule becomes:
scene → necessary state change → choices.

Interpretive narrator filler is banned.

---

## Live play — early technical threads

The player works with Gaius/Lucan/Marcus and others.

Examples explored:
- repeatable workshop standards,
- replaceable wear surfaces,
- hoist locking geometry,
- standardized pump fittings,
- heat-retention wrap,
- cart axle fitting,
- foreman coordination.

Corrections:
- heat-wrap material physics fixed,
- hoist should use geometry rather than friction,
- Roman craftsmen should contribute real expertise,
- protagonist should not be the only intelligent person.

The invention philosophy evolves to:
**small problems prove principles; big projects change capability.**

---

## Machine mystery development

Machine diagnostics produce:
- destination/reference anomalies,
- `155-03-ROMA`,
- indication machine received Rome,
- R-17 fragments,
- accepted reference request,
- degraded link,
- remote acknowledgement.

Player stays rather than jumping when the machine first becomes capable.

---

## Powered workshops / Grand Challenges

The invention system is reframed around Grand Challenges.

Powered Workshop work includes:
- water power,
- line shafts,
- bearings,
- boring,
- grinding,
- belts/gearing,
- disengagement,
- serviceability.

Aulus is introduced as practical millwright.

User catches a major tone error: Aulus starts speaking like a modern safety/process manager.

Correction:
- Romans speak concretely,
- protagonist abstracts.

This becomes global.

---

## Labor/social consequence scene — January AD 160

Sextus Nerius sees powered machinery affect a 12-man finishing shop.

Question changes from "does the machine work?" to:
**what happens to workers and training paths?**

This is used as the model for moving invention consequences beyond technical optimization.

---

## R-17 warning — AD 160

Player investigates R-17.

Deliberate contact produces:

`REQUEST RECEIVED`
`SOURCE: R-17`
`DO NOT JUMP`

Comparison to the original AD 155 logs shows a similar handshake pattern before the Rome lock.

Player shuts the machine down and returns to Rome.

---

## Institution invitation discussion — AD 160–161

User says institutions disappeared from live play.

The system is brought back, but first simulation overcorrects and produces too many invitations.

User challenges:
- are all these invitations warranted?
- what work did I do in that area to deserve them?

Final gate:
**specific inviter + relationship + relevant work + usefulness + sponsor risk**

Ostia craftsmen/merchant association qualifies.

Medical world does not yet qualify as a formal institution track; Serenus may invite player to professional discussions.

In September AD 161:
- Felix sponsors player,
- player becomes member,
- obligations and social history are explicit.

---

## Association dispute — AD 162

Player hears dispute involving Sextus/Lucius:
- worker obligation,
- copied powered-finishing arrangement.

Player asks what association custom already says before proposing modern rules.

Result:
- free worker can still owe debts/terms,
- witnesses matter,
- common craft knowledge is distinct from direct copying of paid development,
- exclusivity must be agreed,
- limited compensation resolves copied arrangement.

Strong institutional gameplay model:
**use Roman custom, people and precedent rather than importing modern IP law.**

---

## Invention focus expands — AD 162

Player chooses invention again.

Power transmission work includes sectional coupling/supports.

User asks: **"am I getting paid for all of this?"**

This exposes a recurring economy design problem.

New hard rule:
before substantial work starts, show who pays/materials/R&D/favor/profit share.

---

## Technology ladder discussion begins

User considers metallurgy but points out:
- a time-machine inventor should know much broader theory,
- rudimentary control and power generation should become possible,
- Hero-era steam phenomena existed,
- hydraulics could help control,
- metallurgy remains an enabling foundation.

Discussion expands to:
- metallurgy,
- hydraulics,
- mechanical feedback,
- steam,
- electrical generation,
- theory and practical capability.

---

## User correction: do not rediscover existing Roman tech

During metallurgy scene, assistant treats heat treatment/hardness-toughness too much like a discovery.

User asks:
**"Don't blacksmiths know this at this point?"**

Then:
**"We should be building on this not redoing stuff that is already known."**

This becomes a hard global rule.

Later user asks:
**"Alloys"**

Metallurgy is reframed:
- Romans already have sophisticated alloys,
- player pushes controlled composition, reproducibility and purpose-specific materials.

---

## Hydraulics correction

User asks whether Roman valves/hydraulics already existed.

Answer: yes, important pieces already exist.

Design correction:
- do not invent valves/pumps/hydraulics,
- push toward high-pressure sealing, accumulators, actuation, control, relief, multi-actuator systems.

Spring-loaded modern-looking relief valve is reconsidered in favor of more historically buildable calibration/weight mechanisms until metallurgy supports reliable springs.

---

## Extensive technology/capability review

User requests an extensive review of the invention ladder.

Key structural result:
- technology must be capability acceleration, not a Civilization-style list of gadgets,
- theory and capability are separate,
- cross-domain dependencies matter,
- most of the full graph should remain hidden from normal UI until relevant.

Major domains:
mathematics, physics, chemistry, metallurgy, hydraulics, mechanics, machine tools, power, control, electricity, optics, medicine, public health, agriculture, materials, construction, transport, navigation, metrology, information, scientific method, communication, mining, manufacturing, energy storage, computing/logic, finance/economics, governance/admin, education.

---

## Live simulation — AD 162–164

Ten-turn simulations explore:
- alloy baseline and purpose-built bronze,
- Roman social/institutional obligations,
- hydraulics/press,
- theory tied to practical problems,
- association standards,
- accumulator,
- metered valve,
- mathematical/physics formalization,
- knowledge reproduction,
- chemistry,
- mechanical governor direction.

By March AD 164, user chooses **Jump**.

---

## Major jump — AD 164 to AD 247

Arrival under Philip the Arab.

World signs:
- steam machinery,
- shaft power,
- industrial smoke/noise,
- changed technical Latin,
- larger/altered Rome.

Marcus Fabius Tertius appears on a bronze plaque as founder.

Player traces personal legacies:
- Marcus → Fabian Works,
- Gaius → respected small-scale maker,
- Livia → household/apprentice support,
- Lucan → metallurgical comparison tradition,
- Serenus → comparative fever-record tradition,
- Felix → network connector,
- Sextus family → mixed industrial fortunes,
- Ostia association → standards/inspection/training/arbitration institution,
- Aulus → practical reliability tradition, later governor lineage.

This future-arrival sequence becomes one of the strongest examples of the intended jump payoff.

---

## 100-critic two-jump simulation

User requests realistic critic review through **two jumps**.

Result:
- mean 8.12,
- second jump 8.62 vs first 7.94,
- strongest praise: inventor fantasy, Grand Challenges, Roman life, collaborators, jump payoff,
- criticism: dialogue, monotony, manual pacing rotation, micro-invention loops, need more personal jump echoes, more NPC autonomy.

Adopted changes:
- systemic scene rotation,
- NPC autonomy,
- stop micro-scenes once bottleneck understood,
- more human jump echoes.

---

## 10×100 current-design simulation

Humanized chat-level review:
- 8.45 mean,
- 95/100 continue,
- strong institution access and Roman culture,
- concerns around modern language, state visibility, negotiation, private life.

---

## 10×1000 current-design simulation

User later requests 10×1000 human-like testing.

Results:
- overall 8.39,
- jump payoff 8.71,
- authenticity 8.58,
- pacing lowest major score at 7.82,
- main risks: engineering dominance, system overload, functional dialogue, Roman baseline, exposition, too-fast tech, invitation gates, economy ledger.

User response:
when game is more developed, better menus should prevent options being buried.

Design interpretation:
- retain deep systems,
- solve overload with hierarchy/menu architecture,
- do not flatten the simulation.

---

## Phase transition: P0 → P1

User asks whether P0 is done.

Initially human gate remains open.

User explicitly says:
**move the 5 human testers later when we have graphics.**

Human gate is removed from P0.

User asks about P1 and then:
**"let's move to the next phase then."**

P1 is defined as **playable game structure / vertical-slice architecture**.

Primary work:
- menu architecture,
- scene routing,
- project state,
- tech/capability foundation,
- NPC autonomy,
- institution gating,
- economy ledger,
- jump persistence.

---

## P1 implementation preparation

User says:
**"Get started."**

Repo inspected.

Verified branch still contains old P0 behavior:
- variable/old pacing,
- auto-end,
- direct stakes,
- old money framing.

Additive P1 foundation prepared locally:
- P1Architecture,
- project terms/state,
- scene pacing,
- invitation gate,
- tests,
- P1 scope.

Not committed because environment lacked .NET SDK and project rules require `dotnet test` before commit.

User says:
**"Next."**

Second additive integration package prepared:
- deterministic scene router,
- economy ledger,
- institution access state,
- additional tests,
- migration notes.

Next planned work:
- fixed one-month turn,
- explicit End Month,
- no auto-end,
- fast-forward stop conditions,
- World integration,
- one real paid-work migration,
- stake/buy-in retirement.

---

## Current handoff request

User says Claude is about to become available and requests:
**"Get everything together that I need to pass over including all of our conversations."**

This package is the result.

Claude should treat:
- the current master handoff as top-level authority,
- P0 v3 as important prior implementation context,
- P1 integration package as uncommitted code requiring tests,
- simulated testing honestly according to its limitations,
- current live AD 247 sequence as design/play evidence, not executable state.
