using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The P1 scene router in play (Corey, locked): optional scenes from every system go through one monthly router; no
    /// third scene of a category in a row while another could happen, unless the player is focused on it; world
    /// interruptions are never held back.
    /// </summary>
    [Collection("Console")]
    public class P1RoutingTests
    {
        /// <summary>Month 6: Cassianus's flooded cellar (work) and the talk at the baths (Roman life) both waiting, after two work scenes.</summary>
        private static Simulation TwoCandidates()
        {
            var sim = new Simulation(TestData.Load(), 42);
            for (int m = 0; m < 5; m++) sim.EndMonth();
            sim.FindCommission("cellarpump")!.Status = CommissionStatus.NotYet;   // still to come
            sim.World.ScenesSeen.Clear();
            var ids = sim.SceneCandidateIds();
            Assert.Contains("commission:cellarpump", ids);
            Assert.Contains("scene:baths-wet-cellar", ids);
            sim.World.ScenePacing.Record(SceneCategory.WorkEconomy);
            sim.World.ScenePacing.Record(SceneCategory.WorkEconomy);
            return sim;
        }

        [Fact]
        public void NoThirdWorkSceneWhileSomethingElseCanHappen()
        {
            var sim = TwoCandidates();
            for (int k = 0; k < 40; k++) Assert.NotEqual("commission:cellarpump", sim.PeekRoutedScene());   // never a third work scene
        }

        [Fact]
        public void ExplicitFocusLetsTheThirdOneThrough()
        {
            var sim = TwoCandidates();
            Assert.True(sim.SetSceneFocus("workeconomy").Ok);
            var picks = Enumerable.Range(0, 40).Select(_ => sim.PeekRoutedScene()).ToList();
            Assert.Contains("commission:cellarpump", picks);
            Assert.Contains("scene:baths-wet-cellar", picks);
        }

        [Fact]
        public void WhenNothingElseCanHappenTheThirdOneMayStillCome()
        {
            var sim = TwoCandidates();
            sim.World.ScenesSeen.AddRange(sim.Data.Content.Scenes.Select(s => s.Id).Where(id => id != "serenus-meet"));   // nothing but the commission left
            foreach (var cm in sim.World.Commissions.Where(cm => cm.Id != "cellarpump")) cm.Status = CommissionStatus.Declined;
            Assert.Equal(new[] { "commission:cellarpump" }, sim.SceneCandidateIds());
            Assert.Equal("commission:cellarpump", sim.PeekRoutedScene());
        }

        [Fact]
        public void TheRouterFiresOneOptionalSceneAMonthAndTheRestWait()
        {
            var sim = TwoCandidates();
            int picks = sim.World.RoutedScenes.Count;
            sim.EndMonth();
            Assert.Equal(picks + 1, sim.World.RoutedScenes.Count);                            // one optional scene a month
            Assert.NotEqual(SceneCategory.WorkEconomy, sim.World.RoutedScenes.Last().Category); // of another kind
            Assert.Equal(CommissionStatus.NotYet, sim.FindCommission("cellarpump")!.Status);  // the work waits
            for (int m = 0; m < 6 && sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet; m++) sim.EndMonth();
            Assert.Equal(CommissionStatus.Offered, sim.FindCommission("cellarpump")!.Status); // then comes
        }

        [Fact]
        public void AWorldInterruptionIsNeverHeldBack()
        {
            // Felix's fever is an interruption: it happens in the month its chance comes up, even after two Personal scenes.
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.World.AccessTo("guild").RecordMemberRelationship("Felix");
                for (int m = 0; m < 120; m++)
                {
                    sim.World.ScenePacing.Record(SceneCategory.Personal);
                    sim.World.ScenePacing.Record(SceneCategory.Personal);
                    sim.EndMonth();
                    Assert.DoesNotContain("felix-fever", sim.World.ReadyLife);             // never queued behind the router
                    if (sim.World.LifeEventLog.ContainsKey("felix-fever")) return;
                }
            }
            Assert.Fail("Felix's fever never came in 40 seeds");
        }

        [Fact]
        public void RoutingIsDeterministic()
        {
            var a = new Simulation(TestData.Load(), 9); var b = new Simulation(TestData.Load(), 9);
            a.ChooseSeeded("workshop"); b.ChooseSeeded("workshop");
            for (int m = 0; m < 48; m++) { a.EndMonth(); b.EndMonth(); }
            Assert.Equal(a.Log.Hash(), b.Log.Hash());
            Assert.Equal(a.World.ScenesSeen, b.World.ScenesSeen);
        }

        [Fact]
        public void TheConsoleTakesAFocus()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.Contains("You keep your mind on RomanLife", ConsoleTests.Play(sim, "focus romanlife"));
            Assert.Equal(SceneCategory.RomanLife, sim.World.SceneFocus);
            ConsoleTests.Play(sim, "focus off");
            Assert.Null(sim.World.SceneFocus);
        }
    }
}
