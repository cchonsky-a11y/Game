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
mv "$OUT/_framework" "$OUT/framework"
find "$OUT" \( -name '*.br' -o -name '*.gz' -o -name '*.pdb' \) -delete
base64 -w0 "$OUT/kit.glb" > "$OUT/kit.txt" && rm "$OUT/kit.glb"      # the art pack as text: see city.js loadKit
mkdir -p "$OUT/data"
cp data/tuning.json "$OUT/data/"
cp -R data/content "$OUT/data/content"
python3 - "$OUT/data/content" <<'PY'
import json, os, sys
d = sys.argv[1]
json.dump(sorted(f for f in os.listdir(d) if f.endswith('.json') and f != 'index.json'), open(os.path.join(d, 'index.json'), 'w'))
PY
echo "Built $OUT ($(du -sh "$OUT" | cut -f1), $(find "$OUT" -type f | wc -l) files)."
# A page for hosts that wrap the page themselves (a claude.ai Artifact): the body and the styles in one file, no <html>/<head>.
python3 - "$OUT" <<'PY'
import sys
out = sys.argv[1]
css = open(out + '/ui.css').read()
css = css.replace("padding:8px max(12px, env(safe-area-inset-right)) 8px max(12px, env(safe-area-inset-left));",
                  "padding-block:8px; padding-left:max(16px, env(safe-area-inset-left, 0px)); padding-right:max(16px, env(safe-area-inset-right, 0px));")
css = css.replace(":root { --paper", ":root { color-scheme: dark; --paper")
html = open(out + '/index.html').read()
body = html[html.index('<body>') + 6:html.index('</body>')]
open(out + '/artifact.html', 'w').write('<title>The Butterfly Effect</title>\n<meta name="claude:viewport-fit-sides" content="cover">\n<style>\n' + css + '\n</style>\n' + body)
PY
