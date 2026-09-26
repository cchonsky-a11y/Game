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
                // Each stage in a different, earlier year than the next.
                Assert.True(warnings[0].Time.Year < warnings[1].Time.Year);
                Assert.True(warnings[1].Time.Year < warnings[2].Time.Year);
                Assert.True(warnings[2].Time.Year < outbreak.Time.Year);
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
                Assert.InRange(sim.World.Plague.OutbreakYear, 161, 164); // inside the 20-turn era (ends AD 165)
            }
        }

        [Fact]
        public void NeglectBringsTheOutbreakSoonerOnAverage()
        {
            double Average(Priority p)
            {
                double sum = 0;
                for (ulong seed = 1; seed <= 100; seed++)
                {
                    var sim = new Simulation(TestData.Load(), seed);
                    sim.World.Gold = 10000; // enough to pay any upkeep, so only the priority differs
                    sim.Found("circle"); // priorities act only once an institution exists
                    foreach (var d in DomainInfo.All) if (p != Priority.Maintain) sim.SetPriority(d, p);
                    RunUntil(sim, 171);
                    sum += sim.World.Plague.OutbreakYear;
                }
                return sum / 100;
            }
            Assert.True(Average(Priority.AcceptRisk) < Average(Priority.Protect));
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
