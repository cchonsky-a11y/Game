using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Butterfly.Core
{
    /// <summary>
    /// The in-era "Why?" (SYSTEMS §1, VISION "Legibility in the moment"): explains current conditions
    /// from the event log's immediate causes. It never traces causes across the jump.
    /// </summary>
    public static class Why
    {
        public static readonly string[] Topics = { "medicine", "governance", "economy", "gold", "plague", "circle", "faction", "promise", "index", "attention" };

        public static string Explain(Simulation sim, string topic)
        {
            topic = (topic ?? "").Trim().ToLowerInvariant();
            if (sim.Arrived)
                return "You have been away for 250 years. What happened in between is lost to you; the world simply is what it is now. (Try 'learn more'.)";
            if (DomainInfo.TryParseDomain(topic, out var d)) return Domain(sim, d);
            switch (topic)
            {
                case "gold": return Gold(sim);
                case "plague":
                case "pestilence": return Plague(sim);
                case "promise": return Promise(sim);
                case "index": return Index(sim);
                case "attention": return Attention(sim);
            }
            var inst = sim.FindInstitution(topic);
            if (inst != null) return Institution(sim, inst);
            return "Why what? Try: " + string.Join(", ", Topics) + ".";
        }

        private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        private static string Domain(Simulation sim, Domain d)
        {
            var s = sim.World[d];
            double benchmark = sim.Benchmark(d, sim.Now.Year);
            double expectation = sim.Expectation(d);
            var sb = new StringBuilder();
            sb.AppendLine(d + " is at " + F(s.Level) + " under " + s.Priority.Label() + " (" + Signed(sim.PriorityLevelChange(d)) + " a year from upkeep).");
            sb.AppendLine("People expect " + F(expectation) + ": " +
                          (s.Peak > benchmark ? "they remember your recent peak of " + F(s.Peak) + " (the memory fades 5 a year)."
                                              : "what was normal in Rome in AD " + sim.Now.Year + "."));
            if (s.Level < expectation)
                sb.AppendLine("The shortfall of " + F(expectation - s.Level) + " adds that much debt each year.");
            sb.AppendLine("Debt " + F(s.Debt) + " (" + s.Tier + "). Debt grows 5% a year until paid down, which costs " +
                          F(sim.PaydownCost(1)) + " gold per point." + TierNote(sim, s.Tier));
            sb.AppendLine("Index sub-score " + F(sim.SubScore(d)) + " (100 = as in real history).");
            AppendRecent(sim, sb, new[] { Simulation.LevelKey(d), Simulation.DebtKey(d) }, 5);
            return sb.ToString().TrimEnd();
        }

        private static string TierNote(Simulation sim, DebtTier tier)
        {
            double chance = sim.T.Get("plague.advanceChance." + tier.ToString().ToLowerInvariant());
            return " At " + tier + ", if this is the region's worst domain, plague warnings advance with " + F(chance * 100) + "% chance a year.";
        }

        private static string Gold(Simulation sim)
        {
            var t = sim.T;
            var sb = new StringBuilder();
            sb.AppendLine("You have " + F(sim.World.Gold) + " gold.");
            sb.AppendLine("Income " + F(sim.YearlyIncome()) + " a year: base " + F(t.Get("gold.incomeBase")) + " + Economy " +
                          F(sim.World[Core.Domain.Economy].Level) + " × " + F(t.Get("gold.incomePerEconomyLevel")) +
                          (sim.World.IncomeBonus != 0 ? " + " + F(sim.World.IncomeBonus) + " from projects" : "") + ".");
            sb.AppendLine("Upkeep " + F(sim.YearlyUpkeepTotal()) + " a year: " +
                          string.Join(", ", DomainInfo.All.Select(x => x + " " + F(sim.YearlyUpkeep(x)) + " (" + sim.World[x].Priority.Label() + ")")) +
                          (sim.Founded().Any(i => !i.Endowed) ? ", institutions " + F(sim.Founded().Count(i => !i.Endowed) * t.Get("institutions.upkeepPerYear")) : "") + ".");
            sb.AppendLine("Settled each turn: " + F((sim.YearlyIncome() - sim.YearlyUpkeepTotal()) * sim.YearsPerTurn) + " per turn.");
            AppendRecent(sim, sb, new[] { "gold" }, 4, skipTypes: new[] { "gold.settle" });
            return sb.ToString().TrimEnd();
        }

        private static string Plague(Simulation sim)
        {
            var p = sim.World.Plague;
            var sb = new StringBuilder();
            if (p.Stage == PlagueState.Quiet)
            {
                sb.AppendLine("No sign of pestilence yet. History says one is coming from the East in the 160s.");
                return sb.ToString().TrimEnd();
            }
            sb.AppendLine(Simulation.PlagueStageText(p.Stage));
            if (p.IsWarning)
            {
                var worst = sim.World.Domains.OrderByDescending(x => x.Tier).First();
                sb.AppendLine("Chance the next stage comes within a year: " + F(sim.PlagueAdvanceChance() * 100) + "%.");
                sb.AppendLine("  Because the region's worst debt tier is " + sim.RegionTier() + " (" + worst.Domain + ")" +
                              (sim.World.CleanWater ? "." : ", and the district fountain is still foul."));
                sb.AppendLine("  Expect the outbreak in about " + (4 - p.Stage) + "–" + (2 * (4 - p.Stage) + 1) + " years; never sooner than " + (4 - p.Stage) + ".");
                sb.AppendLine("If it broke out now, severity would be about " + F(sim.PlagueSeverity(null)) + " with no response:");
                sb.AppendLine("  Hazard " + F(sim.PlagueHazard()) + " = base " + F(sim.T.Get("plague.baseHazard")) + " + Medicine debt " +
                              F(sim.World[Core.Domain.Medicine].Debt) + " × " + F(sim.T.Get("plague.hazardPerMedicineDebt")) +
                              (sim.World.CleanWater ? "" : " + foul water " + F(sim.T.Get("plague.foulWaterHazard"))) + ".");
                sb.AppendLine("  Resilience " + F(sim.PlagueResilience(null) * 100) + "% from Medicine, Governance, preparations and the Circle.");
            }
            if (p.Stage == PlagueState.Passed)
                sb.AppendLine("It struck in AD " + p.OutbreakYear + ": severity " + F(p.Severity) + ", about " + F(p.Deaths) + " thousand dead; response: " + p.Response + ".");
            AppendRecent(sim, sb, new[] { "plague.stage", "plague.severity", "population" }, 4);
            return sb.ToString().TrimEnd();
        }

        private static string Institution(Simulation sim, Institution i)
        {
            var sb = new StringBuilder();
            if (!i.Founded)
            {
                sb.AppendLine(Simulation.Cap(i.Def.Name) + " doesn't exist yet. " + i.Def.Leader + " would lead it. (found " + i.Key + ")");
                return sb.ToString().TrimEnd();
            }
            sb.AppendLine(Simulation.Cap(i.Def.Name) + ", led by " + i.Leader + ": strength " + F(i.Strength) + ", loyalty " + F(i.Loyalty) + ".");
            sb.AppendLine("It is " + (i.Chartered ? "chartered" : "not chartered") + " and " + (i.Endowed ? "endowed" : "not endowed") + ".");
            var q = sim.QualityAtDeparture(i);
            sb.AppendLine("If you left now it would be " + q + ": it would lose " + F(sim.DecayRate(q) * 100) + "% of its strength each decade.");
            sb.AppendLine("Left alone it would most likely drift toward " + sim.ChooseDriftPath(i).Name + ".");
            sb.AppendLine("Loyalty fades " + F(sim.T.Get("institutions.loyaltyFadePerYear")) + " a year unless you oversee it; it grows only while loyalty is at least " +
                          F(sim.T.Get("institutions.growthLoyaltyThreshold")) + ".");
            AppendRecent(sim, sb, new[] { Simulation.StrengthKey(i), Simulation.LoyaltyKey(i) }, 5);
            return sb.ToString().TrimEnd();
        }

        private static string Promise(Simulation sim)
        {
            var p = sim.World.Promise;
            switch (p.Status)
            {
                case PromiseStatus.NotOffered: return "No one has asked anything of you yet.";
                case PromiseStatus.Offered: return "Demetria asked you to stay until the sickness has passed. You haven't answered. (promise yes / promise no)";
                case PromiseStatus.Active: return "You promised Demetria you would stay until the sickness has passed. Leaving before then breaks it and costs the Circle's loyalty.";
                case PromiseStatus.Kept: return "You kept your promise to Demetria.";
                case PromiseStatus.Refused: return "You refused to promise Demetria anything.";
                default: return "You broke your promise to Demetria.";
            }
        }

        private static string Index(Simulation sim)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Index " + F(sim.SphereIndex()) + ": the geometric mean of the three sub-scores, so balance counts.");
            foreach (var d in DomainInfo.All)
                sb.AppendLine("  " + d + ": " + F(sim.World[d].Level) + " vs " + F(sim.Benchmark(d, sim.Now.YearFraction)) + " in real history = " + F(sim.SubScore(d)));
            return sb.ToString().TrimEnd();
        }

        private static string Attention(Simulation sim)
        {
            int reserved = sim.ReservedAttention();
            return "You have " + sim.World.Attention + " of " + sim.AttentionPerTurn + " Attention left this turn" +
                   (reserved > 0 ? " (" + reserved + " already pledged to ongoing work)." : ".") +
                   " Attention never grows; it is spent on projects, overseeing an institution, or your one personal action.";
        }

        /// <summary>Lists the most recent events that changed any of the keys, each with its immediate causes.</summary>
        private static void AppendRecent(Simulation sim, StringBuilder sb, string[] keys, int max, string[]? skipTypes = null)
        {
            var events = sim.Log.Events
                .Where(e => e.Effects.Any(fx => keys.Contains(fx.Key)) && (skipTypes == null || !skipTypes.Contains(e.Type)))
                .Reverse().Take(max).Reverse().ToList();
            if (events.Count == 0) return;
            sb.AppendLine("Recent changes:");
            foreach (var e in events)
            {
                var fx = e.Effects.First(x => keys.Contains(x.Key));
                sb.AppendLine("  " + e.Time.Stamp + "  " + e.Text + (fx.Delta != 0 && !fx.Key.Contains("stage") ? " [" + Signed(fx.Delta) + "]" : ""));
                foreach (var c in e.ImmediateCauses.Select(sim.Log.Get).Where(c => c.Type != "scenario.start" && c.Type != "turn.start"))
                    sb.AppendLine("      because: " + c.Text + " (" + c.Time.Stamp + ")");
            }
        }

        private static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
