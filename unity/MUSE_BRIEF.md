# Brief for Muse: setting up and finishing the Unity client of The Butterfly Effect

> **Paused 2026-11-11 (Corey).** The game is now presented as a walkable Rome in the browser (`src/Butterfly.Web`; see PROTOTYPE_SCOPE.md). The Unity client is kept but not maintained, and this document describes it as it stood.

> Give Muse this whole brief, then one task (§9). Maintained alongside `docs/P2_GRAPHICAL_VERTICAL_SLICE.md`; if the two disagree, that document wins.

---

## 1. What this is and what you're doing

*The Butterfly Effect* is a turn-based historical strategy game set in Rome, AD 155. A stranded time-travelling inventor lives in Rome, takes on work, makes friends, repairs a time machine, decides when to leave, jumps decades forward and walks through the changed city.

The game already exists as a C# simulation with a text console, and a Unity 6 client for it has been written but **never opened in Unity**. Your job is to **set the Unity project up on this Mac, get it running, and finish its presentation**: fix what breaks on first open, style it, add placeholder art and polish the layout. You work on presentation only. You never write game rules.

You have this Mac, a terminal, the repository and the Unity Editor. Work directly: install, run, open, click Play, read the Console, edit files, re-run. **Report only what you actually ran and saw.** If something needs a decision about the game, or a change outside your area (§3, rule 12), stop and ask the developer.

## 2. The architecture (non-negotiable)

```
src/Butterfly.Core          C# library: the simulation, ALL gameplay. Read it if useful; never edit it.
src/Butterfly.Presentation  C# library: read-only screen models + Do(command). Never edit it.
data/                       the game's numbers and words. Never edit it.
unity/ButterflyEffect       Unity 6 project: your work. Draws the models with UI Toolkit, sends commands.
```

- `tools/sync-unity.sh` builds the two libraries as DLLs and copies them, and `data/`, into `unity/ButterflyEffect/Assets/Plugins/` and `Assets/StreamingAssets/data/`. Those copies are git-ignored and generated: **never edit them by hand.** Re-run the script (or the editor menu Butterfly → Sync Core and data) whenever `src/` or `data/` changes.
- The **only** way the client changes the game is `GameSession.Do(command)`, plus three presentation-state calls: `OpenDeparture()`, `CloseDeparture()` and `FinishArrival()`. Everything else on `GameSession` is a read, and reading never changes the game.
- The client keeps only *what it is showing* (which screen, which place is selected, whether a card was set aside). It never keeps or computes game state.

## 3. Hard rules: work that breaks any of these is rejected

1. **No gameplay in Unity.** Never compute or decide Attention, money, prices, dates, months, repair progress, whether an action is allowed, which scene happens, who someone is, or anything about the return. Show what the models say; send what the actions say.
2. **Don't filter, reorder, rename or invent actions.** Show the `GameAction`s the models give, with their `Label`, and send exactly their `Command`. A button never builds its own command except those in §5.
3. **Don't rewrite the game's words.** Model text (labels, scenes, the arrival, the return, the journal) is shown as given. No added commentary, summaries, explanatory tooltips or reaction lines ("That changes things!"). Interface words (button captions such as "Back", "People", "Not now", headings) are fine.
4. **Determinism.** Never use `UnityEngine.Random`, `System.Random`, `Time.time`, `DateTime.Now` or frame timing for anything that reaches the game. The seed is picked once in `ButterflyApp.PickSeed`; leave it. Animation timing is fine.
5. **No saves.** No PlayerPrefs, save files or serialized game state. The replay file in `ButterflyApp.WriteReplay` stays as it is.
6. **No networking, analytics, ads, purchases, or calls to any AI model** at runtime.
7. **Out of scope. Don't build:** audio, 3D or character movement, combat, minigames, a second era, quest logs, family trees, achievements, scoring, extra settings menus, localization.
8. **The return's places** are shown only as *not yet visited*, *seen* or *looked closer* (`SiteState`). Never mark, color, rank, badge, sort or hint at a place as true, false, important, correct, or caused by the player. Site art shows the place, not its meaning.
9. **Leaving is the player's choice.** Only the player's click on "Prepare to leave…" opens the departure screen. Never open it when the machine becomes ready.
10. **Content limits.** No depiction of religious founders. Roman slavery only as part of the backdrop, never glamorized. No gore.
11. **UI Toolkit only**, built in code as the existing scripts do, or USS stylesheets. No uGUI/Canvas, no third-party or Asset Store packages. Unity 6 (6000.0 LTS) APIs.
12. **Edit only files under `unity/ButterflyEffect/`** (scripts, styles and art under `Assets/Butterfly/`, plus the files Unity itself generates: `ProjectSettings/`, `Packages/packages-lock.json`, `.meta` files). If a fix seems to need `src/`, `data/`, `tests/` or `tools/`, stop and tell the developer exactly what is missing and why. Don't work around it in Unity.
13. **Testing is not human testing.** Playing the game yourself is a developer smoke run. Never call it a playtest or report it as player feedback.

## 4. The API (Butterfly.Presentation, namespace `Butterfly.Presentation`)

Read `src/Butterfly.Presentation/Models.cs` and `GameSession.cs` for the exact types. In short, `GameSession` (the app holds one as `app.Session`):

| Member | What it gives |
|---|---|
| `Phase Phase` | `Opening`, `Rome`, `Departure`, `Arrival`, `Return`, `AfterReturn`, `Ended` (the inventor died: show `Ending()`): which top-level screen to show |
| `HudModel Hud()` | Date, Year, Month (1–12), MonthNumber, AttentionFree, AttentionTotal, Reserved, Money, Aurei, Machine, MachineReady, Away, Life (age line), Health (lines; may be empty) |
| `OpeningModel Opening()` | Title, Paragraphs |
| `DecisionCard? CurrentDecision()` | Key, Title, Speaker, Portrait (art key), Text, Choices. Null when nothing waits |
| `List<ActionGroup> Actions()` | Everything available now. `ActionGroup`: Key, Title, Section, Place, Items (`GameAction`: Label, Command) |
| `List<ActionGroup> ActionsAt(placeId)` | The actions at one map place |
| `string LookAround(placeId)` | A free description of a place now |
| `List<string> News()` | The talk of the town |
| `string ReadyLine()` | The machine's "ready" sentence, or "" |
| `List<PersonCard> People()` | Id, Name, Role, Status, Away, Household, CaresAbout, Wants, Portrait |
| `JournalModel Journal()` | Entries, Recent (`FeedLine`s), Ledger |
| `MachineModel Machine()` | Assessed, StepsDone/Total, GoldRestored/Needed, Ready, Panel, Systems, UpgradesDone, Actions |
| `WorkModel Work()` | Commissions, Challenges, UnderWay (`WorkItem`: Title, Who, Portrait, State), Actions |
| `DepartureModel Departure()` | Range, Briefing, InHand, Prepare, CanLeave |
| `EndingModel? Ending()` | Title, Lines: what Rome kept, once the inventor has died. Null otherwise |
| `ArrivalModel? Arrival()` | JumpYears, DepartureYear, ArrivalYear, Beats (Name, Text) |
| `ReturnModel? Return()` | ArrivalYear, YearsAway, Sites, Seen, VisitsRequired, CanFinish, Finished, HasJournal |
| `string LearnMore()` | The optional "Learn more" text after arrival |
| **`Outcome Do(command)`** | **Runs one action.** Ok, Message, Feed (`FeedLine`s: Text, Speaker, Portrait, Stamp), Navigate, Quit |
| `OpenDeparture()` / `CloseDeparture()` / `FinishArrival()` | Presentation state only |

`ReturnSiteModel`: Number, Id, Place, State, Recognition, Contradiction, Lead, Finding, Portrait, Art, VisitCommand, LookCloserCommand.
`RomeMap.Places`: six places (Id, Name, Caption, X, Y in 0–1, ArtKey): lodging, subura, curia, market, changers, forges. Don't add places.

**Don't use `app.Session.Sim`** (the raw simulation) beyond the one existing use (`Sim.Arrived` in `Screens.cs`). If a model lacks something you need, ask the developer for it.

## 5. Commands a button may send

- Any `GameAction.Command` from a model, exactly as given.
- `end` (End Month) and `wait` (fast-forward; it stops on anything that needs the player).
- `jump` from the departure screen's "Leave now", only when `Departure().CanLeave`.
- `site.VisitCommand` / `site.LookCloserCommand` on the return.

Spending the last Attention never ends the month. Only End Month does. Never auto-advance.

## 6. What's in `unity/ButterflyEffect/Assets/Butterfly/`

| File | Role |
|---|---|
| `Scripts/ButterflyApp.cs` | Boots itself in any scene, creates the UIDocument and PanelSettings (1600×1000 reference; narrow layout under 1100 px), owns the session, `Act(command)` → `Do` → `Refresh()`, writes the replay file |
| `Scripts/Screens.cs` | Every screen, in code: Opening, HUD, Rome map and place panel, Decision card, What-happened strip, People, Work, Machine, Journal, Everything, Departure, Arrival, Return |
| `Scripts/Ui.cs` | UI builders and the placeholder palette |
| `Scripts/ArtLibrary.cs` | Art slots: loads `Resources/Art/<kind>s/<name>.png`, else draws a tinted box with initials |
| `Scripts/Butterfly.Unity.asmdef` | References `Butterfly.Core.dll` and `Butterfly.Presentation.dll` explicitly |
| `Editor/ButterflySetup.cs` | Menu: Butterfly → Sync Core and data; Butterfly → Create main scene |
| `Resources/ButterflyTheme.tss` | The runtime theme |

Known gaps: no `ProjectSettings/` and no `.meta` files are committed (Unity creates them on first open), no Unity version is pinned, the editor script has never been compiled, and Unity 6 specifics (a UIDocument created at runtime, the default theme import, the built-in font name `LegacyRuntime.ttf`) are untested. Keep the existing structure and names, and `#nullable enable`. Fix with the smallest change that works.

## 7. Placeholder art

Art is swapped by putting a PNG at its path. No code changes. Make it clearly placeholder in style (flat, painterly or sketched), consistent across the set, Roman 2nd century AD, and **never hinting at story meaning**. Set imported textures to Texture Type *Sprite (2D and UI)* or *Default*, so `Resources.Load<Texture2D>` finds them.

| Kind | Path under `Assets/Butterfly/Resources/` | Shown at about | Make at |
|---|---|---|---|
| Portraits: felix, cassianus, diodoros, serenus, gaius, livia, marcus, lucan, aulus, sextus, hermogenes | `Art/portraits/<id>.png` | 110×130 (also 40×40) | 440×520, head and shoulders |
| Map places: lodging, subura, curia, market, changers, forges | `Art/places/<id>.png` | 340×130 | 1360×520 |
| Rome map | `Art/maps/rome.png` | the map panel | 2000×1250 |
| Machine core | `Art/machines/core.png` | 240×240 | 960×960 |
| Opening, jump | `Art/scenes/opening.png`, `Art/scenes/jump.png` | 860×220, 860×160 | 3440×880, 3440×640 |
| Return sites: felix, cassianus, serenus, gaius, marcus, aulus, fittings, pumps, mills, drawings, guildhall, circle, opening, varro, copyseats, copydrawings, copybearings, landing | `Art/sites/<id>.png` | 150×90 | 600×360 |
| HUD icons: attention, money | `Art/icons/<id>.png` | about 20×20 | 64×64, transparent |

The people (for portraits only):
- Felix: a freedman who fits out ships at Ostia and in Rome.
- Cassianus: a grain and oil merchant.
- Diodoros: Cassianus's porter, from Antioch.
- Titus Aelius Serenus: a physician of the Subura.
- Gaius Fabius Crispus: a bronze worker on the Clivus.
- Livia: Gaius's wife, who runs the household and half the accounts.
- Marcus Fabius Tertius: Gaius's nephew, at the bench.
- Lucan: an old smith near the Porta Trigemina.
- Aulus Septimius Crispus: a millwright on the Janiculum.
- Sextus Nerius: owner of a finishing shop.
- Hermogenes: Senator Varro's freedman steward.

The machine: a modern, damaged device. A sealed Temporal Field Core with improvised support systems around it, some parts stripped for their gold. Never explain or label it.

## 8. How to work in git

- Repository: `https://github.com/cchonsky-a11y/Game`. Start from the default branch and do all your work on a branch named **`muse/unity-slice`**. Never push to any other branch and never merge.
- Commit after each task (or each meaningful fix) with a clear message saying what changed and why. Commit the Unity-generated `ProjectSettings/`, `Packages/packages-lock.json` and `.meta` files for `Assets/Butterfly/`. Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds, or the synced DLLs and data (the project's `.gitignore` covers them).
- Before every commit, run `dotnet test` from the repository root. All tests must pass. You shouldn't have touched anything they cover; if one fails, stop and report it.
- Push `muse/unity-slice` when a task is done, and report what you did.

## 9. Tasks

Do them in order. Each ends with a commit and a short report.

**Task 0: Set up.** Install what's missing: Git, the .NET SDK 8 or later (`dotnet --version`), Unity Hub, and Unity 6 (6000.0 LTS) with Mac Build Support. Clone the repository, create `muse/unity-slice`, and run `tools/sync-unity.sh` (`chmod +x` it first if needed). It must end with "Synced Core, Presentation and data/ into …". If it fails, stop and report the output: that belongs to the core team. Add `unity/ButterflyEffect` to Unity Hub and open it with Unity 6, accepting the version prompt and creating or upgrading the project (enter Safe Mode if offered). Let the import finish. Commit the generated `ProjectSettings/`, `packages-lock.json` and `.meta` files.

**Task 1: First-open fixes.** Read the Console. Fix every red error in `Assets/Butterfly/` with the smallest change, one line per fix saying what was wrong. If an error comes from the DLLs or anything outside your area, don't change it: report it. When it compiles cleanly, run Butterfly → Create main scene, add the scene to the build profile, and press Play. Check that the opening ("The machine stops screaming before you do.") shows and the first choice can be clicked. Fix runtime exceptions the same way.

**Task 2: Smoke run.** Play from the opening through a few months: the map and its places, People, Work, Machine, Journal, End Month, a decision card set aside and reopened. Then use fast-forward and the game's own actions to reach the machine being ready, open and close the departure screen, leave, read the arrival and finish the first return. Note anything that is broken, unreadable, clipped or unreachable, with screenshots. Fix what's broken in presentation; report anything else. This is a developer smoke run, not a playtest. Afterwards, check that the replay file written under `~/Library/Application Support/…/runs/` replays in the console: `dotnet run --project src/Butterfly.Console -- --seed <seed> --inputs <file>`.

**Task 3: Styling pass.** Move the inline placeholder look in `Ui.cs` (colors, sizes, borders, padding, margins) into `Assets/Butterfly/Resources/Butterfly.uss`, loaded at startup, with the palette as USS variables at the top. Keep every screen, element and behavior. Aim for body text of at least 16, clear contrast, generous line spacing in narrative text, and finger-sized buttons. Check it in Play at 1600×1000.

**Task 4: Placeholder art.** Pick one style and make one portrait, then confirm it looks right in the decision card and People screen before doing the rest. Make everything in §7 except the return sites; do those last, plain views only. Check each slot in Play.

**Task 5: HUD icons.** Add the Attention and money icons through `ArtLibrary` (with a drawn fallback, as other slots have). The HUD's numbers and strings still come from `Hud()` unchanged.

**Task 6: iPad and narrow layout.** In the Game view, check iPad landscape (1180×820 and 1366×1024) and a width under 1100. Nothing clipped, text wraps, and the decision card and departure screen fit with all buttons visible. Layout only: never remove, merge or hide what the models give. If iOS Build Support is installed, make an iPad build to check on a device.

**Task 7: Mac build.** File → Build Profiles → macOS → Build. Check that the app starts by double-click with no command line, and plays to the first return. Report where the build is and its size. Don't commit the build.

## 10. How to report

After each task, report:
- what you ran and what you saw (commands, Console output, screenshots), honestly, including what didn't work;
- the files you changed and one line on why for each;
- anything you didn't do because it would break §3, and what the developer should ask the core team for;
- the commit and the push.

If this brief and a request conflict, follow the brief and say so.
