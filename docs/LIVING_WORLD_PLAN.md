# Living world: plan and status

> Corey's directive of 2026-11-11 ("Transform into a living open-world experience"): the player inhabits a historically
> believable Rome; the strategy engine runs beneath it. Two separate acceptance tests: **visual quality** (a distinct
> neighbourhood, not Roman-looking objects) and **world reactivity** (places with people, materials, problems and visible
> consequences, not a doorway that opens a task menu). Perfect one district, the Subura, before widening.
> This file is the working plan and the honest status. The engine's rules stay in SYSTEMS.md.

## 1. What was verified on 2026-11-11 (not assumed)
| Claim | Checked | Result |
|---|---|---|
| The C# engine and its tests | `dotnet test` | 585 pass (584 + one new for the street facts) |
| The browser game plays the whole arc | `tools/web-playthrough.js`, seed 42, scripted | Opening to the finished first return, no errors |
| Frame weight | renderer counters in the Subura | About 320 draw calls and 59,000 triangles before this milestone (light for a phone GPU) |
| Frame rate on real devices | Not measurable here | **Unknown.** This machine has no graphics card (about 1 frame a second in software). No phone or tablet has been tried. |
| Art pack | Rendered in the game's own renderer | Tenements, temple, columns, fountain, amphorae load and look right; people are still simple jointed figures drawn by code |

## 2. What can be reused
The engine (all rules), `Butterfly.Presentation` (screen models and the action catalog), the WebAssembly bridge, the art
pipeline (`art/kit`, Blender by script), the browser client's movement, panels, decision cards, departure, arrival and
return screens, and the scripted playthrough.

## 3. The approach
- **The engine tells the street what is true; the street shows it.** `GameSession.Street()` (`StreetFacts`) gives present
  conditions as plain facts (the fountain's state, the sickness, work done and under way, what Rome can now reproduce,
  years away). The client draws them. It never decides anything.
- **Districts are built from a kit** (materials, buildings, furniture, dress), so a later Rome or another era swaps the kit.
- **People are of two kinds.** The game's named people (from the engine: known, away, what is between you) and background
  citizens (cosmetic, with errands that follow the street's facts).
- **Time is unchanged:** walking and looking are free, actions cost Attention, the month ends only when the player ends it.
- **Browser first, not for ever.** If the look and the crowd outgrow a phone browser, the same engine moves under a native
  engine; nothing in Core or Presentation depends on the browser.

## 4. Milestones for the Subura benchmark
| # | Milestone | Status |
|---|---|---|
| S1 | A real street plan: main street under the firewall of the Forum of Augustus, two lanes, the fountain square, the lodging's yard with the machine, an inner court | **Done 2026-11-11** |
| S2 | The street shows the engine's facts: the fountain foul / under repair / clean / worn / dry; the sickness empties the street; the inventor's work leaves marks (the shared foot on a wall, a standpipe) | **Done 2026-11-11** (first set) |
| S3 | Background citizens with errands: shoppers who stop at shop fronts, water carriers, children at clean water, shopkeepers who look up, neighbours talking | **Done 2026-11-11** (first set) |
| S4 | The same street after a jump: plots burnt out or rebuilt higher, the fountain worn or dry, the work's marks kept | **Built, but too subtle.** From the main street the two eras look alike. Needs authored, recognizable changes per place. |
| S5 | People from the art pack: a rigged figure with walk, idle, talk and carry animations, faces, ages, builds, dress by station | Not started. The largest visual gap. |
| S6 | Interiors: the lodging room, the street bar, a workshop, Serenus's surgery | Not started |
| S7 | Conversations as scenes: the person in front of you, framed, with their offers and what is between you | Partly (a panel with their work; no framing or gesture) |
| S8 | A workshop that reacts: workers, materials, a visible problem, visible improvement after the player's work | Not started |
| S9 | A quieter screen: fewer buttons, markers only when close | Partly (markers and name labels now appear only nearby) |
| S10 | Sound: street, water, voices, footsteps, with a mute control | Not started |
| S11 | Measured performance on an iPhone 13 and an iPad | Not started (needs the devices) |

## 5. Risks
1. **Characters.** Procedural figures are the weakest part of the picture. A rigged, animated figure is the next piece.
2. **Phone performance is unmeasured.** The client lowers its own detail when frames are slow, but that is untested on hardware.
3. **Procedural art has a ceiling.** It is consistent and historically grounded; it is not hand-sculpted. Any asset can
   later be replaced one for one by an artist's.
4. **History.** Details are from memory until checked. Known to fix: the Senate house is drawn in bare brick, which is its
   later (third-century) appearance; in AD 155 it would have been faced. The spiral column belongs to Trajan's Forum, not
   the Roman Forum.

## 6. Next step of highest value
S5 (people), then S4 done properly (authored before-and-after for the lodging, the square and the shops), then S6 and S8.
