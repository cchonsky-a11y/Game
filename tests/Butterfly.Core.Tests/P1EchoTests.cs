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
            var sim = P1Play.AfterThePump(seed);
            sim.World.Gold = 2000;
            return sim;
        }

        /// <summary>Accepts every guild invitation until the access reaches the stage (Felix's fever may slow it).</summary>
        private static void FollowFelix(Simulation sim, InstitutionAccessStage until) => P1Play.FollowFelix(sim, until, mustReach: true);

        private static string Beat(Arrival a, string name) => a.Beats.First(b => b.Name == name).Text;

        [Fact]
        public void AGuildMemberFindsTheValveSeatsSpreadAndFelixRemembered()
        {
            var sim = AfterThePump();
            FollowFelix(sim, InstitutionAccessStage.Member);
            var arrival = sim.JumpForTests();
            Assert.True(sim.CapabilityLevelOf("valveseats") >= CapabilityLevel.Manufacturable);   // at least one rung up the ladder
            Assert.Contains("technical:valveseats", arrival.P1Echoes);
            // Felix is found in the first return, at the age the years away make him (first return, 2026-10-04), not in the beats.
            Assert.DoesNotContain("person:Felix", arrival.P1Echoes);
            var felix = Assert.Single(sim.World.Return!.Sites, s => s.Person == "Felix");
            Assert.Equal("founder", felix.Variant);
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
            // The guest book is now found in the first return's guild hall, so the beat line it would repeat is left out.
            var hall = Assert.Single(sim.World.Return!.Sites, s => s.Id == "guildhall");
            Assert.Equal("guest", hall.Variant);
            Assert.DoesNotContain("access:guild", arrival.P1Echoes);
            Assert.DoesNotContain(sim.InvitationPathDefFor("guild")!.EchoGuest, Beat(arrival, "Discovery"));
            Assert.Contains(sim.World.Return!.Sites, s => s.Person == "Cassianus");      // found in the return, not the beats
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
            var lines = c.Capabilities.SelectMany(x => x.Echo.Values).Concat(c.People.SelectMany(p => p.Echoes.Select(e => e.Text)))
                         .Concat(c.InvitationPaths.Select(p => p.EchoGuest)).Where(s => s.Length > 0).ToList();
            Assert.NotEmpty(lines);
            Assert.All(lines, s => { Assert.DoesNotContain("because", s); Assert.DoesNotContain("you caused", s); });
        }
    }
}
