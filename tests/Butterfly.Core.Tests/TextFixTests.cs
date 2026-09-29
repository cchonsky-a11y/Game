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
    
        [Fact]
        public void WhenYouLeadAnInstitutionItsLinesNeverCallYouInTheThirdPerson()
        {
            // L5 (tester 6): "you of the Guild thinks better of you", "you gives you a larger say".
            var sim = new Simulation(TestData.Load(), 616);
            sim.ChooseSeeded("workshop");
            sim.GrantStake("guild", 0.6);
            sim.GrantStake("faction", 0.3);
            foreach (var key in new[] { "guild", "faction" })
            {
                var inst = sim.World.Institution(key);
                inst.Rank = Simulation.Head;
                inst.Leader = "you";
            }
            while (sim.Now.Year < 174)
            {
                sim.World.Gold = 20000;
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options[0].Id);
                foreach (var inv in sim.Data.Content.Inventions) sim.Invent(inv.Id);
                sim.EndTurn();
            }
            var bad = new System.Text.RegularExpressions.Regex(@"\byou (of|thinks|gives|resents|carries|is |follows|notes|relies)\b");
            var offenders = sim.Log.Events.Where(e => bad.IsMatch(e.Text)).Select(e => e.Text).ToList();
            Assert.True(offenders.Count == 0, string.Join("\n", offenders));
            Assert.Contains(sim.Log.Events, e => e.Text.IndexOf("members of the guild", StringComparison.OrdinalIgnoreCase) >= 0
                                               || e.Text.IndexOf("your say in the guild", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    
        private static string DiscoveryAfter(System.Action<Simulation> shape)
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.JumpForTests();
            shape(sim);
            var a = sim.Arrival!;
            a.Beats.Clear();
            sim.BuildBeats(a);
            return a.Beats.Single(b => b.Name == "Discovery").Text;
        }

        [Fact]
        public void AGuildThrivingAsTheCartelIsDescribedAsTheCartel()
        {
            // L6 (tester 6): "still arguing for open markets and honest coin. Its head is … Master of the Cartel."
            string text = DiscoveryAfter(sim =>
            {
                var g = sim.World.Institution("guild");
                sim.GrantStake("guild", 0.5);
                g.Strength = 60; g.Loyalty = 80;
                g.OrderCamp = 1; g.OrderForce = 1;
                g.DriftPath = g.Def.DriftPaths[1];
                g.HasDrifted = true;
                Assert.Equal(InstitutionOutcome.Thriving, sim.OutcomeOf(g));
            });
            Assert.Contains("the Ostia Grain Cartel", text);
            Assert.DoesNotContain("open markets", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheCirclesCharterIsMentionedOnlyIfItHasOne(bool chartered)
        {
            // L10 (tester 2): "keep a copy of your charter under glass" for a Circle never chartered.
            string text = DiscoveryAfter(sim =>
            {
                var c = sim.World.Institution("circle");
                sim.GrantStake("circle", 0.5);
                c.Strength = 60; c.Loyalty = 80; c.HasDrifted = false;
                c.Chartered = chartered;
                Assert.Equal(InstitutionOutcome.Thriving, sim.OutcomeOf(c));
            });
            Assert.Equal(chartered, text.Contains("charter"));
        }
    
        [Fact]
        public void InventionPayoffsHaveNoEmptyClauses()
        {
            // L7 (testers 2, 6): "the guild:  (if you're a member)".
            var sim = new Simulation(TestData.Load(), 42);
            foreach (var inv in sim.Data.Content.Inventions)
            {
                string t = sim.InventionPayoffText(inv);
                Assert.DoesNotContain(":  (", t);
                Assert.DoesNotContain(";;", t);
                Assert.DoesNotContain("; ;", t);
                Assert.DoesNotContain(",,", t);
                Assert.False(string.IsNullOrWhiteSpace(t), inv.Id);
            }
        }
    }
}
