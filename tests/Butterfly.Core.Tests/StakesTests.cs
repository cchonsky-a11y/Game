using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Stakes, influence and rivals (decided 2026-09-27, P0-18).</summary>
    public class StakesTests
    {
        private static Simulation Rich(ulong seed = 5)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 5000;
            sim.World.Attention = 100;
            return sim;
        }

        [Fact]
        public void EachPercentCostsMoreThanTheLast()
        {
            var sim = Rich();
            var circle = sim.World.Institution("circle");
            double b = sim.T.Get("stakes.costPerPercent.medicine");
            Assert.Equal(b, sim.StakeCost(circle, 1), 6);                       // the first 1%
            Assert.Equal(b * 172.5, sim.ControlCost(Domain.Medicine), 6);       // 0→50%: Σ b(1 + k/10), k = 0..49
            Assert.True(sim.Buy("circle", 1).Ok);
            Assert.Equal(b * 1.1, sim.StakeCost(circle, 1), 6);                 // the second 1%
            Assert.Equal(0.01, circle.Stake, 6);
        }

        [Fact]
        public void CostDependsOnTheDomain()
        {
            var sim = Rich();
            Assert.NotEqual(sim.ControlCost(Domain.Medicine), sim.ControlCost(Domain.Governance));
            Assert.NotEqual(sim.ControlCost(Domain.Governance), sim.ControlCost(Domain.Economy));
        }

        [Theory]
        [InlineData("medicine")]
        [InlineData("governance")]
        [InlineData("economy")]
        public void FoundingCostsAbout65PercentOfControl(string domain)
        {
            var sim = Rich();
            DomainInfo.TryParseDomain(domain, out var d);
            Assert.Equal(System.Math.Round(0.65 * sim.ControlCost(d)), sim.FoundCost(d), 6);
        }

        [Fact]
        public void ThresholdsGiveInfluenceThenVoiceThenControl()
        {
            var sim = Rich();
            var circle = sim.World.Institution("circle");
            Assert.True(sim.Buy("circle", 1).Ok);                                 // a member
            Assert.False(sim.SetPriority(Domain.Medicine, Priority.Protect).Ok);
            Assert.Equal(0, sim.Influence(Domain.Medicine), 6);
            Assert.True(sim.Buy("circle", 9).Ok);                                 // 10%: influence, no say
            Assert.True(sim.Influence(Domain.Medicine) > 0);
            Assert.False(sim.SetPriority(Domain.Medicine, Priority.Protect).Ok);
            Assert.False(sim.Oversee("circle").Ok);
            Assert.True(sim.Buy("circle", 15).Ok);                                // 25%: a voice
            Assert.True(sim.SetPriority(Domain.Medicine, Priority.Protect).Ok);
            Assert.False(sim.Oversee("circle").Ok);
            Assert.False(sim.Charter("circle").Ok);
            Assert.True(sim.Buy("circle", 25).Ok);                                // 50%: control
            Assert.True(sim.Oversee("circle").Ok);
            Assert.True(sim.Charter("circle").Ok);
            Assert.True(sim.Controls(circle));
        }

        [Fact]
        public void BuyingCostsOneAttention()
        {
            var sim = new Simulation(TestData.Load(), 5);
            sim.World.Gold = 1000;
            int before = sim.World.Attention;
            Assert.True(sim.Buy("guild", 5).Ok);
            Assert.Equal(before - sim.T.GetInt("stakes.buyAttention"), sim.World.Attention);
        }

        [Fact]
        public void InfluenceIsControlTimesDomainShareAndSwayDoublesIt()
        {
            var sim = Rich();
            sim.GrantStake("circle", 0.25);
            var circle = sim.World.Institution("circle");
            var sanctuary = sim.World.Institution("sanctuary");
            double share = circle.Strength / (circle.Strength + sanctuary.Strength);
            Assert.Equal(share, sim.DomainShare(circle), 6);
            Assert.Equal(0.5 * share, sim.Influence(Domain.Medicine), 6);        // 25% of the way to control = half
            Assert.Equal(System.Math.Min(1, 2 * 0.5 * share), sim.Sway(Domain.Medicine), 6);
        }

        [Fact]
        public void PrioritiesActInProportionToSway()
        {
            var sim = Rich();
            sim.GrantStake("circle", 0.25);
            sim.SetPriority(Domain.Medicine, Priority.Protect);
            double sway = sim.Sway(Domain.Medicine);
            Assert.True(sway < 1);
            Assert.Equal(sway * sim.T.Get("priorities.levelChangePerYear.protect"), sim.PriorityLevelChange(Domain.Medicine), 6);
        }

        [Fact]
        public void PolicyNeedsAVoiceInGovernance()
        {
            var sim = Rich();
            Assert.False(sim.SetPolicy(PolicyIssue.Coinage, 1).Ok);
            sim.GrantStake("junian", 0.25);
            Assert.True(sim.SetPolicy(PolicyIssue.Coinage, 1).Ok);
            Assert.True(sim.PolicySway() > 0 && sim.PolicySway() <= 1);
        }

        [Fact]
        public void FoundedInstitutionIsControlledButSmall()
        {
            var sim = Rich();
            Assert.True(sim.Found("school").Ok);
            var school = sim.World.Institution("school");
            Assert.True(sim.Controls(school));
            Assert.Equal(sim.T.Get("founding.startStrength"), school.Strength, 6);
            Assert.True(sim.DomainShare(school) < 0.2);                            // little hold over its domain
            Assert.False(sim.Found("school").Ok);
            Assert.True(sim.Invest("school", 100).Ok);
            Assert.Equal(sim.T.Get("founding.startStrength") + 100 * sim.T.Get("stakes.investStrengthPerGold"), school.Strength, 6);
        }

        [Fact]
        public void YoungInstitutionsCanFailButEstablishedOnesDoNot()
        {
            int collapsed = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var sim = Rich(seed);
                sim.Found("school");
                sim.World.Institution("school").Loyalty = 0; // no growth: it stays fragile
                for (int t = 0; t < 12 && !sim.EraOver; t++) sim.EndTurn();
                if (sim.World.Institution("school").Collapsed) collapsed++;
            }
            Assert.InRange(collapsed, 1, 29);

            for (ulong seed = 1; seed <= 10; seed++)
            {
                var sim = Rich(seed);
                sim.Found("club");
                var club = sim.World.Institution("club");
                club.Strength = sim.T.Get("founding.fragileBelow") + 20;
                for (int t = 0; t < 12; t++) sim.EndTurn();
                Assert.False(club.Collapsed);
            }
        }

        [Fact]
        public void CollapseIsLoggedWithCause()
        {
            var data = TestData.Load().WithTuning(new Dictionary<string, double> { { "founding.collapseChancePerYear", 1 } });
            var sim = new Simulation(data, 3);
            sim.World.Gold = 1000;
            sim.Found("house");
            for (int t = 0; t < 4; t++) sim.EndTurn();
            var e = sim.Log.Events.Single(x => x.Type == "institution.collapse");
            Assert.NotEmpty(e.ImmediateCauses);
            Assert.Equal(InstitutionOutcome.Dissolved, sim.OutcomeOf(sim.World.Institution("house")));
        }

        [Fact]
        public void RivalsPushBackFrom20PercentAndHarderWithMoreControl()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            double rate = sim.T.Get("rivalry.strikeChancePerStakePercent");
            sim.GrantStake("guild", 0.19);
            Assert.Equal(0, sim.RivalStrikeChance(guild), 6);
            sim.GrantStake("guild", 0.2);
            Assert.Equal(20 * rate, sim.RivalStrikeChance(guild), 6);
            sim.GrantStake("guild", 0.5);
            Assert.Equal(50 * rate, sim.RivalStrikeChance(guild), 6);
            Assert.True(sim.RivalStrikeChance(guild) > 20 * rate);
            sim.GrantStake("guild", 1);
            Assert.Equal(System.Math.Min(sim.T.Get("rivalry.maxStrikeChancePerYear"), 100 * rate), sim.RivalStrikeChance(guild), 6);
        }

        [Fact]
        public void RivalsStrikeWithCausesAndActors()
        {
            var data = TestData.Load().WithTuning(new Dictionary<string, double> { { "rivalry.strikeChancePerStakePercent", 1 }, { "rivalry.maxStrikeChancePerYear", 1 } });
            var sim = new Simulation(data, 3);
            sim.GrantStake("guild", 0.2); // a voice-level stake is enough to draw fire
            for (int t = 0; t < 4; t++) sim.EndTurn();
            var strikes = sim.Log.Events.Where(x => x.Type == "rivalry.strike").ToList();
            Assert.NotEmpty(strikes); // the bank is the only rival (the house was never founded)
            Assert.All(strikes, x => Assert.Contains(sim.World.Institution("bank").Def.Leader, x.Actors));
            Assert.All(strikes, x => Assert.NotEmpty(x.ImmediateCauses));
        }

        [Fact]
        public void DividendsFollowTheStake()
        {
            var sim = Rich();
            sim.GrantStake("guild", 0.1);
            var guild = sim.World.Institution("guild");
            double net = sim.InstitutionNet(guild);
            Assert.True(net > 0);
            Assert.Equal(0.1 * net, sim.YearlyIncome() - sim.OwnedIncome(), 6);
        }

        [Fact]
        public void MaintenanceDuringAbsenceScalesWithControlAndShare()
        {
            var sim = Rich();
            sim.GrantStake("circle", 0.5);
            var circle = sim.World.Institution("circle");
            double full = circle.Strength * sim.T.Get("jump.maintainPerStrength");
            double expected = full * System.Math.Min(1, 2 * sim.DomainShare(circle));
            Assert.Equal(expected, sim.MaintainBonus(Domain.Medicine), 6);
            sim.GrantStake("circle", 0.25);
            Assert.Equal(expected * 0.5, sim.MaintainBonus(Domain.Medicine), 6);
        }
    }
}
