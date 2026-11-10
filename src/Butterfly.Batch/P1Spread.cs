using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>
    /// The P1 outcome-spread baseline (Stakes and Impact pass, slice 0, 2026-11-10): how different is the world a scripted
    /// player leaves and comes back to, by profile, against a player who does nothing but repair the machine? Scripted runs
    /// of the real build: not human testing. Usage: --p1-spread 1-30 [--out file].
    /// </summary>
    public static class P1Spread
    {
        public static readonly P1Campaign.Profile[] Profiles =
        {
            P1Campaign.Profile.Idle, P1Campaign.Profile.Cooperative, P1Campaign.Profile.Negotiator, P1Campaign.Profile.Selective,
            P1Campaign.Profile.Engineering, P1Campaign.Profile.Relationship,
        };

        private static string F(double x) => x.ToString("0.0", CultureInfo.InvariantCulture);
        private static string Range(IEnumerable<double> xs) { var l = xs.ToList(); return l.Count == 0 ? "–" : F(l.Average()) + " (" + F(l.Min()) + "–" + F(l.Max()) + ")"; }
        private static string Opening(ulong seed) => seed % 2 == 1 ? "workshop" : "fountain";

        public static List<P1Campaign.Result> Run(GameData data, IEnumerable<ulong> seeds) =>
            seeds.SelectMany(s => Profiles.Select(p => P1Campaign.Play(data, s, p, Opening(s)))).ToList();

        private static HashSet<string> SiteSet(P1Campaign.Result r) => new HashSet<string>(r.Sites.Select(s => s.Id + ":" + s.Variant));

        private static double Overlap(HashSet<string> a, HashSet<string> b) =>
            a.Count + b.Count == 0 ? 1 : (double)a.Intersect(b).Count() / a.Union(b).Count();

        public static string Report(IReadOnlyList<P1Campaign.Result> all)
        {
            var sb = new StringBuilder();
            int seeds = all.Select(r => r.Seed).Distinct().Count();
            sb.AppendLine("# P1 outcome spread: baseline before the Stakes and Impact pass");
            sb.AppendLine();
            sb.AppendLine("**Automated, scripted runs of the actual build. Not human playtests.** " + seeds + " seeds × " + Profiles.Length + " profiles = " + all.Count +
                          " runs; odd seeds open with the workshop, even seeds with the fountain; every profile leaves at the first chance from AD 163. " +
                          "Idle repairs the machine and does odd jobs, and takes no commission, challenge or invitation. Regenerate: `dotnet run --project src/Butterfly.Batch -- --p1-spread 1-" + seeds + "`.");
            sb.AppendLine();
            sb.AppendLine("## Can a run be lost?");
            sb.AppendLine();
            sb.AppendLine("- Runs that jumped: " + all.Count(r => r.Jumped) + " of " + all.Count + ". Runs that ended any other way: " + all.Count(r => !r.Jumped) + " (the machine not ready by the end of the era).");
            sb.AppendLine();
            sb.AppendLine("## The world, by profile (mean, min–max)");
            sb.AppendLine();
            sb.AppendLine("| Profile | Left (AD) | Index at departure | Index on first arrival | Commissions done | Challenges done | Capabilities moved | Highest capability level | Attention unused / month |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var p in Profiles)
            {
                var rs = all.Where(r => r.Profile == p && r.Jumped).ToList();
                if (rs.Count == 0) { sb.AppendLine("| " + p + " | never | – | – | – | – | – | – | – |"); continue; }
                sb.AppendLine("| " + p + " | " + Range(rs.Select(r => (double)r.Depart1)) + " | " + Range(rs.Select(r => r.IndexAtDeparture)) + " | " + Range(rs.Select(r => r.IndexAtArrival1)) +
                              " | " + Range(rs.Select(r => (double)r.CommissionsDone)) + " | " + Range(rs.Select(r => (double)r.ChallengesDone)) +
                              " | " + Range(rs.Select(r => (double)r.Capabilities.Count(c => c.Value > CapabilityLevel.None))) +
                              " | " + rs.SelectMany(r => r.Capabilities.Values).DefaultIfEmpty(CapabilityLevel.None).Max() +
                              " | " + Range(rs.Select(r => r.EraMonths == 0 ? 0 : (double)r.IdleAttention / r.EraMonths)) + " |");
            }
            sb.AppendLine();
            var active = all.Where(r => r.Profile != P1Campaign.Profile.Idle && r.Jumped).ToList();
            var idle = all.Where(r => r.Profile == P1Campaign.Profile.Idle && r.Jumped).ToList();
            if (active.Count > 0 && idle.Count > 0)
            {
                sb.AppendLine("- **What playing is worth to the Index:** active profiles arrive at " + F(active.Average(r => r.IndexAtArrival1)) + " on average, Idle at " + F(idle.Average(r => r.IndexAtArrival1)) +
                              " (a gap of " + F(active.Average(r => r.IndexAtArrival1) - idle.Average(r => r.IndexAtArrival1)) + " points; 100 = history).");
                sb.AppendLine("- **Spread among active players on arrival:** " + F(active.Min(r => r.IndexAtArrival1)) + "–" + F(active.Max(r => r.IndexAtArrival1)) + ".");
            }
            sb.AppendLine();
            sb.AppendLine("## Capabilities at departure (runs reaching each level, active profiles)");
            sb.AppendLine();
            var levels = Enum.GetValues(typeof(CapabilityLevel)).Cast<CapabilityLevel>().ToList();
            sb.AppendLine("| Capability | " + string.Join(" | ", levels) + " |");
            sb.AppendLine("|---|" + string.Concat(levels.Select(_ => "---|")));
            foreach (var id in all.SelectMany(r => r.Capabilities.Keys).Distinct().OrderBy(x => x, StringComparer.Ordinal))
                sb.AppendLine("| " + id + " | " + string.Join(" | ", levels.Select(l => active.Count(r => r.Capabilities.TryGetValue(id, out var v) && v == l))) + " |");
            sb.AppendLine();
            sb.AppendLine("## How different are two players' returns?");
            sb.AppendLine();
            sb.AppendLine("Overlap = shared return places with the same variant ÷ all places either saw (1 = the same return; 0 = nothing in common), same seed, mean over seeds.");
            sb.AppendLine();
            sb.AppendLine("| | " + string.Join(" | ", Profiles) + " |");
            sb.AppendLine("|---|" + string.Concat(Profiles.Select(_ => "---|")));
            foreach (var a in Profiles)
            {
                var cells = new List<string>();
                foreach (var b in Profiles)
                {
                    var pairs = all.Where(r => r.Profile == a && r.Jumped)
                                   .Select(r => (r, o: all.FirstOrDefault(x => x.Profile == b && x.Seed == r.Seed && x.Jumped))).Where(t => t.o != null).ToList();
                    cells.Add(pairs.Count == 0 ? "–" : pairs.Average(t => Overlap(SiteSet(t.r), SiteSet(t.o!))).ToString("0.00", CultureInfo.InvariantCulture));
                }
                sb.AppendLine("| " + a + " | " + string.Join(" | ", cells) + " |");
            }
            sb.AppendLine();
            var kinds = all.Where(r => r.Jumped).SelectMany(r => r.Sites.Select(s => s.Id + ":" + s.Variant)).GroupBy(x => x).OrderByDescending(g => g.Count()).ToList();
            sb.AppendLine("Distinct return places (place:variant) seen across all runs: " + kinds.Count + ". Most common: " +
                          string.Join(", ", kinds.Take(8).Select(g => g.Key + " (" + g.Count() + ")")) + ".");
            return sb.ToString();
        }
    }
}
