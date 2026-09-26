#!/usr/bin/env bash
# Builds the three blind-review transcripts: replays the accepted commands of three scripted runs
# (refused commands change nothing, so the run is identical; the fingerprints are compared).
# Run playtests/ai/run.sh first. Usage: playtests/ai/blind.sh (from the repository root)
set -u
cd "$(dirname "$0")/../.."
DLL=src/Butterfly.Console/bin/Debug/net8.0/Butterfly.Console.dll
mkdir -p playtests/ai/blind/scripts playtests/ai/blind/transcripts
i=0
for n in 02-late-workshop-keep-endow-audit 03-early-workshop-break-promise 10-faction-big-endowment-no-audit; do
  i=$((i+1))
  seed=$(grep -m1 '^# seed:' "playtests/ai/scripts/$n.txt" | awk '{print $3}')
  { echo "# Blind-review run $i: the accepted commands of $n"; echo "# seed: $seed"; cat "playtests/ai/checks/$n.accepted.txt"; echo quit; } > "playtests/ai/blind/scripts/player-$i.txt"
  dotnet "$DLL" --seed "$seed" --inputs "playtests/ai/blind/scripts/player-$i.txt" > "playtests/ai/blind/transcripts/player-$i.txt"
  a=$(grep -o "fingerprint (seed [0-9]*): [0-9a-f]*" "playtests/ai/transcripts/$n.txt")
  b=$(grep -o "fingerprint (seed [0-9]*): [0-9a-f]*" "playtests/ai/blind/transcripts/player-$i.txt")
  [ "$a" = "$b" ] && echo "player-$i ($n): identical run ($b)" || echo "player-$i ($n): MISMATCH $a vs $b"
done
