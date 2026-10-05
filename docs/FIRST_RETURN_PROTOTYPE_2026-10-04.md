# FIRST_RETURN_PROTOTYPE_2026-10-04.md: the first return (Part II prototype)

> What was built to test the project's central product question, and what was deliberately left out. SYSTEMS.md (§11) is canonical for the rules; this file explains the design and its limits. Scripted runs of the build are evidence about the systems, **not human testing**. The human experience validation is in `playtests/human/FIRST_RETURN_PROTOCOL.md`.

## 1. The hypothesis

**You are the one person who will not be there for the consequences.** The return after a jump should be the signature experience: recognize something you made, find that it doesn't match what you meant or remember, look closer, and reconstruct what probably happened, without the game saying "because you did X". Whether real players hesitate before leaving, care whom they leave, recognize consequences and want to explore is what the limited console test asks (PROTOTYPE_SCOPE.md, "Authorized").

## 2. Boundaries (what this prototype is not)

- One first return. There is no second era: after the return chapter, the existing simulation continues only so the second jump and regression checks still run.
- No 80–100-year jumps and no generational or population model. Elapsed time covers six people only.
- No change to Attention, the economy, routing weights, the age bonus (0), the jump range (25–60), Grand Challenges or politics.
- No save system (`docs/SAVE_STATE_BOUNDARY.md` lists the new state).
- R-17 is not explained. It appears at most as an unexplained mark, and only in runs that heard the warning.

## 3. The flow

1. **The machine is ready.** The existing armed-jump briefing now lists, as facts only, what you would leave unresolved:
   - the shared foot unfinished;
   - Pollio selling copies while nothing marks yours;
   - Marcus undecided;
   - Aulus's shaft part-built;
   - jobs half done.
   You can leave, or stay and settle some of it. The briefing never says what any of it will lead to.
2. **The jump and the four beats.** These are unchanged in structure and serve as a first impression. On the first arrival:
   - the six core people are not in the Personal echo beat; their age-correct story is in the return;
   - a return site that tells a beat line's story (the guild's guest book, Pollio's seats, the bad copies) replaces that line.
3. **The return chapter.** A numbered list of 4–6 places to look.
   - `visit <n>`: what you recognize, and what doesn't fit, with a lead.
   - `look closer <n>`: follows the lead.
   - `journal`: what you wrote at the time.
   - `done`: once 3 places have been seen. Never all of them.
   - The walk (`visit market` and the others) and `learn more` still work.
4. **The second jump** is offered only after `done`.

The commands reuse the console's arrival handling (`visit` takes a number or a site id before the walk's place names) and the existing `_jumpArmed` briefing. The numbered menu has a "The return · places to look" group.

## 4. The site model (21 authored patterns)

`data/content/returns.json` holds 18 site patterns and 3 journal anchors:

| Kind | Patterns | Evidence |
|---|---|---|
| Human | Felix, Cassianus, Serenus, Gaius, Marcus, Aulus | The person by elapsed time (§6), and what their work became |
| Technical | fittings (the shared foot), pumps (true seats), mills (the Janiculum shaft), drawings | Physical things and practices |
| Institutional | guildhall, circle, opening (the workshop or the fountain), varro (the senator's house) | Rules kept or reasons lost, rolls, plaques, client books |
| Unintended | copyseats (Pollio), copydrawings (Vettius's traced sheets), copybearings (brass blocks) | Copies that spread without the method |
| Journal | foot, seat, calix | The player's words then, against what survives now |
| Mystery | landing | The field where you came down. In a run that heard the R-17 warning, an old cut on a gatepost reads R XVII. |

Each pattern has requirements (does the site exist in this run) and state-conditioned variants: the first variant whose requirements hold decides the recognition, the contradiction, the lead, the investigation, the evidence level (obvious, plausible, contested or lost) and whether credit went to someone else. Every site has an unconditional last variant, so a site that exists always has words.

**Thread first (sharpening pass, 2026-10-04):** if any person's site carries a departure thread (their story turned on what you left resolved or unresolved: Marcus's path, Aulus's shaft, Cassianus's cellar pump against Pollio's copy), the closest such person takes the first human place. The other human place keeps the closeness order. No one is hard-coded, and without a threaded person the old order applies.

**Selection** draws no randomness:
- every candidate whose requirements hold is collected, with people closest first (regard, then authored order);
- the category order in returns.json (Human, Technical, Institutional, Human, Unintended, Journal, Mystery, then the rest) takes the next unused candidate of each kind;
- each kind is capped at `return.maxPerCategory` (2), and the total at `return.sitesMax` (6).
Mystery comes late, so it appears rarely (8 of 300 matrix runs).

**Content limit:** 21 patterns, within the 15–25 target and well under the 30 stop line.

## 4a. Primary sources, not omniscient history (design principle, sharpening pass 2026-10-04)

**A return exposes primary sources, not omniscient history.** The player meets what plausibly survived:
- people, and objects;
- account books and day-books;
- workshop marks and inscriptions;
- charters and minutes;
- copied journal lines;
- an institution's own records;
- physical evidence.

The simulation knows the true causal chain and uses it for grounding, testing and consistency. The player sees only the surviving traces. So:
- some causes are obvious, some contested, some lost;
- witnesses may disagree;
- records may be self-serving or incomplete, or accurate but missing context;
- an institution may keep the rule and forget the reason.

This is not deliberate incoherence: the player should usually be able to form a plausible interpretation.

Institutions speak through their records where the text allows:
- the guild's article on measures, cut in marble, gives the rule and the fine but not the reason;
- the charter's article is in a later hand, "added by vote of the members";
- the members' roll reads "peregrinus, absent, dues unpaid";
- the Circle's dining book has a later hand writing "guest — not of the Circle" and striking through Serenus's "who counts fevers";
- the fountain's plaque credits the street's magistrates.

## 5. Causal grounding

Every site keeps the log events behind its requirements and its chosen variant (`ReturnSite.Grounds`): the commission, challenge, capability advance, life event, decision, access stage or journal note that made it true. A site whose requirements are unconditional (where you came down, where you began with neither project) is grounded in the jump itself.

The player never sees these ids. The log records them as the immediate causes of each `return.visit` and `return.investigate` event, so every site can answer "why does this exist in this run?" Tests check that every site's grounds exist and precede the arrival.

**Recognition, contradiction, investigation.**
- A visit shows the recognition and the contradiction. The contradiction is where the attribution moved, the reason was lost, the copy won, or nothing was kept.
- The lead takes you to a record, an object or a person holding further evidence: an account book, a bar with two marks, a day-book, a charter in a later hand.
- Evidence can be obvious, contested (two stories) or lost (no surviving record). That uncertainty is deliberate.

## 6. Elapsed time (the core cast only)

This reuses the polish design's bands (`docs/P1_POLISH_2026-10-04.md` §6), keyed by the person's age rather than the arrival number. Six people have an age in AD 155 and a deterministic `livesTo` in people.json:

| Person | Age in 155 | Lives to |
|---|---|---|
| Felix | 40 | 68 |
| Cassianus | 45 | 70 |
| Serenus | 34 | 74 |
| Gaius | 30 | 72 |
| Marcus | 16 | 68 |
| Aulus | 48 | 70 |

The person's age at arrival gives the band:
- **self:** alive, below `return.elderAge` (60);
- **elder:** alive, past 60;
- **heirs:** died up to `return.heirsYears` (25) ago, carried by children, a household or an apprentice;
- **memory:** dead longer ago; a name on a shop, a stone, a casebook.

There is no mortality draw. Each person's text has the bands the P1 jump range can reach, falling back to a neighbour.

**Example, leaving in AD 163:**

| Person | 25 years (AD 188) | 60 years (AD 223) |
|---|---|---|
| Gaius | elder (at his shop, white-haired, deaf in one ear) | memory (FABIUS AND SONS; nobody knew a Gaius) |
| Marcus | self (fifty, three apprentices) | heirs (his son keeps the tally book by the household gods) |
| Felix | heirs | memory |
| Serenus | elder | heirs |
| Aulus | heirs | memory |

## 7. Departure-sensitive threads

Four threads, all from existing state. Each has a first-life source, an unresolved state the briefing names, a way to settle it before leaving, and a return manifestation:

| Thread | Source | Settled by | Left unresolved | Settled |
|---|---|---|---|---|
| foot | the standards challenge | finishing the gauges (reproducible) | every shop's foot drifts again; the oldest plug is yours, copied shorter each time | one foot, but under the guild's seal or "the Fabian foot" |
| copies | Pollio's copy of the seat | joining the guild (its seal) | Pollio's seats on half the quays, "stolen from Pollio by a foreigner" | outsold on the quays, sold to bakers who bail anyway |
| marcus | Marcus's path (power stage 3) | staying until he chooses | Gaius took him back as a hand | his tally book, or Priscus's works and its first board in Marcus's hand |
| shaft | the Powered Workshops challenge | finishing its six stages | a rusted shaft over a grain race; your drawing on a millwright's wall | a dozen sheds, and fewer men or more, depending on what you told Sextus Nerius |

**The same-seed test:** `StayingToFinishTheSharedFootChangesWhatTheReturnHolds`. The same life is brought to the moment the machine is ready with the gauges unfinished.
- Leaving now gives fittings `drift` and the journal line kept as luck.
- Staying to finish the gauges gives fittings `guildfoot` (misattributed, grounded in the gauges' capability advance) and the journal line kept as the guild's order.

**The reference playthrough** (seed 2, leaving in AD 159 against staying until AD 170) shows five sites changing: the fittings, the guild hall, Felix's yard, the market gates and the journal.

**The briefing warns, it doesn't predict (sharpening pass, 2026-10-04).** The departure briefing shows what you knowingly leave unresolved, not every consequence. The return records which threads the briefing named (`ReturnChapter.WarnedThreads`; `ReturnSite.Warned`). A site whose thread wasn't named, or that has no thread, is a grounded consequence nobody warned of.

For example, the briefing names Pollio and Marcus. The return also finds the shared foot credited to "the Fabian foot", grounded in the gauges' advance, which the briefing never mentioned. Or the player stays to finish the gauges, the briefing names nothing about the foot, and the return finds the guild owning the foot and charging for its stamp.

In the matrix, 68 threaded sites were named in the briefing and 795 were not. Nothing is a random surprise: every site keeps its grounding.

## 8. The journal

When the work behind a journal anchor first holds, the player's own sentence is written once and never changed, logged as `journal.note`:
- the master foot (shared measures reproducible);
- the true seat (valve seats reproducible);
- the calix (the allotment done).

`journal` lists the entries. A journal site shows THEN (the entry and its year) against NOW (what survives, chosen by state):
- the rule kept without its reason ("TRY EVERY GAUGE AT THE KALENDS. BREAK, DO NOT FILE. BY ORDER OF THE GUILD.");
- another name attached ("Gaius's rule");
- turned into superstition ("Never file a gauge. It brings bad work into the shop.");
- turned into its opposite (Pollio's "cut tight, and tight is true").

## 9. The return gate

The second jump needs `return.visitsRequired` (3) places seen, then `done`. Finishing is a choice, and exhaustive exploration is never required. With at most two of a kind, three visits always cover at least two kinds.

The public `Jump()` enforces the gate. `JumpForTests` (tests of the absence) bypasses it. The scripted runners follow a minimum protocol (`P1Campaign.FollowReturnProtocol`): visit the first three places in listed order, then `done`. That is infrastructure, not a persona strategy.

## 10. Metrics (300-run matrix, scripted)

- **Return:** started in 300 of 300 first jumps and completed in 300; the second jump was never offered early.
- **Sites:** 6 in 299 runs and 4 in 1; none below the minimum of 4.
- **Kinds:** Human 600, Institutional 300, Technical 299, Journal 299, Unintended 292, Mystery 8. Every run has 3 or more kinds.
- **People by band**, after the sharpening pass put threaded people first:

  | Person | Bands found |
  |---|---|
  | Felix | heirs 223, memory 12 |
  | Aulus | heirs 197, memory 85 |
  | Serenus | elder 30, heirs 30 |
  | Marcus | self 11, elder 4, heirs 1 |
  | Gaius | elder 2, heirs 4 |
  | Cassianus | heirs 1 |

  A threaded person comes first in 298 of 300 runs. It is mostly Aulus, because every scripted profile works on his shaft.

- **Threads:** foot (guildfoot 292, drift 5, fabianfoot 2), copies (outsold 266), shaft (shafts 228, idle 54), marcus (undecided 9, priscus 4, stayed 3).
- **Misattribution:** 4.4 sites per return on average.
- **Journal:** 2.3 lines written on average; a journal comparison among the sites in 299 runs.
- **Core metrics unchanged by the return:** 300 of 300 runs jumped twice; both challenges completed in 234; route tallies, the ledger (300 of 300), overbooking (0) and the archive (0 notes, 300 bribes) all as before.

## 11. Known limitations

- **Little variety in the scripted runs.** The profiles all leave in AD 163 and mostly join the guild, so most returns look alike (guildfoot, outsold, Felix's heirs). The variety lives in human choices: leaving early, refusing the guild, the fountain opening.
- **Marcus's thread rarely surfaces.** People are chosen closest-first, and Felix and Serenus usually outrank him. Marcus appears in other sites' evidence (his mark on Aulus's board).
- **Generic leads.** Leads are authored per variant. They are not yet a network where one site points at another.
- **Beats can still overlap a site.** Covered beat lines are suppressed only where a site declares it. Technical "level" lines can still read close to a site's recognition.
- **Second arrival.** It keeps the old arrival-number echoes. Elapsed time is modelled only for the first return.

## 12. Deferred

- the long-jump and generational model;
- elapsed time for everyone;
- a return after the second jump;
- the following, offices and politics in the return;
- a lead network;
- R-17's explanation;
- a save system;
- graphical UI.

What comes next depends on how human testers respond to the return.
