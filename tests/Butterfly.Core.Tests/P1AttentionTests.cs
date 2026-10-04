using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 Attention invariants (Corey, locked): 4 a month; multi-month work reserves future months; new work never silently
    /// overbooks a later month; spending the last Attention never ends the month; zero free Attention is legal; End Month
    /// is explicit; one numbered choice at a time.
    /// </summary>
    [Collection("Console")]
    public class P1AttentionTests
    {
        /// <summary>The pump commission with its terms on the table (three 1-month stages at 1 Attention each).</summary>
        private static Simulation TermsOnTheTable()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            return sim;
        }

        [Fact]
        public void FutureOverbookingIsRefusedWithTheReason()
        {
            var sim = TermsOnTheTable();
            // Two mentoring commitments hold 2 Attention each for months to come: next month is fully pledged.
            sim.World.Commitments.Add(new Commitment("mentor", "circle", 10, 0));
            sim.World.Commitments.Add(new Commitment("mentor", "sanctuary", 10, 0));
            Assert.Equal(sim.AttentionPerTurn, sim.ReservedInMonth(1));
            int attention = sim.World.Attention;
            var r = sim.AcceptCommission("cellarpump");
            Assert.False(r.Ok);
            Assert.Equal("That would reserve 1 Attention next month, but 4 of your 4 are already committed then.", r.Message);
            Assert.Equal(attention, sim.World.Attention);                       // nothing spent
            Assert.Equal(CommissionStatus.TermsOffered, sim.FindCommission("cellarpump")!.Status);
        }

        [Fact]
        public void WorkThatFitsIsAcceptedAndItsMonthsAreReserved()
        {
            var sim = TermsOnTheTable();
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            Assert.Equal(1, sim.ReservedInMonth(1));
            Assert.Equal(1, sim.ReservedInMonth(2));
            Assert.Equal(0, sim.ReservedInMonth(3));                            // three 1-month stages
            sim.EndMonth();
            Assert.Contains(sim.ReservedAttentionParts(), p => p.Attention == 1);
            Assert.Contains("1 reserved", ConsoleTests.Play(sim, "status"));     // shown in the header
        }

        [Fact]
        public void ALaterMonthIsCheckedNotOnlyTheNext()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Commitments.Add(new Commitment("mentor", "circle", 4, 0));    // months 1-3
            sim.World.Commitments.Add(new Commitment("mentor", "sanctuary", 6, 0)); // months 1-5
            var r = sim.CheckFutureAttention(new[] { 0, 0, 0, 0, 3 });
            Assert.NotNull(r);
            Assert.Contains("in 5 months' time, but 2 of your 4", r!.Message);
            Assert.Null(sim.CheckFutureAttention(new[] { 0, 0, 0, 2 }));            // month 4: 2 + 2 fits
        }

        [Fact]
        public void ZeroFreeAttentionIsLegalAndOnlyEndMonthMovesTime()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Commitments.Add(new Commitment("mentor", "circle", 10, 0));
            sim.World.Commitments.Add(new Commitment("mentor", "sanctuary", 10, 0));
            sim.EndMonth();
            Assert.Equal(0, sim.World.Attention);                                 // legal
            int turn = sim.Turn;
            ConsoleTests.Play(sim, "status", "news");
            Assert.Equal(turn, sim.Turn);                                         // nothing passes on its own
            ConsoleTests.Play(sim, "end");
            Assert.Equal(turn + 1, sim.Turn);
        }

        [Fact]
        public void SpendingTheLastAttentionDoesNotEndTheMonth()
        {
            var sim = new Simulation(TestData.Load(), 42);
            ConsoleTests.Play(sim, "work consult", "exchange 10 aurei");
            Assert.Equal(0, sim.World.Attention);
            Assert.Equal(1, sim.Turn);
        }

        [Fact]
        public void OnlyOneNumberedChoiceAtATime()
        {
            var sim = new Simulation(TestData.Load(), 42);
            string output = ConsoleTests.Play(sim, "menu on", "1 2", "3,4");
            Assert.Contains("One choice at a time", output);
            Assert.Null(sim.World.SeededChoice);                                   // neither number was acted on
            Assert.Equal(sim.AttentionPerTurn, sim.World.Attention);
            Assert.DoesNotContain("several", output);
        }
    }
}
