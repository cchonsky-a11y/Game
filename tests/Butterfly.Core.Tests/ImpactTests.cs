using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Bigger impact for Rome's choices, memberships, founding and policy (decided 2026-09-28, Corey; P0-36): answers win
    /// standing in the institutions they touch and stack; offices grow your stake; advocacy without a voice; founding small,
    /// with a power for each; small memberships count and several together help.
    /// </summary>
    public class ImpactTests
    {
        private static Simulation Rich(ulong seed = 71)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 5000;
            sim.World.Attention = 100;
            StakesTests.MeetJoinRequirements(sim);
            return sim;
        }

        private static Simulation At(string eventId, ulong seed, System.Action<Simulation>? setup = null)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("fountain");
            setup?.Invoke(sim);
            int guard = 0;
            while (sim.PendingEvent?.Id != eventId && guard++ < 400)
            {
                sim.World.Gold = System.Math.Max(sim.World.Gold, 2000);
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            Assert.Equal(eventId, sim.PendingEvent?.Id);
            sim.World.Gold = 2000;
            return sim;
        }

        [Fact]
        public void AnAnswerWinsStakeInTheInstitutionsItTouchesIfYouAreAMember()
        {
            var sim = At("flood", 72, s => { s.World.Institution("circle").Stake = 0.05; s.World.Institution("circle").Rank = Simulation.Member; });
            int circle = sim.StakePercent(sim.World.Institution("circle"));
            Assert.True(sim.Decide("relief").Ok);
            Assert.Equal(circle + 2, sim.StakePercent(sim.World.Institution("circle")));
            Assert.Equal(0, sim.StakePercent(sim.World.Institution("sanctuary")));     // favor, not stake, where you aren't a member
        }

        [Fact]
        public void AnswersStack_FloodReliefMakesQuarantineCheaper()
        {
            var sim = At("flood", 73);
            var quarantine = sim.Data.Content.Project("quarantine")!;
            int before = sim.ProjectGold(quarantine);
            Assert.True(sim.Decide("relief").Ok);
            Assert.Contains("suburaTrust", sim.World.Flags);
            Assert.True(sim.ProjectGold(quarantine) < before);
        }

        [Fact]
        public void AnswersStack_TheGrainArrangementMakesTheLateFleetWorse()
        {
            double DebtAfterFleet(bool cartel)
            {
                var sim = At("fleet", 74, s => { if (cartel) s.World.Flags.Add("cartel"); });
                double debt = sim.World[Domain.Governance].Debt;
                sim.Decide("none");
                return sim.World[Domain.Governance].Debt - debt;
            }
            Assert.True(DebtAfterFleet(true) > DebtAfterFleet(false));
        }

        [Fact]
        public void ProfiteerTwiceAndTheStreetTurnsAgainstYou()
        {
            var sim = At("flood", 75);
            double pay = sim.WorkPay("odd");
            sim.Decide("profit");
            Assert.DoesNotContain("streetAgainst", sim.World.Flags);
            while (sim.PendingEvent?.Id != "invasion" && sim.Now.Year < 171)
            {
                sim.World.Gold = System.Math.Max(sim.World.Gold, 2000);
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            sim.Decide("sell");
            Assert.Contains("streetAgainst", sim.World.Flags);
            Assert.Equal(1 - sim.T.Get("events.street.workPay"), sim.StreetWorkFactor(), 6);
            Assert.Contains(sim.Log.Events, e => e.Type == "event.street");
        }

        [Fact]
        public void EveryOptionHasAKnownStyle()
        {
            var styles = new[] { "generous", "profit", "principled", "loyal", "aloof" };
            foreach (var o in TestData.Load().Content.Events.SelectMany(e => e.Options)) Assert.Contains(o.Style, styles);
        }

        [Fact]
        public void AnOfficeLetsYourStakeGrowPastTheMembersCap()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            sim.GrantStake("guild", 0.25);
            guild.JoinedAt = sim.Now.YearFraction - 5;
            guild.Rank = Simulation.Member;
            int year = sim.Now.Year;
            while (sim.Now.Year == year) sim.EndTurn();
            Assert.Equal(25, sim.StakePercent(guild));                                  // a member stops at 25%
            guild.Rank = Simulation.Deputy;
            year = sim.Now.Year;
            sim.World.Gold = 5000;
            while (sim.Now.Year == year) sim.EndTurn();
            Assert.True(sim.StakePercent(guild) > 25);                                  // an officer keeps growing
        }

        [Fact]
        public void AdvocacyMovesPolicyALittleWithoutAVoiceAndOnlyWhileYouAreHere()
        {
            var sim = Rich();
            Assert.False(sim.PolicyHold());
            double history = sim.InflationRate();
            Assert.True(sim.Advocate(PolicyIssue.Coinage, 1).Ok);
            Assert.InRange(sim.PolicySway(), 0.01, sim.T.Get("policy.advocacy.maxSway"));
            Assert.True(sim.InflationRate() < history);
            sim.GrantStake("faction", 0.03);
            sim.World.Institution("faction").Rank = Simulation.Member;
            Assert.True(sim.AdvocacySway() > sim.T.Get("policy.advocacy.baseSway"));       // a membership adds weight
            sim.JumpForTests();
            Assert.Equal(0, sim.PolicySway());
        }

        [Fact]
        public void WithAVoiceYouSetPolicyInsteadOfArguingForIt()
        {
            var sim = Rich();
            sim.GrantStake("faction", 0.25);
            Assert.False(sim.Advocate(PolicyIssue.Coinage, 1).Ok);
        }

        [Fact]
        public void TheClubCarriesYourPolicyAtStrengthEvenWhenSmall()
        {
            var sim = Rich();
            Assert.True(sim.Found("club").Ok);
            Assert.True(sim.PolicyHold());
            Assert.True(sim.PolicySway() >= sim.T.Get("founding.club.policySway") - 1e-9);
        }

        [Fact]
        public void TheSchoolSoftensThePlagueAndTheHousePaysItsFounder()
        {
            var sim = Rich();
            double resilience = sim.PlagueResilience(null), income = sim.OwnedIncome();
            Assert.True(sim.Found("school").Ok);
            Assert.True(sim.PlagueResilience(null) > resilience);
            Assert.True(sim.Found("house").Ok);
            Assert.True(sim.OwnedIncome() > income);
            sim.World.Institution("house").Strength = 40;
            Assert.Equal(sim.T.Get("founding.house.dividendPerYear"), sim.HouseDividend(), 6);
        }

        [Fact]
        public void SeveralMembershipsMakeProjectsCheaperAndRomeReadierForPlague()
        {
            var sim = Rich();
            var market = sim.Data.Content.Project("market")!;
            int gold = sim.ProjectGold(market);
            double resilience = sim.PlagueResilience(null);
            foreach (var id in new[] { "circle", "sanctuary", "guild" }) { sim.GrantStake(id, 0.01); sim.World.Institution(id).Rank = Simulation.Member; }
            Assert.Equal(3 * sim.T.Get("network.projectDiscountPerMembership"), sim.NetworkDiscount(), 6);
            Assert.True(sim.ProjectGold(market) < gold);
            Assert.True(sim.PlagueResilience(null) > resilience);
        }

        [Fact]
        public void ASmallMembershipStillHelpsKeepRomeUpWhileYouAreAway()
        {
            var sim = Rich();
            double bare = sim.MaintainBonus(Domain.Economy);
            sim.GrantStake("guild", 0.02);
            Assert.True(sim.MaintainBonus(Domain.Economy) > bare);
        }
    
        /// <summary>
        /// Free markets come only from the player (Corey, 2026-09-28): left alone, Rome keeps history's policy, history's
        /// inflation and history's economy; advocacy lasts only while the inventor is there to press it.
        /// </summary>
        [Fact]
        public void RomeFreesItsMarketsOnlyIfYouDriveIt()
        {
            var sim = new Simulation(TestData.Load(), 91);
            sim.ChooseSeeded("fountain");
            while (sim.Now.Year < 172)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            foreach (var issue in Simulation.Issues) Assert.Equal(0, sim.Stance(issue));
            Assert.Equal(sim.T.Get("prices.inflationAsHistory"), sim.InflationRate(), 9);
            Assert.Equal(0, sim.PolicyTargetBonus(200), 9);

            var pushed = Rich(92);
            pushed.Advocate(PolicyIssue.Prices, 1);
            Assert.True(pushed.PolicySway() > 0);
            pushed.JumpForTests();
            Assert.Equal(0, pushed.PolicySway());                                         // no one presses it once you're gone
            Assert.Equal(pushed.T.Get("prices.inflationAsHistory"), pushed.InflationRate(), 9);
        }
}
}
