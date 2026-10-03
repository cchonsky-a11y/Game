using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>The P1 state lives in the world (decided 2026-10-02): scene pacing, the ledger, projects and institution access.</summary>
    public class P1WorldTests
    {
        [Fact]
        public void TheWorldStartsWithPacingFromTuningAndAnAccessRecordPerInstitution()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.Equal(sim.T.GetInt("scenes.consecutiveSoftCap"), sim.World.ScenePacing.ConsecutiveSceneSoftCap);
            Assert.Equal(sim.World.Institutions.Select(i => i.Key), sim.World.Access.Select(a => a.InstitutionId));
            Assert.All(sim.World.Access, a => Assert.Equal(InstitutionAccessStage.Unaware, a.Stage));
            Assert.Empty(sim.World.Projects);
            Assert.Equal(sim.T.Get("scenes.repeatedCategoryWeight"), sim.Scenes.RepeatedCategoryWeightMultiplier);
        }

        [Fact]
        public void TheLedgerAccountsForEveryDenariusOfThePlayersMoney()
        {
            // A clean ledger (P1): every change to the player's money is an entry with a reason, and they add up.
            var sim = SnapshotTests.ReferencePlaythroughBeforeJump();
            double start = sim.Log.Events.SelectMany(e => e.Effects).FirstOrDefault(fx => fx.Key == "gold")?.Before ?? 0;
            Assert.True(sim.World.Ledger.Entries.Count > 50);
            Assert.Equal(sim.World.Gold - start, sim.World.Ledger.Net, 6);
            Assert.All(sim.World.Ledger.Entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Reason)));
            Assert.Contains(sim.World.Ledger.Entries, e => e.Kind == LedgerEntryKind.Payment);
            Assert.Contains(sim.World.Ledger.Entries, e => e.Kind == LedgerEntryKind.Expense);
            Assert.Contains(sim.World.Ledger.Entries, e => e.Kind == LedgerEntryKind.InstitutionDues);
        }

        [Fact]
        public void TheSceneRouterUsesTheGamesSeededGenerator()
        {
            string Pick(ulong seed)
            {
                var sim = new Simulation(TestData.Load(), seed);
                var candidates = System.Enum.GetValues(typeof(SceneCategory)).Cast<SceneCategory>()
                    .Select(c => new SceneCandidate(c.ToString(), c)).ToList();
                return string.Join(",", Enumerable.Range(0, 12).Select(_ => sim.Scenes.Choose(candidates, sim.World.ScenePacing)!.Id));
            }
            Assert.Equal(Pick(7), Pick(7));          // deterministic
            Assert.NotEqual(Pick(7), Pick(8));
        }
    }
}
