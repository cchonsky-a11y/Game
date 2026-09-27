using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Regression tests for text bugs found in the automated playtests (playtests/ai/bugs.md).</summary>
    public class TextFixTests
    {
        [Fact]
        public void WrongnessLeavesTheCoinToTheCoinLine()
        {
            // The coin line always follows the Wrongness text, so the Wrongness templates don't describe the coin themselves
            // (found by the exploration runs: "copper washed in silver" then "bronze washed thin with silver").
            var text = TestData.Load().Content.Text;
            foreach (var kv in text.Where(kv => kv.Key.StartsWith("wrongness.")))
            {
                Assert.DoesNotContain("coin", kv.Value);
                Assert.DoesNotContain("silver", kv.Value);
            }
        }

        [Fact]
        public void UnansweredPromiseIsNotDescribedAsARefusal()
        {
            var sim = new Simulation(TestData.Load(), 106);
            sim.World.CompletedProjects.Add("fountain"); // Demetria asks only if she has a reason (decided 2026-09-28)
            while (sim.World.Promise.Status == PromiseStatus.NotOffered) sim.EndTurn();
            var arrival = sim.JumpForTests();
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
                if (foundCircle) { sim.GrantStake("circle", 0.5); sim.Charter("circle"); sim.Endow("circle"); }
                while (sim.World.Plague.Stage != PlagueState.Passed)
                {
                    if (sim.World.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(true);
                    if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                    sim.EndTurn();
                }
                string text = sim.JumpForTests().Beats.First(b => b.Name == "Personal echo").Text;
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
            sim.GrantStake("circle", 0.5);
            sim.Charter("circle");
            Assert.Contains(sim.DepartureBriefing(), l => l.Contains("chartered but not endowed, so it counts as bare"));
        }
    
        [Fact]
        public void AFountainRepairedLaterIsNotDescribedAsFilledIn()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            sim.World.CompletedProjects.Add("fountain");
            sim.World.CleanWater = true;
            sim.World.FountainCondition = 100;
            var arrival = sim.JumpForTests();
            var recognition = arrival.Beats.First(b => b.Name == "Recognition").Text;
            Assert.DoesNotContain("filled in long ago", recognition);
            Assert.Contains("you repaired later", recognition);
        }

        [Fact]
        public void NoBackingDoesNotClaimNoOneKnowsDemetria()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var arrival = sim.JumpForTests();
            Assert.DoesNotContain("No one has heard", arrival.Beats.First(b => b.Name == "Discovery").Text);
        }

        [Fact]
        public void TheBriefingWarnsAboutAnUnansweredPromise()
        {
            var sim = new Simulation(TestData.Load(), 41);
            sim.World.CompletedProjects.Add("fountain");
            while (sim.World.Promise.Status == PromiseStatus.NotOffered) sim.EndTurn();
            Assert.Contains(sim.DepartureBriefing(), l => l.Contains("never have an answer"));
        }
    }
}
