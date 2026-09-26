using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class JumpTests
    {
        private static Simulation Play(ulong seed, string choice, bool foundCircle, bool acceptPromise, int leaveYear)
        {
            var sim = new Simulation(TestData.Load(), seed);
            if (choice != "neither") sim.ChooseSeeded(choice);
            while (sim.Now.Year < leaveYear)
            {
                if (foundCircle && sim.World.Institution("circle").Stake <= 0) sim.GrantStake("circle", 0.5);
                if (sim.World.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(acceptPromise);
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague(sim.AvailablePlagueResponses().First());
                sim.Work("craft"); // no passive income before institutions: the inventor works
                sim.EndTurn();
            }
            return sim;
        }

        [Fact]
        public void JumpSimulates250YearsInDecadeSteps()
        {
            var sim = Play(1, "fountain", true, true, 160);
            int depart = sim.Now.Year;
            var arrival = sim.Jump();
            Assert.Equal(depart + 250, arrival.ArrivalYear);
            Assert.Equal(25, sim.Log.Events.Count(e => e.Type == "jump.decade"));
            Assert.True(sim.Arrived);
            Assert.Throws<System.InvalidOperationException>(() => sim.EndTurn());
        }

        [Theory]
        [InlineData("fountain", true, true, 160)]
        [InlineData("workshop", false, false, 158)]
        [InlineData("neither", true, true, 170)]
        [InlineData("workshop", true, false, 170)]
        public void ArrivalHasFourBeatsAndShowsAllThreeEchoes(string choice, bool circle, bool promise, int leave)
        {
            var arrival = Play(7, choice, circle, promise, leave).Jump();
            Assert.Equal(new[] { "Recognition", "Wrongness", "Personal echo", "Discovery" }, arrival.Beats.Select(b => b.Name));
            Assert.Equal(3, arrival.Echoes.Count);
            Assert.Contains(arrival.Echoes, e => e.Id == "seeded");
            foreach (var echo in arrival.Echoes)
            {
                Assert.False(string.IsNullOrEmpty(echo.AtArrival));
                Assert.Contains(arrival.Beats, b => b.Name == echo.Beat);
            }
            Assert.All(arrival.Beats, b => Assert.DoesNotContain("{", b.Text));
        }

        [Fact]
        public void LeavingBeforeThePlaguePassesBreaksTheAcceptedPromise()
        {
            var sim = Play(3, "fountain", true, true, 163);
            Assert.True(sim.LeavingBreaksPromise);
            double loyalty = sim.World.Institution("circle").Loyalty;
            var arrival = sim.Jump();
            Assert.Equal(PromiseStatus.Broken, sim.World.Promise.Status);
            Assert.Contains(sim.Log.Events, e => e.Type == "promise.broken");
            Assert.Equal("broken", arrival.Echoes.First(e => e.Id == "promise").AtArrival.Replace("NoKeeper", ""));
        }

        [Fact]
        public void PlagueStillStrikesWhileTheInventorIsAway()
        {
            var sim = Play(3, "workshop", false, false, 160);
            var arrival = sim.Jump();
            Assert.True(sim.World.Plague.StruckInAbsence);
            Assert.Contains(arrival.Crises, c => c.Contains("Antonine"));
        }

        [Fact]
        public void LearnMoreShowsIndexAndInstitutionsWithoutCausalChains()
        {
            var arrival = Play(5, "fountain", true, true, 169).Jump();
            string text = arrival.LearnMore();
            Assert.Contains("Index", text);
            Assert.Contains("Physicians' Circle", text);
            Assert.DoesNotContain("because", text);
        }

        [Fact]
        public void JumpIsDeterministic()
        {
            string Run() { var s = Play(9, "fountain", true, true, 166); s.Jump(); return s.Log.Hash(); }
            Assert.Equal(Run(), Run());
        }
    }
}

namespace Butterfly.Core.Tests
{
    public class DepartureBriefingTests
    {
        [Fact]
        public void BriefingNamesDebtsInstitutionsAndThePromise()
        {
            var sim = new Simulation(TestData.Load(), 3);
            sim.World.Gold = 500;
            sim.GrantStake("circle", 0.5);
            sim.World[Domain.Economy].Debt = 10;
            sim.World.Promise.Status = PromiseStatus.Active;
            var lines = System.Linq.Enumerable.ToList(sim.DepartureBriefing());
            Assert.Contains(lines, l => l.StartsWith("Economy debt 10"));
            Assert.Contains(lines, l => l.Contains("would be left bare"));
            Assert.Contains(lines, l => l.Contains("breaks that promise"));
        }
    }
}
