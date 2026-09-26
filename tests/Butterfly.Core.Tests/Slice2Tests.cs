using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class DebtFormulaTests
    {
        [Fact]
        public void CompoundingMatchesBuildGuideReference()
        {
            // BUILD_GUIDE §8: gap 20 per year, 5% compounding, 3 years: 20 -> 41 -> 63.05
            var t = TestData.Load().Tuning;
            double rate = t.Get("debt.compoundRate");
            double accrual = Formulas.DebtAccrual(60, 40, t.Get("debt.accrualRate"));
            Assert.Equal(20, accrual, 6);
            double debt = 0;
            debt = Formulas.DebtStep(debt, accrual, rate);
            Assert.Equal(20, debt, 6);
            debt = Formulas.DebtStep(debt, accrual, rate);
            Assert.Equal(41, debt, 6);
            debt = Formulas.DebtStep(debt, accrual, rate);
            Assert.Equal(63.05, debt, 6);
        }

        [Fact]
        public void GddExampleReachesFragileAfterThreeYears()
        {
            // GDD §8: expectation 60, level 40 -> ≈63 after 3 years -> Fragile.
            var t = TestData.Load().Tuning;
            var tier = Formulas.Tier(63.05, t.Get("debt.tiers.strained"), t.Get("debt.tiers.fragile"), t.Get("debt.tiers.critical"));
            Assert.Equal(DebtTier.Fragile, tier);
        }

        [Fact]
        public void NoAccrualAtOrAboveExpectation()
        {
            Assert.Equal(0, Formulas.DebtAccrual(50, 60, 1));
        }

        [Fact]
        public void ExpectationIsMaxOfBenchmarkAndFadingPeak()
        {
            Assert.Equal(70, Formulas.Expectation(50, 70));
            Assert.Equal(50, Formulas.Expectation(50, 40));
            Assert.Equal(65, Formulas.FadePeak(70, 40, 5));
            Assert.Equal(68, Formulas.FadePeak(70, 68, 5));
        }

        [Fact]
        public void PaydownCostsOneAndAHalfTimesPrevention()
        {
            Assert.Equal(30, Formulas.PaydownCost(10, 2, 1.5), 6);
        }
    }

    public class DomainSimulationTests
    {
        private static void RunYears(Simulation sim, int years)
        {
            for (int i = 0; i < years * 12 / sim.MonthsPerTurn; i++) sim.EndTurn();
        }

        [Fact]
        public void AcceptedRiskReachesStrainedInTwoToThreeYears()
        {
            // PROTOTYPE_SCOPE pass criterion: 2–3 years of accepted risk -> Strained.
            var sim = new Simulation(TestData.Load(), 1);
            sim.SetPriority(Domain.Medicine, Priority.AcceptRisk);
            RunYears(sim, 1);
            Assert.Equal(DebtTier.Stable, sim.World[Domain.Medicine].Tier);
            RunYears(sim, 2);
            Assert.True(sim.World[Domain.Medicine].Tier >= DebtTier.Strained);
        }

        [Fact]
        public void ProtectKeepsDebtAtZeroEarly()
        {
            var sim = new Simulation(TestData.Load(), 1);
            sim.ChooseSeeded("fountain"); // otherwise the broken fountain's fever lowers Medicine in 157
            sim.SetPriority(Domain.Medicine, Priority.Protect);
            RunYears(sim, 3);
            Assert.Equal(0, sim.World[Domain.Medicine].Debt);
        }

        [Fact]
        public void DebtEventsNameTheirImmediateCauses()
        {
            var sim = new Simulation(TestData.Load(), 1);
            var set = sim.SetPriority(Domain.Economy, Priority.AcceptRisk);
            Assert.True(set.Ok);
            RunYears(sim, 1);
            var upkeep = sim.Log.Events.First(e => e.Type == "domain.upkeep" && e.Target == "economy")!;
            var priorityEvent = sim.Log.Events.First(e => e.Type == "priority.set")!;
            Assert.Contains(priorityEvent.Id, upkeep.ImmediateCauses);
            var debt = sim.Log.Events.First(e => e.Type == "debt.accrue" && e.Target == "economy")!;
            Assert.Contains(upkeep.Id, debt.ImmediateCauses);
        }
    }
}
