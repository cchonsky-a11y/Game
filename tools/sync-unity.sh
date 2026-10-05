#!/usr/bin/env bash
# Builds Butterfly.Core and Butterfly.Presentation and copies them, with data/, into the Unity project (P2 graphical slice).
# Run from anywhere; run it again whenever src/ or data/ changes. The copies are git-ignored: src/ and data/ stay the truth.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build src/Butterfly.Presentation -c Release -v q -nologo
OUT=src/Butterfly.Presentation/bin/Release/netstandard2.1
UNITY=unity/ButterflyEffect/Assets
mkdir -p "$UNITY/Plugins" "$UNITY/StreamingAssets"
cp "$OUT/Butterfly.Core.dll" "$OUT/Butterfly.Presentation.dll" "$UNITY/Plugins/"
rm -rf "$UNITY/StreamingAssets/data"
mkdir -p "$UNITY/StreamingAssets/data"
cp data/tuning.json "$UNITY/StreamingAssets/data/"
cp -R data/content "$UNITY/StreamingAssets/data/content"
echo "Synced Core, Presentation and data/ into $UNITY."
