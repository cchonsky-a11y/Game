# P0 playtest kit

For the 5 human tests in BUILD_GUIDE.md §9 (**deferred on 2026-10-02 to a later graphical milestone**; this kit predates P1). The batch gate (`../batch-report.md`) passes for late jumps; for early jumps founding the school wins most automated runs, and Corey decided (2026-09-28) to let the human testers judge that rather than tune it. Watch for it (see below).

## Before each session
1. Pull the branch and run `dotnet test`. Everything must pass.
2. Pick the tester's seed from the table below (each tester gets a different seed; don't reuse seeds across testers).
3. Copy `session-template.md` to `playtests/YYYY-MM-DD-tester-N.md`.
4. Start the game: `dotnet run --project src/Butterfly.Console -- --seed <seed>`.

| Tester | Seed |
|---|---|
| 1 | 101 |
| 2 | 202 |
| 3 | 303 |
| 4 | 404 |
| 5 | 505 |

## During the session
- Say only: *"You're a modern inventor stranded in Rome in AD 155. After each move the game lists what you can do, numbered: type a number, or several like `3 7 1`. You can also type commands; `help` lists them. Please think aloud."*
- The game has one era (AD 155 to about 175), a jump of 25–60 years, an arrival and a walk around the Rome you find, and then an optional second jump. A full session takes about 60–90 minutes.
- **No coaching.** If asked how something works, answer: *"What do you think it does?"* You may point them to `help` and `why <thing>`.
- Note the time of the jump and whether they read the pre-jump briefing.
- When they quit, copy the **run fingerprint** the game prints into the session notes. With the seed and their commands it identifies the run.

## After the arrival
Ask the four questions **in order, word for word** and record the answers verbatim:
1. What were you choosing between during the Rome era?
2. What changed in the world you came back to?
3. Did any of that feel connected to you? Which part?
4. Would you jump again to see what happens next?

Then fill in the scoring section of the session file. After all 5 sessions, fill in `scoring-sheet.md` and record the gate result in `docs/DECISIONS.md`.

## Watch for (open questions from the automated runs)
- **Does founding one institution early feel like the obvious move?** Automated players who found the School of the Fountain and leave early beat everyone else; broad players win if they stay late. Note whether testers found anything, when, and whether they talk about it as a choice.
- **Do they discover free markets?** Rome keeps history's policy unless the player drives it (a voice in a Senate faction or the club, or advocacy while present). Note whether they find `policy` / `advocate` and why they chose their stance.
- **Workshop orders vs odd work:** do they weigh money against standing?
- **The menu:** did they use numbers or commands, and did the menu help them see their choices?

## The target sentence
Listen for anything like: *"The plague hit harder because I left before the fountain was fixed, and the order I founded turned into something I didn't want."* Record it verbatim if it happens.
