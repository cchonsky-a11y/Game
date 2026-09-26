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
            Assert.True(sim.StartProject("physician").Ok); // 2 per turn
            Assert.False(sim.Oversee("circle").Ok);          // not founded yet
            Assert.True(sim.Work().Ok);                     // 1
            Assert.Equal(1, sim.World.Attention);
            Assert.False(sim.Found("circle").Ok);           // needs 2
            Assert.False(sim.StartProject("market").Ok);
            sim.EndTurn();
            Assert.Equal(4 - sim.Data.Content.Project("physician")!.AttentionPerTurn, sim.World.Attention); // still working on it
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
            var def = sim.Data.Content.Project("warehouses")!;
            Assert.True(sim.StartProject("warehouses").Ok);
            for (int i = 1; i < def.Turns; i++)
            {
                sim.EndTurn();
                Assert.Equal(4 - def.AttentionPerTurn, sim.World.Attention);
            }
            sim.EndTurn();
            Assert.Equal(4, sim.World.Attention);
        }

        [Fact]
        public void MentoringCommitmentPaysOffAfterItsTurns()
        {
            var sim = new Simulation(TestData.Load(), 21);
            sim.World.Gold = 1000;
            sim.Found("circle");
            sim.EndTurn();
            var c = sim.World.Institution("circle");
            Assert.True(sim.Mentor("circle").Ok);
            int turns = sim.T.GetInt("commitments.mentor.turns");
            int perTurn = sim.T.GetInt("commitments.mentor.attentionPerTurn");
            for (int i = 1; i < turns; i++)
            {
                sim.EndTurn();
                Assert.Equal(4 - perTurn, sim.World.Attention);
            }
            sim.EndTurn();
            Assert.Contains(sim.Log.Events, e => e.Type == "commitment.complete");
        }

        [Fact]
        public void AttentionDemandIsAtLeastOneAndAHalfTimesSupply()
        {
            var data = TestData.Load();
            Assert.Equal(80, AttentionBudget.Supply(data));
            Assert.Equal(122, AttentionBudget.Demand(data));
            Assert.True(AttentionBudget.Demand(data) >= data.Tuning.Get("attention.demandTarget") * AttentionBudget.Supply(data));
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
