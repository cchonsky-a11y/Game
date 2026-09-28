#!/usr/bin/env python3
"""Play the console game one command at a time, for live (blind) AI playtesters.

  python3 playtests/ai/live/play.py setup                  build a frozen copy of the game (run once, by the coordinator)
  python3 playtests/ai/live/play.py <session> start <seed> start a new game
  python3 playtests/ai/live/play.py <session> "<command>"  type a command, or menu numbers like "3" or "3 7 1"

Each call replays the session's whole input file with --inputs (the game is deterministic), then prints
only what the game printed since the last call. The frozen copy means rebuilding the repo mid-test does not
change a game in progress. Files live in $BUTTERFLY_BLIND_DIR (default /tmp/butterfly-blind):
.game/ (the build and data), sessions/ (inputs and output per session), reports/ (testers' reports)."""
import os, shutil, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
BASE = os.environ.get("BUTTERFLY_BLIND_DIR", "/tmp/butterfly-blind")
GAME_DIR = os.path.join(BASE, ".game")
GAME = os.path.join(GAME_DIR, "Butterfly.Console.dll")
S = os.path.join(BASE, "sessions")

if len(sys.argv) >= 2 and sys.argv[1] == "setup":
    out = os.path.join(BASE, "build")
    subprocess.run(["dotnet", "build", os.path.join(ROOT, "src", "Butterfly.Console"), "-c", "Release", "-o", out, "-nologo", "-v", "q"], check=True)
    shutil.rmtree(GAME_DIR, ignore_errors=True)
    shutil.copytree(out, GAME_DIR)
    shutil.copytree(os.path.join(ROOT, "data"), os.path.join(GAME_DIR, "data"))
    os.makedirs(S, exist_ok=True)
    os.makedirs(os.path.join(BASE, "reports"), exist_ok=True)
    print("Ready: " + GAME_DIR)
    sys.exit(0)

if len(sys.argv) < 3:
    print(__doc__)
    sys.exit(1)
if not os.path.exists(GAME):
    print("The game isn't set up yet: run 'python3 playtests/ai/live/play.py setup' first.")
    sys.exit(1)

sess, cmd = sys.argv[1], sys.argv[2]
os.makedirs(S, exist_ok=True)
inp, seedf, prev = (os.path.join(S, sess + x) for x in (".inputs", ".seed", ".out"))
if cmd == "start":
    open(seedf, "w").write(sys.argv[3] if len(sys.argv) > 3 else "42")
    open(inp, "w").write("@autoend on\n")
    open(prev, "w").write("")
else:
    with open(inp, "a") as f:
        f.write(cmd.replace("\n", " ") + "\n")
seed = open(seedf).read().strip()
out = subprocess.run(["dotnet", GAME, "--seed", seed, "--inputs", inp, "--menu"],
                     capture_output=True, text=True, cwd=GAME_DIR).stdout
body = "\n".join(l for l in out.split("\n") if not l.startswith("Run fingerprint") and "@autoend" not in l).rstrip()
old = open(prev).read()
common = os.path.commonprefix([old, body])
cut = common.rfind("\n") + 1 if len(common) < len(old) else len(common)
open(prev, "w").write(body)
if cmd in ("quit", "exit") and "Run fingerprint" in out:
    print(out.split("Run fingerprint")[-1])
print(body[cut:].rstrip())
