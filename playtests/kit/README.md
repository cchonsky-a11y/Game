# P0 playtest kit

For the 5 human tests in BUILD_GUIDE.md §9. **Run them only after every batch balance criterion passes** (see `../batch-report.md`).

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
- Say only: *"You're a modern inventor stranded in Rome in AD 155. Type `help` for commands. Please think aloud."*
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

## The target sentence
Listen for anything like: *"The plague hit harder because I left before the fountain was fixed, and the order I founded turned into something I didn't want."* Record it verbatim if it happens.
