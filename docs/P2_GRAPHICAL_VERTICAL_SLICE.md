# P2 graphical vertical slice

> Decided by Corey, 2026-10-05: the project moves to a minimal graphical vertical slice, and real human first-return testing waits until it is playable. P1 gameplay is frozen. This file describes what was built, how to run it, and what remains before the five-person test. **Nothing here has been tested by a human, and Unity itself could not be run where this was built** (see "Limitations").

## 1. Architecture

```
data/ (tuning, content)          the game's numbers and words: unchanged
src/Butterfly.Core               the simulation: the only place gameplay lives (unchanged by P2)
src/Butterfly.Presentation  NEW  engine-free adapter: screen models, the action catalog, one GameSession
src/Butterfly.Console            the text client, now drawing its menu from the same catalog
unity/ButterflyEffect       NEW  Unity 6 client: draws the Presentation models with UI Toolkit
```

- **One simulation.** `GameSession` owns the single `Simulation`. Unity never calls gameplay itself: it reads screen models (`Hud()`, `People()`, `Machine()`, `Return()` and the rest) and sends clicks back as commands (`Do("repair coil")`).
- **One catalog.** What the player can do now (`ActionCatalog`) moved out of the console's `Menu.cs` into Presentation, unchanged, and the console now draws its numbered menu from it. Both clients offer exactly the same actions. A port, not a rewrite: 25 scripted console transcripts with the menu on are byte-identical before and after.
- **Commands in the console's grammar.** A click runs the same command a player would type, against the same simulation methods. A test plays a game by clicks and then types the same commands into the console: the event-log hashes match.
- **No game state in the client.** Presentation keeps only what the client is showing (whether the departure screen is open, whether the arrival is still being read, a feed of what happened, the commands run so far). The arrival's beats, the return's places and their words all come from the simulation.
- **Views change nothing.** Every screen model is read-only. A test reads all of them, and looks around every map place, without changing the log hash or what happens next.

### Core changes
None. Core gameplay is untouched and the snapshot hash is unchanged. The console's only change is where its menu comes from.

## 2. Unity version and setup

- **Target:** Unity 6 (6000.0 LTS), UI Toolkit runtime, no extra packages beyond the built-in modules in `Packages/manifest.json`. No `ProjectVersion.txt` is committed: open the folder with the Unity 6 version you have installed.
- **Platforms:** Mac (landscape) first, then iPad (landscape). The layout switches to one column under 1100 reference pixels, so it can be adapted to a phone later.
- **How Unity gets the Core:** `tools/sync-unity.sh` builds `Butterfly.Presentation` (which brings `Butterfly.Core`) as .NET Standard 2.1 DLLs and copies them into `Assets/Plugins/`, and copies `data/` into `Assets/StreamingAssets/data/`. The copies are git-ignored; `src/` and `data/` remain the source of truth. The `Butterfly.Unity` assembly references the two DLLs explicitly.
- **Starting:** `ButterflyApp` boots itself after any scene loads (`RuntimeInitializeOnLoadMethod`). Pressing Play in an empty scene runs the game. A build needs one scene: **Butterfly → Create main scene** makes it.

### How to run (on a Mac)
1. Install the .NET SDK (8 or later) and Unity 6 through Unity Hub.
2. From the repository root: `tools/sync-unity.sh`
3. Unity Hub → Add → `unity/ButterflyEffect`, open it with Unity 6.
4. **Butterfly → Create main scene** (once), then Play. To make a tester build: File → Build Profiles → macOS → Build.
5. After changing `src/` or `data/`: run the sync again (or **Butterfly → Sync Core and data** in the editor).

Each run writes `runs/seed-<seed>.txt` under Unity's `Application.persistentDataPath` (on a Mac: `~/Library/Application Support/<company>/<product>/runs/`): the seed and every command, as a console inputs file. It is not a save; it lets an observer replay exactly what a tester did: `dotnet run --project src/Butterfly.Console -- --seed <seed> --inputs <file>`.

## 3. Project structure

| Path | What |
|---|---|
| `src/Butterfly.Presentation/GameSession.cs` | The session: phase, screen models, `Do(command)` |
| `src/Butterfly.Presentation/ActionCatalog.cs` | What can be done now, grouped by section and map place (shared with the console) |
| `src/Butterfly.Presentation/Models.cs` | HUD, decision card, people, journal, machine, work, departure, arrival, return models |
| `src/Butterfly.Presentation/RomeMap.cs` | The map's places (the walk's five places and the lodging) |
| `src/Butterfly.Presentation/ArtManifest.cs` | Every art slot, derived from content |
| `unity/ButterflyEffect/Assets/Butterfly/Scripts/ButterflyApp.cs` | Boot, panel settings, the redraw loop, replay file |
| `.../Scripts/Screens.cs` | Every screen, built in code |
| `.../Scripts/Ui.cs`, `ArtLibrary.cs` | Placeholder look; art slots with drawn fallbacks |
| `.../Editor/ButterflySetup.cs` | Editor menu: sync, create the scene |
| `.../Resources/ButterflyTheme.tss` | The default runtime theme |
| `tools/sync-unity.sh` | Copies the DLLs and data in |
| `tests/Butterfly.Core.Tests/P2PresentationTests.cs` | The adapter's tests (below) |

## 4. Screens

| Screen | Shows | Notes |
|---|---|---|
| **Opening** | The locked opening text ("The machine stops screaming before you do."), the opening art slot, the seed | Then the hour-one choice as a decision card |
| **HUD** (every era screen) | Date, Attention (free/total, pips, what is reserved), money and aurei, machine state; Rome, People, Work, Machine, Journal, Everything; End Month, Fast-forward; "Prepare to leave…" once ready | Spending the last Attention never ends the month; only End Month does |
| **Rome** | A stylized map with the six places, each showing how many things can be done there; a side panel with the place's art, a free look-around (`Simulation.Visit`), its actions, and (at the lodging) the news and the machine's readiness in the simulation's words | Only supported places are on the map |
| **Decision card** (narrative view) | Speaker, portrait slot, title, text, clickable choices, one at a time: the hour-one choice, Demetria's request, the plague, Rome's dated choices, an office, an invitation, a commission's terms | Can be set aside ("Not now"); it waits as a button in the HUD |
| **What happened** strip | The simulation's answer to the last action and what happened because of it (scenes, news), with portraits | Said once; no commentary added |
| **People** | Portrait, name, role, how they stand now, away or ill, household, what they care about and want | The same facts the console shows; no hidden state |
| **Work** | People's work (with the client's portrait and the terms or stage), Grand Challenges (question, stage, next step), what is under way, and the work actions | Not a dashboard: cards and sentences |
| **Machine** | Core art slot, the three systems with progress, gold restored, upgrades, the panel's own lines, repair actions; the readiness line once ready | R-17 stays unexplained: only the panel's words |
| **Journal** | The inventor's journal entries, what happened lately, the latest ledger lines, the seed and replay path | Reachable from the HUD, and from the return |
| **Everything** | Every other action available now, by section (institutions, policy, priorities, money) | The depth the other screens organize |
| **Departure** | The range, what you hold, the briefing (what is unresolved, as facts; the R-17 warning if heard), deposit and bury, **Stay in Rome** and **Leave now** | Opened only by the player; closable; leaving stays voluntary |
| **Arrival** | The jump art slot, "It carries you N years", the arrival's beats one by one | Then into the city |
| **Return** | The changed city as place cards with art slots; each marked only *not yet visited*, *seen* or *looked closer*; a selected place shows what you recognize, what contradicts it, and "Look closer: …"; walking around; the journal; **Finish looking** once the simulation allows | Never marks a place true, false, important or your doing (the model has no such field; a test checks) |

### Navigation flow
Opening → hour-one choice → Rome (map, sections, End Month) ⇄ decision cards as they come → … machine ready (the HUD says so; nothing opens by itself) → **Prepare to leave** → Departure (stay, or prepare and leave) → Arrival beats → Return (places, look closer, journal, walk) → Finish looking → the slice ends (walk on, learn more, or close). The second jump is not offered in the graphical client: nothing after the first return is built.

### P1 actions reachable graphically
Everything the console's numbered menu offers, through the screens above: the hour-one choice; Demetria's promise; the plague responses; Rome's dated choices; offices; odd jobs; the workshop's orders, apprentices and expansion; machine assessment, repairs, upgrades, the reference channel, restoring gold and buying it back; the money changers; public works; practical projects; commissions (look, accept, ask for more, decline); Grand Challenge stages; invitations; institutions (gifts, the bank's shares, attending and voting, founding, investing, overseeing, chartering); policy and advocacy; priorities; deposit and bury; the jump; the return's visits, looking closer, the journal and finishing; the walk and learn more. A test checks that every action group the catalog can produce has a home on a screen.

**Typed-only, as in the console** (not on the console's menu either, so not on a screen): `craft` and `consult` work, `paydown`, `endow`, `audit`, `orders` (last orders), `mentor`, `resign`, `apprentice dismiss`, `focus`, `why`, `log`. The departure screen therefore offers deposit and bury but not paying down debt, endowing, auditing or last orders. Whether any of these belong on a screen is a design question for Corey.

## 5. Placeholder art manifest

Art is replaced by dropping an image at its Resources path (`unity/ButterflyEffect/Assets/Butterfly/Resources/<path>.png`). No scene or script changes. Without an image, a tinted placeholder with initials is drawn. `ArtManifest.Slots()` lists every slot from content, so a new person or site gets a slot without code.

| Key | Resources path | Shows |
|---|---|---|
| map.rome | Art/maps/rome | The stylized map of Rome behind the places (places sit at fixed 0–1 positions in `RomeMap.cs`) |
| machine.core | Art/machines/core | The machine: the Temporal Field Core and its support systems |
| scene.opening | Art/scenes/opening | The opening: dirt, a mule cart, Rome alive |
| scene.jump | Art/scenes/jump | The jump: decades passing in the dark |
| place.lodging | Art/places/lodging | Your lodging, where the machine stands |
| place.subura | Art/places/subura | The Subura |
| place.curia | Art/places/curia | The Forum and the Curia |
| place.market | Art/places/market | The market |
| place.changers | Art/places/changers | The money changers |
| place.forges | Art/places/forges | The forges |
| portrait.felix | Art/portraits/felix | Felix, a freedman who fits out ships at Ostia and in Rome |
| portrait.cassianus | Art/portraits/cassianus | Cassianus, a grain and oil merchant |
| portrait.diodoros | Art/portraits/diodoros | Diodoros, Cassianus's porter, from Antioch |
| portrait.serenus | Art/portraits/serenus | Titus Aelius Serenus, a physician of the Subura |
| portrait.gaius | Art/portraits/gaius | Gaius Fabius Crispus, a bronze worker on the Clivus |
| portrait.livia | Art/portraits/livia | Livia, Gaius's wife, who runs the household and half the accounts |
| portrait.marcus | Art/portraits/marcus | Marcus Fabius Tertius, Gaius's nephew, at the bench |
| portrait.lucan | Art/portraits/lucan | Lucan, an old smith near the Porta Trigemina |
| portrait.aulus | Art/portraits/aulus | Aulus Septimius Crispus, a millwright on the Janiculum |
| portrait.sextus | Art/portraits/sextus | Sextus Nerius, owner of a finishing shop |
| portrait.hermogenes | Art/portraits/hermogenes | Hermogenes, Senator Varro's freedman steward |
| site.felix … site.landing | Art/sites/<id> | The return's places: felix, cassianus, serenus, gaius, marcus, aulus, fittings, pumps, mills, drawings, guildhall, circle, opening, varro, copyseats, copydrawings, copybearings, landing |

A return place that follows a person shows that person's portrait (older, in the art pass, is a later question); the others show their site art. Site art must not hint at what a place means: it shows the place.

**Still needed for the art pass:** all of the above (everything is placeholder now), a typeface, a real palette and USS styles in place of `Ui.cs`'s inline placeholder look, and icons for Attention and money.

## 6. Tests (scripted, not human)

`tests/Butterfly.Core.Tests/P2PresentationTests.cs`, 9 tests (14 cases):
- **A full playthrough by clicking alone**, six seeds: a scripted clicker that uses only what the screens offer (decision cards, the catalog, the departure screen, the return's places) plays from the AD 155 opening through machine repair, opens the departure screen, leaves, reads the arrival and finishes the first return. It never calls the simulation directly.
- **Clicking equals typing:** the clicked game's commands typed into the console give the same event-log hash.
- **Looking changes nothing:** every screen model and every look-around, then the game continues exactly as one where nobody looked.
- **The departure screen** opens only on request, stays closed when the machine becomes ready, closes on "stay" or any other action.
- **Return sites** carry only their state; unvisited places show nothing; places looked into show their finding.
- **The second jump waits** for the return, as in the console.
- **Every offered action** is understood by the session; **every action group** has a graphical home; **every person, place and return site** has an art slot.

All 570 tests pass, including determinism and the snapshot (unchanged).

## 7. Limitations

- **Unity was not run.** Unity cannot be installed in the environment this was built in. The runtime scripts compile with no warnings against Unity reference assemblies (UnityEngine 2021.3 modules from NuGet, used only for the check and not committed); the editor script could not be compile-checked. Unity 6-specific behavior (UIDocument created at runtime, the default theme import, the built-in font name) is untested. **The first open in Unity 6 is the real smoke test**, and may need small fixes.
- **No `.meta` files** are committed; Unity creates them on first import.
- **Labels are the console's labels** ("take <order> (… denarii)", "attend guild, vote …", "coinage sound"). They were kept so both clients stay identical; a wording pass for the graphical client is a presentation change and can be made in the catalog for both.
- **The plague's decision card** lists all three responses, as the console's menu does; the simulation refuses one that isn't available and says why.
- **No save system**, no audio, no animation, no accessibility work, no localization. The map is a placeholder with a drawn river.

## 8. What remains before the five-person test

1. Open the project in Unity 6 on the Mac, fix whatever the first run shows, and play from the opening through the finished return by hand (a developer smoke run, not a human test).
2. Make a Mac build that a tester can open by double-clicking (no command line), with a seed set by the observer.
3. Check the human-test readiness list below on that build.
4. A minimal art pass if Corey wants one before testing (at least portraits for the core cast and the map).
5. Update `playtests/human/FIRST_RETURN_PROTOCOL.md`'s Setup to name the build, and decide whether the observer reads the replay file.

### Human-test readiness checklist (to be checked in Unity on a Mac build; none of it is checked yet)
- [ ] A non-developer can start the game without a command line
- [ ] They can tell where they are (date, place) and click choices
- [ ] Attention, time and money are visible
- [ ] They meet characters (portraits, names) and see how they stand
- [ ] They can do work and repair the machine
- [ ] They can continue after the machine is ready, and choose when to leave
- [ ] The jump and the arrival play
- [ ] They can move around the return, look closer, open the journal and finish the return
