# STAKES_PROPOSALS.md — rules and numbers awaiting Corey's approval (Stakes and Impact pass)

Like P0_PROPOSALS and P1_PROPOSALS: each entry is a proposed rule or number. Nothing here is canon or built until Corey approves it (slice 1a was approved and built on 2026-11-10; see the notes under its table); approved entries then go SYSTEMS → GDD Appendix A → tuning → DECISIONS → content → tests. Direction and reasons: `docs/STAKES_AND_IMPACT_PROPOSAL.md`.

## Slice 1a: a life that can be spent, and lost

Slice 1 is split in two. 1a is the inventor's own body: age, illness, death. 1b (Notice: what Rome thinks you are, and the first threat events) follows.

| Id | Area | Proposal | Status |
|---|---|---|---|
| S-01 | Life budget | The inventor's age is shown wherever the date is shown, with the healthy years left: "Age 36 · about 49 healthy years left". Starts at 28 (SYSTEMS §3); healthy years **85** (Corey, 2026-11-10; proposed as 70). One year older for each year lived in an era; none during a jump (canon). The departure briefing states it as a fact: "You are 40. You have spent 12 of your 57 healthy years in Rome." No new mechanic in Rome beyond S-02 to S-06; the point is that the player sees what staying costs. | **Approved 2026-11-10, built** |
| S-02 | Catching the plague | When an outbreak is resolved while the inventor is in Rome, the inventor may fall ill. Chance = **3.5** × the share of Rome that dies in that outbreak, at most 0.6 (history's 10% dead gives 35%; a contained 3% gives about 10%). (Proposed as 3; raised on Corey's "a little higher".) Precautions the inventor has actually taken each take a fifth off: the district fountain repaired; soap and boiled linen; quarantine rules or the quarantine response; a fever ward. All four: chance × 0.2. Drawn from the seeded generator; logged with the outbreak as its cause. | **Approved 2026-11-10, built** |
| S-03 | The illness | Ill: 0 Attention for 2 months, then −1 for 4 months (SYSTEMS §3 already has −1 when unwell). Work under way pauses, and people react through their existing lives (a client may walk; a friend may sit with you). 3 healthy years are lost for good. | **Approved 2026-11-10, built** |
| S-04 | Dying of it | Chance of death once ill: **0.30** (proposed as 0.25; raised on Corey's "a little higher"). Care lowers it: a physician who knows you (Serenus) −0.08; membership of the Physicians' Circle −0.05; distilled wine for wounds done −0.04; the Tiber Island sanctuary's benefactor −0.03. Never below 0.05. A careless player who stays through the outbreak: about 1 game in 10 ends here. A careful one: under 1 in 100. | **Approved 2026-11-10, built** |
| S-05 | Leaving the city | From the plague's second warning, a new choice: withdraw to the country until it passes (as the rich did). Cost: no work and no Attention in Rome for the months away (proposed 6), 15 aurei at AD 155 prices, and those who stayed remember it (regard −1 with people who know you well; a promise to Demetria counts as broken if it needed you there). Effect: the chance of falling ill × 0.25. Leaving by machine before the outbreak avoids it entirely: the first real reason to jump early. | **Approved 2026-11-10, built** |
| S-06 | Ordinary fevers | Each late summer in Rome (Sextilis–September), a 6% chance of the Roman fever: 0 Attention for 1 month, −1 the next. No death from it under 50; from 50, a 3% chance, lowered by the same care as S-04. Chance +1 point for each year of age over 40. | **Approved 2026-11-10, built** |
| S-07 | A run that ends | Death without a successor ends the run (canon: SYSTEMS §3, GDD §14). Succession and saves are not built, so in this prototype death is final. The game does not stop on a blank screen: it runs the same absence a jump would (drawn from the range the machine had), then shows what Rome kept of the inventor (the people echoes, what the work became, the journal's lines) under a closing line with the age and year of death. No arrival, no return places. The seed and the replay file allow the same game to be started again. | **Approved 2026-11-10, built** |
| S-08 | Saying what can end the run | The status screen and the plague's warnings say it plainly once it is true: "You can catch this." The departure briefing and the outbreak's decision card show the chance as a band (low / moderate / high), never the number or the outcome, as for other risks. | **Approved 2026-11-10, built** |

**Built 2026-11-10 (Corey: S-02 to S-08 good; 85 healthy years; deaths a little higher; fleeing allowed; death ends the run).** Two small numbers were needed that the approved text didn't give, and stay proposed:

| Id | Area | Proposal | Status |
|---|---|---|---|
| S-05a | Leaving the city | "People who know you well" = regard 2 or more (`health.withdraw.wellKnownRegard`). | Proposed |
| S-08a | Risk bands | Risk of falling ill shown as low under 10%, moderate under 25%, high from 25% (`health.band.*`). | Proposed |

Not built from S-03: people reacting to the illness (a client walking, a friend sitting with you). Work only waits. It needs authored scenes and is left for the slice that touches people.

**What this changes for existing tests.** As built, health draws from its own seeded generator derived from the run's seed, so the snapshot hash did not change and other draws were not reshuffled; determinism (same seed and inputs, same log) is kept and tested. The batch strategies and scripted profiles stay in Rome (they never withdraw); a death ends their run and is counted.

**Open questions for Corey.**
1. Is about 7 in 100 deaths for an unprepared player the right order of size, or should a first death be rarer (or commoner)?
2. S-05: is fleeing the city something the inventor should be able to do, given the promise to Demetria?
3. S-07: is "the run ends and the game shows what Rome kept" right for now, or should a death restart the era?
