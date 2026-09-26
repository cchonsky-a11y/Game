using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Institution gold during the absence (decided 2026-09-26). Only in the 30 years after departure:
    /// holdings grow with the economy (0–1.5% a year by Economy level, never a fixed rate), corruption is
    /// checked each decade, and institutions pay down debt in their own domain at the 1.5× premium.
    /// Loyalty decides whether an institution pays; corruption decides whether the gold survives.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Yearly growth of institution holdings: 0–1.5% scaled by the Economy level.</summary>
        public double HoldingsGrowthRate() =>
            T.Get("institutions.holdings.growthMaxRatePerYear") * Math.Max(0, Math.Min(1, World[Domain.Economy].Level / T.Get("domains.maxLevel")));

        public double IntegrityModifier(Institution i) => T.Get("corruption.integrity." + i.Def.LeaderIntegrity);

        public bool LargeHoldings(Institution i) => i.Holdings >= T.Get("institutions.holdings.largeHoldings");

        /// <summary>Chance per decade = base × exposure × (1 − audit) × (1 − integrity).</summary>
        public double CorruptionChance(Institution i)
        {
            double exposure = T.Get(LargeHoldings(i) ? "corruption.exposureLarge" : "corruption.exposureSmall");
            double audit = i.AuditCharter ? T.Get("corruption.auditReduction") : 0;
            return T.Get("corruption.baseHazardPerDecade") * exposure * (1 - audit) * (1 - IntegrityModifier(i));
        }

        /// <summary>Corruption risk band shown before the jump (never the outcome).</summary>
        public string CorruptionRiskBand(Institution i)
        {
            double c = CorruptionChance(i);
            return c >= T.Get("corruption.riskBands.highFrom") ? "High" : c >= T.Get("corruption.riskBands.mediumFrom") ? "Medium" : "Low";
        }

        /// <summary>Severity weights (Minor, Major, Total): by audit charter, shifted by leader integrity.</summary>
        public double[] CorruptionWeights(Institution i)
        {
            string set = i.AuditCharter ? "audited" : "unprotected";
            double minor = T.Get("corruption.weights." + set + ".minor");
            double major = T.Get("corruption.weights." + set + ".major");
            double total = T.Get("corruption.weights." + set + ".total");
            double shift = T.Get("corruption.weights.integrityShift");
            if (i.Def.LeaderIntegrity == "venal") { double m = Math.Min(minor, shift); minor -= m; total += m; }
            if (i.Def.LeaderIntegrity == "honest") { double m = Math.Min(total, shift); total -= m; minor += m; }
            return new[] { minor, major, total };
        }

        /// <summary>Start of a decade inside the 30-year window: the corruption check, then debt payment.</summary>
        private void HoldingsDecadeStart(Institution i, Arrival arrival)
        {
            if (!Holds(i)) return;
            if (Rng.Chance(1 - Math.Pow(1 - CorruptionChance(i), StepFraction))) Corrupt(i, arrival);
            PayDebtFromHoldings(i, arrival);
        }

        /// <summary>End of a decade inside the window: holdings grow with the economy (never a fixed rate).</summary>
        private void HoldingsDecadeGrowth(Institution i)
        {
            if (!Holds(i)) return;
            double before = i.Holdings;
            i.Holdings *= Math.Pow(1 + HoldingsGrowthRate(), _stepYears);
            Record("holdings.grow", i.Key, CausesOf(LevelKey(Domain.Economy)), new[] { i.Leader },
                new[] { new Effect(HoldingsKey(i), before, i.Holdings) },
                Cap(i.Def.ShortName) + "'s holdings grow with Rome's economy.");
        }

        private bool Holds(Institution i) => HasInfluence(i) && i.Strength >= T.Get("institutions.dissolvedBelow") && i.Holdings > 0;

        private void Corrupt(Institution i, Arrival arrival)
        {
            var w = CorruptionWeights(i);
            double roll = Rng.NextDouble() * (w[0] + w[1] + w[2]);
            var level = roll < w[0] ? CorruptionLevel.Minor : roll < w[0] + w[1] ? CorruptionLevel.Major : CorruptionLevel.Total;
            string key = level.ToString().ToLowerInvariant();
            double before = i.Holdings;
            double lost = i.Holdings * T.Get("corruption.loss." + key);
            i.Holdings -= lost;
            i.GoldLostToCorruption += lost;
            if (level > i.Corruption) i.Corruption = level;
            var e = Record("institution.corruption", i.Key, CausesOf(HoldingsKey(i)), new[] { i.Leader },
                new[] { new Effect(HoldingsKey(i), before, i.Holdings) },
                (level == CorruptionLevel.Minor ? "Some of " + i.Def.Name + "'s gold goes missing."
                 : level == CorruptionLevel.Major ? "Half of " + i.Def.Name + "'s treasury is embezzled; it stops paying the city's debts."
                 : i.Def.Name + "'s treasury is stolen outright.") + " (" + level + " corruption)");

            var gov = World[Domain.Governance];
            double debtBefore = gov.Debt;
            gov.Debt += T.Get("corruption.governanceDebt." + key);
            Record("debt.corruption", Domain.Governance.Key(), new[] { e.Id }, new[] { i.Leader },
                new[] { new Effect(DebtKey(Domain.Governance), debtBefore, gov.Debt) }, "The scandal adds to Governance debt.");
            UpdateTier(Domain.Governance);

            if (level == CorruptionLevel.Total)
            {
                i.ForcedOutcome = i.Loyalty < T.Get("corruption.rogueBelowLoyalty") ? InstitutionOutcome.Rogue : InstitutionOutcome.Captured;
                Record("institution.captured", i.Key, new[] { e.Id }, new[] { i.Leader }, null,
                    Cap(i.Def.Name) + (i.ForcedOutcome == InstitutionOutcome.Rogue ? " goes rogue." : " is captured by those who robbed it."));
            }
            arrival.Corruption.Add("AD " + Now.Year + ": " + level + " corruption in " + i.Def.Name + " (" + F(lost) + " gold lost)");
        }

        /// <summary>Loyal institutions pay in full, drifted ones partially, rogue ones not at all; major or total corruption stops payment.</summary>
        public double PaymentShare(Institution i)
        {
            if (i.Loyalty < T.Get("institutions.rogueBelowLoyalty") || i.ForcedOutcome == InstitutionOutcome.Rogue) return 0;
            if (i.Corruption >= CorruptionLevel.Major) return 0;
            double share = i.HasDrifted || i.Loyalty < T.Get("institutions.lowLoyalty") ? T.Get("institutions.holdings.partialPaymentShare") : 1;
            if (i.Corruption == CorruptionLevel.Minor) share *= T.Get("corruption.minorPaymentFactor");
            return share;
        }

        private void PayDebtFromHoldings(Institution i, Arrival arrival)
        {
            var d = World[i.Def.Maintains];
            double share = PaymentShare(i);
            if (d.Debt <= 0 || i.Holdings <= 0 || share <= 0) return;
            double points = Math.Min(d.Debt * share, i.Holdings / PaydownCost(1));
            if (points <= 0) return;
            double cost = PaydownCost(points);
            double debtBefore = d.Debt, holdingsBefore = i.Holdings;
            d.Debt -= points;
            i.Holdings -= cost;
            i.DebtPaidAway += points;
            Record("debt.paidByInstitution", d.Domain.Key(), CausesOf(DebtKey(d.Domain), HoldingsKey(i)), new[] { i.Leader },
                new[] { new Effect(DebtKey(d.Domain), debtBefore, d.Debt), new Effect(HoldingsKey(i), holdingsBefore, i.Holdings) },
                Cap(i.Def.ShortName) + " pays " + F(cost) + " gold to clear " + F(points) + " " + d.Domain + " debt.");
            UpdateTier(d.Domain);
        }
    }
}
