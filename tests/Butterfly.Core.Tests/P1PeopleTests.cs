using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 recurring people (decided 2026-10-02; master handoff §11): their lives go on without the player and can derail the
    /// player's plan. Felix's fever pauses the guild's invitations; a fire hurts Cassianus and Diodoros; a neighbour's bad
    /// copy spreads the valve seats without you.
    /// </summary>
    [Collection("Console")]
    public class P1PeopleTests
    {
        private static Simulation AfterThePump(ulong seed)
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

        [Fact]
        public void EveryPersonIsFullyDrawnAndEveryLifeEventIsWellFormed()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var c = sim.Data.Content;
            Assert.True(c.People.Count >= 3);
            foreach (var p in c.People)
                foreach (var field in new[] { p.Role, p.Goal, p.Vulnerability, p.CaresAbout, p.Distrusts, p.Household, p.Status, p.Opinion, p.Interest, p.Voice })
                    Assert.False(string.IsNullOrWhiteSpace(field), p.Id);
            foreach (var e in c.Lives)
            {
                Assert.Contains(c.People, p => p.Id == e.Person);
                Assert.All(e.StatusChanges, kv => Assert.Contains(c.People, p => p.Id == kv.Key));
                foreach (var r in e.Requires.Concat(new[] { "life:" + e.Id })) sim.Holds(r);   // parses
                if (e.Capability.Length > 0) Assert.NotNull(sim.CapabilityDefOf(e.Capability));
                Assert.InRange(e.Chance, 0.0, 1.0);
            }
            foreach (var p in c.People) sim.Holds(p.Known);
        }

        [Fact]
        public void YouKnowPeopleOnlyOnceYouHaveMetThem()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.Empty(sim.KnownPeople());
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            Assert.Contains(sim.KnownPeople(), p => p.Id == "Felix");
            Assert.Contains(sim.KnownPeople(), p => p.Id == "Cassianus");
            Assert.DoesNotContain(sim.KnownPeople(), p => p.Id == "Diodoros");      // not until the fire
        }

        [Fact]
        public void FelixLaidUpMeansNoInvitationsUntilHeIsBack()
        {
            var sim = AfterThePump(42);
            var felix = sim.PersonOf("Felix")!;
            felix.AwayUntilTurn = sim.Turn + 6;
            Assert.False(sim.InvitationGate(sim.InvitationPathDefFor("guild")!).IsWarranted);
            for (int m = 0; m < 5; m++) { sim.EndMonth(); Assert.Equal(InvitationOffer.None, sim.InvitationState("guild")!.Pending); }
            for (int m = 0; m < 6 && sim.InvitationState("guild")!.Pending == InvitationOffer.None; m++) sim.EndMonth();
            Assert.Equal(InvitationOffer.Guest, sim.InvitationState("guild")!.Pending);
        }

        /// <summary>Plays on from the pump, never answering anything, until a life event happens (or gives up).</summary>
        private static Simulation UntilLife(string id, out ulong seed)
        {
            for (seed = 1; seed <= 30; seed++)
            {
                var sim = AfterThePump(seed);
                for (int m = 0; m < 120 && !sim.World.LifeEventLog.ContainsKey(id); m++) sim.EndMonth();
                if (sim.World.LifeEventLog.ContainsKey(id)) return sim;
            }
            throw new Xunit.Sdk.XunitException(id + " never happened in 30 seeds");
        }

        [Fact]
        public void AFireHurtsCassianusAndDiodorosWithoutThePlayer()
        {
            var sim = UntilLife("warehouse-fire", out _);
            var e = sim.Log.Get(sim.World.LifeEventLog["warehouse-fire"]);
            Assert.DoesNotContain("player", e.Actors);
            Assert.Contains("Diodoros", e.Actors);
            Assert.NotEmpty(e.ImmediateCauses);                                         // the finished pump made it matter
            Assert.Contains("burned", sim.PersonOf("Diodoros")!.Status);
            Assert.Contains(sim.KnownPeople(), p => p.Id == "Diodoros");
        }

        [Fact]
        public void ABadCopySpreadsTheValveSeatsWithoutYou()
        {
            var sim = UntilLife("pollio-copy", out _);
            var cap = sim.World.Capabilities.First(c => c.Id == "valveseats");
            Assert.Equal(CapabilityLevel.Reproducible, cap.Level);              // the bad copy never raises true maturity
            Assert.Equal(CapabilitySpread.Copied, cap.Spread);
            Assert.True(cap.Distorted);
            Assert.True(cap.Misattributed);
            var spread = sim.Log.Events.Last(e => e.Type == "capability.spread" && e.Target == "capability.valveseats");
            Assert.Equal(new[] { "Cassianus" }, spread.Actors);
            Assert.Contains(sim.World.LifeEventLog["pollio-copy"], spread.ImmediateCauses);
        }

        [Fact]
        public void LivesAreDeterministic()
        {
            var a = AfterThePump(7); var b = AfterThePump(7);
            for (int m = 0; m < 60; m++) { a.EndMonth(); b.EndMonth(); }
            Assert.Equal(a.Log.Hash(), b.Log.Hash());
            Assert.Equal(a.World.LifeEventLog.Keys.OrderBy(k => k), b.World.LifeEventLog.Keys.OrderBy(k => k));
        }

        [Fact]
        public void ThePeopleCommandShowsWhoYouKnow()
        {
            var sim = AfterThePump(42);
            string output = ConsoleTests.Play(sim, "people");
            Assert.Contains("Felix, a freedman", output);
            Assert.Contains("Cassianus", output);
            Assert.DoesNotContain("Diodoros", output);
        }
    }
}
