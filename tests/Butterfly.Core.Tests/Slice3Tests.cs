using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class ProjectAndGoldTests
    {
        private static Simulation NewSim() => new Simulation(TestData.Load(), 3);

        [Fact]
        public void ContentHasTwoToThreeProjectsPerDomain()
        {
            var projects = TestData.Load().Content.Projects;
            foreach (var d in DomainInfo.All)
                Assert.InRange(projects.Count(p => p.Domain == d), 2, 3);
        }

        [Fact]
        public void ProjectCostsGoldAndRaisesDomainWithCause()
        {
            var sim = NewSim();
            double gold = sim.World.Gold;
            double level = sim.World[Domain.Medicine].Level;
            var fountain = sim.Data.Content.Project("fountain")!;
            Assert.True(sim.StartProject("fountain").Ok);
            Assert.Equal(gold - fountain.Gold, sim.World.Gold);
            sim.EndTurn();
            Assert.Equal(level + fountain.LevelGain, sim.World[Domain.Medicine].Level);
            Assert.True(sim.World.CleanWater);

            var start = sim.Log.Events.First(e => e.Type == "project.start");
            var done = sim.Log.Events.First(e => e.Type == "project.complete");
            Assert.Contains(start.Id, done.ImmediateCauses);
            Assert.Contains("player", done.Actors);
        }

        [Fact]
        public void MultiTurnProjectCompletesAfterItsTurns()
        {
            var sim = NewSim();
            sim.World.Gold = 500;
            Assert.True(sim.StartProject("warehouses").Ok);
            sim.EndTurn();
            sim.EndTurn();
            Assert.DoesNotContain("warehouses", sim.World.CompletedProjects);
            sim.EndTurn();
            Assert.Contains("warehouses", sim.World.CompletedProjects);
        }

        [Fact]
        public void CannotStartWithoutGoldOrTwice()
        {
            var sim = NewSim();
            sim.World.Gold = 10;
            Assert.False(sim.StartProject("physician").Ok);
            sim.World.Gold = 500;
            Assert.True(sim.StartProject("physician").Ok);
            Assert.False(sim.StartProject("physician").Ok);
            Assert.False(sim.StartProject("nonsense").Ok);
        }

        [Fact]
        public void IncomeAndUpkeepSettleEachTurnScaledPerYear()
        {
            var sim = NewSim();
            var t = sim.T;
            double gold = sim.World.Gold;
            double expected = gold + (sim.YearlyIncome() - sim.YearlyUpkeepTotal()) * sim.YearsPerTurn;
            sim.EndTurn();
            Assert.Equal(expected, sim.World.Gold, 6);
        }

        [Fact]
        public void UnpaidUpkeepActsAsAcceptedRisk()
        {
            var sim = NewSim();
            foreach (var d in DomainInfo.All) sim.SetPriority(d, Priority.Protect);
            sim.World.Gold = 0;
            sim.World.IncomeBonus = -1000; // no income at all
            for (int i = 0; i < 4; i++) sim.EndTurn();
            double neglect = sim.T.Get("priorities.levelChangePerYear.acceptRisk");
            Assert.Equal(sim.T.Get("domains.startLevel.medicine") + neglect, sim.World[Domain.Medicine].Level, 6);
        }

        [Fact]
        public void PaydownCostsOneAndAHalfTimesPreventionInGame()
        {
            var sim = NewSim();
            sim.World[Domain.Economy].Debt = 30;
            sim.World.Gold = 1000;
            Assert.True(sim.PayDown(Domain.Economy, 10).Ok);
            Assert.Equal(20, sim.World[Domain.Economy].Debt, 6);
            Assert.Equal(1000 - 10 * sim.T.Get("debt.preventionGoldPerPoint") * 1.5, sim.World.Gold, 6);
        }
    }

    public class HardConstraintTests
    {
        [Fact]
        public void ShippedContentPassesSystems14Checks()
        {
            Assert.Empty(ContentChecks.Check(TestData.Load().Content));
        }

        [Fact]
        public void ChecksCatchForbiddenContent()
        {
            var problems = new List<string>();
            ContentChecks.CheckVerb("bad", new[] { "enslaved-labor" }, new[] { "Buy slaves for the mine" }, problems);
            Assert.Equal(2, problems.Count);
        }
    }
}
