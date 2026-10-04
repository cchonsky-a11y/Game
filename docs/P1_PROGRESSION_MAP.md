# P1_PROGRESSION_MAP.md: the progression graph as built

> Audit of the gates in `data/content/` and the P1 code as of the hardening pass (2026-10-04). It describes what the build does, not new rules; SYSTEMS.md stays canonical. Requirement syntax: `a|b` = either, `!x` = not, a list = all of (see `Simulation.Holds`). Challenge `routes` = any route, each route all of.
>
> **Gate kinds.** **Exclusive**: an intended consequence of a choice (losing it is the point). **Relationship**: needs a person who knows you. **Capability**: needs what Rome can do (knowing is not making). **Economic**: costs gold or Attention, never blocks outright. **SPOF**: an accidental single point of failure. Each one found is listed under "Fixed in this pass".

## 1. Opening (hour one)

| Choice | Gives | Exclusive to it |
|---|---|---|
| Workshop | The smith's workshop project; then seasonal **craft orders** (P0 workshop, 2–3 Attention, standing effects); machine step/upgrade `workshop` | Craft orders; the flywheel upgrade at its usual price |
| Fountain | The district fountain (Medicine); Serenus can come (one of four ways); **the Subura allotment** chain (P1-20); machine `medicineWork` | The neighborhood water chain; an early way to Gaius |

The two are meant to differ. The workshop earns from craft, the fountain from neighborhood and public-health work. Neither raises the pay for odd jobs.

## 2. People: how you meet them

| Person | Known when | Entrances | Kind |
|---|---|---|---|
| Felix | `commission:cellarpump:Offered` | The pump offer comes to every player from month 4 | — (always) |
| Cassianus | same | same | — |
| Diodoros | `life:warehouse-fire` (needs the pump done) | One: Cassianus's household | Exclusive (losing the pump loses Cassianus's household thread, a favor job with no pay) |
| Serenus | `scene:serenus-meet` | Felix's vow (his fever needs only that you know him), the fountain, a physician project, or the promise | Relationship (4 ways) |
| Gaius | `scene:gaius-meet \| commission:allotment:Done` | Shared measures (standards stage 2), **or** the allotment (fountain) | Relationship / capability (2 ways; was 1) |
| Livia, Marcus | `knows:Gaius` + month 14 | Through Gaius | Relationship |
| Lucan | `knows:Gaius` + the power challenge open | Through Gaius | Relationship |
| Aulus | gauges reproducible + `knows:Gaius` | Through Gaius, once gauges exist | Capability + relationship |
| Sextus | power stage 5 | Through powered workshops | Capability (intended: he is its consequence) |

## 3. Paid work (15 commissions)

| Commission | Requires | Entrances / alternates | Leads to | Kind |
|---|---|---|---|---|
| cellarpump (Cassianus) | month 4 | Everyone | guild referral, valve seats, standards route A, Diodoros | — |
| hoist (Aemilius) | pump walked or declined; month 9 | The second chance | guild referral, standards route B | Exclusive with the pump (you get one of them) |
| bilges (Lollius) | guild Guest + pump or hoist done | Either of Felix's jobs | crane | Relationship |
| crane (Lollius) | bilges done; month 30 | — | — | Repeat work |
| fevers (Pomponia) | knows Serenus + the casebook | Serenus (4 ways) | households | Relationship |
| households (Iunia) | fevers done; month 34 | — | — | Repeat work |
| baths (Philippus) | a baths scene (`baths-wet-cellar \| baths-regular`); month 10 | Any visit to the baths | fuller's curse; standards route D | **Was SPOF** (fixed) |
| allotment (Subura) | fountain repaired; month 8 | Fountain opening (or a later fountain project) | Gaius, argiletum, standards route C | Exclusive to the fountain (intended identity) |
| argiletum | allotment done + Gaius; month 26 | — | — | Repeat, larger work |
| jars (Diodoros) | `life:diodoros-deliveries` | Cassianus's household | Diodoros's regard | Exclusive (a favor, no money) |
| drawings (Vettius) | guild Member + metrology reproducible | Guild (2 ways) + standards (4 ways) | profit share; copies | Relationship + capability |
| millbearing (Statius) | knows Aulus | Through Gaius and gauges | sluice | Relationship + capability |
| sluice (three mills) | millbearing done; month 36 | — | — | Repeat work |
| fountainworks (Gaius's freedman) | knows Gaius; month 30 | Gaius (2 ways) | — | Relationship |
| drains (Clivus magistrates) | knows Gaius; month 40 | Gaius (2 ways) | Livia's regard | Relationship |

Generic odd jobs are the fallback. The menu offers them only while no commission or challenge stage is under way. Craft and consult are typed legacy commands.

## 4. Institutions

| Institution | Access | Evidence | Alternates | Kind |
|---|---|---|---|---|
| Merchants' Guild of Ostia | invitation (Felix) | relationship: knows Felix; work: pump **or** hoist done; usefulness: valve seats reproducible **or** Felix's regard 1 | Two of Felix's jobs | Relationship. Losing both closes the guild: an exclusive consequence of refusing Felix twice. It blocks nothing essential, since every guild-sourced resource has a stranger's price and the machine's bronze has an open-market price. |
| Physicians' Circle | invitation (Serenus) | relationship: knows Serenus; work: P0 join condition (a Medicine project or the promise); usefulness: Serenus's regard 2 | Serenus (4 ways); join condition (2 ways) | Relationship; legacy P0 `join:` condition |
| Tiber Island sanctuary | gifts (`give`) | — | — | Economic |
| Banking house of Octavius | shares (legacy stakes, kept) | first purchase ≥ 5% | — | Economic |
| Caecilian / Junian factions | patronage (closed in P1) | — | none in P1 | OPEN DESIGN QUESTION (patron's introduction) |

## 5. Grand Challenges

**Measurement and standards** (goal: gauges reproducible). It opens by any route (first that holds, once):

| Route | Requires | The encounter |
|---|---|---|
| pump | valve seats reproducible + pump done | A Clivus bronzesmith's seat cut by eye leaks |
| hoist | hoist done | Another smith's pawl from your drawing won't catch |
| allotment | allotment done + month 12 | Two calices stamped as one quinaria pass different water |
| baths | baths done + month 14 | Another plumber's plug won't seat in your stopcock |

Stages:

| Stage | Needs | Kind |
|---|---|---|
| master | tin bronze through the guild (Guest), else ×2 | Economic |
| shared | `access:guild:KnowsMember \| knows:Gaius` and Felix or Gaius (stand-in text) | Relationship. **Was SPOF** (fixed): it needed a guild contact only Felix's two jobs gave. |
| gauges | Noric steel through the guild (Member), else ×2; metrology reproducible | Capability + economic |
| fits | — | — |

**Powered workshops** opens on gauges reproducible + knows Aulus (one route). This is an intended capability gate: it builds on standards, which now has four entrances. Stages need Aulus or Gaius (relationship). Endurance takes Marcus, else Gaius two months slower. Resources are ×2 without the guild. Life can delay it (the naming day; Aulus's back). The consequence is Sextus and his twelve men (a triggered choice).

## 6. Machine, R-17 and the jump

| Step | Needs | Alternate | Kind |
|---|---|---|---|
| Assessment | 2 months, 1 Attention | — | — |
| Bronze (coil) | `tradeMember` (guild or bank) | open market, 20 gold | Economic; legacy P0 requirement |
| Glassware (coolant) | `medicineWork` (fountain, physician, quarantine, midwives, Circle or sanctuary) | glassblower, 25 gold | Economic; legacy P0 |
| Archives (chronometer) | `factionMember` | 15 gold | **Legacy P0 dependency:** the factions take no members in P1, so this is always the gold price |
| Other 6 steps | gold, Attention, time | — | Economic |
| Upgrades (contacts, lens, flywheel) | as above, optional | higher gold | Economic |
| Gold restored | the scavenged amount, fixed | — | Economic |
| R-17 | scenes after 3/6/9 steps; `listen` after R-17 ACTIVE | — | Never blocks the jump |
| Jump | all 9 steps + gold; the player chooses when | — | — |

## 7. Exclusivities (intended)

- The pump or the hoist: the hoist comes only if the pump is lost.
- Diodoros's branch after the fire: the deliveries or Antioch (or neither).
- Marcus: stays (regard ≥ 2) or leaves for Priscus.
- The factions won't share a member (P0 rule; closed in P1 anyway).
- Workshop craft orders against the fountain's neighborhood chain.

## 8. Legacy P0 dependencies still in the graph

- The Circle's work evidence uses the P0 `join:` condition (Medicine project or promise).
- Machine requirements `tradeMember` / `factionMember` read P0 `Backed` (membership/stake); `factionMember` is unreachable in P1 (gold alternative).
- Workshop orders, odd jobs, craft and consult are P0 work systems.
- Domains, debt, the plague, policy, advocacy, Rome's dated choices (now granting standing), the bank's stakes, offices and voice via the internal stake field, and the batch/explorer/snapshot purchase.

## 9. Fixed in this pass

1. Measurement and standards opened only from the pump (seed 3 never saw it). It now has four routes.
2. The baths job needed a scene that can only play before the pump is offered. Any baths visit now counts.
3. The standards "shared" stage needed a guild contact only Felix's jobs gave. Gaius can vouch.
4. Gaius, and everything behind him (Livia, Marcus, Lucan, Aulus, power, millbearing, sluice, fountainworks, drains), hung on standards stage 2 alone. The fountain's allotment is a second way.
5. Felix's fever (and so the vow, one of the four ways to Serenus) needed a guild referral. It now needs only that you know Felix. Found by the strategy matrix: a player who lost three jobs met no one new for six years.
6. Unintended echoes came from one life event (Pollio's copy, which needs the pump). Three more bad copies (Vettius's drawings, Serenus's tables, brass bearings) make the echo kind resilient.

## 10. Known remaining narrow points (not fixed; judged intended or acceptable)

- **Aulus has one introducer (Gaius).** Gaius never leaves Rome in content, so this is not a dead end.
- **Powered workshops has one route.** It is the intended sequel to standards (capability), and its question needs gauges.
- **The guild closes if the player refuses both of Felix's jobs.** This is a consequence, and nothing it gates is required.
- **Diodoros and the jars favor depend on the pump.** This is Cassianus's household thread, with no money in it.
