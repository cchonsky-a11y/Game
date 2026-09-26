using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class InstitutionDecayTests
    {
        private static double Rate(string key) => TestData.Load().Tuning.Get("institutions.decayPerDecade." + key);

        [Theory]
        [InlineData("bare", 5.7)]       // BUILD_GUIDE §8: 80 × 0.9^25 ≈ 5.7
        [InlineData("chartered", 37.4)] // 80 × 0.97^25 ≈ 37.4
        [InlineData("strong", 62.2)]    // 80 × 0.99^25 ≈ 62.2
        public void DecayOver250YearsMatchesReference(string quality, double expected)
        {
            Assert.Equal(expected, Formulas.Decay(80, Rate(quality), 25), 1);
        }

        [Theory]
        [InlineData(InstitutionQuality.Bare, 5.7)]
        [InlineData(InstitutionQuality.CharteredAndEndowed, 37.4)]
        [InlineData(InstitutionQuality.Strong, 62.2)]
        public void DecadeStepsInTheSimulationMatchReference(InstitutionQuality quality, double expected)
        {
            var sim = new Simulation(TestData.Load(), 11);
            var circle = sim.World.Institution("circle");
            sim.GrantStake("circle", 0.5);
            circle.Strength = 80;
            circle.Loyalty = 80;
            circle.Quality = quality;
            circle.DriftPath = circle.Def.DriftPaths[0];
            for (int decade = 1; decade <= 25; decade++) sim.InstitutionDecadeStep(circle, decade);
            Assert.Equal(expected, circle.Strength, 1);
        }
    }

    public class InstitutionTests
    {
        private static Simulation Rich()
        {
            var sim = new Simulation(TestData.Load(), 12);
            sim.World.Gold = 5000;
            sim.World.Attention = 100; // these tests check institution rules, not Attention
            StakesTests.MeetJoinRequirements(sim);
            return sim;
        }

        [Fact]
        public void ThreeInstitutionsEachWithLeaderAndTwoDriftPaths()
        {
            // Per domain (decided 2026-09-27): two established rivals you can buy into, and one you can found.
            var defs = TestData.Load().Content.Institutions;
            Assert.Equal(9, defs.Count);
            foreach (var d in DomainInfo.All)
            {
                Assert.Equal(2, defs.Count(x => x.Maintains == d && !x.IsOwn));
                Assert.Equal(1, defs.Count(x => x.Maintains == d && x.IsOwn));
            }
            Assert.All(defs, d =>
            {
                Assert.False(string.IsNullOrWhiteSpace(d.Leader));
                Assert.Equal(2, d.DriftPaths.Count);
            });
        }

        [Fact]
        public void FoundCharterEndowOversee()
        {
            var sim = Rich();
            Assert.False(sim.Charter("circle").Ok);
            Assert.False(sim.Found("circle").Ok);          // established: you buy into it, you don't found it
            Assert.True(sim.Buy("circle", 50).Ok);
            Assert.False(sim.Buy("school", 1).Ok);         // your own: you found it, you don't buy it
            Assert.True(sim.Charter("circle").Ok);
            Assert.True(sim.Endow("circle").Ok);
            var c = sim.World.Institution("circle");
            double loyalty = c.Loyalty;
            Assert.True(sim.Oversee("circle").Ok);
            Assert.True(c.Loyalty > loyalty);
            Assert.False(sim.Oversee("circle").Ok); // once per turn
            Assert.Contains(sim.Log.Events, e => e.Type == "institution.buy" && e.Actors.Contains("Demetria of Pergamon"));
        }

        [Fact]
        public void EndowedInstitutionsCostNoUpkeep()
        {
            var sim = Rich();
            sim.GrantStake("faction", 0.5);
            var f = sim.World.Institution("faction");
            double costs = sim.InstitutionCosts(f);
            sim.Endow("faction");
            Assert.Equal(costs - sim.T.Get("institutions.upkeepPerYear.faction"), sim.InstitutionCosts(f), 6);
        }

        [Fact]
        public void QualityRequiresCharterAndEndowment()
        {
            var sim = Rich();
            sim.GrantStake("circle", 0.5);
            var c = sim.World.Institution("circle");
            Assert.Equal(InstitutionQuality.Bare, sim.QualityAtDeparture(c));
            sim.Charter("circle");
            Assert.Equal(InstitutionQuality.Bare, sim.QualityAtDeparture(c));
            sim.Endow("circle");
            c.Loyalty = 50;
            Assert.Equal(InstitutionQuality.CharteredAndEndowed, sim.QualityAtDeparture(c));
            c.Loyalty = 90;
            Assert.Equal(InstitutionQuality.Strong, sim.QualityAtDeparture(c)); // start Index is 100: thriving
        }

        [Fact]
        public void DriftPathFollowsPreAuthoredConditions()
        {
            var sim = Rich();
            sim.GrantStake("faction", 0.5);
            var f = sim.World.Institution("faction");
            Assert.Equal("oligarchs", sim.ChooseDriftPath(f).Id);
            sim.Charter("faction");
            Assert.Equal("reformers", sim.ChooseDriftPath(f).Id);
            var c = sim.World.Institution("circle");
            Assert.Equal("collegium", sim.ChooseDriftPath(c).Id);
            sim.World.Plague.Response = "hospice";
            Assert.Equal("hospice", sim.ChooseDriftPath(c).Id);
        }

        [Fact]
        public void OutcomesFollowStrengthLoyaltyAndDrift()
        {
            var sim = Rich();
            sim.GrantStake("faction", 0.5);
            var f = sim.World.Institution("faction");
            Assert.Equal(InstitutionOutcome.Thriving, sim.OutcomeOf(f));
            f.HasDrifted = true;
            f.DriftPath = f.Def.DriftPaths.First(p => p.Id == "oligarchs");
            Assert.Equal(InstitutionOutcome.Captured, sim.OutcomeOf(f));
            f.DriftPath = f.Def.DriftPaths.First(p => p.Id == "reformers");
            Assert.Equal(InstitutionOutcome.Drifted, sim.OutcomeOf(f));
            f.Loyalty = 5;
            Assert.Equal(InstitutionOutcome.Rogue, sim.OutcomeOf(f));
            f.Strength = 5;
            Assert.Equal(InstitutionOutcome.Dissolved, sim.OutcomeOf(f));
            Assert.Equal(InstitutionOutcome.NotBacked, sim.OutcomeOf(sim.World.Institution("circle")));
        }

        [Fact]
        public void LoyaltyFadesEachYearWithoutOversight()
        {
            var sim = Rich();
            sim.GrantStake("circle", 0.5);
            double loyalty = sim.World.Institution("circle").Loyalty;
            for (int i = 0; i < 12 / sim.MonthsPerTurn; i++) sim.EndTurn();
            Assert.Equal(loyalty - sim.T.Get("institutions.loyaltyFadePerYear"), sim.World.Institution("circle").Loyalty, 6);
        }
    }
}
