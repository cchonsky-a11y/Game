# Automated playtests (AI)

Automated checks run **before** human testing. They change no game rules. **Nothing here is gate evidence.** The P0 gate needs the 5 human testers in `playtests/kit/`.

## How to run
```
playtests/ai/run.sh      # plays every script in scripts/, writes transcripts/ and checks/
playtests/ai/blind.sh    # rebuilds the three blind-review transcripts in blind/ (run run.sh first)
```

Scripted mode in the console (normal interactive play is unchanged):
```
dotnet run --project src/Butterfly.Console -- --seed 101 --inputs playtests/ai/scripts/01-early-fountain-keep-paydown.txt --checks out.txt
```
- `--inputs <file>`: one command per line, echoed into the transcript; `#` starts a comment. `@until <year>|era-end|ready: cmd; cmd; ...` repeats the commands (which should include `end`) until that year, the end of the era, or the machine is ready to jump. Loops are capped at 200 cycles and reported as possible dead ends.
- `--checks <file>` writes harness findings separately, so transcripts stay clean. It also writes `<file>.accepted.txt`, the commands the game accepted.
  - **Impossible states:** checked after every command. Negative gold, NaN or negative debt, levels outside 0–100, Attention outside 0–4, negative holdings, and strength or loyalty out of range.
  - **Arrival:** exactly 4 beats in order, 3 Echoes including the seeded choice, each surfaced in a beat.
  - **SYSTEMS rules against the log:** paydown and institution payments cost 1.5× prevention; decay per decade matches quality; holdings grow 0–1.5% a year and only in the 30-year window; corruption only in the window; no debt compounding after 30 years; the Index is the geometric mean; each jump lasts 25–60 years in 5-year steps, as drawn (SYSTEMS §11); turn length stays within the cap; three warnings precede the outbreak, in order by month (the third shares the outbreak's year, AD 166).
  - **Text:** unfilled `{placeholders}`, doubled spaces, repeated words ("the the"), empty text.

## Contents
- `scripts/`: 24 playthroughs (seed in each header). Refreshed 2026-09-29 for the machine repair rule: each script that jumps assesses and repairs the machine and puts its 60 aurei back first (`@until ready`); the Circle and the faction became the School and the Club, which the player founds and controls. 14–16 never repair the machine on purpose, so they end without an arrival. They cover early and late jumps; promises kept, broken, refused and unanswered; fountain, workshop and neither; endowing vs. paying down; with and without an audit charter; both leader integrities; and odd play (doing nothing, one domain only, jumping on turn 1, never jumping, invalid input, Protect everywhere).
- `transcripts/`, `checks/`: output of the last run.
- `bugs.md`: bugs ranked by severity, with what was fixed and what awaits approval.
- `text-clarity.md`: wording and legibility issues.
- `blind/`: the blind first-time-player review (**advisory, not gate evidence**). `reviewed-transcripts.txt` is exactly what the reviewer saw; `transcripts/` holds the same runs after the later clarity fixes.
