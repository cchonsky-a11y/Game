using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Rules decided in the 2026-09-26 review of docs/P0_PROPOSALS.md.</summary>
    public class DecisionTests
    {
        [Fact]
        public void PlagueOddsUseMedicineDebtOnly()
        {
            var sim = new Simulation(TestData.Load(), 1);
            double baseline = sim.PlagueAdvanceChance();
            sim.World[Domain.Governance].Tier = DebtTier.Critical;
            sim.World[Domain.Economy].Tier = DebtTier.Critical;
            Assert.Equal(baseline, sim.PlagueAdvanceChance(), 9);
            sim.World[Domain.Medicine].Tier = DebtTier.Critical;
            Assert.True(sim.PlagueAdvanceChance() > baseline);
        }

        [Fact]
        public void GovernanceAndEconomyDebtRaisePlagueSeverity()
        {
            var sim = new Simulation(TestData.Load(), 1);
            double calm = sim.PlagueSeverity(null);
            sim.World[Domain.Governance].Tier = DebtTier.Fragile;
            sim.World[Domain.Economy].Tier = DebtTier.Strained;
            double expected = calm * (1 + sim.T.Get("plague.severityPerTier.fragile") + sim.T.Get("plague.severityPerTier.strained"));
            Assert.Equal(expected, sim.PlagueSeverity(null), 6);
        }

        [Fact]
        public void DebtStopsCompoundingThirtyYearsAfterDeparture()
        {
            var t = TestData.Load().Tuning;
            int cap = t.GetInt("debt.compoundingCapYearsAfterDeparture");
            double debt = 10;
            for (int year = 1; year <= 60; year++)
                debt = Formulas.DebtStep(debt, 0, Formulas.AbsenceCompoundRate(year, cap, t.Get("debt.compoundRate")));
            // Grows for 30 years (10 × 1.05^30 ≈ 43.2), then holds.
            Assert.Equal(10 * System.Math.Pow(1.05, 30), debt, 6);
        }

        [Fact]
        public void CompoundingCapIsThirtyYears()
        {
            Assert.Equal(30, TestData.Load().Tuning.GetInt("debt.compoundingCapYearsAfterDeparture"));
        }
    }
}
