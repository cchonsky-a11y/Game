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
    
        private static Simulation AtFlood()
        {
            var sim = Until(161, 10);
            if (sim.PendingEvent?.Id != "flood") sim.EndTurn();
            Assert.Equal("flood", sim.PendingEvent!.Id);
            return sim;
        }

        private static string Personal(Arrival a) => a.Beats.First(b => b.Name == "Personal echo").Text;

        [Fact]
        public void RomeRemembersYourAnswerAtArrival()
        {
            var sim = AtFlood();
            sim.World.Gold = 1000;
            Assert.True(sim.Decide("relief").Ok);
            var first = Personal(sim.JumpForTests());
            Assert.Contains("hospes", first);
            var second = Personal(sim.JumpForTests());
            Assert.Contains("walked on the water", second);           // the same choice, aged
            Assert.DoesNotContain("hospes", second);
        }

        [Fact]
        public void AChoiceYouLetPassLeavesNoMark()
        {
            var sim = AtFlood();
            for (int t = 0; t < sim.T.GetInt("events.lapseTurns"); t++) sim.EndTurn();
            var personal = Personal(sim.JumpForTests());
            Assert.DoesNotContain("hospes", personal);
            Assert.DoesNotContain("drying his grain", personal);
        }

        [Fact]
        public void AMarkInAnInstitutionShowsItsPresentNameOrThatItIsGone()
        {
            foreach (bool gone in new[] { false, true })
            {
                var sim = Until(160, 9, 53);
                var sanctuary = sim.World.Institution("sanctuary");
                sanctuary.Stake = 0.05; sanctuary.Rank = Simulation.Member; sanctuary.Loyalty = 60;
                while (sim.PendingEvent?.Id != "sanctuaryWing" && sim.Now.Year < 161) { if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id); sim.EndTurn(); }
                Assert.Equal("sanctuaryWing", sim.PendingEvent!.Id);
                sim.World.Gold = 1000;
                Assert.True(sim.Decide("clinic").Ok);
                if (gone) sanctuary.Collapsed = true;
                var personal = Personal(sim.JumpForTests());
                Assert.Contains(gone ? "stands empty" : "twenty beds", personal);
                Assert.DoesNotContain("{", personal);
            }
        }

        [Fact]
        public void AtMostTheSetNumberOfMarksPerArrival()
        {
            var sim = new Simulation(TestData.Load(), 54);
            sim.ChooseSeeded("workshop");
            sim.World.Institution("sanctuary").Stake = 0.05;
            sim.World.Institution("sanctuary").Rank = Simulation.Member;
            while (sim.Now.Year < 173)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                sim.World.Gold = 5000;
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.First().Id);
                sim.EndTurn();
            }
            var marks = sim.Data.Content.Events.SelectMany(e => e.Options).Where(o => o.Mark != null).Select(o => o.Mark!.Substring(0, 20)).ToList();
            var personal = Personal(sim.JumpForTests());
            int shown = marks.Count(m => personal.Contains(m));
            Assert.InRange(shown, 1, sim.T.GetInt("events.marksPerArrival"));
        }
}
}
