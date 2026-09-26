using System;
using System.Globalization;
using System.IO;
using Butterfly.Batch;
using Butterfly.Core;

// Automated strategy runner. Usage: dotnet run --project src/Butterfly.Batch -- --runs 100 [--out playtests/batch-report.md]
int runs = 100;
string? outPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--runs") runs = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--out") outPath = args[i + 1];
}

var data = GameData.LoadDefault();
var results = BatchRunner.RunAll(data, runs);
string report = BatchRunner.Report(results, runs);
Console.WriteLine(report);
if (outPath != null)
{
    File.WriteAllText(outPath, report);
    Console.WriteLine("Saved to " + outPath);
}
