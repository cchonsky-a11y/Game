using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 Grand Challenges (decided 2026-10-02): the pump raises the question of shared measures; four chunky stages move
    /// Rome's measures and gauges; people, capabilities and resources can hold a stage up (PROPOSED P1-10).
    /// </summary>
    [Collection("Console")]
    public class P1ChallengeTests
    {
        private static Simulation AfterThePump(ulong seed = 42)
        {
            var sim = P1Play.AfterThePump(seed);
            // The question comes through the scene router, one optional scene a month: wait for it.
            for (int m = 0; m < 24 && sim.FindChallenge("standards")!.Status == ChallengeStatus.NotYet; m++) sim.EndMonth();
            sim.PersonOf("Felix")!.AwayUntilTurn = 0;                          // these tests aren't about Felix's fever
            sim.World.Gold = 5000;
            return sim;
        }

        private static void FollowFelix(Simulation sim, InstitutionAccessStage until) => P1Play.FollowFelix(sim, until);

        private static void RunStage(Simulation sim)
        {
            var r = sim.StartChallengeStage("standards");
            Assert.True(r.Ok, r.Message);
            while (sim.FindChallenge("standards")!.Status == ChallengeStatus.Working) sim.EndMonth();
        }

        [Fact]
        public void ThePumpRaisesTheQuestion()
        {
            var fresh = new Simulation(TestData.Load(), 42);
            Assert.Equal(ChallengeStatus.NotYet, fresh.FindChallenge("standards")!.Status);
            var sim = AfterThePump();
            Assert.Equal(ChallengeStatus.Open, sim.FindChallenge("standards")!.Status);
            Assert.Contains(sim.Log.Events, e => e.Type == "challenge.open" && e.Text.Contains("without seeing the original"));
        }

        [Fact]
        public void AStrangerPaysTwiceForTheTin()
        {
            var sim = AfterThePump();
            var master = sim.NextStage(sim.FindChallenge("standards")!)!;
            Assert.False(sim.HasSource(master));
            double stranger = sim.StageGold(master);
            Assert.Contains("stranger's price", sim.StageLine(master));
            FollowFelix(sim, InstitutionAccessStage.Guest);
            Assert.True(sim.HasSource(master));
            Assert.Equal(sim.Priced(master.Gold), sim.StageGold(master), 6);   // the usual price (prices drift while you wait)
            Assert.True(stranger > sim.StageGold(master) * 1.9);
        }

        [Fact]
        public void TheSharedFootWaitsForFelix()
        {
            var sim = AfterThePump();
            RunStage(sim);
            var shared = sim.NextStage(sim.FindChallenge("standards")!)!;
            Assert.Equal("shared", shared.Id);
            Assert.Null(sim.StageBlocker(shared));
            sim.PersonOf("Felix")!.AwayUntilTurn = sim.Turn + 3;
            Assert.Contains("Felix is laid up", sim.StageBlocker(shared));
            Assert.False(sim.StartChallengeStage("standards").Ok);
        }

        [Fact]
        public void KnowingIsNotMakingTheGaugesBeforeTheMeasures()
        {
            var sim = AfterThePump();
            var c = sim.FindChallenge("standards")!;
            c.StageIndex = 2;   // jump ahead to the gauges without shared measures
            Assert.Contains("shared measures", sim.StageBlocker(sim.NextStage(c)!));
        }

        [Fact]
        public void FourStagesMakePartsThatFitAcrossShops()
        {
            var sim = AfterThePump();
            FollowFelix(sim, InstitutionAccessStage.Member);
            sim.World.Gold = 5000;
            int ledgerBefore = sim.World.Ledger.Entries.Count(e => e.Kind == LedgerEntryKind.Materials);
            var r = sim.StartChallengeStage("standards");
            Assert.True(r.Ok, r.Message);
            sim.EndMonth();
            Assert.Contains(sim.ReservedAttentionParts(), p => p.What == "A master foot in hard bronze");   // Attention held each month
            while (sim.FindChallenge("standards")!.Status == ChallengeStatus.Working) sim.EndMonth();
            for (int i = 0; i < 3; i++)
            {
                while (sim.IsPersonAway("Felix")) sim.EndMonth();
                RunStage(sim);
            }
            Assert.Equal(ChallengeStatus.Done, sim.FindChallenge("standards")!.Status);
            Assert.True(sim.CapabilityLevelOf("gauges") >= CapabilityLevel.Reproducible);
            Assert.True(sim.CapabilityLevelOf("metrology") >= CapabilityLevel.Reproducible);
            Assert.Equal(ledgerBefore + 4, sim.World.Ledger.Entries.Count(e => e.Kind == LedgerEntryKind.Materials));
            Assert.Contains(sim.Log.Events, e => e.Type == "challenge.complete");
            // The jump then finds the measures carried on by the guild.
            var arrival = sim.JumpForTests();
            Assert.Contains("technical:gauges", arrival.P1Echoes);
        }

        [Fact]
        public void TheConsoleShowsTheChallenge()
        {
            var sim = AfterThePump();
            string output = ConsoleTests.Play(sim, "challenge", "challenge begin standards", "challenge");
            Assert.Contains("Measurement and standards", output);
            Assert.Contains("Next: A master foot in hard bronze", output);
            Assert.Contains("Under way: A master foot in hard bronze", output);
        }
    }
}
