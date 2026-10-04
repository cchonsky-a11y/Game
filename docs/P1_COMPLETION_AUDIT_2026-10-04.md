# P1 completion audit (2026-10-04)

> An audit of the P1 target in PROTOTYPE_SCOPE.md against the build at the end of the hardening pass. It is **not** a declaration that P1 is complete: green tests are necessary, not sufficient, and every judgment of feel below rests on scripted runs, not people.
>
> - Evidence: 482 passing tests; the 300-run strategy matrix (`playtests/ai/live/2026-10-04-p1-strategy-matrix.md`); the two-jump validation (`--p1-validate`); the gate map (`docs/P1_PROGRESSION_MAP.md`).
> - **No human has played P1.** Human testing is deferred to a graphical milestone, and AI or persona runs are advisory.

**Verdicts:** PASS = built, exercised and holding up in the evidence. PARTIAL = built but with a known gap, or not judgeable without people. FAIL = missing or broken.

## The P1 target, step by step

| # | Criterion | Verdict | Evidence | Gap |
|---|---|---|---|---|
| 1 | **Arrival in AD 155** | PASS | The opening ("The machine stops screaming before you do"; the systems test; the mule cart; "Ancient Rome. Not ruins. Alive."). Nothing about plague, jump economics or Echoes up front (`NarrativeRulesTests`). The machine's gold is scavenged from its parts. | — |
| 2 | **Understanding without tutorial overload** | PARTIAL | Eight menu sections, one numbered choice at a time, Active / Available / Blocked / Emerging / Archived lists, explicit terms. | Cannot be judged without people. The menu is still a console. View models exist for a later UI. |
| 3 | **Establishing oneself** | PASS | The first commission is offered in month 5–6 (both openings, all profiles), the first paid completion in month 7–11. The hour-one choice (workshop or fountain) gives each opening its own economy. | — |
| 4 | **Roman life** | PASS | About half of routed scenes and 30–33% of all scenes are Roman life, every year of the era. Texture recurs with new words; religion, baths, markets, festivals, law, patronage and households are woven into work scenes (the Lares, Portunus, the crossroads shrine). | Most authored texture is Roman life, so when no progression waits, runs of 7–9 Roman-life scenes occur. |
| 5 | **Relationships** | PASS | 10 recurring people with goals, vulnerabilities, households and offscreen lives (23 life events). Regard moves with work and choices; people refuse, leave, quarrel and copy. Scripted runs know 8–10 people. | — |
| 6 | **Paid work and negotiation** | PASS | 15 commissions across domains, each stating who pays and who buys materials. Asking for more can win, meet a firm no, or lose the job (Negotiator: about 3 lost a run). Favors pay nothing; profit shares are now paid; the ledger reconciles in 300 of 300 runs. | — |
| 7 | **Attention choices** | PARTIAL | Hard future limit, with no silent overbooking (0 in 300 runs; tests). A third of months end with 0 free, and challenge stages are deferred 1–5 times a run. | 40–55% of months end with 2+ unused for the scripted profiles. Attention binds in bursts, not monthly. Not retuned (out of scope). |
| 8 | **Technology problems** | PASS | Hidden capability network (19 nodes, Roman baseline, edge levels, maturity apart from spread). Every commission stage moves a capability. Bad copies spread without raising maturity. | — |
| 9 | **A Grand Challenge through more than one path** | PASS | Measurement and standards has four routes: the pump, the hoist, the Subura allotment and the baths. Observed: pump 288, hoist 8, allotment 3, never 1 (a player who lost three jobs). Its second stage no longer needs a guild contact (Gaius can vouch). Both challenges were completed in 267 of 300 runs. | Powered workshops has one route, by design: it is standards' sequel. |
| 10 | **Institution access through relationships** | PARTIAL | The guild (Felix; either of two jobs) and the Physicians' Circle (Serenus; four ways to meet him) are by invitation, with separate evidence and a vote that can refuse. The sanctuary takes gifts. Rome's choices give standing, not shares. | The Senate factions have no P1 path (OPEN: patron's introduction). The bank keeps legitimate stakes. Offices still read the internal stake (legacy). |
| 11 | **NPC autonomy** | PASS | Life events with windows, declining chances and exclusive branches. People leave Rome, fall ill (Felix's fever no longer waits on your guild standing), refuse, quarrel and copy. 9–12 life events per scripted era. | — |
| 12 | **Machine mystery** | PASS | The R-17 thread follows the repairs. The DO NOT JUMP warning was reached in 293 of 300 runs, then the notebook. It never blocks the jump. R-17 stays unexplained (OPEN, as intended). | — |
| 13 | **Preparing the machine** | PASS | Assessment and nine repair steps, each with a Roman source or a gold alternative; the scavenged gold is restored; optional upgrades lengthen the jump. | The chronometer archives step asks for faction membership P1 never grants, so it always costs gold (legacy). |
| 14 | **A first jump with technical, personal, institutional and unintended echoes** | PASS | 298 of 300 first arrivals show all four kinds (was 263 before this pass). Bad copies now come from four threads. The people remembered are those you were closest to, at most two. 0 repeated sentences. | Echoes are keyed by arrival number, not years elapsed (matters only if the range changes). |
| 15 | **Continuing** | PARTIAL | After arrival the player can walk the city (present conditions, not causes), read the Learn-more chronicle, and jump again. | Playing on in a later era is out of P1 scope, so "continuing" is the walk and the second jump. |
| 16 | **A second jump** | PASS | 300 of 300 runs jumped twice. Second arrivals avoid repeating lines, prefer people not yet featured, and use later-aged lines. | — |
| 17 | **History evolving** | PASS | History is the baseline (curves to AD 420, the historical plagues on their dates, the coin's debasement). Every departure from it is logged as the player's mark. Carried capabilities climb one rung per 20 years only through a surviving member house. | — |

## Things that must be absent

| Must be absent | Verdict | Evidence |
|---|---|---|
| Critical blockers | PASS | 300 of 300 matrix runs reached two jumps. The accidental single points of failure found (standards from one job, the baths scene, the shared-foot guild contact, Felix's fever behind the guild, unintended echoes from one event) are fixed. |
| Generic-work grind | PARTIAL | Odd jobs are a fallback, offered only when nothing is under way. Cooperative players spend 14–19 of 96 months on them. Profiles that refuse or lose work spend 19–39 on average and up to 52 (Engineering fountain), so a player who turns work down still grinds. |
| Silent overbooking | PASS | 0 in 300 runs; covered by tests. |
| One-client dead ends | PASS | No opportunity hangs on one client: standards has 4 routes, the guild 2, Serenus 4, Gaius 2. The narrow points kept by judgment are in `P1_PROGRESSION_MAP.md` §10. |
| Engineering monopoly | PASS | Engineering is 12–13% of all scenes and 2% of routed scenes. The Engineering-focused profile is the poorest at departure and does the most odd jobs. |
| Roman culture disappearing | PASS | Roman life appears every year of the era, at about a third of all scenes. |
| Ownership abstractions dominating | PARTIAL | Seats aren't sold at the guild, the Circle, the sanctuary or the factions, and Rome's choices give standing. But the bank's stakes remain (legitimately), offices and voice read the internal stake, and the generic arrival line can say "you held N% of" for institutions without their own text. |
| Duplicated jump text | PASS | 0 repeated sentences between arrivals and 0 duplicate scene texts in 300 runs, after fixing the drift-description and sponsorship repeats. |

## Summary

- **PASS: 12.** Arrival, establishing oneself, Roman life, relationships, paid work, technology, a Grand Challenge by several paths, NPC autonomy, the machine mystery, machine preparation, the first jump's four echo kinds, the second jump and history evolving.
- **PARTIAL: 5.** Understanding without overload (needs people), Attention (binds in bursts), institution access (factions open), continuing (scope), and two absence checks: generic grind for players who refuse work, and legacy ownership numbers.
- **FAIL: none.**

P1's structure is complete enough to be judged by people. **What is left is mostly design decisions and polish, not missing systems:**

- the patron path into the factions;
- the jump-range model;
- whether Attention should bind more often;
- the remaining legacy stake readings;
- a less console-bound presentation of the eight views.

## Legacy P0 systems still running

- Domains, debt and expectations; the plague and its historical recurrences; economic policy and advocacy; Rome's dated choices (now granting standing); the workshop and its orders; odd jobs (craft and consult as typed commands).
- The machine repair track and its P0 requirement types; offices, voice and policy reading the internal stake; the bank's stakes.
- The P0 batch strategies, explorer and snapshot using the legacy purchase; the walk.

## Open design decisions for Corey

1. **What R-17 is** (deliberately unexplained).
2. **The patron's-introduction path** into a senator's following, within the six-month "socially minor" rule. Until then the factions are closed, and the archives machine step always costs gold.
3. **The jump-range model:** A constrained random window, B a player-chosen band with uncertainty, or C capability-improved targeting. See the strategy-matrix report. Echoes are keyed by arrival, not elapsed years.
4. **Whether Attention should bind more often.** Measured, not retuned.
5. **Locking 3:1 progression routing.** Recommended, with evidence. Whether to add an age bonus for long-waiting scenes is a separate choice.
6. **Approving the proposed numbers P1-01 to P1-22,** including this pass's P1-20 (fountain neighborhood work), P1-21 (profit shares) and P1-22 (bad copies).
7. **Whether the generic arrival line should drop stake percentages** for the bank and sanctuary.

## Polish-pass update (2026-10-04, later)

See `docs/P1_POLISH_2026-10-04.md` for the full findings. Changes since the audit above:

| Item | Before | After |
|---|---|---|
| 10. Institution access through relationships | PARTIAL: factions had no P1 path | Still PARTIAL, closer. A senator's house is reachable as a **client** through Cassianus or Serenus (the salutatio, then a favor with costs). The following, its offices and policy stay OPEN. |
| Generic-work grind | PARTIAL | Still PARTIAL by design: players who refuse work lean on odd jobs. Odd-job months no longer repeat one sentence, and pay is unchanged. |
| 14. Four echo kinds on the first arrival | 298 of 300 | 297 of 300 on the current content. Repeated arrival sentences 0, after fixing the rogue-house repeat. |
| Wealth across jumps | Correct, but scattered in the briefing | One briefing block (carry, deposit or bury, bank, jar, coin in hand, institutions' money). 7 tests. |
| Content safety | Requirements validated | Event, effect, workshop, invention and machine content validated too |
| Long progression waits | Attributed to category exclusion | Traced to a crowded pool. An age bonus is built but off (PROPOSED P1-23). |

**Still PARTIAL:**
- understanding without overload (needs people);
- Attention (needs people);
- institution access (the factions' following);
- "continuing" (scope);
- the grind for players who refuse work, and legacy ownership numbers in a few places.


## Freeze-pass update (2026-10-04)

- **13. Preparing the machine:** the gap is closed. The archives step takes a senator's note (standing at Varro's salutatio as his client), with the clerk's bribe as fallback; it no longer always costs gold.
- **9. A Grand Challenge through more than one path:** the credit now goes to the first route that became eligible (strict causality). Route tallies are unchanged in 300 runs.
- **P1-20, P1-21, P1-22:** approved provisionally. **P1-23** (age bonus) stays off. Attention is not retuned and needs human validation. The jump range is unchanged; its redesign is deferred.
- Evidence after the pass: 510 tests pass; 300 of 300 matrix runs jump twice; the ledger reconciles in 300; no overbooking; 0 repeated arrival sentences; both challenges done in 259 runs (266 before; the drop is the Relationship profile now waiting on the salutatio before its chronometer repair, a script change).
