using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 polish pass (2026-10-04): the router remembers how long each progression candidate has waited. An optional age
    /// bonus (scenes.progressionAgePerMonth, PROPOSED P1-23) is off by default, so routing is unchanged until Corey decides.
    /// </summary>
    [Collection("Console")]
    public class P1PacingAgeTests
    {
        private static Simulation Play(GameData data, int months)
        {
            var sim = new Simulation(data, 7);
            sim.ChooseSeeded("fountain");
            for (int m = 0; m < months; m++) sim.EndMonth();
            return sim;
        }

        [Fact]
        public void TheAgeBonusIsOffByDefault()
        {
            Assert.Equal(0, TestData.Load().Tuning.Get("scenes.progressionAgePerMonth"));
        }

        [Fact]
        public void WaitingProgressionCandidatesAreRememberedUntilPicked()
        {
            var sim = Play(TestData.Load(), 30);
            var waiting = sim.SceneCandidateIds().Where(id => !id.StartsWith("scene:")).ToList();
            Assert.All(sim.World.CandidateSince.Keys, k => Assert.False(k.StartsWith("scene:")));    // texture never ages
            Assert.All(sim.World.CandidateSince.Values, since => Assert.True(since <= sim.Turn));
            Assert.DoesNotContain(sim.World.CandidateSince.Keys, k => sim.World.RoutedScenes.Any(r => r.Id == k && r.Turn >= sim.World.CandidateSince[k]));
        }

        [Fact]
        public void WithTheBonusOnLongWaitsGetShorter()
        {
            // Counterfactual, in memory only: the same seeds, with and without a 25%-a-month bonus.
            int Longest(GameData data)
            {
                int worst = 0;
                for (ulong seed = 1; seed <= 6; seed++)
                {
                    var sim = new Simulation(data, seed);
                    sim.ChooseSeeded(seed % 2 == 1 ? "workshop" : "fountain");
                    var since = new Dictionary<string, int>();
                    for (int m = 0; m < 96; m++)
                    {
                        foreach (var id in sim.SceneCandidateIds().Where(i => !i.StartsWith("scene:"))) if (!since.ContainsKey(id)) since[id] = sim.Turn;
                        int before = sim.World.RoutedScenes.Count;
                        sim.EndMonth();
                        foreach (var r in sim.World.RoutedScenes.Skip(before))
                            if (since.TryGetValue(r.Id, out int s)) { worst = System.Math.Max(worst, r.Turn - s); since.Remove(r.Id); }
                    }
                }
                return worst;
            }
            var off = TestData.Load();
            var on = off.WithTuning(new Dictionary<string, double> { { "scenes.progressionAgePerMonth", 0.25 } });
            int longestOff = Longest(off), longestOn = Longest(on);
            Assert.True(longestOff > 3, "baseline longest wait " + longestOff);   // there is something to shorten
            Assert.True(longestOn <= longestOff, longestOn + " vs " + longestOff);
        }
    }
}
