# The Butterfly Effect

A turn-based historical strategy game for Apple platforms. Currently in prototype stage (P0: text-based Butterfly Test in C#).

- Picking this up cold (any AI or person): `docs/HANDOFF.md`
- Start here: `docs/BUILD_GUIDE.md`
- Current scope: `docs/PROTOTYPE_SCOPE.md`
- Agent instructions: `CLAUDE.md`

## Running P0

Requires the .NET SDK (8.0 or later).

```
dotnet build
dotnet test                                                   # tests: formulas, determinism, systems, sync with SYSTEMS.md
dotnet run --project src/Butterfly.Console -- --seed 42       # play (type 'help')
dotnet run --project src/Butterfly.Batch -- --runs 100 --out playtests/batch-report.md
```

- Placeholder numbers awaiting approval, and open questions: `docs/P0_PROPOSALS.md`
- Latest balance report: `playtests/batch-report.md`
- Automated playtests (scripted console runs, harness checks, blind AI review): `playtests/ai/` (`playtests/ai/run.sh`)
