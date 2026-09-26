using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Regression tests for text bugs found in the automated playtests (playtests/ai/bugs.md).</summary>
    public class TextFixTests
    {
        [Fact]
        public void UnansweredPromiseIsNotDescribedAsARefusal()
        {
            var sim = new Simulation(TestData.Load(), 106);
            while (sim.World.Promise.Status == PromiseStatus.NotOffered) sim.EndTurn();
            var arrival = sim.Jump();
            Assert.Equal("unanswered", arrival.Echoes.First(e => e.Id == "promise").AtArrival);
            Assert.DoesNotContain("would not promise", arrival.Beats.First(b => b.Name == "Personal echo").Text);
        }

        [Fact]
        public void AsHistoryWrongnessDoesNotDenyTheOtherEchoes()
        {
            Assert.DoesNotContain("Nothing you did", TestData.Load().Content.Template("wrongness.asHistory"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void KeptPromiseEchoReadsCorrectlyWithOrWithoutTheCircle(bool foundCircle)
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.World.Gold = 500;
                sim.World.Attention = 100;
                if (foundCircle) { sim.Found("circle"); sim.Charter("circle"); sim.Endow("circle"); }
                while (sim.World.Plague.Stage != PlagueState.Passed)
                {
                    if (sim.World.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(true);
                    if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                    sim.EndTurn();
                }
                string text = sim.Jump().Beats.First(b => b.Name == "Personal echo").Text;
                Assert.DoesNotContain("the the", text, System.StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("the an", text, System.StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void BriefExplainsWhyAChartedInstitutionCountsAsBare()
        {
            var sim = new Simulation(TestData.Load(), 1);
            sim.World.Gold = 500;
            sim.World.Attention = 100;
            sim.Found("circle");
            sim.Charter("circle");
            Assert.Contains(sim.DepartureBriefing(), l => l.Contains("chartered but not endowed, so it counts as bare"));
        }
    }
}
