# Simulated human playtest: blind AI testers, round 1 (seeds 611–620)

> **Advisory only, not P0 gate evidence.** Each tester was a fresh AI agent that saw only the game's own output (no code, docs or data) and played through `playtests/ai/live/play.py`. The five human testers in `playtests/kit/` remain the gate.
> Played on the build of 2026-09-28, before fixes A1–A10, T1–T3 and L1–L11. Several bugs quoted in the reports are now fixed (see the last table).

## Who finished

| # | Persona | Seed | Hour one | Left Rome | Arrivals | Rating |
|---|---|---|---|---|---|---|
| 1 | Strategy veteran (Civ/Paradox) | 611 | workshop (fountain later) | AD 174, the era's end | AD 234 (skipped by the double-jump bug), 274 | **8/10** |
| 2 | Roman history enthusiast | 612 | fountain | AD 173 | AD 233 (skipped by the double-jump bug), 268 | **8/10** |
| 3 | Casual newcomer | 613 | fountain | AD 170 | AD 220, 260 | **8/10** |
| 4 | Economist | 614 | workshop (fountain later) | AD 171 | AD 212, 257 | **8/10** |
| 5 | Kind idealist | 615 | — | stopped at AD 170 (usage limit) | — | — |
| 6 | Profit-seeker | 616 | workshop | AD 173 | AD 228 (skipped by the double-jump bug), 268 | **7/10** |
| 7 | Impatient early jumper | 617 | workshop | **AD 159** | AD 189, 219 | **5/10** |
| 8 | Institution builder | 618 | workshop | stopped at AD 174 (usage limit) | — | — |
| 9 | Tinkerer-inventor | 619 | — | stopped at AD 174 (usage limit) | — | — |
| 10 | Cautious explorer | 620 | — | stopped at AD 174 (usage limit) | — | — |

**Six of ten finished. Mean rating 7.3; five of the six rated 7 or higher. All six would jump again.** Tester 7's answer was qualified: only with another era to play.

## Against the P0 questions

- **Did they feel their actions changed the world they returned to? Yes, for 6 of 6,** each pointing to a specific Echo, unprompted:
  - "For the one who stayed" over the Circle's door, from the promise they kept (testers 1, 2, 3, 4, 6);
  - their misspelled name on the officers' roll (1, 2, 3, 6);
  - "AQUA PURA" on the fountain rim and "hospes" by the flood line (2, 3, 4);
  - the street of forges and the Tertian ship fittings (1, 3, 6);
  - the guild they backed becoming the Grain Cartel: "it hurts in a good way" (1, 2, 3);
  - the workshop become a stable, and "about 82 thousand dead… You aren't in any of them" (7).
- **Could they say what they chose between? Yes, all six.** Money now against standing later. Medicine against trade (quarantine hurt Ostia). Galen against their own Circle. Profit against relief. Keeping the promise against leaving once the machine was ready.
- **Do they want to jump again? Yes, all six.** Several wanted to see whether the Hospice or the Club survives the third-century crisis.
- **The target sentence** (*"the plague hit harder because I left before the fountain was fixed"*) came nearest from tester 7: "Fever in the district… You remember choosing the workshop over the fountain." Tester 3: "`why plague`: 2% would die, not history's 10%, and it lists 'the fountain runs clean.' My hour-one choice matters!"

## What worked

- The arrival writing: every finished tester quoted a line back.
- `why` explanations: "I understood the tradeoff completely" (1); "exactly who pushed back" (4).
- Rome's dated choices as real dilemmas, Galen above all.
- The jump briefing: "this is where the decisions felt real" (4).
- Real prices at the market (4).

## What hurt (in order of how many testers raised it)

1. **The double jump ate the first arrival** (1, 2, 6). **Fixed** (A4–A6).
2. **The middle years (about 167–173) felt grindy:** "consult, end, consult, end", with offices taking Attention (1, 2, 3, 4, 6). *Design question for Corey.*
3. **Turn-ending surprises:** a paydown or the last Attention ended the turn, so an extra `end` skipped one (1, 2, 3, 4, 6). **Fixed** (L2). A warning now appears when all Attention is pledged (L3).
4. **The plague response's cost was hidden, and it drained the purse** (1, 2, 3, 4). *Open (L12).*
5. **"The Subura has seen you profit from its bad years twice"** came to players who gave flood relief. The trigger was selling the designs or taking the guild's loan for the river forge (1, 3, 4). *New: Corey to decide whether those answers should count as profiteering.*
6. **The numbered menu renumbers after each action**, so "3 1" or a scripted number picked the wrong thing (3, 4). *New.*
7. **`endow` suggested without control** (1, 2, 3). **Fixed** (L11). **Gold lost at the second jump** (3, 4, 7). **Fixed** (A4).
8. **Camp leadership was opaque** ("9–0 votes and the Cartel leads"), and a Circle that became the camp you ordered was labelled "Drifted" (1, 2, 3, 4). *Partly open (L13). Label question for Corey.*
9. **Leaving early was easy**, skipping the plague and the promise (7). *Design question.*
10. **Consulting dominated income** (4, 6, 7). *Design question.*

## New findings from reports 1, 3 and 4 (not yet in the bug list)

| Finding | Tester |
|---|---|
| The "profit twice" street backlash counts selling the designs and the guild's loan as profiteering; players didn't expect it and weren't told. | 1, 3, 4 |
| The menu renumbers after each action; multi-number input picks unintended items (hired apprentices, answered an event). | 3, 4 |
| Plain `consult` is an unknown command (it must be `work consult`). | 3 |
| Grammar: "The healers of the Tiber Island sanctuary works…" and "…is still there and still itself". | 3, 4 |
| The plague response lapsed within the turn it was offered ("There is no outbreak to respond to"). Other decisions give 3 turns. | 4 |
| `why policy` says a policy change takes 1 Attention; it takes 2. | 4 |
| The charter message is priced in "gold" (30 gold) while everything else is in denarii. | 4 |
| A rival attack was printed twice, with the wrong before-and-after strength. | 4 |
| The Wrongness beat says the denarius "still rings true… never cheapened" while the changers show its silver falling. | 4 |
| "The yard you rented" appears at arrival though the player never expanded the workshop. | 4 |
| The briefing said the player would leave "AD 172"; the departure was November 171. | 4 |
| `log` doesn't work after the game ends. | 3 |
| A founded School at strength 25 dissolved at once from bare decay; the founding text doesn't warn about that. | 1 |
| Text from another game appeared in one session. Most likely the test harness (a shared session file), not the game; worth checking in `play.py`. | 4 |

## Already fixed since this round (each with a regression test)

| Fixed | Reported by |
|---|---|
| Double jump skipping the first arrival (A4–A6) | 1, 2, 6 |
| `bury` and `deposit` refused at the second jump (A4); the second briefing showed things you couldn't act on (A5) | 3, 4, 7 |
| Menu said `join` (A1) | 3, 7 |
| Workshop fate told two ways (A2); "1 aurei" (A3); lowercase "the" (A7); Demetria unintroduced (A8); "repaired late" (A9); plague-year arrival (A10) | 1, 7 |
| Turn ended by a paydown (L2); fully committed turns passed without warning (L3); apprentices left unannounced (L4) | 1, 2, 3, 4, 6 |
| "you of the Guild thinks…" (L5); a thriving Cartel described as free traders (L6); empty invention clause (L7); `buy bank` (L8) | 1, 2, 3, 4, 6 |
| Stale scenes on in-era walks (L9); charter under glass (L10); `endow` hint without control (L11); "without your presence" (L15) | 1, 2, 3, 4 |

## Reports

Full reports: `t1.md`, `t2.md`, `t3.md`, `t4.md`, `t6.md` and `t7.md` in this folder. Testers 5, 8, 9 and 10 wrote no report; their play stopped at the usage limit.
