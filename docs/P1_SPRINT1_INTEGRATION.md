# P1 Sprint 1 — Integration pass

This pass connects the first P1 state models into reusable services while the legacy P0 simulation remains operational.

## Added

### Deterministic scene router
- Uses the project's seeded `Rng` only.
- Applies the locked soft cap after two consecutive meaningful scenes of one category.
- Player explicit focus removes the repetition penalty for that category.
- World-interrupt scenes are not suppressed by player focus or recent category repetition.

### Explicit economy ledger
- Records payments, expenses, materials, profit share, dues, favors and adjustments.
- Project accounting turns `ProjectTerms` into visible agreement/completion ledger entries.
- The ledger is additive during migration; it does not silently mutate legacy `World.Gold` yet.

### Relationship-first institution access state
- Records awareness, member relationship, guest visits, repeat invitation, sponsorship, membership and office.
- A warranted invitation cannot skip relationship or guest history.
- Invitations from an unrelated institution cannot advance another institution's access state.
- New P1 content should use this path rather than direct stake purchases.

## Next migration target

1. Make **one month** the fixed simulation turn.
2. Remove normal auto-end; only explicit End Month advances the calendar.
3. Preserve explicit fast-forward as an opt-in action that stops on meaningful interruptions.
4. Add `ProjectState`, `EconomyLedger`, `ScenePacingState` and institution access collections to `World`.
5. Migrate one real content thread end-to-end (paid technical commission → scene routing → ledger → propagation).
6. Begin retiring direct institution stake/buy-in commands from the player-facing interface.

## Validation status

These files are prepared as additive P1 code. The current working environment does not include the .NET SDK, so they must not be committed until `dotnet test` is run successfully in an SDK-capable environment, per project rules.
