using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>The pestilence's historical recurrences, AD 189 and the Plague of Cyprian (decided 2026-09-28, SYSTEMS §6).</summary>
    public class RecurrenceTests
    {
        private static Simulation Departed(ulong seed, System.Action<Simulation>? before = null)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("fountain");
            while (sim.Now.Year < 172)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            before?.Invoke(sim);
            return sim;
        }

        [Fact]
        public void TheRecurrencesAreStepsInHistory()
        {
            var sim = new Simulation(TestData.Load(), 81);
            foreach (int year in new[] { 189, 251 })
                foreach (var d in DomainInfo.All)
                    Assert.True(sim.Benchmark(d, year + 1) < sim.Benchmark(d, year + 0.75));
        }

        [Fact]
        public void TheyStrikeOnTheirDatesWhileYouAreAway()
        {
            var sim = Departed(82);
            var a1 = sim.JumpForTests();
            var a2 = sim.JumpForTests();
            int last = a2.ArrivalYear;
            Assert.Equal(last >= 189, sim.Log.Events.Any(e => e.Type == "crisis.recurrence" && e.Time.Year == 189));
            Assert.Equal(last >= 251, sim.Log.Events.Any(e => e.Type == "crisis.recurrence" && e.Time.Year == 251));
            Assert.True(sim.Log.Events.Count(e => e.Type == "crisis.recurrence") <= 2);
        }

        [Fact]
        public void BetterPreparedRomeTakesLessThanHistory()
        {
            var sim = new Simulation(TestData.Load(), 83);
            double bare = sim.RecurrenceRatio(189, "none");
            sim.World.PlagueResilienceBonus += 0.2;
            Assert.True(sim.RecurrenceRatio(189, "none") < bare);
            sim.World[Domain.Medicine].Debt = 200;
            Assert.True(sim.RecurrenceRatio(189, "none") > 0);
        }
    }
}
