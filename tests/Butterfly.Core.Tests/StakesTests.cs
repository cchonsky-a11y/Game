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
            MeetJoinRequirements(sim);
            return sim;
        }

        /// <summary>These tests check stakes, not joining requirements.</summary>
        internal static void MeetJoinRequirements(Simulation sim)
        {
            sim.World.CompletedProjects.Add("fountain");
            sim.World.CompletedProjects.Add("workshop");
        }

        [Fact]
        public void EachPercentCostsMoreThanTheLast()
        {
            var sim = Rich();
            var circle = sim.World.Institution("circle");
            double b = sim.T.Get("stakes.costPerPercent.medicine");
            double newcomer = sim.T.Get("stakes.newcomerPremium");
            Assert.Equal(b * newcomer, sim.StakeCost(circle, 1), 6);            // the first 1%, at the newcomer premium
            Assert.Equal(b * 172.5, sim.ControlCost(Domain.Medicine), 6);       // 0→50%: Σ b(1 + k/10), k = 0..49 (normal price)
            Assert.True(sim.Buy("circle", 1).Ok);
            Assert.Equal(b * 1.1 * newcomer, sim.StakeCost(circle, 1), 6);      // the second 1%
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
            MeetJoinRequirements(sim);
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
        public void BuyingAStakeProvokesNoOne()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            sim.GrantStake("guild", 1);
            Assert.Equal(0, sim.RivalStrikeChance(guild), 6); // same share of the Economy as at the start
        }

        [Fact]
        public void EstablishedInstitutionsDrawFireWhenTheyGrowPastTheirStartingShare()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            sim.GrantStake("guild", 0.5);
            double baseline = sim.DomainShare(guild);
            guild.Strength += 20;
            double excess = (sim.DomainShare(guild) - baseline) * 100;
            Assert.Equal(sim.T.Get("rivalry.chanceAtThreshold") + sim.T.Get("rivalry.chancePerSharePoint") * excess, sim.RivalStrikeChance(guild), 6);
            double before = sim.RivalStrikeChance(guild);
            guild.Strength += 20;
            Assert.True(sim.RivalStrikeChance(guild) > before); // more of the domain, more pushback
        }

        [Fact]
        public void OwnInstitutionsDrawFireFrom20PercentOfTheDomain()
        {
            var sim = Rich();
            sim.Found("school");
            var school = sim.World.Institution("school");
            Assert.True(sim.DomainShare(school) < 0.2);
            Assert.Equal(0, sim.RivalStrikeChance(school), 6);
            school.Strength = 20; // 20 / (30 + 50 + 20) = 20%
            Assert.Equal(sim.T.Get("rivalry.chanceAtThreshold"), sim.RivalStrikeChance(school), 6);
            school.Strength = 40;
            Assert.True(sim.RivalStrikeChance(school) > sim.T.Get("rivalry.chanceAtThreshold"));
        }

        [Fact]
        public void RivalsStrikeWithCausesAndActors()
        {
            var data = TestData.Load().WithTuning(new Dictionary<string, double> { { "rivalry.chanceAtThreshold", 1 }, { "rivalry.maxStrikeChancePerYear", 1 } });
            var sim = new Simulation(data, 3);
            sim.GrantStake("guild", 0.5);
            var guild = sim.World.Institution("guild");
            guild.Strength = 90; // far past its starting share of the Economy
            guild.Loyalty = 100;
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
    
        // ---- joining requirements (decided 2026-09-28) ---------------------------

        private static Simulation Bare()
        {
            var sim = new Simulation(TestData.Load(), 5);
            sim.World.Gold = 5000;
            sim.World.Attention = 100;
            return sim;
        }

        [Fact]
        public void TheCircleWantsMedicineWorkOrThePromise()
        {
            var sim = Bare();
            Assert.False(sim.Buy("circle", 1).Ok);
            sim.World.CompletedProjects.Add("physician");
            Assert.True(sim.Buy("circle", 1).Ok);
            var promised = Bare();
            promised.World.Promise.Status = PromiseStatus.Active;
            Assert.True(promised.Buy("circle", 1).Ok);
        }

        [Fact]
        public void TheSanctuaryAsksNothing() => Assert.True(Bare().Buy("sanctuary", 1).Ok);

        [Fact]
        public void TheGuildAndTheJuniansWantABusinessOrProperty()
        {
            var sim = Bare();
            Assert.False(sim.Buy("guild", 1).Ok);
            Assert.False(sim.Buy("junian", 1).Ok);
            sim.World.CompletedProjects.Add("warehouses");
            Assert.True(sim.Buy("guild", 1).Ok);
            Assert.True(sim.Buy("junian", 1).Ok);
        }

        [Fact]
        public void TheCaeciliansWantPatronage()
        {
            var sim = Bare();
            Assert.False(sim.Buy("faction", 1).Ok);
            sim.World.ConsultJobs = sim.T.GetInt("joining.patronageConsultJobs");
            Assert.True(sim.Buy("faction", 1).Ok);
        }

        [Fact]
        public void ConsultingCountsTowardPatronage()
        {
            var sim = new Simulation(TestData.Load(), 5);
            Assert.True(sim.Work("consult").Ok);
            Assert.Equal(1, sim.World.ConsultJobs);
        }

        [Fact]
        public void TheBankWantsADepositFirst()
        {
            var sim = Bare();
            int min = sim.T.GetInt("joining.bankMinFirstPercent");
            Assert.False(sim.Buy("bank", min - 1).Ok);
            Assert.True(sim.Buy("bank", min).Ok);
            Assert.True(sim.Buy("bank", 1).Ok); // only the first purchase has a minimum
        }

        [Fact]
        public void TheFactionsWontShareAMember()
        {
            var sim = Bare();
            sim.World.CompletedProjects.Add("workshop");
            sim.World.ConsultJobs = 5;
            Assert.True(sim.Buy("junian", 10).Ok);
            Assert.False(sim.Buy("faction", 1).Ok);
            var small = Bare();
            small.World.CompletedProjects.Add("workshop");
            small.World.ConsultJobs = 5;
            Assert.True(small.Buy("junian", 9).Ok);
            Assert.True(small.Buy("faction", 1).Ok); // under 10% doesn't count
        }

        [Fact]
        public void OwnInstitutionsHaveNoRequirement() => Assert.True(Bare().Found("school").Ok);
    
        // ---- entry fee and annual dues (decided 2026-09-28) ----------------------

        [Fact]
        public void JoiningChargesTheEntryFeeOnce()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            double fee = sim.T.Get("joining.entryFee.guild");
            double gold = sim.World.Gold;
            Assert.True(sim.Buy("guild", 1).Ok);
            Assert.Equal(fee + sim.T.Get("stakes.costPerPercent.economy") * sim.T.Get("stakes.newcomerPremium"), gold - sim.World.Gold, 6);
            gold = sim.World.Gold;
            double next = sim.StakeCost(guild, 1);
            Assert.True(sim.Buy("guild", 1).Ok);
            Assert.Equal(next, gold - sim.World.Gold, 6); // no fee the second time
        }

        [Fact]
        public void DuesGrowWithYourStake()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            Assert.Equal(0, sim.AnnualDues(guild), 6);
            sim.GrantStake("guild", 0.01);
            double member = sim.AnnualDues(guild);
            Assert.Equal(sim.T.Get("joining.duesBasePerYear.guild") + sim.T.Get("joining.duesPerStakePercentPerYear"), member, 6);
            sim.GrantStake("guild", 0.5);
            Assert.True(sim.AnnualDues(guild) > member);
        }

        [Fact]
        public void YourOwnInstitutionsChargeNoDues()
        {
            var sim = Rich();
            sim.Found("house");
            Assert.Equal(0, sim.AnnualDues(sim.World.Institution("house")), 6);
        }

        [Fact]
        public void DuesArePaidEachTurn()
        {
            var sim = new Simulation(TestData.Load(), 5);
            sim.GrantStake("sanctuary", 0.2);
            var sanctuary = sim.World.Institution("sanctuary");
            sim.World.Gold = 100;
            double expected = sim.YearlyIncome() - sim.YearlyUpkeepTotal();
            Assert.True(sim.YearlyUpkeepTotal() >= sim.AnnualDues(sanctuary));
            sim.EndTurn();
            Assert.Equal(100 + expected * sim.YearsPerTurn, sim.World.Gold, 6);
        }
    
        // ---- time and gold: seniority and the newcomer premium (decided 2026-09-28) ----

        [Fact]
        public void TheNewcomerPremiumFadesOverFiveYears()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            Assert.Equal(sim.T.Get("stakes.newcomerPremium"), sim.NewcomerPremium(guild), 6);
            sim.Buy("guild", 1);
            double atJoin = sim.NewcomerPremium(guild);
            while (sim.YearsAsMember(guild) < 2.5) sim.EndTurn();
            Assert.True(sim.NewcomerPremium(guild) < atJoin && sim.NewcomerPremium(guild) > 1);
            while (sim.YearsAsMember(guild) < sim.T.Get("stakes.premiumFadeYears")) sim.EndTurn();
            Assert.Equal(1, sim.NewcomerPremium(guild), 6);
            Assert.Equal(1, sim.NewcomerPremium(sim.World.Institution("school")), 6); // your own: no premium
        }

        [Fact]
        public void SeniorityGrowsYourStakeUpTo25Percent()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            sim.Buy("guild", 23);
            int before = sim.StakePercent(guild);
            while (sim.YearsAsMember(guild) < 1.5) sim.EndTurn();
            Assert.Equal(before + sim.T.GetInt("stakes.seniorityPercentPerYear"), sim.StakePercent(guild));
            Assert.Contains(sim.Log.Events, e => e.Type == "institution.seniority" && e.Effects.Any(fx => fx.Key == "guild.stake"));
            while (sim.YearsAsMember(guild) < 6) sim.EndTurn();
            Assert.Equal(25, sim.StakePercent(guild)); // seniority alone never goes past a voice
        }

        [Fact]
        public void UnpaidDuesEarnNoSeniority()
        {
            var sim = new Simulation(TestData.Load(), 5);
            MeetJoinRequirements(sim);
            sim.World.Gold = 100;
            Assert.True(sim.Buy("sanctuary", 1).Ok);
            var sanctuary = sim.World.Institution("sanctuary");
            while (sim.YearsAsMember(sanctuary) < 2.5) { sim.World.Gold = 0; sim.EndTurn(); }
            Assert.Equal(1, sim.StakePercent(sanctuary));
        }
    }
}
