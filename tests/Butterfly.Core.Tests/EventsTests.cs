using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Decision events and leaders' requests (decided 2026-09-28, P0-33 and P0-32).</summary>
    public class EventsTests
    {
        private static Simulation Until(int year, int month, ulong seed = 51)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (sim.Now.TotalMonths + sim.MonthsPerTurn < SimTime.FromYear(year, month - 1).TotalMonths)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            return sim;
        }

        [Fact]
        public void TheFloodComesOnItsDateAndTheChoiceHasCosts()
        {
            var sim = Until(161, 10);
            if (sim.PendingEvent?.Id != "flood") sim.EndTurn();
            Assert.Equal("flood", sim.PendingEvent!.Id);
            Assert.Contains(sim.PendingDecisions(), r => r.Contains("tiber"));
            sim.World.Gold = 1000;
            double gold = sim.World.Gold, gov = sim.World[Domain.Governance].Level;
            Assert.True(sim.Decide("relief").Ok);
            Assert.True(sim.World.Gold < gold);
            Assert.True(sim.World[Domain.Governance].Level > gov);
            Assert.Null(sim.PendingEvent);
            Assert.False(sim.Decide("relief").Ok);
        }

        [Fact]
        public void YouCantPayForWhatYouCantAfford()
        {
            var sim = Until(161, 10);
            if (sim.PendingEvent?.Id != "flood") sim.EndTurn();
            sim.World.Gold = 0;
            Assert.False(sim.Decide("relief").Ok);
            Assert.True(sim.Decide("profit").Ok);
            Assert.True(sim.World.Gold > 0);
        }

        [Fact]
        public void AnUnansweredChoicePassesYouBy()
        {
            var sim = Until(161, 10);
            if (sim.PendingEvent?.Id != "flood") sim.EndTurn();
            for (int t = 0; t < sim.T.GetInt("events.lapseTurns"); t++) sim.EndTurn();
            Assert.Contains(sim.Log.Events, e => e.Type == "event.decide" && e.Target == "flood" && e.Text.Contains("let it pass"));
        }

        [Fact]
        public void LeadersOnlyAskTheirMembers()
        {
            var sim = Until(158, 3);
            sim.EndTurn();
            Assert.NotEqual("praetor", sim.PendingEvent?.Id);          // not a member of the faction
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "event.offer" && e.Target == "praetor");
        }

        [Fact]
        public void ARequestMovesTheCamps()
        {
            var sim = Until(164, 5, 52);
            var guild = sim.World.Institution("guild");
            guild.Stake = 0.05; guild.Rank = Simulation.Member; guild.Loyalty = 60;
            while (sim.PendingEvent?.Id != "cartel" && sim.Now.Year < 165) { if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id); sim.EndTurn(); }
            Assert.Equal("cartel", sim.PendingEvent!.Id);
            double lean = guild.Lean;
            Assert.True(sim.Decide("join").Ok);
            Assert.True(guild.Lean < lean);                            // toward the cartel
        }

        [Fact]
        public void EveryEventIsWellFormed()
        {
            var events = TestData.Load().Content.Events;
            Assert.True(events.Count >= 10);
            foreach (var e in events)
            {
                Assert.True(e.Options.Count >= 2);
                Assert.InRange(e.Year, 155, 175);
            }
        }
    }
}
