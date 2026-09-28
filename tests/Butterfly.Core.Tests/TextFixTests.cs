using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Regression tests for text bugs found in the automated playtests (playtests/ai/bugs.md).</summary>
    public class TextFixTests
    {
        [Fact]
        public void TheSecondArrivalReadsAsAReturn()
        {
            var sim = new Simulation(TestData.Load(), 41);
            sim.ChooseSeeded("fountain");
            while (sim.Now.Year < 170) { if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); sim.EndTurn(); }
            var first = sim.JumpForTests();
            var second = sim.JumpForTests();
            string r1 = first.Beats.First(b => b.Name == "Recognition").Text, r2 = second.Beats.First(b => b.Name == "Recognition").Text;
            Assert.DoesNotContain("again", r1);
            Assert.Contains("again", r2);
            Assert.NotEqual(first.Beats.First(b => b.Name == "Personal echo").Text, second.Beats.First(b => b.Name == "Personal echo").Text);
            foreach (var b in second.Beats) Assert.DoesNotContain("{", b.Text);
        }

        [Fact]
        public void WrongnessLeavesTheCoinToTheCoinLine()
        {
            // The coin line always follows the Wrongness text, so the Wrongness templates don't describe the coin themselves
            // (found by the exploration runs: "copper washed in silver" then "bronze washed thin with silver").
            var text = TestData.Load().Content.Text;
            foreach (var kv in text.Where(kv => kv.Key.StartsWith("wrongness.") || kv.Key.StartsWith("wrongness2.")))
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
            Assert.Contains("you later repaired", recognition);
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
    
        [Theory]
        [InlineData(1, "1 aureus")]
        [InlineData(2, "2 aurei")]
        public void GoldOnArrivalAgreesWithItsNumber(int extra, string expected)
        {
            // Tester 7: "It is still there: 1 aurei" and "The 1 aurei you couldn't carry".
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Aurei = sim.CarryAurei + extra + extra;
            Assert.True(sim.Bury(extra).Ok);
            var a = sim.JumpForTests();
            string text = string.Join(" ", a.Beats.Select(b => b.Text)) + a.LearnMore();
            Assert.True(text.Contains("still there: " + expected) || text.Contains("found your " + expected), text);   // the jar may have been found
            Assert.Contains("(" + expected + ")", text);
            Assert.DoesNotContain("1 aurei", text);
            Assert.Equal("0 aurei", Simulation.Aurei(0));
        }
    
        [Fact]
        public void TheCampSentenceStartsWithACapitalAndFollowsYourVote()
        {
            // Tester 7: "You spoke for the Infirmary of the Island. the Keepers of the Island Shrine lead."
            // Tester 6: voting for the Free Traders was answered "the Ostia Grain Cartel lead, as you want."
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Gold = 5000;
            Assert.True(sim.Buy("sanctuary", 1).Ok);
            var inst = sim.World.Institution("sanctuary");
            string msg = "";
            for (int k = 0; k < 6; k++)
            {
                msg = sim.Attend("sanctuary", inst.Def.DriftPaths[k < 5 ? 0 : 1].Id).Message;
                sim.EndTurn();
            }
            Assert.DoesNotMatch(@"\. [a-z]", msg);
            Assert.Contains("Leading now: ", msg);
            Assert.DoesNotContain("as you want", msg);   // this vote went to the other camp
        }
    
        [Fact]
        public void LeavingBeforeTheWarningsNeverNamesDemetriaOnEitherArrival()
        {
            // Tester 7 left in AD 159, never met Demetria, and on the second arrival read "Demetria's list of those who stayed".
            var sim = new Simulation(TestData.Load(), 42);
            var first = sim.JumpForTests();
            Assert.Equal("notOffered", first.Echoes.Single(e => e.Id == "promise").AtArrival);
            Assert.DoesNotContain("Demetria", first.Beats[2].Text);
            var second = sim.JumpForTests();
            Assert.Equal("notOffered", second.Echoes.Single(e => e.Id == "promise").AtArrival);
            Assert.DoesNotContain("Demetria", second.Beats[2].Text);
        }
    
        [Fact]
        public void TheFountainRepairedAfterwardsReadsAsChronologyNotWorkmanship()
        {
            // Tester 7: "the fountain you repaired later" / "you repaired late" read oddly.
            var text = TestData.Load().Content;
            foreach (var key in new[] { "recognition.unchosenFountain.laterRuns", "recognition.unchosenFountain.laterDry", "recognition2.unchosenFountain.laterRuns", "recognition2.unchosenFountain.laterDry" })
            {
                string t = text.Template(key);
                Assert.Contains("fountain you later repaired", t);
                Assert.DoesNotContain("repaired late", t);
            }
        }
    }
}
