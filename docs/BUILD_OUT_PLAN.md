# Build-out plan: the rest of the game

> Corey, 2026-11-11: "Based on everything discussed so far, extrapolate the rest of the game and build it out."
> This is the order of work. It is built one slice at a time, each ending with passing tests, the browser playthrough and the documents in sync. Where a rule or number is needed that Corey has not given, Claude decides it in line with what was discussed, marks it `PROPOSED`, and logs it in DECISIONS.md under "decided on Corey's behalf" so he can overrule it. Nothing here overrides VISION.md's pillars or the hard constraints (SYSTEMS §14).
> Historical content is checked before it becomes game text.

## What exists (2026-11-11)
- The engine (Butterfly.Core): Rome AD 155–175, monthly turns, people, commissions, two Grand Challenges, institutions by invitation, the machine, the plague, one built jump and the first return; 584 tests.
- Stakes and Impact pass, slice 1a: age and healthy years (following Rome's medicine), catching the plague, fleeing by station, the fever, death that ends the run.
- The browser client: a walkable Subura, Forum, market, money changers and forges in placeholder blocks, with every action of the game reachable, played by script from the opening to the finished return.

## Order of work

### A. Make Rome worth walking (presentation)
| # | Slice | Done when |
|---|---|---|
| A1 | A link Corey can open: publish the browser build | He plays it on his own device |
| A2 | Touch controls and a phone layout checked on a small screen | The scripted playthrough passes at phone size |
| A3 | People in the world: every known person stands where they work, talking to them opens their scenes and work; decision cards come from the person in front of you | No job or invitation needs a menu to reach |
| A4 | The districts as places: Ostia and the Janiculum added (work already happens there); landmarks; day and season light | Each place in the game's text can be walked to |
| A5 | The return, walked: the same streets rebuilt from what happened (who is alive, what stands, what was copied), each return place at its own spot | Two different lives give visibly different streets |
| A6 | Replaceable art: models and portraits loaded from an asset manifest, blocks as fallback | Art can be swapped without code |

### B. Stakes and Impact pass (rules; `docs/STAKES_AND_IMPACT_PROPOSAL.md` §9)
| # | Slice |
|---|---|
| B1 | 1b Notice: readings, protection, the accusation and hearing, the trade's anger, being kept (STAKES_PROPOSALS S-10 to S-17) |
| B2 | 2 The machine: three ways to do each repair, risk bands, systems tests, jumping early, malfunctions |
| B3 | 3 Rooting: what lasts, what compounds; capability work reaches the domains and the Index |
| B4 | 4 Leverage and pacing: handing work to people, shorter waits, small jobs taper off after the first year |
| B5 | 5 Wonders: the geared calculator first (also the chronometer's proper repair), then spectacles and lenses, variolation, electro-gilding, the balloon, movable type |
| B6 | 6 Holders and divergence: who holds what while you are away; human events that move; returns that differ in kind |
After each: rerun `--p1-spread` and compare with the 2026-11-10 baseline (active players 97–108.5; no run lost).

### C. The story
| # | Slice |
|---|---|
| C1 | Someone to go home to, and a reason the inventor launched early |
| C2 | R-17: one concrete revelation at each return, and an answer by the end |
| C3 | A person who wants the machine or wants the inventor stopped |
| C4 | Notable Romans once the inventor is known: Galen (in Rome from 162), the court of Marcus Aurelius |
| C5 | Succession: an heir who can carry the journal and the machine, so death need not end the run |

### D. Beyond the first return
| # | Slice |
|---|---|
| D1 | Saves (needed before anything longer than one sitting) |
| D2 | The second life: playing on in the Rome you returned to (about AD 190–215), with the people's children |
| D3 | The second jump and return, with the long-absence model (generations, not people) |
| D4 | Eras 2 to 8 (GDD §5), one at a time, each with its own district set, people, work, crisis and wonder; the fuel puzzle joins here |
| D5 | The ending: home, and what the world became |

### E. Shipping
Apple devices (a wrapped web app or a native client over the same Core), audio, accessibility, art.

## Honest size
A1 to A3 and B1 are days of work each. B2 to B6 and C1 to C3 are a week or more each. D2 onward is the bulk of the game: each later era is roughly the size of everything built for Rome so far. The plan is worked in order; it is not one sitting.
