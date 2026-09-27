using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>Economic policy issues (decided 2026-09-27).</summary>
    public enum PolicyIssue { Coinage, Prices, Property, Taxes }

    /// <summary>
    /// Economic policy (decided 2026-09-27): set through a Governance institution you have a voice in, and adopted in proportion to your sway. Each issue has an Austrian stance,
    /// Rome's historical practice, or an interventionist stance. Austrian stances make the Economy grow faster than
    /// history but provoke backlash from those who profit from intervention. Interventionist stances give a quick
    /// boom but build malinvestment, which ends in a bust after three visible warnings.
    /// </summary>
    public sealed partial class Simulation
    {
        public static readonly PolicyIssue[] Issues = { PolicyIssue.Coinage, PolicyIssue.Prices, PolicyIssue.Property, PolicyIssue.Taxes };

        /// <summary>Words for the Austrian (+1) and interventionist (−1) stances on each issue.</summary>
        public static string StanceWord(PolicyIssue issue, int stance)
        {
            if (stance == 0) return "as history";
            switch (issue)
            {
                case PolicyIssue.Coinage: return stance > 0 ? "sound" : "debase";
                case PolicyIssue.Prices: return stance > 0 ? "free" : "controlled";
                case PolicyIssue.Property: return stance > 0 ? "secure" : "discretionary";
                default: return stance > 0 ? "light" : "heavy";
            }
        }

        public static bool TryParsePolicy(string issueText, string stanceText, out PolicyIssue issue, out int stance)
        {
            issue = PolicyIssue.Coinage;
            stance = 0;
            var found = Issues.Where(i => i.ToString().ToLowerInvariant().StartsWith(issueText.ToLowerInvariant(), StringComparison.Ordinal) && issueText.Length >= 3).ToList();
            if (found.Count != 1) return false;
            issue = found[0];
            string s = stanceText.ToLowerInvariant();
            if (s == "history" || s == "historical") { stance = 0; return true; }
            if (s == StanceWord(issue, 1)) { stance = 1; return true; }
            if (s == StanceWord(issue, -1)) { stance = -1; return true; }
            return false;
        }

        public int Stance(PolicyIssue i) => World.Policy[(int)i];

        /// <summary>Policy needs a voice (25%+) in a working Governance institution: politics is where policy is made.</summary>
        public bool PolicyHold() => HasHold(Domain.Governance);

        /// <summary>The Governance institution that carries your line in the Curia.</summary>
        public Institution? PolicyInstitution => VoiceIn(Domain.Governance);

        /// <summary>How much of your policy Rome actually adopts: your sway over Governance (decided 2026-09-27).</summary>
        public double PolicySway() => PolicyHold() ? Sway(Domain.Governance) : 0;

        public CommandResult SetPolicy(PolicyIssue issue, int stance)
        {
            if (!PolicyHold()) return CommandResult.Fail("You have no voice in Rome's policy yet. You need " + F(VoiceAt * 100) +
                                                         "% of a Governance institution (buy faction / buy junian, or found club).");
            if (Stance(issue) == stance) return CommandResult.Fail(issue + " is already " + StanceWord(issue, stance) + ".");
            var attention = CheckAttention(T.GetInt("policy.attention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("policy.attention"));
            int before = Stance(issue);
            World.Policy[(int)issue] = stance;
            var faction = PolicyInstitution!;
            var e = Record("policy.set", issue.ToString().ToLowerInvariant(), null, new[] { "player", faction.Leader },
                new[] { new Effect("policy." + issue.ToString().ToLowerInvariant(), before, stance) },
                faction.Leader + " carries your line in the Curia: " + issue + " becomes " + StanceWord(issue, stance) +
                " (your sway over Governance: " + F(PolicySway() * 100) + "%).");
            if (stance > 0) Backlash(issue, e.Id);
            return CommandResult.Success(issue + ": " + StanceWord(issue, stance) + ".");
        }

        /// <summary>Those who profited from intervention push back when an Austrian stance is adopted.</summary>
        private void Backlash(PolicyIssue issue, int causeId)
        {
            var faction = PolicyInstitution!;
            string who = issue == PolicyIssue.Coinage ? "Mint officials and the creditors of the treasury"
                       : issue == PolicyIssue.Prices ? "Grain dealers who lived off fixed prices"
                       : issue == PolicyIssue.Property ? "Insiders who were used to favors and seizures"
                       : "Contractors paid from the levies";
            ChangeLoyalty(faction, -T.Get("policy.backlash.factionLoyalty"), "policy.backlash", new[] { causeId }, new[] { faction.Leader },
                who + " lean on the senators of " + faction.Def.ShortName + ".");
            AddDebt(Domain.Governance, T.Get("policy.backlash.governanceDebt") * PolicySway(), "policy.backlash", causeId, who + " stir up trouble in the Forum.");
        }

        internal void AddDebt(Domain d, double amount, string type, int causeId, string text)
        {
            var s = World[d];
            double before = s.Debt;
            s.Debt += amount;
            Record(type, d.Key(), new[] { causeId }, new[] { "world" }, new[] { new Effect(DebtKey(d), before, s.Debt) }, text);
            UpdateTier(d);
        }

        /// <summary>
        /// This year's inflation (decided 2026-09-28): Rome's historical practice debases the coin slowly; a debasement
        /// policy speeds it up and sound coin stops it, each in proportion to your sway over Governance.
        /// </summary>
        public double InflationRate()
        {
            double history = T.Get("prices.inflationAsHistory");
            int coin = Stance(PolicyIssue.Coinage);
            if (coin == 0) return history;
            double target = coin > 0 ? T.Get("prices.inflationSound") : T.Get("prices.inflationDebase");
            return history + PolicySway() * (target - history);
        }

        /// <summary>A price in AD 155 gold, at today's price level.</summary>
        public double Priced(double gold) => Math.Round(gold * World.PriceLevel, 1);

        private void PricesYearTick()
        {
            double rate = InflationRate();
            if (rate <= 0) return;
            double before = World.PriceLevel;
            World.PriceLevel *= 1 + rate;
            Record("prices.rise", "prices", CausesOf("policy.coinage"), new[] { "world" },
                new[] { new Effect("prices.level", before, World.PriceLevel) },
                "Prices rise " + F(rate * 100) + "% this year as the denarius is " + (Stance(PolicyIssue.Coinage) < 0 ? "debased by policy" : "quietly debased, as Rome's mint has always done") +
                " (prices now " + F((World.PriceLevel - 1) * 100) + "% above AD 155).");
        }

        /// <summary>Tax on work income: 10% as history, lighter or heavier by policy.</summary>
        public double WorkTaxRate()
        {
            int t = Stance(PolicyIssue.Taxes);
            return t > 0 ? T.Get("policy.tax.light") : t < 0 ? T.Get("policy.tax.heavy") : T.Get("personal.workTaxRate");
        }

        public int AustrianCount() => Issues.Count(i => Stance(i) > 0);
        public int InterventionCount() => Issues.Count(i => Stance(i) < 0);

        /// <summary>
        /// Yearly: stances move the Economy against history; intervention builds malinvestment; riots; the bust track.
        /// Every effect is scaled by your sway over Governance.
        /// </summary>
        private void PolicyYearTick()
        {
            if (PolicyHold())
            {
                double sway = PolicySway();
                double offset = sway * (AustrianCount() * T.Get("policy.austrianEconomyPerYear") + InterventionCount() * T.Get("policy.interventionBoomPerYear"));
                if (offset != 0)
                    ChangeLevel(Domain.Economy, offset, "policy.effect", CausesOf("policy.coinage", "policy.prices", "policy.property", "policy.taxes"),
                        new[] { "world" }, "Rome's economic policy moves the Economy " + Signed(offset) + " against history" +
                        (InterventionCount() > 0 ? " (part of it a boom built on intervention)." : "."));
                if (Stance(PolicyIssue.Taxes) < 0)
                    ChangeLevel(Domain.Governance, sway * T.Get("policy.heavyTaxGovernancePerYear"), "policy.effect", CausesOf("policy.taxes"), new[] { "world" },
                        "Heavy levies pay for public works and the grain dole.");
                if (InterventionCount() > 0)
                {
                    double before = World.Malinvestment;
                    World.Malinvestment += sway * InterventionCount() * T.Get("policy.malinvestmentPerStancePerYear");
                    Record("policy.malinvestment", "economy", CausesOf("policy.coinage", "policy.prices", "policy.property", "policy.taxes"), new[] { "world" },
                        new[] { new Effect("economy.malinvestment", before, World.Malinvestment) },
                        "Cheap coin and fixed prices steer money into ventures that only pay while the boom lasts.");
                }
                if (Stance(PolicyIssue.Prices) > 0 && World.Plague.Stage >= 2 && World.Plague.Stage <= PlagueState.Outbreak)
                    AddDebt(Domain.Governance, sway * T.Get("policy.breadRiotDebtPerYear"), "policy.backlash", CauseOf("policy.prices"),
                        "Bread prices climb with the sickness; crowds blame the free market.");
            }
            BustYearTick();
            PricesYearTick();
        }

        public static string BustStageText(int stage)
        {
            switch (stage)
            {
                case 1: return "Boom warning 1 of 3: prices outrun wages; everyone is building, and nobody is saving.";
                case 2: return "Boom warning 2 of 3: moneychangers at the Forum refuse the newest coin at face value.";
                case 3: return "Boom warning 3 of 3: credit dries up at Ostia; half-built warehouses stand empty.";
                default: return "Bust: the boom collapses.";
            }
        }

        private void BustYearTick()
        {
            var b = World.Bust;
            if (b.Stage == 0)
            {
                if (World.Malinvestment >= T.Get("policy.bust.firstWarningAt")) AdvanceBust();
                return;
            }
            if (b.StageEnteredYear >= Now.Year) return;
            double chance = Math.Min(0.9, World.Malinvestment * T.Get("policy.bust.advancePerMalinvestment"));
            if (Rng.Chance(chance)) AdvanceBust();
        }

        private void AdvanceBust()
        {
            var b = World.Bust;
            int before = b.Stage;
            b.Stage++;
            b.StageEnteredYear = Now.Year;
            var e = Record(b.Stage == 4 ? "bust.outbreak" : "bust.warning", "economy", new[] { b.LastEventId, CauseOf("economy.malinvestment") }, new[] { "world" },
                new[] { new Effect("bust.stage", before, b.Stage) }, BustStageText(b.Stage));
            b.LastEventId = e.Id;
            if (b.Stage == 4) Bust(null);
        }

        /// <summary>The bust: the malinvestment is liquidated. Damage scales with it; Economy debt clears and expectations reset.</summary>
        internal void Bust(Arrival? arrival)
        {
            var b = World.Bust;
            double m = World.Malinvestment;
            double goldBefore = World.Gold;
            World.Gold -= World.Gold * T.Get("policy.bust.goldLossShare");
            var e = Record("bust.toll", "economy", new[] { b.LastEventId }, new[] { "world" },
                new[] { new Effect("economy.malinvestment", m, 0), new Effect(GoldKey, goldBefore, World.Gold) },
                "The bust: ventures fail, lenders call in debts, and the boom's gains vanish.");
            ChangeLevel(Domain.Economy, -m * T.Get("policy.bust.economyDamagePerMalinvestment"), "bust.damage", new[] { e.Id }, new[] { "world" }, "The bust strikes the Economy.");
            ChangeLevel(Domain.Governance, -m * T.Get("policy.bust.governanceDamagePerMalinvestment"), "bust.damage", new[] { e.Id }, new[] { "world" }, "The bust shakes trust in the magistrates.");
            var eco = World[Domain.Economy];
            double debt = eco.Debt;
            eco.Debt = 0;
            eco.Peak = eco.Level;
            if (debt > 0) Record("debt.release", "economy", new[] { e.Id }, new[] { "world" }, new[] { new Effect(DebtKey(Domain.Economy), debt, 0) }, "The bust clears the Economy's debt.");
            UpdateTier(Domain.Economy);
            World.Malinvestment = 0;
            b.Stage = 0;
            b.Busts++;
            arrival?.Crises.Add("AD " + Now.Year + ": an economic bust after a boom built on intervention");
        }

        // ---- absence -----------------------------------------------------------

        /// <summary>While the faction survives, the stances you left keep shaping the Economy's target during the absence.</summary>
        /// <summary>
        /// Flat pull of the other three issues on the Economy's target during an absence. Coinage acts through
        /// <see cref="CoinRelief"/> instead: it spares or hastens Rome's historical debasement (decided 2026-09-28).
        /// </summary>
        internal double PolicyTargetBonus()
        {
            var others = Issues.Where(i => i != PolicyIssue.Coinage).ToList();
            return PolicySway() * (others.Count(i => Stance(i) > 0) * T.Get("policy.absence.austrianTargetPerStance")
                                   + others.Count(i => Stance(i) < 0) * T.Get("policy.absence.interventionTargetPerStance"));
        }

        // ---- the coin: Rome's own debasement (decided 2026-09-28) --------------------------------

        /// <summary>Silver share of Rome's everyday silver coin in a year, as history had it.</summary>
        public double HistoricalSilver(double year) =>
            Formulas.Interpolate(T.GetArray("history.coin.years"), T.GetArray("history.coin.silver"), year);

        /// <summary>How far Rome's historical debasement has gone by a year: 0 in AD 155, 1 at the coin's low.</summary>
        public double DebasementProgress(double year)
        {
            double start = HistoricalSilver(T.Get("time.startYear")), low = HistoricalSilver(T.Get("policy.coin.lowYear"));
            return start > low ? Math.Max(0, Math.Min(1, (start - HistoricalSilver(year)) / (start - low))) : 0;
        }

        /// <summary>The coin's share of a domain's decline from AD 155 to the coin's low, leaving out the plague's step.</summary>
        public double CoinDeclineSpan(Domain d)
        {
            double share = d == Domain.Economy ? T.Get("policy.coin.economyShare") : d == Domain.Governance ? T.Get("policy.coin.governanceShare") : 0;
            double decline = Benchmark(d, T.Get("time.startYear")) - Benchmark(d, T.Get("policy.coin.lowYear")) - HistoricalPlagueDrop(d);
            return share * Math.Max(0, decline);
        }

        /// <summary>How much of a domain's historical level the coin has cost by a year.</summary>
        public double CoinDecline(Domain d, double year) => CoinDeclineSpan(d) * DebasementProgress(year);

        /// <summary>
        /// What the coinage policy does to Rome's debasement while it stands: sound coin spares it (by sway),
        /// Rome's practice follows history, a debasement policy adds to it.
        /// </summary>
        public double CoinStanceFactor()
        {
            int coin = Stance(PolicyIssue.Coinage);
            return coin > 0 ? PolicySway() : coin < 0 ? -PolicySway() * T.Get("policy.coin.debaseExtra") : 0;
        }

        /// <summary>
        /// The coin's effect on a domain's target by a year of an absence: the historical debasement since you left,
        /// spared or added to while the policy's institution stands. Once no one defends the coin, the relief is gone.
        /// </summary>
        public double CoinRelief(Domain d, double year) =>
            CoinStanceFactor() * (CoinDecline(d, year) - CoinDecline(d, DepartureYear));

        internal void PolicyDecadeStep(Arrival arrival)
        {
            if (PolicyHold() && InterventionCount() > 0) World.Malinvestment += StepFraction * PolicySway() * InterventionCount() * T.Get("policy.absence.malinvestmentPerStancePerDecade");
            if (World.Malinvestment >= T.Get("policy.bust.firstWarningAt") && Rng.Chance(Math.Min(0.9, World.Malinvestment * T.Get("policy.bust.advancePerMalinvestment"))))
            {
                var e = Record("bust.outbreak", "economy", null, new[] { "world" }, null, "A boom built on intervention collapses.");
                World.Bust.LastEventId = e.Id;
                Bust(arrival);
            }
        }

        private bool SoundMoneyAndFreePrices() => Stance(PolicyIssue.Coinage) > 0 && Stance(PolicyIssue.Prices) > 0;
    }
}
