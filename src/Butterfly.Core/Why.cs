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
        public static readonly string[] Topics = { "medicine", "governance", "economy", "gold", "plague", "circle", "sanctuary", "school", "faction", "junian", "club", "guild", "bank", "house", "policy", "promise", "index", "attention", "machine" };

        public static string Explain(Simulation sim, string topic)
        {
            topic = (topic ?? "").Trim().ToLowerInvariant();
            if (sim.Arrived)
                return "You have been away for " + (sim.Arrival?.JumpYears ?? 0) + " years. What happened in between is lost to you; the world simply is what it is now. (Try 'learn more'.)";
            if (DomainInfo.TryParseDomain(topic, out var d)) return Domain(sim, d);
            switch (topic)
            {
                case "gold": return Gold(sim);
                case "plague":
                case "pestilence": return Plague(sim);
                case "promise": return Promise(sim);
                case "index": return Index(sim);
                case "policy": return Policy(sim);
                case "attention": return Attention(sim);
                case "machine": return "The time machine: " + sim.MachineStepsDone + " of " + sim.MachineStepsTotal + " repair steps done. All are needed before it can jump.\n  " +
                                       string.Join("\n  ", sim.MachineStatus());
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
            sb.AppendLine(d + " is at " + F(s.Level) + ". " + (!sim.HasHold(d)
                ? "You have no voice in it, so it follows Rome's real history (" + Signed(sim.HistoricalTrend(d)) + " last year). Only your projects and their consequences move it."
                : "Under " + s.Priority.Label() + " it moves " + Signed(sim.PriorityLevelChange(d)) + " a year against history."));
            sb.AppendLine("Your influence over it: " + F(sim.Influence(d) * 100) + "%, so your priorities and policy take " + F(sim.Sway(d) * 100) + "% effect. Who holds it:");
            foreach (var i in sim.InDomain(d).OrderByDescending(x => x.Strength))
                sb.AppendLine("  " + Simulation.Cap(i.Def.Name) + ": strength " + F(i.Strength) + ", " + F(sim.DomainShare(i) * 100) + "% of " + d +
                              (i.Stake > 0 ? ", you hold " + sim.StakePercent(i) + "%" : ""));
            sb.AppendLine("People expect " + F(expectation) + ": " +
                          (s.Peak > benchmark ? "they remember your recent peak of " + F(s.Peak) + " (the memory fades 5 a year)."
                                              : "what was normal in Rome in AD " + sim.Now.Year + "."));
            if (s.Level < expectation)
                sb.AppendLine("The shortfall of " + F(expectation - s.Level) + " adds that much debt each year.");
            sb.AppendLine("Debt " + F(s.Debt) + " (" + s.Tier + "). Debt grows 5% a year until paid down, which costs " +
                          sim.Money(sim.PaydownCost(1)) + " per point." + TierNote(sim, d, s.Tier));
            sb.AppendLine("Index sub-score " + F(sim.SubScore(d)) + " (100 = as in real history).");
            AppendRecent(sim, sb, new[] { Simulation.LevelKey(d), Simulation.DebtKey(d) }, 5);
            return sb.ToString().TrimEnd();
        }

        private static string TierNote(Simulation sim, Domain d, DebtTier tier)
        {
            if (d == Core.Domain.Medicine)
                return " Each point of it makes the plague hit harder (hazard +" + F(sim.T.Get("plague.hazardPerMedicineDebt")) + " a point).";
            double extra = sim.T.Get("plague.severityPerTier." + tier.ToString().ToLowerInvariant());
            return extra > 0 ? " At " + tier + ", it makes any plague " + F(extra * 100) + "% more severe." : " At Stable, it doesn't worsen the plague.";
        }

        /// <summary>How hard the plague would hit today, against history's toll.</summary>
        private static void AppendPlagueOutlook(Simulation sim, StringBuilder sb)
        {
            double deathRate = sim.T.Get("plague.deathRatePerSeverity");
            sb.AppendLine("If it broke out now with no response, about " + F(sim.PlagueSeverity(null) * deathRate * 100) + "% of Rome would die (history: " +
                          F(sim.HistoricalPlagueSeverity * deathRate * 100) + "%):");
            sb.AppendLine("  Hazard " + F(sim.PlagueHazard()) + " (history " + F(sim.HistoricalPlagueHazard) + ") = base " + F(sim.T.Get("plague.baseHazard")) + " + Medicine debt " +
                          F(sim.World[Core.Domain.Medicine].Debt) + " × " + F(sim.T.Get("plague.hazardPerMedicineDebt")) +
                          (sim.World.CleanWater ? "; the fountain runs clean" : " + foul water " + F(sim.T.Get("plague.foulWaterHazard"))) + ".");
            sb.AppendLine("  Resilience " + F(sim.PlagueResilience(null) * 100) + "% (history " + F(sim.HistoricalPlagueResilience * 100) +
                          "%) from Medicine, Governance, preparations and institutions you steer.");
            if (sim.PlagueSeverityMultiplier() > 1)
                sb.AppendLine("  Governance and Economy debt make it " + F((sim.PlagueSeverityMultiplier() - 1) * 100) + "% worse.");
        }

        private static string Gold(Simulation sim)
        {
            var t = sim.T;
            var sb = new StringBuilder();
            sb.AppendLine("You have " + sim.Money(sim.World.Gold) + " and " + sim.AureiText(sim.World.Aurei) + " (an aureus now costs " + F(Math.Round(sim.AureusInDenarii, 1)) + " denarii at the changers, before their fee).");
            sb.AppendLine("Income " + sim.Money(sim.YearlyIncome()) + " a year" + (sim.YearlyIncome() <= 0
                ? ": none. You earn by working (work odd / craft / consult); owned property and well-run institutions add income."
                : ": " + string.Join(", ", new[] { sim.OwnedIncome() > 0 ? "property you own " + sim.Money(sim.OwnedIncome()) : null }
                      .Concat(sim.Backed().Where(i => sim.InstitutionNet(i) > 0).Select(i => "your " + sim.StakePercent(i) + "% of " + i.Def.ShortName + "'s surplus " + sim.Money(i.Stake * sim.InstitutionNet(i))))
                      .Where(x => x != null)) + "."));
            sb.AppendLine("Work is taxed at " + F(sim.WorkTaxRate() * 100) + "%. Domains: " +
                          string.Join(", ", DomainInfo.All.Select(x => sim.HasHold(x)
                              ? x + " paid for by " + sim.Maintainer(x)!.Def.ShortName + " (" + sim.World[x].Priority.Label() + ")"
                              : x + " runs without you")) + ".");
            foreach (var i in sim.Backed())
                sb.AppendLine("  " + Simulation.Cap(i.Def.ShortName) + " (you hold " + sim.StakePercent(i) + "%): earns " + sim.Money(sim.InstitutionIncome(i)) + " (grows with its strength), costs " +
                              sim.Money(sim.InstitutionCosts(i)) + " (running " + sim.Money(i.Endowed ? 0 : t.Get("institutions.upkeepPerYear." + i.Key)) + " + " +
                              i.Def.Maintains + " upkeep " + sim.Money(sim.PriorityUpkeep(i)) + ") → " +
                              (sim.InstitutionNet(i) >= 0 ? "surplus " + sim.Money(sim.InstitutionNet(i)) + ", your share " + sim.Money(i.Stake * sim.InstitutionNet(i)) + "."
                                                          : "short " + sim.Money(-sim.InstitutionNet(i)) + ", your share to cover " + sim.Money(i.Stake * -sim.InstitutionNet(i)) + "."));
            if (sim.AnnualDuesTotal() > 0)
                sb.AppendLine("Dues " + sim.Money(sim.AnnualDuesTotal()) + " a year: " + string.Join(", ", sim.Backed().Where(i => sim.AnnualDues(i) > 0)
                    .Select(i => i.Def.ShortName + " " + sim.Money(sim.AnnualDues(i)) + " (" + sim.StakePercent(i) + "%)")) + ". More stake, more dues.");
            sb.AppendLine("Settled each month: " + sim.Money((sim.YearlyIncome() - sim.YearlyUpkeepTotal()) * sim.YearsPerTurn) + " per month.");
            AppendRecent(sim, sb, new[] { "gold" }, 4, skipTypes: new[] { "gold.settle" });
            return sb.ToString().TrimEnd();
        }

        private static string Plague(Simulation sim)
        {
            var p = sim.World.Plague;
            var sb = new StringBuilder();
            if (p.Stage == PlagueState.Quiet)
            {
                sb.AppendLine("No sign of pestilence yet. History says one reaches Rome from the East in AD " + sim.HistoricalOutbreakYear +
                              ", with its first rumors in " + sim.PlagueStageDate(1).Display + ".");
                AppendPlagueOutlook(sim, sb);
                return sb.ToString().TrimEnd();
            }
            sb.AppendLine(Simulation.PlagueStageText(p.Stage));
            if (p.IsWarning)
            {
                sb.AppendLine("The next stage comes in " + sim.PlagueStageDate(p.Stage + 1).Display + "; the outbreak in " +
                              sim.PlagueStageDate(PlagueState.Outbreak).Display + ", as in history. You can't change when, only how hard it hits.");
                AppendPlagueOutlook(sim, sb);
            }
            if (sim.OutbreakAwaitingResponse)
            {
                double rate = sim.T.Get("plague.deathRatePerSeverity");
                sb.AppendLine("How many would die, by response (history: " + F(sim.HistoricalPlagueSeverity * rate * 100) + "%):");
                foreach (var r in sim.AvailablePlagueResponses())
                    sb.AppendLine("  respond " + r + ": about " + F(sim.PlagueSeverity(r) * rate * 100) + "% of Rome");
                if (!sim.CanDirectPlagueResponse())
                    sb.AppendLine("  No one will take orders from you, so Rome responds as it did in history.");
            }
            if (p.Stage == PlagueState.Passed)
                sb.AppendLine("It struck in AD " + p.OutbreakYear + ": severity " + F(p.Severity) + ", about " + F(p.Deaths) + " thousand dead; response: " + p.Response + ".");
            AppendRecent(sim, sb, new[] { "plague.stage", "plague.severity", "population" }, 4);
            return sb.ToString().TrimEnd();
        }

        private static string Institution(Simulation sim, Institution i)
        {
            var sb = new StringBuilder();
            if (!i.Exists)
            {
                sb.AppendLine(i.Collapsed ? Simulation.Cap(i.Def.Name) + " failed before it was established."
                    : Simulation.Cap(i.Def.Name) + " doesn't exist yet. " + i.Def.Leader + " would lead it. (found " + i.Key + ": " + sim.Money(sim.FoundCost(i.Def.Maintains)) +
                      "; you would control it, but it would start at strength " + F(sim.T.Get("founding.startStrength")) + " and may fail until it reaches " +
                      F(sim.T.Get("founding.fragileBelow")) + ")");
                return sb.ToString().TrimEnd();
            }
            sb.AppendLine(Simulation.Cap(i.Def.Name) + ", led by " + i.Leader + ": strength " + F(i.Strength) + ", " + F(sim.DomainShare(i) * 100) + "% of " + i.Def.Maintains + ".");
            sb.AppendLine("You hold " + sim.StakePercent(i) + "%: " + StakeMeaning(sim, i));
            if (!sim.Controls(i))
            {
                AppendRecent(sim, sb, new[] { Simulation.StrengthKey(i), Simulation.StakeKey(i) }, 3);
                return sb.ToString().TrimEnd();
            }
            sb.AppendLine("Loyalty " + F(i.Loyalty) + ".");
            sb.AppendLine("It is " + (i.Chartered ? "chartered" : "not chartered") + " and " + (i.Endowed ? "endowed" : "not endowed") + ".");
            var q = sim.QualityAtDeparture(i);
            sb.AppendLine("If you left now it would be " + q + ": it would lose " + F(sim.DecayRate(q) * 100) + "% of its strength each decade.");
            sb.AppendLine("Left alone it would most likely drift toward " + sim.ChooseDriftPath(i).Name + ".");
            sb.AppendLine("Loyalty fades " + F(sim.T.Get("institutions.loyaltyFadePerYear")) + " a year unless you oversee it; it grows only while loyalty is at least " +
                          F(sim.T.Get("institutions.growthLoyaltyThreshold")) + ".");
            AppendRecent(sim, sb, new[] { Simulation.StrengthKey(i), Simulation.LoyaltyKey(i) }, 5);
            return sb.ToString().TrimEnd();
        }

        private static string StakeMeaning(Simulation sim, Institution i)
        {
            string next = i.Def.IsOwn ? "" : " Next 1% costs " + sim.Money(sim.StakeCost(i, 1)) + ". Dues " + sim.Money(sim.AnnualDues(i)) + " a year.";
            if (sim.Controls(i)) return "you control it (oversee, mentor, charter, endow, audit, invest)." + next;
            int to = sim.NextThresholdPercent(i);
            string gap = " " + to + "% would cost " + sim.Money(sim.StakeCost(i, to - sim.StakePercent(i))) + " more.";
            if (sim.HasVoice(i)) return "a voice (priorities in its domain" + (i.Def.Maintains == Core.Domain.Governance ? ", policy" : "") + "), no control." + gap;
            if (sim.HasInfluence(i)) return "it counts toward your influence over " + i.Def.Maintains + ", but gives you no say." + gap;
            if (i.Stake > 0) return "a member's share: a little of its surplus, no say." + gap;
            return "nothing yet. To join it asks for " + sim.JoinRequirementText(i) + (sim.JoinBlocker(i) == null ? " (you qualify)" : " (you don't yet)") +
                   "; joining costs " + sim.Money(sim.BuyCost(i, 1)) + " (entry fee " + sim.Money(sim.EntryFee(i)) + " + the first 1%), then dues of " +
                   sim.Money(sim.T.Get("joining.duesBasePerYear." + i.Key)) + " a year plus " + sim.Money(sim.T.Get("joining.duesPerStakePercentPerYear")) + " per percent you hold.";
        }

        private static string Policy(Simulation sim)
        {
            var sb = new StringBuilder();
            sb.AppendLine(sim.PolicyHold() ? Simulation.Cap(sim.PolicyInstitution!.Def.ShortName) + " carries your line on Rome's economic policy (policy <issue> <stance>, 1 Attention); " +
                                             "your sway over Governance is " + F(sim.PolicySway() * 100) + "%, so that much of it takes effect."
                                           : "You have no voice in policy yet: you need " + F(sim.VoiceAt * 100) + "% of a Governance institution. Until then Rome keeps its own practice.");
            foreach (var i in Simulation.Issues)
                sb.AppendLine("  " + i + ": " + Simulation.StanceWord(i, sim.Stance(i)) + "   (options: " + Simulation.StanceWord(i, 1) + ", " + Simulation.StanceWord(i, -1) + ", history)");
            sb.AppendLine("Austrian stances (sound, free, secure, light) grow the Economy " + F(sim.T.Get("policy.austrianEconomyPerYear")) +
                          " a year each against history, but those who profit from intervention push back when you adopt them.");
            sb.AppendLine("Interventionist stances boost it " + F(sim.T.Get("policy.interventionBoomPerYear")) + " a year each for now, and build malinvestment that ends in a bust.");
            sb.AppendLine("Malinvestment now: " + F(sim.World.Malinvestment) + (sim.World.Bust.Stage > 0 ? ". " + Simulation.BustStageText(sim.World.Bust.Stage) : "."));
            sb.AppendLine("Work tax: " + F(sim.WorkTaxRate() * 100) + "%.");
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
            return "You have " + sim.World.Attention + " of " + sim.AttentionPerTurn + " Attention left this month" +
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
                sb.AppendLine("  " + e.Time.Stamp + "  " + e.Text + (fx.Delta != 0 && !fx.Key.Contains("stage") && !fx.Key.EndsWith(".stake") ? " [" + Signed(fx.Delta) + "]" : ""));
                // Skip causes that are just the previous step of the same trend (e.g. last year's loyalty fade).
                foreach (var c in e.ImmediateCauses.Select(sim.Log.Get)
                             .Where(c => c.Type != "scenario.start" && c.Type != "turn.start" && !(c.Type == e.Type && c.Target == e.Target)))
                    sb.AppendLine("      because: " + c.Text + " (" + c.Time.Stamp + ")");
            }
        }

        private static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
