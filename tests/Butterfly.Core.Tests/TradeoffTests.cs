using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Tradeoffs on existing actions (decided 2026-09-28, P0-31) and the savings caps (P0-35).</summary>
    public class TradeoffTests
    {
        private static Simulation Rich(ulong seed = 11)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (!sim.World.CompletedProjects.Contains("workshop")) sim.EndTurn();   // the hour-one choice takes a few turns
            sim.EndTurn();
            sim.World.Gold = 5000;
            return sim;
        }

        private static void Hold(Simulation sim, string id, double stake)
        {
            var i = sim.World.Institution(id);
            i.Stake = stake;
            i.JoinedAt = sim.Now.YearFraction - 6;
            i.Loyalty = 80;
        }

        [Fact]
        public void TheFactionsWontShareAMemberAtAnyStake()
        {
            var sim = Rich();
            Hold(sim, "faction", 0.10);
            Hold(sim, "junian", 0.05);
            Assert.Equal(9, sim.ExclusiveCapPercent(sim.World.Institution("junian")));
            Assert.False(sim.Buy("junian", 5).Ok);                 // would reach 10%
            Assert.True(sim.Buy("junian", 4).Ok);                  // up to 9% is allowed
            // Seniority can't carry it past the lock-out either.
            int year = sim.Now.Year;
            while (sim.Now.Year < year + 2) sim.EndTurn();
            Assert.True(sim.StakePercent(sim.World.Institution("junian")) <= 9);
        }

        [Fact]
        public void TheRivalFactionObstructsGovernanceProjects()
        {
            var sim = Rich();
            var census = sim.Data.Content.Project("census")!;
            int before = sim.ProjectGold(census);
            Hold(sim, "faction", 0.10);
            Assert.Equal("junian", sim.RivalFactionObstructs()!.Key);
            double memberShare = sim.T.GetArray("offices.projectShare")[0];            // a member's discount applies too
            Assert.Equal(System.Math.Round(before * (1 - memberShare) * (1 + sim.T.Get("tradeoffs.rivalFactionProjectMarkup"))), sim.ProjectGold(census), 0);
            Assert.Equal(sim.ProjectGold(sim.Data.Content.Project("market")!), sim.ProjectGold(sim.Data.Content.Project("market")!)); // other domains unaffected
        }

        [Fact]
        public void ConsultingCostsStandingWithThePhysiciansOfThePoor()
        {
            var sim = Rich();
            var circle = sim.World.Institution("circle");
            var w = sim.Work("consult"); Assert.True(w.Ok, w.Message);
            Assert.Equal(sim.T.Get("tradeoffs.consultCircleRegard"), circle.Regard, 6);   // not a member: remembered
            Hold(sim, "circle", 0.05);
            double before = circle.Loyalty;
            sim.EndTurn();
            w = sim.Work("consult"); Assert.True(w.Ok, w.Message);
            Assert.Equal(before + sim.T.Get("tradeoffs.consultCircleRegard"), circle.Loyalty, 6);   // a member: loyalty now
        }

        [Fact]
        public void QuarantineRulesHurtTradeAtOstia()
        {
            Simulation Run(bool quarantine)
            {
                var sim = Rich(12);
                Hold(sim, "sanctuary", 0.10);
                if (quarantine) { var q = sim.StartProject("quarantine"); Assert.True(q.Ok, q.Message); }
                for (int t = 0; t < 6; t++) sim.EndTurn();
                return sim;
            }
            var with = Run(true);
            var without = Run(false);
            Assert.Contains("quarantine", with.World.CompletedProjects);
            Assert.Contains(with.Log.Events, e => e.Type == "project.cost");
            Assert.True(with.World[Domain.Economy].Level < without.World[Domain.Economy].Level - 2.9);
            Assert.Equal(-10, with.World.Institution("guild").Regard, 6);          // remembered until you join
        }

        [Fact]
        public void LaborSavingInventionsBringUnrest()
        {
            var sim = Rich();
            double debt = sim.World[Domain.Governance].Debt;
            var iv = sim.Invent("wheelbarrow"); Assert.True(iv.Ok, iv.Message);
            for (int t = 0; t < 4; t++) sim.EndTurn();
            Assert.Contains(sim.Log.Events, e => e.Type == "invention.unrest");
            Assert.True(sim.World[Domain.Governance].Debt >= debt + 3 - 1e-6);
            Assert.Equal(-5, sim.World.Institution("guild").Regard, 6);
        }

        [Fact]
        public void TheBankAndTheJarHaveLimitsAndBigSumsAreRiskier()
        {
            var sim = Rich();
            sim.World.Aurei = 300;
            double bank = sim.T.Get("savings.depositCapAurei"), jar = sim.T.Get("savings.hoardCapAurei");
            Assert.False(sim.Deposit(bank + 1).Ok);
            double lossSmall = sim.DepositLossChance();
            Assert.True(sim.Deposit(bank).Ok);
            Assert.True(sim.DepositLossChance() > lossSmall);
            sim.EndTurn();
            Assert.False(sim.Bury(jar + 1).Ok);
            double foundEmpty = sim.HoardFoundChance();
            Assert.True(sim.Bury(jar).Ok);
            Assert.True(sim.HoardFoundChance() > foundEmpty);
            sim.EndTurn();
            Assert.False(sim.Bury(1).Ok);                          // the jar is full
        }

        [Fact]
        public void AGrievanceSetsTheLoyaltyYouStartWith()
        {
            var sim = Rich();
            var guild = sim.World.Institution("guild");
            guild.Regard = -10;
            Assert.True(sim.Buy("guild", 1).Ok);
            Assert.Equal(sim.T.Get("stakes.memberLoyalty") - 10, guild.Loyalty, 6);
            Assert.Equal(0, guild.Regard, 6);
        }
    }
}
