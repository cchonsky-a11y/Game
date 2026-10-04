using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Corey, 2026-10-04: true maturity and spread are separate; dependency edges may name the level they need; a carried
    /// capability climbs the full ladder; every arrival keeps human echoes, never repeated word for word.
    /// </summary>
    [Collection("Console")]
    public class P1MaturityTests
    {
        private static Simulation GuildMemberAfterThePump(ulong seed = 42)
        {
            var sim = P1Play.AfterThePump(seed);
            sim.World.Gold = 5000;
            P1Play.FollowFelix(sim, InstitutionAccessStage.Member);
            Assert.Equal(InstitutionAccessStage.Member, sim.World.AccessTo("guild").Stage);
            return sim;
        }

        [Fact]
        public void ABadCopySpreadsWithoutRaisingMaturityAndCorrectWorkRaisesIt()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.SpreadCapability("valveseats", CapabilitySpread.Widespread, true, true, null, "test", "Pollio");
            var cap = sim.World.Capabilities.First(c => c.Id == "valveseats");
            Assert.Equal(CapabilityLevel.None, cap.Level);
            Assert.Equal(CapabilitySpread.Widespread, cap.Spread);
            Assert.True(sim.AdvanceCapability("valveseats", CapabilityLevel.Reproducible, null, "test"));
            Assert.Equal(CapabilityLevel.Reproducible, cap.Level);
            Assert.True(cap.Distorted);                                          // the bad copies are still about
        }

        [Fact]
        public void EdgesCanNameTheLevelTheyNeed()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var steam = sim.CapabilityDefOf("steam")!;
            Assert.Equal(CapabilityLevel.Manufacturable, steam.Edges.First(e => e.Id == "steelgrades").Level);
            foreach (var id in new[] { "metrology", "records", "gauges", "drawings", "steelgrades", "lathework", "interchange" })
                Assert.True(sim.AdvanceCapability(id, CapabilityLevel.Reproducible, null, "test"), id);
            // Steel only reproducible: steam can't be built, and the reason names the level.
            Assert.Contains("graded tool steel (manufacturable)", sim.CapabilityBlocker("steam", CapabilityLevel.Prototype));
            Assert.Null(sim.CapabilityBlocker("steam", CapabilityLevel.Theory));      // theory needs no foundry
            Assert.True(sim.AdvanceCapability("steelgrades", CapabilityLevel.Manufacturable, null, "test"));
            Assert.Null(sim.CapabilityBlocker("steam", CapabilityLevel.Prototype));
            // Default edges keep the old rule: as far as the target, up to reproducible.
            Assert.Null(sim.CapabilityBlocker("lathework", CapabilityLevel.Institutionalized));
        }

        [Fact]
        public void EveryEdgeNamesAKnownCapabilityAndLevel()
        {
            var caps = TestData.Load().Content.Capabilities;
            foreach (var c in caps)
                foreach (var e in c.Edges)
                {
                    Assert.Contains(caps, x => x.Id == e.Id);
                    if (e.Level != null) Assert.InRange((int)e.Level, (int)CapabilityLevel.Theory, (int)CapabilityLevel.Institutionalized);
                }
        }

        [Fact]
        public void ACarriedCapabilityClimbsTheFullLadder()
        {
            var sim = GuildMemberAfterThePump();
            var before = sim.CapabilityLevelOf("valveseats");
            Assert.Equal(CapabilityLevel.Reproducible, before);
            var arrival = sim.JumpForTests();
            int steps = arrival.JumpYears / sim.T.GetInt("capabilities.carriedYearsPerLevel");
            var ladder = new[] { CapabilityLevel.Reproducible, CapabilityLevel.Manufacturable, CapabilityLevel.Economical, CapabilityLevel.Adopted, CapabilityLevel.Institutionalized };
            Assert.Equal(ladder[System.Math.Min(4, steps)], sim.CapabilityLevelOf("valveseats"));
            Assert.True(steps >= 1);
            Assert.NotEqual(CapabilityLevel.Adopted, ladder[1]);                // manufacturable is a real rung, not skipped
        }

        [Fact]
        public void TheArrivalShowsTheTrueLineageAndTheBadCopiesApart()
        {
            var sim = GuildMemberAfterThePump();
            sim.SpreadCapability("valveseats", CapabilitySpread.Copied, true, true, null, "test", "Pollio");
            var arrival = sim.JumpForTests();
            Assert.Contains("technical:valveseats", arrival.P1Echoes);
            Assert.Contains("unintended:valveseats", arrival.P1Echoes);
            var discovery = arrival.Beats.First(b => b.Name == "Discovery").Text;
            Assert.Contains(sim.CapabilityDefOf("valveseats")!.Echo["distorted"], discovery);
        }

        [Fact]
        public void LaterJumpsKeepHumanEchoesAndNeverRepeatThem()
        {
            var sim = GuildMemberAfterThePump();
            var first = sim.JumpForTests();
            Assert.Contains(sim.World.Return!.Sites, s => s.Category == ReturnCategory.Human);   // the first return holds the people
            var firstPersonal = first.Beats.First(b => b.Name == "Personal echo").Text;
            Butterfly.Batch.P1Campaign.FollowReturnProtocol(sim);
            Assert.True(sim.CanJumpAgain);
            var second = sim.JumpForTests();
            Assert.Contains(second.P1Echoes, e => e.StartsWith("person:"));
            var secondPersonal = second.Beats.First(b => b.Name == "Personal echo").Text;
            // Every person line on the second arrival is new, and later lines (fromJump 2) come first.
            var lines = sim.Data.Content.People.SelectMany(p => p.Echoes).ToList();
            var shownSecond = lines.Where(l => secondPersonal.Contains(l.Text)).ToList();
            Assert.NotEmpty(shownSecond);
            Assert.All(shownSecond, l => Assert.DoesNotContain(l.Text, firstPersonal));
            Assert.Contains(shownSecond, l => l.FromJump == 2);
            // Nothing in the second arrival's beats repeats a first-arrival echo word for word.
            foreach (var l in lines.Where(l => firstPersonal.Contains(l.Text)))
                Assert.DoesNotContain(l.Text, string.Join(" ", second.Beats.Select(b => b.Text)));
        }
    }
}
