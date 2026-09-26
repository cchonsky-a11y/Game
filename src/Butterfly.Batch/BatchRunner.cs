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
        public double DebtAtDeparture;
        public double DebtPaidByInstitutions;
        public double HoldingsAtDeparture;
        public double HoldingsOnArrival;
        public CorruptionLevel WorstCorruption;
        public bool Audited;
        public int Decisions;
        public Dictionary<string, InstitutionOutcome> Institutions = new Dictionary<string, InstitutionOutcome>();
        public Dictionary<Domain, int> FirstStrainedYear = new Dictionary<Domain, int>();
        public string LogHash = "";
    }

    public static class BatchRunner
    {
        public static readonly (string Name, int Year)[] Timings = { ("Early", 160), ("Late", 165) };

        /// <summary>The three strategies PROTOTYPE_SCOPE names for the balance gate.</summary>
        public static readonly string[] ScopeStrategies = { "Balanced", "Specialized", "Neglectful" };

        /// <summary>The three ways of handling debt at departure (Balanced is the Pay-down variant).</summary>
        public static readonly string[] DebtStrategies = { "Balanced", "Endow", "Split" };

        public const double Cap = 0.65;
        public const double AuditFlag = 0.80;

        public static Strategy[] Strategies() => new Strategy[]
        {
            new BalancedStrategy(), new SpecializedStrategy(), new NeglectfulStrategy(), new EndowStrategy(), new SplitStrategy()
        };

        public static string Label(string strategy) => strategy == "Balanced" ? "Balanced (Pay-down)" : strategy;

        public static RunResult Play(GameData data, Strategy strategy, ulong seed, string timing, int jumpYear)
        {
            var sim = new Simulation(data, seed);
            while (sim.Now.Year < jumpYear)
            {
                strategy.PlayTurn(sim);
                sim.EndTurn();
            }
            strategy.BeforeJump(sim);
            double debtAtDeparture = sim.World.Domains.Sum(d => d.Debt);
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
                DebtAtDeparture = debtAtDeparture,
                DebtPaidByInstitutions = sim.World.Institutions.Sum(i => i.DebtPaidAway),
                HoldingsAtDeparture = sim.World.Institutions.Sum(i => i.HoldingsAtDeparture),
                HoldingsOnArrival = sim.World.Institutions.Sum(i => i.Holdings),
                WorstCorruption = sim.World.Institutions.Select(i => i.Corruption).DefaultIfEmpty(CorruptionLevel.None).Max(),
                Audited = sim.World.Institutions.Any(i => i.AuditCharter),
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
        /// Win = the strategy with the highest arrival Index for the same seed and timing, among the given
        /// strategies (ties split). Returns win rate per (timing, strategy).
        /// </summary>
        public static Dictionary<(string Timing, string Strategy), double> WinRates(List<RunResult> results, IEnumerable<string>? among = null)
        {
            var set = among?.ToList();
            var pool = set == null ? results : results.Where(r => set.Contains(r.Strategy)).ToList();
            var wins = new Dictionary<(string, string), double>();
            foreach (var group in pool.GroupBy(r => (r.Timing, r.Seed)))
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
            foreach (var g in pool.GroupBy(r => (r.Timing, r.Strategy)))
                rates[g.Key] = (wins.TryGetValue(g.Key, out var v) ? v : 0) / g.Count();
            return rates;
        }

        /// <summary>For each strategy and seed, the timing with the higher arrival Index wins (ties split).</summary>
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

        /// <summary>No strategy in the set wins more than 65% of seeds in the timing.</summary>
        public static bool CapHolds(List<RunResult> results, IEnumerable<string> among, string timing)
        {
            var rates = WinRates(results, among);
            return among.All(s => rates[(timing, s)] <= Cap);
        }

        /// <summary>Gate A (PROTOTYPE_SCOPE): within each timing, among Balanced/Specialized/Neglectful, none above 65% and both Balanced and Specialized viable.</summary>
        public static bool ScopeGatePasses(List<RunResult> results, string timing)
        {
            var rates = WinRates(results, ScopeStrategies);
            return CapHolds(results, ScopeStrategies, timing) && rates[(timing, "Balanced")] > 0 && rates[(timing, "Specialized")] > 0;
        }

        public static bool AllStrategiesGatePasses(List<RunResult> results, string timing) =>
            CapHolds(results, Strategies().Select(s => s.Name), timing);

        public static bool DebtGatePasses(List<RunResult> results, string timing) => CapHolds(results, DebtStrategies, timing);

        public static bool TimingGatePasses(List<RunResult> results) => TimingWinRates(results).Values.All(v => v <= Cap);

        /// <summary>Share of best runs (highest arrival Index per seed and timing, all strategies) that bought an audit charter.</summary>
        public static double AuditRateInBestRuns(List<RunResult> results)
        {
            var best = results.GroupBy(r => (r.Timing, r.Seed)).Select(g => g.OrderByDescending(r => r.IndexAfter).ThenBy(r => r.Strategy, StringComparer.Ordinal).First()).ToList();
            return best.Count(r => r.Audited) / (double)best.Count;
        }

        /// <summary>Gates A–C. The timing gate (D) is reported but not applicable to P0 (deferred to P3, decided 2026-09-27).</summary>
        public static bool AllGatesPass(List<RunResult> results) =>
            Timings.All(t => ScopeGatePasses(results, t.Name) && AllStrategiesGatePasses(results, t.Name) && DebtGatePasses(results, t.Name));

        public static string Report(GameData data, List<RunResult> results, int runs)
        {
            var ci = CultureInfo.InvariantCulture;
            string F0(double v) => v.ToString("0", ci);
            string F1(double v) => v.ToString("0.0", ci);
            string Pct(double v) => (v * 100).ToString("0", ci) + "%";
            string Verdict(bool pass) => pass ? "**PASS**" : "**FAIL**";
            var names = Strategies().Select(s => s.Name).ToList();
            var rates = WinRates(results);
            var sb = new StringBuilder();
            sb.AppendLine("# P0 batch balance report");
            sb.AppendLine();
            sb.AppendLine(runs + " seeded runs × " + names.Count + " strategies × 2 jump timings (Early: leave at the start of AD 160, before the outbreak; Late: leave at the start of AD 165, when the era's 20 turns end).");
            sb.AppendLine("A strategy **wins** a seed when it has the highest arrival Index among the compared strategies for that seed and timing (ties split).");
            sb.AppendLine("Balanced is the Pay-down variant; Endow and Split play the same era but leave debt to their institutions (Endow) or pay half of it (Split).");
            sb.AppendLine();
            sb.AppendLine("Attention (P0 pacing): demand " + F0(AttentionBudget.Demand(data)) + " vs supply " + F0(AttentionBudget.Supply(data)) +
                          " = " + F1(AttentionBudget.Demand(data) / AttentionBudget.Supply(data)) + "× (target ≥ " + F1(data.Tuning.Get("attention.demandTarget")) + "×).");
            sb.AppendLine();
            sb.AppendLine("## Outcomes");
            sb.AppendLine();
            sb.AppendLine("| Timing | Strategy | Win rate (all 5) | Index at departure | Index at arrival (mean) | min–max | Plague severity | Crises in absence | Promise kept / broken | Player actions |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in results.GroupBy(r => (r.Timing, r.Strategy)))
            {
                var list = g.ToList();
                sb.AppendLine("| " + g.Key.Timing + " | " + Label(g.Key.Strategy) + " | " + Pct(rates[g.Key]) + " | " + F0(list.Average(r => r.IndexBefore)) +
                              " | " + F0(list.Average(r => r.IndexAfter)) + " | " + F0(list.Min(r => r.IndexAfter)) + "–" + F0(list.Max(r => r.IndexAfter)) +
                              " | " + F1(list.Average(r => r.Severity)) + " | " + F1(list.Average(r => r.CrisesInAbsence)) +
                              " | " + list.Count(r => r.Promise == PromiseStatus.Kept) + " / " + list.Count(r => r.Promise == PromiseStatus.Broken) +
                              " | " + F1(list.Average(r => r.Decisions)) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Institution gold and corruption");
            sb.AppendLine();
            sb.AppendLine("| Timing | Strategy | Debt left at departure | Debt retired by institutions | Holdings at departure | Holdings on arrival | Audit bought | Corrupted | Minor / Major / Total |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var g in results.GroupBy(r => (r.Timing, r.Strategy)))
            {
                var list = g.ToList();
                sb.AppendLine("| " + g.Key.Timing + " | " + Label(g.Key.Strategy) + " | " + F1(list.Average(r => r.DebtAtDeparture)) + " | " + F1(list.Average(r => r.DebtPaidByInstitutions)) +
                              " | " + F0(list.Average(r => r.HoldingsAtDeparture)) + " | " + F0(list.Average(r => r.HoldingsOnArrival)) +
                              " | " + Pct(list.Count(r => r.Audited) / (double)list.Count) + " | " + Pct(list.Count(r => r.WorstCorruption != CorruptionLevel.None) / (double)list.Count) +
                              " | " + list.Count(r => r.WorstCorruption == CorruptionLevel.Minor) + " / " + list.Count(r => r.WorstCorruption == CorruptionLevel.Major) +
                              " / " + list.Count(r => r.WorstCorruption == CorruptionLevel.Total) + " |");
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
                    sb.Append("| " + g.Key.Timing + " | " + Label(g.Key.Strategy) + " | " + inst + " |");
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
                sb.AppendLine("- " + Label(s.Key) + ": first debt tier change " +
                              (strained.Count > 0 ? F1(strained.Min() - 155) + "–" + F1(strained.Average() - 155) + " years in (earliest–mean)" : "never") + ".");
            }
            sb.AppendLine();
            sb.AppendLine("## Balance criteria");
            sb.AppendLine();
            var scope = WinRates(results, ScopeStrategies);
            var debt = WinRates(results, DebtStrategies);
            sb.AppendLine("**A. Balance vs. specialization (PROTOTYPE_SCOPE):** among Balanced, Specialized and Neglectful, within each timing no strategy wins more than 65% and Balanced and Specialized both win some.");
            foreach (var t in Timings.Select(x => x.Name))
                sb.AppendLine("- " + t + ": " + string.Join(", ", ScopeStrategies.Select(s => s + " " + Pct(scope[(t, s)]))) + " → " + Verdict(ScopeGatePasses(results, t)));
            sb.AppendLine();
            sb.AppendLine("**B. All five strategies:** within each timing no strategy wins more than 65%.");
            foreach (var t in Timings.Select(x => x.Name))
                sb.AppendLine("- " + t + ": " + string.Join(", ", names.Select(s => Label(s) + " " + Pct(rates[(t, s)]))) + " → " + Verdict(AllStrategiesGatePasses(results, t)));
            sb.AppendLine();
            sb.AppendLine("**C. Debt at departure:** among Pay-down (Balanced), Endow and Split, within each timing none wins more than 65%.");
            foreach (var t in Timings.Select(x => x.Name))
                sb.AppendLine("- " + t + ": " + string.Join(", ", DebtStrategies.Select(s => Label(s) + " " + Pct(debt[(t, s)]))) + " → " + Verdict(DebtGatePasses(results, t)));
            sb.AppendLine();
            var timingRates = TimingWinRates(results);
            sb.AppendLine("**D. Jump timing — NOT APPLICABLE TO P0 (deferred to P3, decided 2026-09-27):** staying longer has no cost until aging and machine-discovery risk exist. Reported for information only.");
            sb.AppendLine("- " + string.Join(", ", timingRates.Select(kv => kv.Key + " " + Pct(kv.Value))) + " → " + (TimingGatePasses(results) ? "would pass" : "would fail") + " (not counted)");
            foreach (var g in results.GroupBy(r => r.Strategy))
                sb.AppendLine("  - " + Label(g.Key) + ": " + string.Join(", ", TimingWinRates(g.ToList()).Select(kv => kv.Key + " " + Pct(kv.Value))));
            sb.AppendLine();
            double audit = AuditRateInBestRuns(results);
            sb.AppendLine("**Audit charter check (flag, not a gate):** " + Pct(audit) + " of the best runs (top arrival Index per seed and timing) bought an audit charter" +
                          (audit > AuditFlag ? " → ⚠ **FLAG** (above " + Pct(AuditFlag) + ": the audit charter may be a must-buy)" : " → no flag (at or below " + Pct(AuditFlag) + ")."));
            sb.AppendLine();
            sb.AppendLine("**All applicable balance criteria (A–C): " + (AllGatesPass(results) ? "PASS" : "FAIL") + "**");
            return sb.ToString();
        }
    }
}
