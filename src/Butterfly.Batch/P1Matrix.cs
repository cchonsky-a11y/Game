using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>
    /// The P1 strategy matrix (hardening pass, 2026-10-04): many seeds × five scripted profiles, both openings, through an
    /// era and two jumps. Scripted runs of the real build: not human testing, not persona feedback. Usage:
    /// --p1-matrix 1-60 [--out file].
    /// </summary>
    public static class P1Matrix
    {
        public static readonly P1Campaign.Profile[] Profiles =
            { P1Campaign.Profile.Cooperative, P1Campaign.Profile.Negotiator, P1Campaign.Profile.Selective, P1Campaign.Profile.Engineering, P1Campaign.Profile.Relationship };

        public static List<P1Campaign.Result> Run(GameData data, IEnumerable<ulong> seeds) =>
            seeds.SelectMany(seed => Profiles.Select(p => P1Campaign.Play(data, seed, p, seed % 2 == 1 ? "workshop" : "fountain"))).ToList();

        private static string F(double x) => x.ToString("0.0", CultureInfo.InvariantCulture);
        private static string Range(IEnumerable<double> xs) { var l = xs.ToList(); return l.Count == 0 ? "–" : F(l.Average()) + " (" + F(l.Min()) + "–" + F(l.Max()) + ")"; }
        private static string Range(IEnumerable<int> xs) => Range(xs.Select(x => (double)x));

        public static string Report(IReadOnlyList<P1Campaign.Result> all, double weight = 3)
        {
            var sb = new StringBuilder();
            var cats = Enum.GetValues(typeof(SceneCategory)).Cast<SceneCategory>().ToList();
            int seeds = all.Select(r => r.Seed).Distinct().Count();
            sb.AppendLine("# P1 strategy matrix: " + seeds + " seeds × " + Profiles.Length + " scripted profiles (" + all.Count + " runs)");
            sb.AppendLine();
            sb.AppendLine("Progression routing weight: " + F(weight) + (Math.Abs(weight - 3) > 1e-9 ? " (**counterfactual experiment, in memory only; the game's tuning is 3**)" : " (the game's tuning)") + ".");
            sb.AppendLine();
            sb.AppendLine("**Automated, scripted runs of the actual build (`dotnet run --project src/Butterfly.Batch -- --p1-matrix ...`). Not human playtests, not persona feedback.** Odd seeds open with the workshop, even seeds with the fountain. Every profile assesses and repairs the machine, restores its gold, opens the R-17 channel when it can, takes workshop orders when it has a workshop, does odd jobs only when no commission is under way and money is short (below 30 aurei at AD 155 prices), and jumps once the machine is ready and the year is at least AD 163, then jumps again.");
            sb.AppendLine();
            sb.AppendLine("- **A Cooperative:** looks at and accepts every job; never asks for more; accepts every invitation; starts every challenge stage it can.");
            sb.AppendLine("- **B Negotiator:** as A, but asks for more on every offer.");
            sb.AppendLine("- **C Selective:** turns down favors, profit shares and shared development, and any job when next month is already 3 Attention deep; starts a challenge stage only with twice its cost in hand.");
            sb.AppendLine("- **D Engineering-focused:** stays focused on Engineering scenes; turns down jobs that open as Roman life, city or personal scenes; buys machine upgrades when it has 60+ aurei.");
            sb.AppendLine("- **E Relationship / Roman-life:** stays focused on Roman-life scenes; answers Rome's and people's choices with their first option; starts a challenge stage only when no paid job is under way.");
            sb.AppendLine();
            sb.AppendLine("## By profile and opening (mean, min–max)");
            sb.AppendLine();
            sb.AppendLine("| Profile | Opening | Runs | First commission (month) | Odd-job months / era | Commissions offered / accepted / walked / declined / done | Challenge stages / done | Members | Gold at departure | Min gold | First jump (yrs) | Quiet months | Attention conflicts |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in all.GroupBy(r => (r.Profile, r.Choice)).OrderBy(g => g.Key.Profile).ThenBy(g => g.Key.Choice))
            {
                var l = g.ToList();
                sb.AppendLine("| " + g.Key.Profile + " | " + g.Key.Choice + " | " + l.Count + " | " + Range(l.Select(r => r.FirstCommissionTurn)) + " | " +
                    Range(l.Select(r => r.OddJobMonths)) + " / " + F(l.Average(r => r.EraMonths)) + " | " +
                    F(l.Average(r => r.CommissionsOffered)) + " / " + F(l.Average(r => r.CommissionsAccepted)) + " / " + F(l.Average(r => r.CommissionsWalked)) + " / " +
                    F(l.Average(r => r.CommissionsDeclined)) + " / " + F(l.Average(r => r.CommissionsDone)) + " | " +
                    F(l.Average(r => r.ChallengeStages)) + " / " + F(l.Average(r => r.ChallengesDone)) + " | " + F(l.Average(r => r.Members)) + " | " +
                    Range(l.Select(r => r.GoldAtDeparture)) + " | " + F(l.Min(r => r.MinGold)) + " | " + Range(l.Where(r => r.Jump1 > 0).Select(r => r.Jump1)) + " | " +
                    Range(l.Select(r => r.QuietMonths)) + " | " + l.Sum(r => r.AttentionConflicts) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Income by source (era totals, aurei at the prices of the day; mean per run)");
            sb.AppendLine();
            sb.AppendLine("| Profile | Opening | Commissions (incl. shares) | Workshop | Odd jobs | Profit-share payments |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var g in all.GroupBy(r => (r.Profile, r.Choice)).OrderBy(g => g.Key.Profile).ThenBy(g => g.Key.Choice))
                sb.AppendLine("| " + g.Key.Profile + " | " + g.Key.Choice + " | " + F(g.Average(r => r.CommissionIncome)) + " | " + F(g.Average(r => r.OrderIncome)) + " | " +
                              F(g.Average(r => r.OddJobIncome)) + " | " + F(g.Average(r => r.ProfitSharePayments)) + " |");
            sb.AppendLine();
            sb.AppendLine("## Scene routing (all runs)");
            sb.AppendLine();
            sb.AppendLine("| Profile | " + string.Join(" | ", cats) + " | Routed / run | Longest routed run (max) | Longest all-scene run (max) | Progression waiting (months / era) | Longest wait for one progression scene (months, max) |");
            sb.AppendLine("|---|" + string.Concat(cats.Select(_ => "---|")) + "---|---|---|---|---|");
            foreach (var g in all.GroupBy(r => r.Profile).OrderBy(g => g.Key))
            {
                var l = g.ToList();
                double routed = l.Sum(r => r.Routed.Values.Sum());
                double scenes = l.Sum(r => r.AllScenes.Values.Sum());
                sb.AppendLine("| " + g.Key + " | " + string.Join(" | ", cats.Select(c =>
                        F(100.0 * l.Sum(r => r.Routed.TryGetValue(c, out int n) ? n : 0) / Math.Max(1, routed)) + "% (" +
                        F(100.0 * l.Sum(r => r.AllScenes.TryGetValue(c, out int m) ? m : 0) / Math.Max(1, scenes)) + "%)")) +
                    " | " + F(routed / l.Count) + " | " + l.Max(r => r.LongestRoutedRun) + " | " + l.Max(r => r.LongestSceneRun) + " (" + string.Join("/", l.Where(r => r.LongestSceneRun == l.Max(x => x.LongestSceneRun)).Select(r => r.LongestRunCategory).Distinct()) + ") | " +
                    Range(l.Select(r => r.ProgressionPendingMonths)) + " | " + l.Max(r => r.LongestProgressionWait) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("Cells: share of routed optional scenes (share of all meaningful scenes, including the player's own work and interruptions, in brackets). \"Progression waiting\" counts months in which a commission, invitation, challenge or life development was a router candidate as the month ended; \"longest wait\" is the most months any one such candidate waited before it was picked.");
            sb.AppendLine();
            sb.AppendLine("Longest waits by candidate (max months as a candidate before it was picked; mean over runs where it was picked; runs where it was still waiting at departure):");
            sb.AppendLine();
            sb.AppendLine("| Candidate | Max wait | Mean wait | Runs picked | Still waiting at departure (runs; mean months waited) |");
            sb.AppendLine("|---|---|---|---|---|");
            var ids = all.SelectMany(r => r.Waits.Keys.Select(k => k.StartsWith("never:") ? k.Substring(6) : k)).Distinct().ToList();
            foreach (var id in ids.OrderByDescending(id => all.Max(r => r.Waits.TryGetValue(id, out int w) ? w : 0)).Take(20))
            {
                var picked = all.Where(r => r.Waits.ContainsKey(id)).Select(r => r.Waits[id]).ToList();
                var never = all.Where(r => r.Waits.ContainsKey("never:" + id)).Select(r => r.Waits["never:" + id]).ToList();
                sb.AppendLine("| " + id + " | " + (picked.Count == 0 ? 0 : picked.Max()) + " | " + (picked.Count == 0 ? "–" : F(picked.Average())) + " | " + picked.Count + " | " +
                              never.Count + (never.Count == 0 ? "" : "; " + F(never.Average())) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Attention (era months)");
            sb.AppendLine();
            sb.AppendLine("| Profile | Opening | Months ending with 0 free | 1 free | 2+ free | Idle Attention / month | Overbooking refusals (future months) | Challenge stages refused for Attention |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var g in all.GroupBy(r => (r.Profile, r.Choice)).OrderBy(g => g.Key.Profile).ThenBy(g => g.Key.Choice))
            {
                var l = g.ToList();
                sb.AppendLine("| " + g.Key.Profile + " | " + g.Key.Choice + " | " + F(l.Average(r => r.FreeAttention0)) + " | " + F(l.Average(r => r.FreeAttention1)) + " | " +
                              F(l.Average(r => r.FreeAttention2Plus)) + " | " + F(l.Sum(r => r.IdleAttention) / Math.Max(1.0, l.Sum(r => r.EraMonths))) + " | " +
                              F(l.Average(r => r.AttentionConflicts)) + " | " + F(l.Average(r => r.StageAttentionRefusals)) + " |");
            }
            sb.AppendLine();
            sb.AppendLine("## Jumps (measured, not changed)");
            sb.AppendLine();
            var j1 = all.Where(r => r.Jump1 > 0).ToList();
            var j2 = all.Where(r => r.Jump2 > 0).ToList();
            sb.AppendLine("- Runs that jumped: " + j1.Count + " of " + all.Count + "; jumped twice: " + j2.Count + ".");
            sb.AppendLine("- Departure years: " + string.Join(", ", j1.GroupBy(r => r.Depart1).OrderBy(g => g.Key).Select(g => "AD " + g.Key + " × " + g.Count())) + ".");
            sb.AppendLine("- First jump distances: " + string.Join(", ", j1.GroupBy(r => r.Jump1).OrderBy(g => g.Key).Select(g => g.Key + " yrs × " + g.Count())) +
                          "; ranges offered at departure: " + string.Join(", ", j1.GroupBy(r => r.RangeMin + "–" + r.RangeMax).OrderBy(g => g.Key).Select(g => g.Key + " × " + g.Count())) + ".");
            sb.AppendLine("- Second jump distances: " + string.Join(", ", j2.GroupBy(r => r.Jump2).OrderBy(g => g.Key).Select(g => g.Key + " yrs × " + g.Count())) +
                          "; ranges offered: " + string.Join(", ", j2.GroupBy(r => r.Range2Min + "–" + r.Range2Max).OrderBy(g => g.Key).Select(g => g.Key + " × " + g.Count())) + ".");
            sb.AppendLine("- First arrival years: " + Range(j1.Select(r => r.Arrive1)) + "; second arrival years: " + Range(j2.Select(r => r.Arrive2)) + ".");
            sb.AppendLine("- Machine upgrades bought (Engineering profile): " + Range(all.Where(r => r.Profile == P1Campaign.Profile.Engineering).Select(r => r.UpgradesDone)) + ".");
            sb.AppendLine();
            sb.AppendLine("## Integrity");
            sb.AppendLine();
            sb.AppendLine("- Measurement and standards opened by: " + string.Join(", ", all.GroupBy(r => r.StandardsRoute.Length == 0 ? "never" : r.StandardsRoute).OrderBy(g => g.Key)
                              .Select(g => g.Key + " × " + g.Count())) + "; by opening: " + string.Join("; ", all.GroupBy(r => r.Choice).Select(g => g.Key + " " +
                              string.Join(", ", g.GroupBy(r => r.StandardsRoute.Length == 0 ? "never" : r.StandardsRoute).OrderBy(x => x.Key).Select(x => x.Key + " × " + x.Count())))) + ".");
            sb.AppendLine("- Grand Challenges completed: both in " + all.Count(r => r.ChallengesDone == 2) + " runs, one in " + all.Count(r => r.ChallengesDone == 1) + ", none in " + all.Count(r => r.ChallengesDone == 0) + ".");
            sb.AppendLine("- Ledger reconciles at departure: " + all.Count(r => r.LedgerReconciles) + " of " + all.Count + ".");
            sb.AppendLine("- Lowest gold in any run: " + F(all.Min(r => r.MinGold)) + ".");
            sb.AppendLine("- Duplicate scene texts in an era: " + all.Sum(r => r.DuplicateTexts) + "; repeated sentences between arrivals: " + all.Sum(r => r.RepeatedArrivalSentences) + ".");
            foreach (var r in all) foreach (var d in r.DuplicateList) sb.AppendLine("  - seed " + r.Seed + " " + r.Profile + ", twice in the era: \"" + d + "…\"");
            foreach (var r in all) foreach (var x in r.RepeatedSentences) sb.AppendLine("  - seed " + r.Seed + " " + r.Profile + ", on both arrivals: \"" + x + "\"");
            sb.AppendLine("- R-17: reached the warning in " + all.Count(r => r.Warned) + " of " + all.Count + " runs; furthest scene: " +
                          string.Join(", ", all.GroupBy(r => r.R17Reached).OrderBy(g => g.Key).Select(g => g.Key + " × " + g.Count())) + ".");
            sb.AppendLine("- Echo kinds on the first arrival: " + string.Join(", ", all.SelectMany(r => r.Echoes1.Select(e => e.Split(':')[0])).GroupBy(x => x).OrderBy(g => g.Key).Select(g => g.Key + " " + g.Count())) +
                          "; second: " + string.Join(", ", all.SelectMany(r => r.Echoes2.Select(e => e.Split(':')[0])).GroupBy(x => x).OrderBy(g => g.Key).Select(g => g.Key + " " + g.Count())) + ".");
            sb.AppendLine("- People remembered on the first arrival: " + string.Join(", ", all.SelectMany(r => r.Echoes1.Where(e => e.StartsWith("person:")).Select(e => e.Substring(7)))
                              .GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) + "; second: " +
                          string.Join(", ", all.SelectMany(r => r.Echoes2.Where(e => e.StartsWith("person:")).Select(e => e.Substring(7)))
                              .GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) + ".");
            return sb.ToString();
        }
    }
}
