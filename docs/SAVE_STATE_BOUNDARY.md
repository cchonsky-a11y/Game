# SAVE_STATE_BOUNDARY.md: what a save must hold (note only; not built)

> Written in the P1 correctness pass (2026-10-04) so that graphical/UI work does not assume that serializing `World` is enough to save a game. **No save system is built or in scope** (PROTOTYPE_SCOPE.md: saves are out of scope). This lists the state a deterministic continuation needs, as the code stands.

**The test of a save:** load it and play on with the same inputs. The event log hash must match a game that never stopped. Anything below that is left out breaks determinism or silently changes the game.

## 1. `World` (serialize as a whole)

Most game state lives here. It includes some state that is easy to mistake for a UI cache:
- domains, institutions, access stages, people, commissions, the ledger, capabilities (level, spread, distortion, misattribution);
- **Grand Challenges**, including `FirstEligibleRoute` / `FirstEligibleTurn` (route credit) and the stage in progress (`MonthsLeft`, `ReservedFromTurn`, `DoneBy`);
- **scene routing:** `ScenePacing` (the recent categories), `RoutedScenes`, `CandidateSince` (how long progression candidates have waited), `ScenesSeen`, `ScenePlays`, `SceneLastTurn`, `SceneFocus`, `ReadyLife`, `TriggeredEvents`;
- machine work in progress (`ActiveMachineSteps`, `MachineDone`, gold restored), active practical projects and inventions, reserved Attention, flags, the promise.
- **the first return (2026-10-04):** `World.Return` holds the departure and arrival years, the jump years, the chosen sites with their fixed words, variant, band, thread, evidence and grounding event ids, the sites visited and looked into, whether the return is finished (the second-jump gate), the threads the briefing named when you left (`WarnedThreads`) and each site's `Warned` mark. `World.Journal` holds the entries as written: id, year, text and event id. Site words are fixed when chosen, so a loaded game must not re-choose them.

## 2. On `Simulation`, outside `World`

| State | Where | Why it matters |
|---|---|---|
| **The generator's position** | `Rng._state` (private) | Every later draw. The seed alone is not enough mid-game. There is no accessor for the raw state yet; `Rng.Clone()` copies it in memory only. |
| Calendar | `Now`, `Turn`, `MonthsPerTurn` | Time, durations, reservations |
| Era and jumps | `_eraStart`, `_firstDepartureYear`, `DepartureYear`, `JumpsMade`, `IsAway`, `Arrived`, `Arrival`, `_leftRome` (snapshot at departure), `_warningsBeforeDeparture`, `_stepYears` | Arrival beats, echoes, the walk, jump range |
| The pending decision and past answers | `_pendingEvent`, `_pendingSinceTurn`, `_eventsSeen`, `_marksShown`, `_eventChoices` | Rome's choices and lapses; `answered:` requirements and return sites read past answers |
| The first return in preparation | `_preparingReturn`, `_warnedAtDeparture` | Set at departure and read at the arrival, both inside one jump (one command); the result lives on in `World.Return.WarnedThreads` |
| News and pacing | `_localHeard`, `_localSlot`, `_localThisTurnFrom`, `_turnEventStart`, `_lastChange` | What has been said, what is new this month |
| Workshop | `_orderBoard`, `_orderSlot`, `_ordersTakenThisSeason` | Craft orders |
| Savings across a jump | `_depositSince`, `_depositReturned`, `_depositLost`, `_hoardLost` | The bank and the jar |
| Absence history | `_absenceBusts`, `_recurrencesStruck`, `_expandEventId` | Busts, historical plagues, causes |
| The event log | `Log` | "Why?", causes, echoes, and the determinism hash |

Derived and rebuildable (no need to save): `_sceneRouter` (rebuilt from the generator and tuning), the content and tuning (loaded from `data/`). A save should still record the content and tuning versions it was made with.

## 3. Console-only state

The console keeps a little of its own state, such as `_jumpArmed` (the jump briefing has been shown) and the menu mode. A graphical client will have its own equivalents. None of it is game state, but a client must not rely on it surviving a reload.

## 4. When saves are built

- Serialize `World` and the `Simulation` fields above, including the generator state. Test it with a save/load/continue determinism test against an uninterrupted game.
- Keep previews and views side-effect free (they are, since this pass), so a client can render freely before saving.
