using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 access beyond the invitation paths (2026-10-04): the Senate factions aren't bought into (a patron's introduction,
    /// not yet built); the sanctuary takes gifts; Rome's choices win standing, never shares, except at the bank.
    /// </summary>
    [Collection("Console")]
    public class P1AccessTests
    {
        [Fact]
        public void TheFactionsDoNotSellPlaces()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Gold = 5000;
            string output = ConsoleTests.Play(sim, "buy faction 5", "buy junian 1", "menu on", "menu");
            Assert.Contains("a patron's introduction", output);
            Assert.False(sim.World.Institution("faction").Backed);
            Assert.False(sim.World.Institution("junian").Backed);
            Assert.DoesNotContain("buy faction", output.Replace("> buy faction", ""));
        }

        [Fact]
        public void RomesChoicesGiveSharesOnlyAtTheBank()
        {
            var c = TestData.Load().Content;
            var stakes = c.Events.SelectMany(e => e.Options).SelectMany(o => o.Effects).Where(f => f.Type == "stake").ToList();
            Assert.NotEmpty(stakes);
            Assert.All(stakes, f => Assert.Equal("bank", f.Institution));
            Assert.True(c.Events.SelectMany(e => e.Options).SelectMany(o => o.Effects).Count(f => f.Type == "standing") >= 10);
        }

        [Fact]
        public void TheSanctuaryNamesItsGiversBenefactors()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Gold = 5000;
            Assert.Contains("takes gifts", ConsoleTests.Play(sim, "buy sanctuary 1"));
            Assert.False(sim.World.Institution("sanctuary").Backed);
            var r = sim.Give("sanctuary");
            Assert.True(r.Ok, r.Message);
            Assert.Contains("benefactors", r.Message);
            Assert.Contains("among its benefactors", ConsoleTests.Play(sim, "institutions"));
        }
    }
}
