using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Camps, offices and last orders (decided 2026-09-28, P0-32).</summary>
    public class OfficesTests
    {
        private static Simulation Setup(ulong seed = 21)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (!sim.World.CompletedProjects.Contains("workshop")) sim.EndTurn();
            sim.EndTurn();
            sim.World.Gold = 5000;
            return sim;
        }

        private static Institution Member(Simulation sim, string id, double stake, double years, double loyalty = 80)
        {
            var i = sim.World.Institution(id);
            i.Stake = stake;
            i.Rank = Simulation.Member;
            i.JoinedAt = sim.Now.YearFraction - years;
            i.Loyalty = loyalty;
            return i;
        }

        private static void NextYear(Simulation sim)
        {
            int y = sim.Now.Year;
            while (sim.Now.Year == y) sim.EndTurn();
        }

        [Fact]
        public void AMeetingIsAVoteAndOfficeWeighsMore()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.05, 1);
            double start = guild.Lean;
            Assert.True(sim.Attend("guild", "cartel").Ok);
            double memberStep = start - guild.Lean;
            Assert.True(memberStep > 0);
            Assert.Equal(1, guild.Votes[1]);
            sim.EndTurn();
            guild.Rank = Simulation.Officer;
            double before = guild.Lean;
            Assert.True(sim.Attend("guild", "freetraders").Ok);
            Assert.Equal(2 * memberStep, guild.Lean - before, 6);
            Assert.False(sim.Attend("bank", "whatever").Ok);       // not a member
        }

        [Fact]
        public void WithoutACampNamedYouVoteAsYouUsuallyDo()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.05, 1);
            Assert.True(sim.Attend("guild", "free").Ok);
            sim.EndTurn();
            Assert.True(sim.Attend("guild").Ok);
            Assert.Equal(2, guild.Votes[0]);
        }

        [Fact]
        public void QualifyingMembersAreOfferedOfficeAndDutiesCostAttention()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.10, 3, 70);
            NextYear(sim);
            Assert.Equal(Simulation.Officer, guild.OfferedRank);
            Assert.Contains(sim.Log.Events, e => e.Type == "office.offer" && e.Text.Contains("quaestor"));
            int free = sim.World.Attention;
            Assert.True(sim.AnswerOffice("guild", true).Ok);
            Assert.Equal(Simulation.Officer, guild.Rank);
            sim.EndTurn();
            Assert.Equal(sim.AttentionPerTurn - sim.ReservedAttention(), sim.World.Attention);
            Assert.True(sim.OfficeDuties() >= 1);
            Assert.True(sim.Resign("guild").Ok);
            Assert.Equal(Simulation.Member, guild.Rank);
        }

        [Fact]
        public void DeputyNeedsYourCampToLeadAndForeignersHitACeiling()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.30, 6);
            guild.Rank = Simulation.Officer;
            guild.Votes[0] = 3;
            guild.Lean = -0.5;                                      // the cartel leads
            Assert.Contains("camp", sim.OfficeBlocker(guild, Simulation.Deputy));
            guild.Lean = 0.5;
            Assert.Null(sim.OfficeBlocker(guild, Simulation.Deputy));
            var faction = Member(sim, "faction", 0.60, 10);
            faction.Rank = Simulation.Deputy;
            Assert.Contains("foreigner", sim.OfficeBlocker(faction, Simulation.Head));
        }

        [Fact]
        public void TheGuildElectsItsQuinquennalisEveryFiveYears()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.30, 6);
            guild.Rank = Simulation.Deputy;
            guild.Votes[0] = 4;
            guild.Lean = 0.6;
            while (sim.Now.Year % 5 != 4) NextYear(sim);
            guild.Loyalty = 90; guild.Lean = 0.6;
            NextYear(sim);                                           // the election year begins
            Assert.True(guild.OfferedRank == Simulation.Head, sim.OfficeBlocker(guild, Simulation.Head) + " year " + sim.Now.Year + " lean " + guild.Lean + " loyalty " + guild.Loyalty + " stake " + guild.Stake);
            Assert.True(sim.AnswerOffice("guild", true).Ok);
            Assert.Equal("you", guild.Leader);
        }

        [Fact]
        public void LastOrdersWeighByOfficeLoyaltyAndRecord()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.10, 3, 100);
            Assert.True(sim.Orders("guild", "freetraders").Ok);
            double asMember = sim.LastOrderForce(guild);
            guild.Rank = Simulation.Head;
            double asHead = sim.LastOrderForce(guild);
            Assert.True(asHead > 5 * asMember);
            guild.Votes[1] = 5;                                      // a record against that camp
            Assert.True(sim.LastOrderForce(guild) < asHead);
            Assert.False(sim.Orders("bank", "counting").Ok);          // not a member
            guild.Rank = Simulation.Officer;
            Assert.False(sim.Orders("guild", "freetraders", 1).Ok);   // only the head names a successor
        }

        [Fact]
        public void AtDepartureYourOrdersSetThePathAndYourSuccessor()
        {
            var sim = Setup();
            var guild = Member(sim, "guild", 0.60, 8, 100);
            guild.Rank = Simulation.Head;
            guild.Leader = "you";
            guild.Votes[0] = 6;
            guild.Lean = -0.3;                                       // the cartel leads...
            Assert.True(sim.Orders("guild", "freetraders", 1).Ok);   // ...but you back the free traders and name Felix
            var arrival = sim.JumpForTests();
            Assert.Equal("freetraders", guild.DriftPath!.Id);
            Assert.Equal("honest", guild.Integrity);
            Assert.True(guild.OrderForce >= 0.6);
            var discovery = arrival.Beats.First(b => b.Name == "Discovery").Text;
            Assert.Contains("quinquennalis", discovery);
            Assert.Contains("parting words", discovery);
        }

        [Fact]
        public void YourOrdersSlowDrift()
        {
            double Drift(bool orders)
            {
                var sim = Setup(22);
                var guild = Member(sim, "guild", 0.30, 8, 100);
                guild.Votes[0] = 5;
                if (orders) { guild.Rank = Simulation.Deputy; sim.Orders("guild", "freetraders"); }
                sim.JumpForTests();
                return guild.Drift;
            }
            Assert.True(Drift(true) < Drift(false));
        }
    }
}
