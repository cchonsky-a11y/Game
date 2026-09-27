using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class IndexTests
    {
        [Fact]
        public void GeometricMeanMatchesGddExamples()
        {
            // GDD §14: balanced ≈ 102 (arithmetic 106); lopsided ≈ 72 (arithmetic 100).
            Assert.Equal(102, Formulas.GeometricMean(new double[] { 150, 140, 120, 110, 100, 90, 80, 60 }), 0);
            Assert.Equal(72, Formulas.GeometricMean(new double[] { 250, 200, 100, 100, 60, 40, 30, 20 }), 0);
        }

        [Fact]
        public void GeometricMeanOfEqualValuesIsThatValue()
        {
            Assert.Equal(80, Formulas.GeometricMean(new double[] { 80, 80, 80 }), 9);
        }

        [Fact]
        public void BalanceBeatsLopsidednessAtTheSameTotal()
        {
            Assert.True(Formulas.GeometricMean(new double[] { 100, 100, 100 }) > Formulas.GeometricMean(new double[] { 160, 90, 50 }));
        }

        [Fact]
        public void StartingIndexIsOneHundred()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.Equal(100, sim.SphereIndex(), 6);
        }

        [Fact]
        public void SubScoreIsLevelOverHistory()
        {
            Assert.Equal(120, Formulas.SubScore(60, 50), 9);
        }
    }

    public class WhyTests
    {
        [Fact]
        public void WhyDomainNamesLevelExpectationDebtAndCauses()
        {
            var sim = new Simulation(TestData.Load(), 2);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 500;
            sim.GrantStake("circle", 0.5);
            sim.SetPriority(Domain.Medicine, Priority.AcceptRisk);
            while (sim.Now.Year < 158) sim.EndTurn();
            string why = Why.Explain(sim, "medicine");
            Assert.Contains("Medicine is at", why);
            Assert.Contains("expect", why);
            Assert.Contains("Debt", why);
            Assert.Contains("because: You set Medicine to Accept Risk", why);
            Assert.Contains("because: You can afford only one", why); // fever linked to the seeded choice
        }

        [Fact]
        public void WhyWorksForEveryTopicWithoutJargon()
        {
            var sim = new Simulation(TestData.Load(), 2);
            sim.World.Gold = 500;
            sim.GrantStake("circle", 0.5);
            while (sim.World.Plague.Stage < 2) sim.EndTurn();
            foreach (var topic in Why.Topics)
            {
                string text = Why.Explain(sim, topic);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("Exception", text);
                Assert.DoesNotContain(".level", text); // no internal keys leak into explanations
                Assert.DoesNotContain(".debt", text);
            }
            Assert.Contains("The next stage comes in", Why.Explain(sim, "plague"));
        }

        [Fact]
        public void NoCausalTracingAfterTheJump()
        {
            var sim = new Simulation(TestData.Load(), 2);
            sim.JumpForTests();
            Assert.Contains("lost to you", Why.Explain(sim, "medicine"));
        }
    }
}
