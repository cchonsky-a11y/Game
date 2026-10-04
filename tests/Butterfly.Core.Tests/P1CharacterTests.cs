using System.IO;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Recurring people with lives of their own (2026-10-04): each has offscreen developments and private scenes that come
    /// through the router from world state, can say no, and changes even if the player looks away. The legacy smith is
    /// Successus now, so Marcus Fabius Tertius is the only Tertius.
    /// </summary>
    [Collection("Console")]
    public class P1CharacterTests
    {
        private static readonly string[] Cast = { "Felix", "Cassianus", "Diodoros", "Serenus", "Gaius", "Livia", "Marcus", "Lucan", "Aulus", "Sextus" };

        [Fact]
        public void TheOldSmithIsSuccessusAndTertiusIsOnlyMarcus()
        {
            var c = TestData.Load().Content;
            Assert.Equal("Successus", c.Smith);
            var dir = Path.Combine(GameData.FindDataDirectory(System.AppContext.BaseDirectory), "content");
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                string text = File.ReadAllText(file).Replace("Marcus Fabius Tertius", "");
                Assert.DoesNotContain("Tertius", text);
            }
        }

        [Fact]
        public void EveryoneInTheCastHasALifeOfTheirOwn()
        {
            var c = TestData.Load().Content;
            foreach (var who in Cast)
            {
                Assert.Contains(c.People, p => p.Id == who);
                Assert.True(c.Lives.Count(l => l.Person == who) >= 1, who + " has no offscreen life");
                Assert.True(c.Scenes.Any(s => s.Text.Contains(c.People.First(p => p.Id == who).Name.Split(' ')[0])) ||
                            c.Lives.Any(l => l.Person == who), who);
            }
        }

        private static Simulation OnlyScene(string id, ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 5000;
            sim.World.ScenesSeen.AddRange(sim.Data.Content.Scenes.Where(s => s.Id != id).Select(s => s.Id));
            foreach (var cm in sim.World.Commissions) cm.Status = CommissionStatus.Declined;
            return sim;
        }

        [Fact]
        public void CassianusAsksYouToWitnessAndYouMaySayNo()
        {
            var sim = OnlyScene("cassianus-contract");
            sim.PersonOf("Cassianus")!.Happened.Add("cassianus-betrothal");
            sim.World.LifeEventLog["cassianus-betrothal"] = 1;
            sim.World.ScenesSeen.Remove("cassianus-contract");
            for (int m = 0; m < 3 && sim.PendingEvent?.Id != "witness"; m++) sim.EndMonth();
            Assert.Equal("witness", sim.PendingEvent?.Id);
            int regard = sim.PersonOf("Cassianus")!.Regard;
            Assert.True(sim.Decide("decline").Ok);
            Assert.Equal(regard - 1, sim.PersonOf("Cassianus")!.Regard);
        }

        [Fact]
        public void GaiusTurnsDownTheSecondShop()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.FindChallenge("power")!.Status = ChallengeStatus.Done;
                for (int m = 0; m < 12; m++) sim.EndMonth();
                if (!sim.World.LifeEventLog.ContainsKey("gaius-no") && !sim.World.ReadyLife.Contains("gaius-no")) continue;
                for (int m = 0; m < 6 && !sim.World.LifeEventLog.ContainsKey("gaius-no"); m++) sim.EndMonth();
                Assert.Contains("refusing to open a second", sim.PersonOf("Gaius")!.Status);
                return;
            }
            Assert.Fail("Gaius never said no in 20 seeds");
        }

        [Fact]
        public void LucanGrowsOldAndDiesWhetherYouWatchOrNot()
        {
            int died = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.World.ScenesSeen.Add("lucan-advice");                   // you know him; then you look away
                for (int m = 0; m < 140; m++) sim.EndMonth();
                if (sim.World.LifeEventLog.ContainsKey("lucan-death"))
                {
                    died++;
                    Assert.True(sim.PersonOf("Lucan")!.Gone);
                    Assert.True(sim.World.LifeEventLog.ContainsKey("lucan-eyes"));
                }
            }
            Assert.InRange(died, 1, 19);                                     // likely, never certain
        }

        [Fact]
        public void FelixAsksForMoneyOnlyIfHeTrustsYou()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.PersonOf("Felix")!.Happened.Add("felix-son");
            sim.World.LifeEventLog["felix-son"] = 1;
            var loan = sim.Data.Content.Scenes.First(s => s.Id == "felix-loan");
            Assert.False(loan.Requires.All(sim.Holds));                      // regard first
            sim.PersonOf("Felix")!.Regard = 2;
            Assert.True(loan.Requires.All(sim.Holds));
        }
    }
}
