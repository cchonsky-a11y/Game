# Bug list: automated playtests, 2026-09-26

24 scripted playthroughs plus 3 blind-review replays. **Harness result after fixes:** 0 crashes, 0 impossible states, 0 contradictions with SYSTEMS.md, all arrivals with 4 beats and 3 Echoes. The only dead end is script 14 (never jumps), which is by design: the player is never forced to jump.

Severity: **High** breaks a required feature or the arrival's credibility; **Medium** misleads the player; **Low** is polish.

## Fixed directly (no rule changes)

| # | Sev. | Bug | Found in | Fix |
|---|---|---|---|---|
| B1 | High | Personal echo read "In the the Physicians' Circle…" / "In the the Hospice…" whenever the keeper institution survived; the first fix then produced "In the an old house…". | 22, blind 1 | Templates take the article from the name; new test; the harness now flags repeated words. |
| B2 | High | C7 corruption risk was never visible in normal play. The brief was printed before any endowment and never refreshed (0 of 24 transcripts showed a risk line). | all | The brief is reprinted after every paydown, endow or audit during jump preparation. |
| B3 | Medium | `endow <inst> <more than you have>` failed outright, so large endowments silently didn't happen (8 scripts). | 02, 07, 10… | `endow <inst> all`. `paydown` already clamps. |
| B4 | Medium | "As history" Wrongness said "Nothing you did seems to have lasted" right after Recognition showed something that lasted. | 02, 20 | Reworded: "beyond the few things you recognize…". |
| B5 | Medium | An unanswered promise was described as a refusal ("would not promise"). | 06, 23 | New "unanswered" text; test. |
| B6 | Medium | Demetria asked for a promise when the player had never met her (no Circle). | blind 3 | She is introduced ("a Greek physician who treats the Subura's poor") when the Circle doesn't exist. |
| B7 | Low | Text referred to a fountain that was filled in: the keeper fallback "the house by the fountain", the hospice "beside the fountain", the physician "near the fountain". | 02, 18 | Neutral locations in the Subura. |
| B8 | Low | Captured faction: "Prefect of the City, holds the city" (redundant). The corrupted-institution text repeated the treasury line. | 10, 11 | Reworded. |
| B9 | Low | `why` listed "because: last year's loyalty fade" chains, and "(−1 a year from upkeep)" suggested that upkeep causes the loss. | 18 | Same-trend causes hidden; reworded. |
| B10 | Low | Hidden costs and state (blind review): no costs shown on institution actions or the audit; Attention left not shown after actions; no reason when hospice isn't offered; no warning that unspent gold is lost at the jump; the absence crisis list said "response: hospice" as if the player chose it. | blind 1–3 | Costs and Attention left in confirmations; audit cost in the brief; hospice requirement shown; lost-gold line in the brief; "chosen by your institutions after you left". |
| B11 | Low | "Chartered" institution shown as "bare" with no reason. | 02 | The brief explains "chartered but not endowed, so it counts as bare". |

## Open: gameplay or pacing (awaiting your approval; not changed)

| # | Sev. | Finding | Evidence | Recommended fix |
|---|---|---|---|---|
| G1 | High | **Collapse spiral without a surviving, endowed institution.** Leaving early or leaving debt behind ends at Index 14–21 with 7–17 catastrophic recurrences. Rome is "half-empty" in most such arrivals, so the arrival stops varying. | 03, 06, 10, 15, 16; blind 2–3 ("felt punished rather than curious") | After any plague, block recurrence for 3 decades (historical plagues were generations apart), **or** cap recurrence severity at "grave". Either keeps neglect costly without an identical arrival every time. |
| G2 | Medium | Mid-to-late era has little to decide once the plague has passed ("oversee, work, end" on repeat). | blind 1, 3; 02, 13 | Confirm with humans first (partly an artifact of scripted play). If confirmed: one authored decision per year after the plague (e.g., the opening it created), or end the era when the plague has passed plus two years. |
| G3 | Medium | Auto-advance skips turns when no gold-costing action is affordable, even though overseeing or mentoring is still possible. The blind reviewer wanted to act in skipped turns. | blind 1 | Split `end` (one turn) from `wait` (advance until something happens). This changes the P0 pacing rule in PROTOTYPE_SCOPE, so it needs your decision. |
| G4 | Medium | The crisis clearing all debt reads as "the plague made things better" (every tier goes back to Stable). | blind 1, 3 | Show one line at the outbreak: "The crisis wipes out the debt, but the damage stays." Rule unchanged; the wording is yours. |
| G5 | Low | A "High" corruption risk that never happens feels meaningless. | blind 3 | Show rough odds with the band ("High: about 1 in 3 over 30 years"). This goes beyond C7's band-only rule, so it needs your OK. |
| G6 | Low | Attention looked non-binding to the blind reader, despite demand of 122 vs supply 80 on paper. Scripted players used 1–3 per turn. | blind all | Watch in human tests before changing anything. |
| G7 | Low | Never jumping has no ending; the game continues indefinitely. | 14 | After the era, say "The prototype ends when you jump." |
| G8 | Low | "Mild" for about 59,000 dead is jarring. | blind 1 | Relabel the severity words (e.g., "lighter than feared" / "grave" / "catastrophic"). Wording is yours. |

---

# Live blind testers, 2026-09-28 (seeds 611–620; reports in `playtests/ai/live/reports/2026-09-28/`)

Finished reports: t2 (history buff, 8/10), t6 (profit-seeker, 7/10), t7 (early jumper, 5/10). The other seven were stopped by usage limits partway through the era.

## Fixed (bug and text only; no rule changed)

| # | Bug | Found by | Fix |
|---|---|---|---|
| A1 | The menu said `join sanctuary`; typing it was "Unknown command". | t7 | The label names the real command: `buy into sanctuary, 1%`. |
| A2 | Workshop fate told two ways: Recognition "a stable now" / "fallen in", `visit forges` "a single cold forge". | t7 | The forges use the fate's own words on each arrival. |
| A3 | "1 aurei". | t7 | One formatter (`Simulation.Aurei`) for every count of gold. |
| A4 | At the second jump, `bury`/`deposit` were offered but refused; the gold was lost. | t7 | Both work after an arrival (no Attention: no turns remain); the menu offers them for gold over the purse. |
| A5 | The second jump's briefing showed Rome's debts and offered paydown, endow, audit, exchange. | t7 | After an arrival it shows only the range and your gold. |
| A6, double jump | One `jump` at the first arrival jumped again at once (the first arrival was never walked); whether it did depended on what was typed in between. | t2, t6 | The jump flag is cleared when the jump is made. |
| A7 | "You spoke for X. the Y lead." and "the Cartel lead, as you want" after voting Free Traders. | t7, t6 | "Leading now: Y", and "as you want" follows the vote just cast. |
| A8 | A player who left in AD 159 was told on the second arrival about "Demetria's list of those who stayed". | t7 | Whether the warnings had begun is settled at the first departure; the first-arrival line no longer names her. |
| A9 | "the fountain you repaired late". | t7 | "the fountain you later repaired". |
| A10 | Arriving in AD 189 during the recurrence, only `learn more` said so. | t7 | Wrongness beat and the Subura show a sick city in a recurrence year. |
| Crash | `jump`, `jump` on an unready machine threw and ended the game. | scripted playtests | Armed only when the machine is ready. |

Note: the 24 scripts in `scripts/` predate the rule that the machine must be repaired before a jump, so none of them reaches an arrival any more (DEAD END in `run.sh`). Their saved transcripts are from before that rule. They need rewriting (assess, repair, restore the gold) before they check arrivals again.

## Open: bugs and text (fix freely, with a test)

| # | Bug | Found by |
|---|---|---|
| L1 | Auto-advance let an open prompt lapse unseen (the treasury loan), costing loyalty. | t6 |
| L2 | Paying down debt with 0 Attention silently ends the turn; the next `end` then skips a whole turn. | t2, t6 |
| L3 | Starting a commitment that uses all Attention passes several turns at once ("4 turns pass") without warning first. | t2 |
| L4 | Apprentices left over unpaid wages; it appeared only in `log`, not in the turn summary. | t2 |
| L5 | Once the player heads the guild, texts put "you" in the head's place ("you of the Guild thinks better of you"). | t6 |
| L6 | Guild text "still arguing for open markets and honest coin. Its head is … Master of the Cartel." | t6 |
| L7 | The inventions list shows an empty reward clause: "the guild:  (if you're a member)". | t2, t6 |
| L8 | `buy bank` without a percent fails (the bank's first purchase is 5%); default it to the first purchase. | t6 |
| L9 | Walking around during the era never ages: in AD 173 the Subura's fountain "runs brown" (fixed in 155) and the smith is still "clearing space for a second forge". | t2 |
| L10 | Discovery says the Circle keeps "a copy of your charter under glass" though it was never chartered. | t2 |
| L11 | The jump briefing suggests `endow circle`/`endow guild` to a player below the 50% that endowing needs. | t2 |
| L12 | `why plague` shows "about 2%" for every response; the hospice's cost wasn't shown before choosing. | t2 |
| L13 | "Neither camp leads yet" after 12–0 votes for one camp. | t2 |
| L14 | Grammar: "Your work as senior physician … keep Medicine up"; "Theon … follows their own judgment"; the menu truncates event titles ("A levy for: pay your share…"). | t2 |
| L15 | The School's log says "Without your presence…" while the player is in Rome. | t2 |
| L16 | The forges say "the water wheel you paid for" when the guild lent the money. | t2 |
| L17 | On the second arrival, the Subura's "when you left" population is the first arrival's, not the departure from Rome's. | t2 |
| L18 | Circle seniority stops at 40% with no explanation until the institutions screen. | t2 |

## Design observations for Corey (not changed)

- **Leaving early is easy** (t7): gold back by turn 6 through consulting, repairs in parallel, one membership frees steps; left in AD 159 and missed the plague, the promise and Rome's choices.
- **Consulting dominates income** (t6, t7); the warehouses and workshop orders look poor beside it.
- **Middle years feel like a grind** (t2, t6: "consult, end, consult, end"); offices take much of the Attention.
- **The office offer repeats every January** after being declined (t2, t6).
- **The fountain feels weak** for its cost (t7: +1 Medicine for 900 denarii).
- **The Curia says the same line on both arrivals** (t7).
- **What testers remembered were narrative echoes**, not numbers: "For the one who stayed", the name on the officers' roll, the workshop turned stable, "You aren't in any of them", the Cartel their guild became.
- **Question:** after an arrival, `deposit` goes to the same banking house of Octavius whatever became of it. Should it depend on the bank's fate?
