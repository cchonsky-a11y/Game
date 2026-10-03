# PROTOTYPE_SCOPE.md — Current Milestone: P1 Playable Game Structure

> This file defines the **only** work in scope right now. It replaces the P0 scope (kept at `docs/archive/PROTOTYPE_SCOPE_P0.md`).
> Decided by Corey (handoff of 2026-10-02, `docs/handoff/2026-10-02/`): P0 is complete. The five-human test is deferred to a later graphical milestone. The project is in **P1**.
> VISION.md and SYSTEMS.md describe the long-term game; they are not a build list.

## The P1 target
A fresh player can **arrive in AD 155 → establish themselves → form recurring relationships → complete explicit paid work → begin a Grand Challenge → earn institutional access through people → prepare the machine → make a meaningful jump → see persistent consequences**, through the intended game structure rather than ad hoc console flow.

P1 is architecture, not content volume. The P0 executable keeps running while its systems migrate one at a time. Every migrated system replaces its P0 version; old and new never both remain player-facing.

## Locked rules for P1 (Corey)

| Area | Rule |
|---|---|
| Calendar | **1 turn = 1 calendar month**, fixed. No player-facing turn-length choice. Durations are authored in months. Machine assessment takes 2 months. The era still runs AD 155–175 by the calendar. |
| Attention | 4 a month. Future commitments are visible; no silent overbooking. **Spending the last Attention never ends the month.** **End Month** is the normal advance. An optional fast-forward stops on meaningful interruptions. |
| Menus | Eight sections: **Now, Projects, People, Institutions, Knowledge, Civilization, Machine, Journal**. Depth is organized, not flattened. Within lists: Active / Available now / Blocked / Emerging / Archived. |
| Scene pacing | Eight scene categories. After two consecutive meaningful scenes of one category a third is strongly deprioritized unless the player explicitly stays focused. World interruptions are never suppressed. A deterministic router, not a writing guideline. |
| Work and money | No generic consulting grind. Work grows: encounter → help/diagnosis → prototype → paid commission → repeat work → scaled opportunity. Before substantial work starts, the game states who pays, who pays for materials, any profit share, self-funded R&D, or a favor. No invisible free labor. Negotiation can fail. A clean **ledger** records money. |
| Institutions | No buying stakes. Access goes **Aware → knows a member → guest → invited back → sponsored candidate → member → office**. A forward invitation needs a specific inviter, an existing relationship, relevant work in that domain, demonstrated usefulness, and an inviter willing to take the social risk. Fame alone never qualifies. Institutions bring obligations (dues, meals, mutual aid, funerals, disputes, standards), not buffs. |
| Technology | A hidden **capability network**, not a tech tree. Theory is tracked separately from demonstrated, prototype, reproducible, manufacturable, economical, adopted and institutionalized. **Roman baseline first:** never "invent" what Rome already had; formalize, improve, combine, scale or standardize it (`docs/handoff/2026-10-02/03_TECHNOLOGY_CAPABILITY_LADDER.md`). Small problems prove principles; **Grand Challenges** change capability. Once a bottleneck is understood, stop the micro-scenes. |
| People | Recurring characters have a goal, a vulnerability, loyalties, rivals, a household, opinions and a life offscreen. They can cancel, fall ill, quarrel, copy an idea, leave, fail or improve on it. |
| Narrative | **Say it once:** scene → necessary state change → choices. No narrator commentary after a line ("That lands.", "That changes things." and the rest of the banned list in the master handoff §7). No clipped title-card fragments. Romans speak concretely; the inventor does the abstraction. Roman life (households, baths, religion, patronage, markets, festivals, games, law, class) is a primary pillar, woven into routine. |
| Opening | "The machine stops screaming before you do." A non-travel systems test, then dirt, a mule cart, Latin: "Ancient Rome. Not ruins. Alive." Nothing about plague, jump economics, ranges or Echoes up front. The machine's gold is scavenged from its components. |
| Machine | Support systems around an intact Temporal Field Core. The quiet R-17 mystery is not explained early. A ready machine says: "The machine is ready. You can leave now, or remain in Rome and continue your work." No hidden gate against early jumps. |
| Jump | The payoff: physical, personal, institutional and unintended echoes, distortions and failures. The player reconstructs the links; `why` reveals details. |
| Early reputation | For about six months the inventor is socially minor; no senators or imperial plots early. |

## Order of work (master handoff §25)
1. P1 Sprint 1 foundation (menu sections, scene router, project terms, ledger, invitation access). **Done.**
2. Fixed 1-month turns, no normal auto-end, explicit End Month, fast-forward that stops on interruptions.
3. Pacing state, ledger, projects and institution access in `World`.
4. One real paid commission end-to-end (encounter → terms → stages → scenes → ledger → completion → referral).
5. Institution access in content; retire player-facing stake buying institution by institution.
6. Hidden technology/capability graph with Roman-baseline metadata and checks.
7. NPC autonomous progression.
8. Jump-echo model: personal, technical, institutional, unintended.
9. View models for the eight menu sections.
10. Narrative/dialogue cleanup, resource bottlenecks for Grand Challenges.
11. Throughout: SYSTEMS → GDD Appendix A → tuning → DECISIONS → content → tests in sync.

## Still running from P0 until migrated
Everything in `docs/archive/PROTOTYPE_SCOPE_P0.md` that P1 has not yet replaced keeps working and keeps its tests: domains and debt, the plague and its recurrences, policy, the workshop, Rome's dated choices, the machine repair track, two jumps and the walk. Each is reviewed when its P1 replacement lands; an old bug item is fixed only if its system survives.

## Out of scope (do not build)
- Unity, graphics, audio, a UI beyond the console (P1 builds view models a later UI will consume).
- Eras after Rome as playable eras, multiple regions, diplomacy, warfare.
- The fuel puzzle (P2), repair tiers, malfunctions.
- Saves, iCloud, purchases, analytics, networking.
- Any language-model integration.
- Human playtests (deferred to a graphical milestone). AI or persona testing is advisory and is never called human testing.

## Hard constraints (SYSTEMS.md §14) still apply
Player institutions never use enslaved labor. No atrocity verbs. Religious founders are never depicted, nudged or erased. No weapon of mass destruction as a player tool. Exploitation always costs the Index. Roman slavery may be depicted as part of Roman life, never as a player tool.
