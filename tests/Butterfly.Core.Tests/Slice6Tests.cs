using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class AttentionTests
    {
        [Fact]
        public void FourAttentionPerTurnConsumedByDecisions()
        {
            var sim = new Simulation(TestData.Load(), 21);
            sim.World.Gold = 1000;
            Assert.Equal(4, sim.World.Attention);
            Assert.True(sim.StartProject("physician").Ok);
            Assert.True(sim.Found("circle").Ok);
            Assert.True(sim.Oversee("circle").Ok);
            Assert.True(sim.Work().Ok);
            Assert.Equal(0, sim.World.Attention);
            Assert.False(sim.StartProject("market").Ok);
            sim.EndTurn();
            Assert.Equal(4, sim.World.Attention);
        }

        [Fact]
        public void OnlyOnePersonalActionPerTurn()
        {
            var sim = new Simulation(TestData.Load(), 21);
            Assert.True(sim.Work().Ok);
            Assert.False(sim.Work().Ok);
        }

        [Fact]
        public void MultiTurnWorkReservesAttentionInLaterTurns()
        {
            var sim = new Simulation(TestData.Load(), 21);
            sim.World.Gold = 1000;
            Assert.True(sim.StartProject("warehouses").Ok); // 3 turns, 1 Attention each
            sim.EndTurn();
            Assert.Equal(3, sim.World.Attention);
            sim.EndTurn();
            Assert.Equal(3, sim.World.Attention);
            sim.EndTurn();
            Assert.Equal(4, sim.World.Attention);
        }

        [Fact]
        public void MentoringCommitmentPaysOffAfterItsTurns()
        {
            var sim = new Simulation(TestData.Load(), 21);
            sim.World.Gold = 1000;
            sim.Found("circle");
            var c = sim.World.Institution("circle");
            Assert.True(sim.Mentor("circle").Ok);
            double strength = c.Strength;
            sim.EndTurn();
            Assert.Equal(3, sim.World.Attention);
            sim.EndTurn();
            sim.EndTurn();
            Assert.True(c.Strength >= strength + sim.T.Get("commitments.mentor.strength") - sim.T.Get("institutions.witherPerYear"));
            Assert.Contains(sim.Log.Events, e => e.Type == "commitment.complete");
        }
    }

    public class SeededChoiceTests
    {
        private static void RunTo(Simulation sim, int year)
        {
            while (sim.Now.Year < year) sim.EndTurn();
        }

        [Fact]
        public void StartingGoldAffordsOnlyOne()
        {
            var sim = new Simulation(TestData.Load(), 31);
            Assert.True(sim.ChooseSeeded("workshop").Ok);
            Assert.False(sim.ChooseSeeded("fountain").Ok);
            Assert.True(sim.World.Gold < sim.Data.Content.Project("fountain")!.Gold);
        }

        [Fact]
        public void ChoosingTheWorkshopBringsFeverLinkedToTheChoice()
        {
            var sim = new Simulation(TestData.Load(), 31);
            sim.ChooseSeeded("workshop");
            RunTo(sim, 158);
            var fever = sim.Log.Events.Single(e => e.Type == "seeded.payoff");
            var choice = sim.Log.Events.Single(e => e.Type == "seeded.choice");
            Assert.Contains(choice.Id, fever.ImmediateCauses);
            Assert.Contains("fountain", fever.Text);
        }

        [Fact]
        public void ChoosingTheFountainLosesTheSmith()
        {
            var sim = new Simulation(TestData.Load(), 31);
            sim.ChooseSeeded("fountain");
            RunTo(sim, 158);
            var payoff = sim.Log.Events.Single(e => e.Type == "seeded.payoff");
            Assert.Contains("smith", payoff.Text);
        }

        [Fact]
        public void TheChoiceLapsesIfIgnored()
        {
            var sim = new Simulation(TestData.Load(), 31);
            sim.EndTurn();
            sim.EndTurn();
            Assert.Equal("neither", sim.World.SeededChoice);
        }
    }

    public class PromiseTests
    {
        private static Simulation ToFirstWarning(ulong seed)
        {
            var sim = new Simulation(TestData.Load(), seed);
            while (sim.World.Promise.Status == PromiseStatus.NotOffered) sim.EndTurn();
            return sim;
        }

        [Fact]
        public void LeaderOffersPromiseWithFirstWarning()
        {
            var sim = ToFirstWarning(41);
            Assert.Equal(1, sim.World.Plague.Stage);
            var offer = sim.Log.Events.Single(e => e.Type == "promise.offer");
            var warning = sim.Log.Events.First(e => e.Type == "plague.warning");
            Assert.Contains(warning.Id, offer.ImmediateCauses);
        }

        [Fact]
        public void AcceptedPromiseConflictsWithLeavingBeforeThePlaguePasses()
        {
            var sim = ToFirstWarning(41);
            Assert.True(sim.AnswerPromise(true).Ok);
            Assert.True(sim.LeavingBreaksPromise);
            while (sim.World.Plague.Stage != PlagueState.Passed) sim.EndTurn();
            Assert.False(sim.LeavingBreaksPromise);
            Assert.Equal(PromiseStatus.Kept, sim.World.Promise.Status);
            Assert.True(sim.PromiseKept());
        }

        [Fact]
        public void RefusingIsFinal()
        {
            var sim = ToFirstWarning(41);
            Assert.True(sim.AnswerPromise(false).Ok);
            Assert.False(sim.AnswerPromise(true).Ok);
            Assert.False(sim.LeavingBreaksPromise);
        }
    }
}
