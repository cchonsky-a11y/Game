using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Walking around Rome (decided 2026-09-28): at every arrival, including the first, a few places show what is there now
    /// and, after a jump, what changed since you left, in words and numbers. Present conditions only: no causal chains.
    /// </summary>
    public sealed partial class Simulation
    {
        public static readonly string[] WalkPlaces = { "market", "changers", "forges", "curia", "subura" };

        private static readonly string[] WorkshopInventions = { "wheelbarrow", "collar", "triphammer", "lathe", "bellows", "furnace", "bookkeeping", "bills", "audit" };
        private static readonly string[] HealingInventions = { "soap", "spirits", "ward" };

        /// <summary>What Rome looked like when you last left it (for "then" comparisons after a jump).</summary>
        private sealed class Snapshot
        {
            public int Year;
            public double PriceLevel, WageLevel, Population, Silver;
        }

        private Snapshot? _leftRome;

        private Snapshot TakeSnapshot() => new Snapshot
        {
            Year = Now.Year, PriceLevel = World.PriceLevel, WageLevel = WageLevel(), Population = World.Population, Silver = CoinSilverNow(),
        };

        /// <summary>
        /// The denarius's silver now: history's, spared or hastened by a coinage policy that still stands after a jump.
        /// </summary>
        public double CoinSilverNow()
        {
            double now = HistoricalSilver(Now.YearFraction);
            if (!Arrived || _leftRome == null) return now;
            double decline = HistoricalSilver(_leftRome.Year) - now;
            return Math.Max(0.01, Math.Min(_leftRome.Silver, now + CoinStanceFactor() * decline));
        }

        /// <summary>A place's scene by how that domain compares with history now.</summary>
        private string Band(Domain d)
        {
            double sub = SubScore(d), band = T.Get("walk.asHistoryBand");
            return sub > 100 + band ? "high" : sub < 100 - band ? "low" : "asHistory";
        }

        private static string R(double v, string format = "0.#") => v.ToString(format, CultureInfo.InvariantCulture);

        public string Visit(string place)
        {
            string p = (place ?? "").Trim().ToLowerInvariant();
            bool first = !Arrived;
            var text = Data.Content;
            var v = new Dictionary<string, string>();
            var lines = new List<string>();
            double wheat = T.Get("walk.wheatModiusDenarii"), wage = T.Get("walk.laborerDayDenarii");
            switch (p)
            {
                case "market": case "forum":
                {
                    double wheatNow = wheat * World.PriceLevel, wageNow = wage * WageLevel();
                    v["wheat"] = R(wheatNow); v["wage"] = Math.Abs(wageNow - 1) < 0.05 ? "1 denarius" : R(wageNow) + " denarii"; v["kg"] = R(wageNow / wheatNow * T.Get("walk.kgPerModius"));
                    if (first) { lines.Add(text.Template("walk.market.start", v)); break; }
                    v["wheatThen"] = R(wheat * _leftRome!.PriceLevel);
                    v["kgThen"] = R(wage * _leftRome.WageLevel / (wheat * _leftRome.PriceLevel) * T.Get("walk.kgPerModius"));
                    lines.Add(text.Template("walk.market." + Band(Domain.Economy)));
                    lines.Add(text.Template("walk.market.prices", v));
                    break;
                }
                case "changers": case "money": case "bank":
                {
                    double silver = CoinSilverNow();
                    v["aureus"] = R(AureusInDenarii);
                    if (first) { v["silver"] = R(silver * 100, "0"); lines.Add(text.Template("walk.changers.start", v)); break; }
                    v["aureusThen"] = first ? " (the official rate)" : " (" + R(Denarii(_leftRome!.PriceLevel)) + " when you left)";
                    v["silver"] = R(silver * 100, "0");
                    v["silverThen"] = first ? "" : " (" + R(_leftRome!.Silver * 100, "0") + "% when you left)";
                    v["trust"] = text.Template("walk.changers.trust." + (silver >= 0.7 ? "good" : silver >= 0.5 ? "fair" : "poor"));
                    lines.Add(text.Template("walk.changers", v));
                    if (World.Invented.Contains("bills")) lines.Add(text.Template("walk.changers.bills"));
                    break;
                }
                case "forges": case "guild": case "workshop":
                {
                    if (first)
                    {
                        bool partner = World.SeededChoice == "workshop" || World.CompletedProjects.Contains("workshop");
                        lines.Add(text.Template(partner ? "walk.forges.start.partner" : "walk.forges.start"));
                        break;
                    }
                    if (OwnsWorkshop)
                    {
                        string fate = WorkshopFate();
                        // The same fate as the Recognition beat, in the same words (tester 7 saw "a stable" there and "a single cold forge" here).
                        lines.Add(text.Template("walk.forges.workshop." + (fate == "street" ? "high" : fate == "working" ? "asHistory" : JumpsMade >= 2 ? "gone2" : "gone")));
                        if (fate != "gone" && WorkshopSize >= 2) lines.Add(text.Template("walk.forges.size." + WorkshopSize));
                        else if (fate == "gone" && WorkshopSize >= 3) lines.Add(text.Template("walk.forges.ruin"));
                        if (World.Apprentices > 0 && fate != "gone") lines.Add(text.Template("walk.forges.apprentices"));
                    }
                    else lines.Add(text.Template("walk.forges.noWorkshop"));
                    var guild = World.Institution("guild");
                    if (HasInfluence(guild)) lines.Add(Cap(CurrentName(guild)) + ": " + OutcomeOf(guild).ToString().ToLowerInvariant() + ", strength " + R(guild.Strength, "0") + ".");
                    var inUse = WorkshopInventions.Where(World.Invented.Contains).Select(id => text.Template("walk.use." + id)).ToList();
                    if (inUse.Count > 0) lines.Add(text.Template("walk.forges.inUse", new Dictionary<string, string> { { "inventions", string.Join("; ", inUse) } }));
                    break;
                }
                case "curia": case "senate":
                {
                    if (first) { lines.Add(text.Template("walk.curia.start")); break; }
                    lines.Add(text.Template("walk.curia." + Band(Domain.Governance)));
                    foreach (var i in Influential().Where(i => i.Def.Maintains == Domain.Governance))
                        lines.Add(Cap(CurrentName(i)) + ": " + OutcomeOf(i).ToString().ToLowerInvariant() + ", strength " + R(i.Strength, "0") + ".");
                    var policy = Issues.Where(i => Stance(i) != 0 && PolicySway() > 0).Select(i => i.ToString().ToLowerInvariant() + " " + StanceWord(i, Stance(i))).ToList();
                    if (policy.Count > 0) lines.Add(text.Template("walk.curia.policy", new Dictionary<string, string> { { "policy", string.Join(", ", policy) } }));
                    break;
                }
                case "subura": case "fountain":
                {
                    v["population"] = R(Math.Round(World.Population) * 1000, "#,0");
                    if (first) { lines.Add(text.Template("walk.subura.start", v)); break; }
                    v["populationThen"] = R(Math.Round(_leftRome!.Population) * 1000, "#,0");
                    lines.Add(text.Template("walk.subura." + Band(Domain.Medicine)));
                    bool runs = World.CleanWater && World.FountainCondition >= T.Get("jump.fountainRunsAt");
                    lines.Add(text.Template("walk.subura.fountain." + (runs ? "runs" : World.CleanWater ? "dry" : "foul")));
                    lines.Add(text.Template("walk.subura.population", v));
                    var inUse = HealingInventions.Where(World.Invented.Contains).Select(id => text.Template("walk.use." + id)).ToList();
                    if (inUse.Count > 0) lines.Add(text.Template("walk.subura.inUse", new Dictionary<string, string> { { "inventions", string.Join("; ", inUse) } }));
                    break;
                }
                default:
                    return "Visit where? " + string.Join(", ", WalkPlaces) + ".";
            }
            return string.Join(" ", lines);
        }
    }
}
