using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class PlagueTests
    {
        private static void RunUntil(Simulation sim, int year)
        {
            while (sim.Now.Year < year)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                sim.EndTurn();
            }
        }

        [Fact]
        public void ThreeWarningStagesPrecedeTheOutbreakInEveryRun()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                if (seed % 2 == 0) foreach (var d in DomainInfo.All) sim.SetPriority(d, Priority.AcceptRisk);
                RunUntil(sim, 170);
                var warnings = sim.Log.Events.Where(e => e.Type == "plague.warning").ToList();
                var outbreak = sim.Log.Events.Single(e => e.Type == "plague.outbreak");
                Assert.Equal(3, warnings.Count);
                // Each stage on its own turn, before the next.
                Assert.True(warnings[0].Time.TotalMonths < warnings[1].Time.TotalMonths);
                Assert.True(warnings[1].Time.TotalMonths < warnings[2].Time.TotalMonths);
                Assert.True(warnings[2].Time.TotalMonths < outbreak.Time.TotalMonths);
                Assert.Equal(PlagueState.Passed, sim.World.Plague.Stage);
            }
        }

        [Fact]
        public void OutbreakHappensWithinTheScenarioWindow()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                RunUntil(sim, 171);
                Assert.Equal(166, sim.World.Plague.OutbreakYear); // as in history, inside the era (AD 155–175)
            }
        }

        [Fact]
        public void ThePlagueComesOnItsHistoricalDatesWhateverThePlayerDoes()
        {
            string Dates(Priority p, ulong seed)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.World.Gold = 10000;
                sim.GrantStake("circle", 0.5);
                foreach (var d in DomainInfo.All) if (p != Priority.Maintain) sim.SetPriority(d, p);
                RunUntil(sim, 171);
                return string.Join(",", sim.Log.Events.Where(e => e.Type == "plague.warning" || e.Type == "plague.outbreak").Select(e => e.Time.Stamp));
            }
            for (ulong seed = 1; seed <= 20; seed++)
                Assert.Equal(Dates(Priority.Protect, seed), Dates(Priority.AcceptRisk, seed));
            Assert.Equal("AD 165-07,AD 166-01,AD 166-07,AD 166-10", Dates(Priority.Maintain, 1));
        }

        [Fact]
        public void LeftAloneThePlagueStrikesAsHistoryHadIt()
        {
            // Rome as history had it (levels on the curve, fountain foul, no debt, no response): the historical toll.
            var sim = new Simulation(TestData.Load(), 3);
            Assert.Equal(sim.HistoricalPlagueSeverity, sim.PlagueSeverity("none"), 6);
            Assert.Equal(0.10, sim.PlagueSeverity("none") * sim.T.Get("plague.deathRatePerSeverity"), 6);
            // Whatever it turns out to be, each domain loses the historical drop scaled by severity against history's.
            RunUntil(sim, 175);
            double ratio = sim.World.Plague.Severity / sim.HistoricalPlagueSeverity;
            var damage = sim.Log.Events.Where(e => e.Type == "plague.damage").ToList();
            foreach (var d in DomainInfo.All)
            {
                var e = damage.Single(x => x.Target == d.Key());
                Assert.Equal(-sim.HistoricalPlagueDrop(d) * ratio, e.Effects[0].After - e.Effects[0].Before, 6);
            }
        }

        [Fact]
        public void HistoryFollowedWithoutThePlaguesStepCountedTwice()
        {
            // A domain that follows history loses the plague once, at the outbreak, not again in the year after.
            var sim = new Simulation(TestData.Load(), 3);
            Assert.Equal(0, sim.HistoricalPlagueDrop(Domain.Medicine) - (sim.Benchmark(Domain.Medicine, 166) - sim.Benchmark(Domain.Medicine, 167)), 9);
            RunUntil(sim, 168);
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "domain.upkeep" && e.Target == "medicine" && e.Time.Year == 167);
        }

        [Fact]
        public void SeverityScalesWithMedicineDebtAndFallsWithResponse()
        {
            var sim = new Simulation(TestData.Load(), 5);
            double low = sim.PlagueSeverity(null);
            sim.World[Domain.Medicine].Debt = 60;
            double high = sim.PlagueSeverity(null);
            Assert.True(high > low);
            Assert.True(sim.PlagueSeverity("quarantine") < high);
        }

        [Fact]
        public void CleanWaterLowersHazard()
        {
            var sim = new Simulation(TestData.Load(), 5);
            double foul = sim.PlagueHazard();
            sim.World.CleanWater = true;
            Assert.Equal(foul - sim.T.Get("plague.foulWaterHazard"), sim.PlagueHazard(), 6);
        }

        [Fact]
        public void OutbreakDamagesReleasesDebtAndOpensAPath()
        {
            var sim = new Simulation(TestData.Load(), 9);
            sim.SetPriority(Domain.Medicine, Priority.AcceptRisk);
            RunUntil(sim, 170);
            Assert.Contains(sim.Log.Events, e => e.Type == "plague.toll");
            Assert.Contains(sim.Log.Events, e => e.Type == "debt.release" && e.Target == "medicine");
            Assert.Contains(sim.Log.Events, e => e.Type == "plague.opening");
            Assert.NotNull(sim.World.Plague.Opening);
            Assert.True(sim.World.Population < sim.T.Get("plague.startPopulation"));
        }

        [Fact]
        public void DifferentSeedsProduceDifferentLogs()
        {
            string Run(ulong seed)
            {
                var sim = new Simulation(TestData.Load(), seed);
                RunUntil(sim, 170);
                return sim.Log.Hash();
            }
            var hashes = Enumerable.Range(1, 10).Select(s => Run((ulong)s)).Distinct().Count();
            Assert.True(hashes > 1);
            Assert.Equal(Run(77), Run(77));
        }
    }
}
