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
