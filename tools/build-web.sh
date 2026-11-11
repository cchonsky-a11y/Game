#!/usr/bin/env bash
# Builds the browser client (the walkable-district test): Butterfly.Core and Butterfly.Presentation compiled to WebAssembly,
# the page in src/Butterfly.Web/wwwroot, and a copy of data/. Output: build/web (git-ignored). Serve that folder with any
# static web server, e.g.  python3 -m http.server 8080 --directory build/web
set -euo pipefail
cd "$(dirname "$0")/.."
OUT=build/web
rm -rf build/web-publish "$OUT"
dotnet publish src/Butterfly.Web -c Release -o build/web-publish -v q -nologo
mv build/web-publish/wwwroot "$OUT"
rm -rf build/web-publish
find "$OUT" \( -name '*.br' -o -name '*.gz' -o -name '*.pdb' \) -delete
mkdir -p "$OUT/data"
cp data/tuning.json "$OUT/data/"
cp -R data/content "$OUT/data/content"
python3 - "$OUT/data/content" <<'PY'
import json, os, sys
d = sys.argv[1]
json.dump(sorted(f for f in os.listdir(d) if f.endswith('.json') and f != 'index.json'), open(os.path.join(d, 'index.json'), 'w'))
PY
echo "Built $OUT ($(du -sh "$OUT" | cut -f1), $(find "$OUT" -type f | wc -l) files)."
