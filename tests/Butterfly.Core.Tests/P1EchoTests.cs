using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 jump echoes (decided 2026-10-02; master handoff §19): the arrival mixes technical, personal, institutional and
    /// unintended echoes, as present conditions. A reproducible capability carried by an institution you belong to spreads
    /// while you are away (PROPOSED P1-09).
    /// </summary>
    [Collection("Console")]
    public class P1EchoTests
    {
        private static Simulation AfterThePump(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Working) sim.EndMonth();
            sim.World.Gold = 2000;
            return sim;
        }

        /// <summary>Accepts every guild invitation until the access reaches the stage (Felix's fever may slow it).</summary>
        private static void FollowFelix(Simulation sim, InstitutionAccessStage until)
        {
            for (int m = 0; m < 60 && sim.World.AccessTo("guild").Stage < until; m++)
            {
                if (sim.InvitationState("guild")!.Pending != InvitationOffer.None) Assert.True(sim.AcceptInvitation("guild").Ok);
                if (sim.World.AccessTo("guild").Stage < until) sim.EndMonth();
            }
            Assert.True(sim.World.AccessTo("guild").Stage >= until);
        }

        private static string Beat(Arrival a, string name) => a.Beats.First(b => b.Name == name).Text;

        [Fact]
        public void AGuildMemberFindsTheValveSeatsSpreadAndFelixRemembered()
        {
            var sim = AfterThePump();
            FollowFelix(sim, InstitutionAccessStage.Member);
            var arrival = sim.JumpForTests();
            Assert.True(sim.CapabilityLevelOf("valveseats") >= CapabilityLevel.Adopted);
            Assert.Contains("technical:valveseats", arrival.P1Echoes);
            Assert.Contains("person:Felix", arrival.P1Echoes);
            Assert.Contains("Felix who connected the shops", Beat(arrival, "Personal echo"));
            var def = sim.CapabilityDefOf("valveseats")!;
            Assert.Contains(def.Echo[sim.CapabilityLevelOf("valveseats").ToString()], Beat(arrival, "Discovery"));
            var carried = sim.Log.Events.Last(e => e.Type == "capability.advance");
            Assert.Contains(sim.World.Institution("guild").Leader, carried.Actors);    // carried by the guild, not by you
            Assert.Contains(sim.Log.Events.Last(e => e.Type == "jump.depart").Id, carried.ImmediateCauses);
        }

        [Fact]
        public void AGuestWhoNeverJoinedFindsOnlyTheBookOfSuppers()
        {
            var sim = AfterThePump();
            FollowFelix(sim, InstitutionAccessStage.Guest);
            var before = sim.CapabilityLevelOf("valveseats");
            var arrival = sim.JumpForTests();
            Assert.Equal(before, sim.CapabilityLevelOf("valveseats"));                   // no member, no carrier
            Assert.Contains("access:guild", arrival.P1Echoes);
            Assert.Contains(sim.InvitationPathDefFor("guild")!.EchoGuest, Beat(arrival, "Discovery"));
            Assert.Contains("person:Cassianus", arrival.P1Echoes);
        }

        [Fact]
        public void WithoutTheWorkThereIsNoP1Echo()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            for (int m = 0; m < 12; m++) { if (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Offered) sim.DeclineCommission("cellarpump"); sim.EndMonth(); }
            var arrival = sim.JumpForTests();
            Assert.Empty(arrival.P1Echoes);
        }

        [Fact]
        public void TheEchoesDescribeThePresentNotTheChain()
        {
            // No causal chains after the jump (PROTOTYPE_SCOPE): echo lines never say "because" or "you caused".
            var c = TestData.Load().Content;
            var lines = c.Capabilities.SelectMany(x => x.Echo.Values).Concat(c.People.SelectMany(p => p.Echoes.Select(e => e.Value)))
                         .Concat(c.InvitationPaths.Select(p => p.EchoGuest)).Where(s => s.Length > 0).ToList();
            Assert.NotEmpty(lines);
            Assert.All(lines, s => { Assert.DoesNotContain("because", s); Assert.DoesNotContain("you caused", s); });
        }
    }
}
