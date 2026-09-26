using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Rules decided on 2026-09-27 (G1, G3, G4, G8).</summary>
    public class Decisions0927Tests
    {
        [Fact]
        public void EndTurnAdvancesExactlyOneTurnWhenAnActionIsPossible()
        {
            var sim = new Simulation(TestData.Load(), 3);
            sim.ChooseSeeded("fountain");
            Assert.Equal(1, sim.EndTurnAndSkipIdle());
            Assert.Equal(2, sim.Turn);
        }

        [Fact]
        public void EndTurnSkipsOnlyTurnsWithNoFreeAttention()
        {
            var sim = new Simulation(TestData.Load(), 3);
            sim.World.Gold = 1000;
            sim.ChooseSeeded("fountain");
            sim.EndTurn();
            sim.Found("circle");                      // 2 Attention now
            sim.EndTurn();
            Assert.True(sim.StartProject("warehouses").Ok); // 2 per turn for 4 turns
            Assert.True(sim.Mentor("circle").Ok);           // 2 per turn for 5 turns
            int advanced = sim.EndTurnAndSkipIdle();
            Assert.True(advanced > 1);                      // fully committed turns pass on their own
            Assert.False(sim.NoActionPossible());
        }

        [Theory]
        [InlineData(0.04, "contained")]
        [InlineData(0.05, "severe")]
        [InlineData(0.149, "severe")]
        [InlineData(0.15, "catastrophic")]
        public void SeverityIsLabelledByShareOfPopulationDead(double share, string label)
        {
            Assert.Equal(label, new Simulation(TestData.Load(), 1).SeverityLabel(share));
        }

        [Fact]
        public void TollAlwaysShowsTheDeathToll()
        {
            var sim = new Simulation(TestData.Load(), 9);
            while (sim.World.Plague.Stage != PlagueState.Passed) { if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); sim.EndTurn(); }
            var toll = sim.Log.Events.Single(e => e.Type == "plague.toll");
            Assert.Matches(@"about \d+ thousand dead, \d+% of Rome", toll.Text);
            Assert.Contains(sim.Log.Events, e => e.Type == "plague.passed" && e.Text.StartsWith("The sickness burns itself out. Nothing is fixed"));
        }

        [Fact]
        public void NoOutbreakWithinThirtyYearsOfTheLastOne()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                foreach (var d in DomainInfo.All) sim.SetPriority(d, Priority.AcceptRisk);
                sim.Jump();
                var years = sim.Log.Events.Where(e => e.Type == "plague.toll").Select(e => e.Time.Year).ToList();
                for (int i = 1; i < years.Count; i++) Assert.True(years[i] - years[i - 1] >= 30, "seed " + seed + ": outbreaks in " + years[i - 1] + " and " + years[i]);
            }
        }

        [Fact]
        public void UnmaintainedDomainsStopAccruingDebtAfterTheWindow()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                foreach (var d in DomainInfo.All) sim.SetPriority(d, Priority.AcceptRisk);
                while (sim.Now.Year < 165) { if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); sim.EndTurn(); }
                sim.Jump();
                // No institutions at all: after year 30, a domain's debt never rises within a decade (no accrual, no compounding).
                foreach (var e in sim.Log.Events.Where(e => e.Type == "jump.domain" && e.Time.Year - sim.DepartureYear > 30))
                {
                    var debt = e.Effects.First(fx => fx.Key.EndsWith(".debt"));
                    Assert.True(debt.After <= debt.Before + 1e-9, "seed " + seed + " " + e.Target + " debt rose at " + e.Time.Stamp);
                }
            }
        }
    }
}

namespace Butterfly.Core.Tests
{
    public class LongRunTargetTests
    {
        [Fact]
        public void LongRunTargetKeepsThirtyPercentOfTheDepartureDeviation()
        {
            var sim = new Simulation(TestData.Load(), 5);
            sim.World[Domain.Economy].Level = 75; // 20 above the AD 155 baseline of 55
            sim.Jump();
            int start = sim.DepartureYear + 40;    // a decade after the window
            double baseline = sim.Benchmark(Domain.Economy, start + 10);
            Assert.Equal(baseline + 0.3 * 20, sim.DecadeTarget(Domain.Economy, start), 6);
        }

        [Fact]
        public void DeficitsCarryForwardToo()
        {
            var sim = new Simulation(TestData.Load(), 5);
            sim.World[Domain.Economy].Level = 35; // 20 below the baseline
            sim.Jump();
            int start = sim.DepartureYear + 40;
            Assert.Equal(sim.Benchmark(Domain.Economy, start + 10) - 6, sim.DecadeTarget(Domain.Economy, start), 6);
        }
    }
}
