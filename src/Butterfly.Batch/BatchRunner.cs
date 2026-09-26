using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>Everything the report needs from one automated playthrough.</summary>
    public sealed class RunResult
    {
        public ulong Seed;
        public string Strategy = "";
        public string Timing = "";
        public int DepartureYear;
        public double IndexBefore;
        public double IndexAfter;
        public int OutbreakYear;
        public double Severity;
        public bool OutbreakWhileAway;
        public int FirstWarningYear;
        public PromiseStatus Promise;
        public int CrisesInAbsence;
        public double DebtPaidByInstitutions;
        public bool Corrupted;
        public double GoldSpentOnProjects;
        public int Decisions;
        public Dictionary<string, InstitutionOutcome> Institutions = new Dictionary<string, InstitutionOutcome>();
        public Dictionary<Domain, int> FirstStrainedYear = new Dictionary<Domain, int>();
        public string LogHash = "";
    }

    public static class BatchRunner
    {
        public static readonly (string Name, int Year)[] Timings = { ("Early", 160), ("Late", 165) };

        public static Strategy[] Strategies() => new Strategy[] { new BalancedStrategy(), new SpecializedStrategy(), new NeglectfulStrategy() };

        public static RunResult Play(GameData data, Strategy strategy, ulong seed, string timing, int jumpYear)
        {
            var sim = new Simulation(data, seed);
            while (sim.Now.Year < jumpYear)
            {
                strategy.PlayTurn(sim);
                sim.EndTurn();
            }
            strategy.BeforeJump(sim);
            var arrival = sim.Jump();

            var r = new RunResult
            {
                Seed = seed,
                Strategy = strategy.Name,
                Timing = timing,
                DepartureYear = arrival.DepartureYear,
                IndexBefore = arrival.IndexBefore,
                IndexAfter = arrival.IndexAfter,
                OutbreakYear = sim.World.Plague.OutbreakYear,
                Severity = sim.World.Plague.Severity,
                OutbreakWhileAway = sim.World.Plague.StruckInAbsence,
                FirstWarningYear = sim.Log.Events.First(e => e.Type == "plague.warning").Time.Year,
                Promise = sim.World.Promise.Status,
                CrisesInAbsence = sim.Log.Events.Count(e => e.Type == "crisis.recurrence"),
                DebtPaidByInstitutions = sim.World.Institutions.Sum(i => i.DebtPaidAway),
                Corrupted = sim.World.Institutions.Any(i => i.Corruption != CorruptionLevel.None),
                Decisions = sim.Log.Events.Count(e => e.Actors.Contains("player") && e.Type != "personal.work" && e.Type != "jump.arrive"),
                LogHash = sim.Log.Hash(),
            };
            foreach (var i in sim.World.Institutions) r.Institutions[i.Key] = sim.OutcomeOf(i);
            foreach (var d in DomainInfo.All)
            {
                var strained = sim.Log.Events.FirstOrDefault(e => e.Type == "debt.tier" && e.Target == d.Key() && e.Time.Year <= arrival.DepartureYear);
                r.FirstStrainedYear[d] = strained?.Time.Year ?? 0;
            }
            return r;
        }

        public static List<RunResult> RunAll(GameData data, int runs, ulong firstSeed = 1)
        {
            var results = new List<RunResult>();
            foreach (var (timing, year) in Timings)
                foreach (var strategy in Strategies())
                    for (int i = 0; i < runs; i++)
                        results.Add(Play(data, strategy, firstSeed + (ulong)i, timing, year));
            return results;
        }

        /// <summary>
        /// Win = the strategy with the highest arrival Index for the same seed and timing (ties split).
        /// Returns win rate per (timing, strategy).
        /// </summary>
        public static Dictionary<(string Timing, string Strategy), double> WinRates(List<RunResult> results)
        {
            var wins = new Dictionary<(string, string), double>();
            foreach (var group in results.GroupBy(r => (r.Timing, r.Seed)))
            {
                double best = group.Max(r => Math.Round(r.IndexAfter, 6));
                var winners = group.Where(r => Math.Round(r.IndexAfter, 6) == best).ToList();
                foreach (var w in winners)
                {
                    var key = (w.Timing, w.Strategy);
                    wins[key] = (wins.TryGetValue(key, out var v) ? v : 0) + 1.0 / winners.Count;
                }
            }
            var rates = new Dictionary<(string, string), double>();
            foreach (var g in results.GroupBy(r => (r.Timing, r.Strategy)))
            {
                int seeds = g.Count();
                rates[g.Key] = (wins.TryGetValue(g.Key, out var v) ? v : 0) / seeds;
            }
            return rates;
        }

        /// <summary>
        /// Timing check (decided 2026-09-26): for each strategy and seed, which jump timing gave the higher
        /// arrival Index (ties split). Returns the share of those comparisons won by each timing.
        /// </summary>
        public static Dictionary<string, double> TimingWinRates(List<RunResult> results)
        {
            var wins = Timings.ToDictionary(t => t.Name, t => 0.0);
            int comparisons = 0;
            foreach (var g in results.GroupBy(r => (r.Strategy, r.Seed)))
            {
                double best = g.Max(r => Math.Round(r.IndexAfter, 6));
                var winners = g.Where(r => Math.Round(r.IndexAfter, 6) == best).ToList();
                foreach (var w in winners) wins[w.Timing] += 1.0 / winners.Count;
                comparisons++;
            }
            return wins.ToDictionary(kv => kv.Key, kv => kv.Value / comparisons);
        }

        /// <summary>Gate 1: within each timing, no strategy wins more than 65% and Balanced and Specialized both win some.</summary>
        public static bool StrategyGatePasses(List<RunResult> results, string timing)
        {
            var rates = WinRates(results);
            double cap = 0.65;
            return Strategies().All(s => rates[(timing, s.Name)] <= cap) && rates[(timing, "Balanced")] > 0 && rates[(timing, "Specialized")] > 0;
        }

        /// <summary>Gate 2: neither timing wins more than 65% of runs across all strategies.</summary>
        public static bool TimingGatePasses(List<RunResult> results) => TimingWinRates(results).Values.All(v => v <= 0.65);

        public static string Report(List<RunResult> results, int runs)
        {
            var ci = CultureInfo.InvariantCulture;
            string F0(double v) => v.ToString("0", ci);
            string F1(double v) => v.ToString("0.0", ci);
            string Pct(double v) => (v * 100).ToString("0", ci) + "%";
            var rates = WinRates(results);
            var sb = new StringBuilder();
            sb.AppendLine("# P0 batch balance report");
            sb.AppendLine();
            sb.AppendLine(runs + " seeded runs × 3 strategies × 2 jump timings (Early: leave at the start of AD 160, before the outbreak; Late: leave at the start of AD 165, when the era's 20 turns end).");
            sb.AppendLine("A strategy **wins** a seed when it has the highest arrival Index among the three strategies for that seed and timing (ties split).");
            sb.AppendLine();
            sb.AppendLine("| Timing | Strategy | Win rate | Index at departure | Index at arrival (mean) | min–max | Plague severity | Outbreak while away | Crises in absence | Institution debt paid | Corruption (runs) | Promise kept / broken | Player actions |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in results.GroupBy(r => (r.Timing, r.Strategy)))
            {
                var list = g.ToList();
                sb.AppendLine("| " + g.Key.Timing + " | " + g.Key.Strategy + " | " + Pct(rates[g.Key]) + " | " + F0(list.Average(r => r.IndexBefore)) +
                              " | " + F0(list.Average(r => r.IndexAfter)) + " | " + F0(list.Min(r => r.IndexAfter)) + "–" + F0(list.Max(r => r.IndexAfter)) +
                              " | " + F1(list.Average(r => r.Severity)) + " | " + Pct(list.Count(r => r.OutbreakWhileAway) / (double)list.Count) +
                              " | " + F1(list.Average(r => r.CrisesInAbsence)) + " | " + F1(list.Average(r => r.DebtPaidByInstitutions)) +
                              " | " + list.Count(r => r.Corrupted) +
                              " | " + list.Count(r => r.Promise == PromiseStatus.Kept) + " / " + list.Count(r => r.Promise == PromiseStatus.Broken) +
                              " | " + F1(list.Average(r => r.Decisions)) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Institution outcomes on arrival");
            sb.AppendLine();
            sb.AppendLine("| Timing | Strategy | Institution | Thriving | Drifted | Captured | Dissolved | Rogue | Not founded |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var g in results.GroupBy(r => (r.Timing, r.Strategy)))
                foreach (var inst in new[] { "circle", "faction" })
                {
                    var outcomes = g.Select(r => r.Institutions[inst]).ToList();
                    sb.Append("| " + g.Key.Timing + " | " + g.Key.Strategy + " | " + inst + " |");
                    foreach (var o in new[] { InstitutionOutcome.Thriving, InstitutionOutcome.Drifted, InstitutionOutcome.Captured, InstitutionOutcome.Dissolved, InstitutionOutcome.Rogue, InstitutionOutcome.NotFounded })
                        sb.Append(" " + outcomes.Count(x => x == o) + " |");
                    sb.AppendLine();
                }
            sb.AppendLine();
            sb.AppendLine("## Pacing");
            sb.AppendLine();
            var late = results.Where(r => r.Timing == "Late").ToList();
            sb.AppendLine("- First plague warning: AD " + late.Min(r => r.FirstWarningYear) + "–" + late.Max(r => r.FirstWarningYear) +
                          "; outbreak: AD " + late.Min(r => r.OutbreakYear) + "–" + late.Max(r => r.OutbreakYear) + " (mean " + F1(late.Average(r => r.OutbreakYear)) + ").");
            foreach (var s in late.GroupBy(r => r.Strategy))
            {
                var strained = s.SelectMany(r => r.FirstStrainedYear.Values.Where(y => y > 0)).ToList();
                sb.AppendLine("- " + s.Key + ": first debt tier change on average " +
                              (strained.Count > 0 ? F1(strained.Min() - 155) + "–" + F1(strained.Average() - 155) + " years in (earliest–mean)" : "never") + ".");
            }
            sb.AppendLine();
            sb.AppendLine("## Pass criteria (PROTOTYPE_SCOPE)");
            sb.AppendLine();
            sb.AppendLine("Gate 1, per timing: within each timing, no strategy wins more than 65% of seeds, and Balanced and Specialized both win some.");
            foreach (var timing in Timings.Select(t => t.Name))
            {
                double b = rates[(timing, "Balanced")], s = rates[(timing, "Specialized")], n = rates[(timing, "Neglectful")];
                sb.AppendLine("- " + timing + ": Balanced " + Pct(b) + ", Specialized " + Pct(s) + ", Neglectful " + Pct(n) + " → " +
                              (StrategyGatePasses(results, timing) ? "**PASS**" : "**FAIL**"));
            }
            var timingRates = TimingWinRates(results);
            sb.AppendLine();
            sb.AppendLine("Gate 2, timing: for each strategy and seed, the timing with the higher arrival Index wins; neither timing may win more than 65% overall.");
            sb.AppendLine("- " + string.Join(", ", timingRates.Select(kv => kv.Key + " " + Pct(kv.Value))) + " → " +
                          (TimingGatePasses(results) ? "**PASS**" : "**FAIL**"));
            foreach (var g in results.GroupBy(r => r.Strategy))
            {
                var tr = TimingWinRates(g.ToList());
                sb.AppendLine("  - " + g.Key + ": " + string.Join(", ", tr.Select(kv => kv.Key + " " + Pct(kv.Value))));
            }
            sb.AppendLine();
            bool all = Timings.All(t => StrategyGatePasses(results, t.Name)) && TimingGatePasses(results);
            sb.AppendLine("**Overall balance gate: " + (all ? "PASS" : "FAIL") + "**");
            return sb.ToString();
        }
    }
}
