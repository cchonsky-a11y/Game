using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Butterfly.Batch;
using Butterfly.Core;

// Automated strategy runner. Usage: dotnet run --project src/Butterfly.Batch -- --runs 100 [--out playtests/batch-report.md]
// Exploration with randomized players through both jumps: --explore 100 [--out playtests/explore-report.md]
int runs = 100;
int explore = 0;
string? outPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--runs") runs = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--out") outPath = args[i + 1];
    if (args[i] == "--explore") explore = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--idle") { explore = int.Parse(args[i + 1], CultureInfo.InvariantCulture); Explorer.Idle = true; }
}

var data = GameData.LoadDefault();
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] != "--p1-validate") continue;
    // Executable P1 validation through two jumps (scripted, not human): --p1-validate 1,2,3 [--out file]
    var seeds = args[i + 1].Split(',').Select(x => ulong.Parse(x, CultureInfo.InvariantCulture)).ToList();
    var played = seeds.Select(s => P1Campaign.Play(data, s)).ToList();
    string p1 = P1Campaign.Report(played);
    Console.WriteLine(p1);
    if (outPath != null) { File.WriteAllText(outPath, p1); Console.WriteLine("Saved to " + outPath); }
    return;
}
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] != "--p1-matrix") continue;
    // The P1 strategy matrix (scripted, not human): --p1-matrix 1-60 [--weight W] [--out file]
    var bounds = args[i + 1].Split('-').Select(x => ulong.Parse(x, CultureInfo.InvariantCulture)).ToArray();
    var range = Enumerable.Range((int)bounds[0], (int)(bounds[bounds.Length - 1] - bounds[0] + 1)).Select(x => (ulong)x);
    // --weight W: a counterfactual experiment with scenes.progressionWeight = W in memory only (data/tuning.json is untouched).
    var run = data;
    for (int k = 0; k < args.Length - 1; k++)
        if (args[k] == "--weight")
            run = data.WithTuning(new System.Collections.Generic.Dictionary<string, double> { { "scenes.progressionWeight", double.Parse(args[k + 1], CultureInfo.InvariantCulture) } });
    string m = P1Matrix.Report(P1Matrix.Run(run, range), run.Tuning.Get("scenes.progressionWeight"));
    Console.WriteLine(m);
    if (outPath != null) { File.WriteAllText(outPath, m); Console.WriteLine("Saved to " + outPath); }
    return;
}
int trace = 0;
for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--trace") trace = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
if (trace > 0)
{
    // One exploration run, turn by turn (usage: --trace <run>).
    Explorer.Trace = Console.WriteLine;
    for (int i = 0; i < args.Length; i++) if (args[i] == "--idle") Explorer.Idle = true;
    var one = Explorer.Play(data, trace, 1000UL + (ulong)trace);
    Console.WriteLine(one.Persona.Describe());
    foreach (var b in one.Bugs) Console.WriteLine("BUG " + b);
    return;
}
if (explore > 0)
{
    // Randomized players through both jumps: bugs and how much choices matter (usage: --explore 100 [--out file]).
    var explored = Explorer.RunAll(data, explore, out var determinism);
    string text = Explorer.Report(explored, determinism);
    Console.WriteLine(text);
    if (outPath != null) { File.WriteAllText(outPath, text); Console.WriteLine("Saved to " + outPath); }
    return;
}
var results = BatchRunner.RunAll(data, runs);
string report = BatchRunner.Report(data, results, runs);
Console.WriteLine(report);
if (outPath != null)
{
    File.WriteAllText(outPath, report);
    Console.WriteLine("Saved to " + outPath);
}
