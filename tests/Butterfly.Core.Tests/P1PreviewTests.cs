using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 correctness pass (2026-10-04): a read-only view or preview never changes the game. The scene preview used to run the
    /// router on the game's own generator, so looking ahead moved every later draw.
    /// </summary>
    [Collection("Console")]
    public class P1PreviewTests
    {
        private static Simulation Playing(ulong seed)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded(seed % 2 == 0 ? "fountain" : "workshop");
            for (int m = 0; m < 6; m++) sim.EndMonth();
            return sim;
        }

        private static void Look(Simulation sim)
        {
            for (int k = 0; k < 25; k++) sim.PeekRoutedScene();
            sim.SceneCandidateIds();
            foreach (var section in System.Enum.GetValues(typeof(MenuSection)).Cast<MenuSection>()) sim.ViewOf(section);
        }

        [Fact]
        public void PreviewDoesNotMoveTheGenerator()
        {
            var sim = Playing(42);
            ulong next = sim.Rng.Clone().NextULong();
            int events = sim.Log.Events.Count;
            Look(sim);
            Assert.Equal(next, sim.Rng.Clone().NextULong());
            Assert.Equal(events, sim.Log.Events.Count);
        }

        [Fact]
        public void RepeatedPreviewsAgree()
        {
            var sim = Playing(7);
            var first = sim.PeekRoutedScene();
            Assert.NotNull(first);
            for (int k = 0; k < 20; k++) Assert.Equal(first, sim.PeekRoutedScene());
        }

        [Theory]
        [InlineData(3ul)]
        [InlineData(42ul)]
        [InlineData(1001ul)]
        public void LookingAheadNeverChangesWhatHappens(ulong seed)
        {
            var looked = Playing(seed);
            var plain = Playing(seed);
            for (int m = 0; m < 30; m++)
            {
                Look(looked);
                looked.EndMonth();
                plain.EndMonth();
                Assert.Equal(plain.World.RoutedScenes.Last(), looked.World.RoutedScenes.Last());
            }
            Assert.Equal(plain.Log.Hash(), looked.Log.Hash());
        }
    }
}
