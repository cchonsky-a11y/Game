#!/usr/bin/env bash
# Runs every scripted playthrough in playtests/ai/scripts and saves transcripts and harness checks.
# Usage: playtests/ai/run.sh   (from the repository root)
set -u
cd "$(dirname "$0")/../.."
dotnet build src/Butterfly.Console -v q -nologo >/dev/null || exit 1
DLL=src/Butterfly.Console/bin/Debug/net8.0/Butterfly.Console.dll
mkdir -p playtests/ai/transcripts playtests/ai/checks
for script in playtests/ai/scripts/*.txt; do
  name=$(basename "$script" .txt)
  seed=$(grep -m1 '^# seed:' "$script" | awk '{print $3}')
  dotnet "$DLL" --seed "$seed" --inputs "$script" --checks "playtests/ai/checks/$name.txt" > "playtests/ai/transcripts/$name.txt" 2>&1
  code=$?
  if [ $code -ne 0 ]; then echo "CRASH | exit code $code" >> "playtests/ai/checks/$name.txt"; fi
  printf '%-40s exit %d  %s\n' "$name" "$code" "$(tail -n +2 "playtests/ai/checks/$name.txt" 2>/dev/null | cut -c1-110 | head -1)"
done
