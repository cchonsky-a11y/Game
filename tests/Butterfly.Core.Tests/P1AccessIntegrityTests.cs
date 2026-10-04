using System.Linq;
using System.Reflection;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 correctness pass (2026-10-04): institution access is decided by the simulation, not the console, so a future UI calling
    /// <see cref="Simulation.Buy"/> can't bring back the P0 stake purchase. The P0 purchase survives only as an internal,
    /// regression-only seam for the P0 batch, explorer and snapshot.
    /// </summary>
    [Collection("Console")]
    public class P1AccessIntegrityTests
    {
        private static Simulation Rich()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 20000;
            return sim;
        }

        [Theory]
        [InlineData("faction")]
        [InlineData("junian")]
        public void ASenatorsHouseCantBeBought(string id)
        {
            var sim = Rich();
            var r = sim.Buy(id, 10);
            Assert.False(r.Ok);
            Assert.Contains("patron's introduction", r.Message);
            Assert.Equal(0, sim.StakePercent(sim.World.Institution(id)));
            Assert.False(sim.World.Institution(id).Backed);
        }

        [Theory]
        [InlineData("guild")]
        [InlineData("circle")]
        public void AnInvitationPathCantBeBoughtPast(string id)
        {
            var sim = Rich();
            sim.World.Flags.Add("promise");
            var r = sim.Buy(id, 5);
            Assert.False(r.Ok);
            Assert.Contains("members bring you in", r.Message);
            Assert.False(sim.World.Institution(id).Backed);
            Assert.True(sim.World.AccessTo(id).Stage < InstitutionAccessStage.Member);
        }

        [Fact]
        public void TheSanctuaryTakesGiftsNotPurchases()
        {
            var sim = Rich();
            var bought = sim.Buy("sanctuary", 1);
            Assert.False(bought.Ok);
            Assert.Contains("give sanctuary", bought.Message);
            Assert.False(sim.World.Institution("sanctuary").Backed);
            Assert.True(sim.Give("sanctuary").Ok);                                      // the gift route still works
            Assert.True(sim.World.Institution("sanctuary").Backed);
            Assert.False(sim.Give("bank").Ok);                                          // and is only for those who take gifts
        }

        [Fact]
        public void TheBankStillSellsRealShares()
        {
            var sim = Rich();
            int first = sim.T.GetInt("joining.bankMinFirstPercent");
            var r = sim.Buy("bank", first);
            Assert.True(r.Ok, r.Message);
            Assert.Equal(first, sim.StakePercent(sim.World.Institution("bank")));
            Assert.Null(sim.AccessRefusal(sim.World.Institution("bank")));
        }

        [Fact]
        public void YourOwnInstitutionsAreFoundedNotBought()
        {
            var sim = Rich();
            var own = sim.World.Institutions.First(i => i.Def.IsOwn);
            var r = sim.Buy(own.Key, 5);
            Assert.False(r.Ok);
            Assert.Contains("found it instead", r.Message);
            Assert.False(sim.BuyLegacyStakeForP0Regression(own.Key, 5).Ok);           // not even through the legacy seam
        }

        [Fact]
        public void TheLegacySeamStillBuysP0StakesForRegression()
        {
            var sim = Rich();
            Assert.False(sim.Buy("sanctuary", 1).Ok);
            var r = sim.BuyLegacyStakeForP0Regression("sanctuary", 3);
            Assert.True(r.Ok, r.Message);
            Assert.Equal(3, sim.StakePercent(sim.World.Institution("sanctuary")));
            sim.EndMonth();
            var bank = sim.BuyLegacyStakeForP0Regression("bank", sim.T.GetInt("joining.bankMinFirstPercent"));
            Assert.True(bank.Ok, bank.Message);
        }

        [Fact]
        public void TheLegacySeamIsNotPartOfThePublicApi()
        {
            var legacy = typeof(Simulation).GetMethod("BuyLegacyStakeForP0Regression", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            Assert.NotNull(legacy);
            Assert.True(legacy.IsAssembly);                                              // internal: tests and the batch runner only
            Assert.Null(typeof(Simulation).GetMethod("BuyLegacyStakeForP0Regression", BindingFlags.Instance | BindingFlags.Public));
            Assert.True(typeof(Simulation).GetMethod("Buy", BindingFlags.Instance | BindingFlags.Public)!.IsPublic);
        }

        [Fact]
        public void EveryAccessKindInContentIsCovered()
        {
            var sim = Rich();
            var kinds = sim.World.Institutions.Where(i => !i.Def.IsOwn)
                .Select(i => sim.OnInvitationPath(i) ? "invitation" : i.Def.Access).Distinct().OrderBy(k => k).ToList();
            Assert.Equal(new[] { "gifts", "invitation", "patronage", "shares" }, kinds);
            foreach (var i in sim.World.Institutions.Where(i => !i.Def.IsOwn))
                Assert.Equal(i.Def.Access == "shares" && !sim.OnInvitationPath(i), sim.AccessRefusal(i) == null);
        }
    }
}
