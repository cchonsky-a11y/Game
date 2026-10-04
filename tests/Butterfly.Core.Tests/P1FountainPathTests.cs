using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Hardening pass (2026-10-04): the fountain opening had no paid work of its own and met Gaius only after shared measures,
    /// so it leaned on odd jobs. It now has a neighborhood chain (the Subura allotment, then the Argiletum's four fountains)
    /// that introduces Gaius and raises the standards question, while the workshop keeps its orders and craft income.
    /// </summary>
    [Collection("Console")]
    public class P1FountainPathTests
    {
        private static Simulation At(string choice, ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded(choice);
            sim.World.Gold = 5000;
            return sim;
        }

        private static void Play(Simulation sim, string id)
        {
            var c = sim.FindCommission(id)!;
            for (int m = 0; m < 60 && c.Status == CommissionStatus.NotYet; m++) sim.EndMonth();
            Assert.Equal(CommissionStatus.Offered, c.Status);
            while (sim.World.Attention < 1) sim.EndMonth();
            Assert.True(sim.LookAtCommission(id).Ok);
            while (sim.World.Attention < 1 || sim.ReservedInMonth(1) > 3 || sim.ReservedInMonth(2) > 3) sim.EndMonth();
            var r = sim.AcceptCommission(id);
            Assert.True(r.Ok, r.Message);
            while (c.Status == CommissionStatus.Working) sim.EndMonth();
            Assert.Equal(CommissionStatus.Done, c.Status);
        }

        [Fact]
        public void TheFountainHasAPaidNeighborhoodChainWithoutAWorkshop()
        {
            var sim = At("fountain");
            Assert.False(sim.OwnsWorkshop);
            double before = sim.World.Gold;
            Play(sim, "allotment");
            var d = sim.Data.Content.Commissions.First(x => x.Id == "allotment");
            Assert.Equal(ProjectFundingModel.ClientPaid, d.FundingModel);
            Assert.True(sim.World.Gold > before);                                          // paid, not a favor
            Assert.True(sim.Knows("Gaius"));                                               // met through the work, not through shared measures
            Play(sim, "argiletum");                                                        // the repeat, larger job comes from the same neighborhood
            Assert.Contains(sim.World.Ledger.Entries, e => e.Counterparty.Contains("Argiletum"));
        }

        [Fact]
        public void TheFountainCanOpenMeasurementAndStandardsWithoutThePump()
        {
            var sim = At("fountain");
            sim.FindCommission("cellarpump")!.Status = CommissionStatus.Declined;
            Play(sim, "allotment");
            for (int m = 0; m < 30 && sim.FindChallenge("standards")!.Status == ChallengeStatus.NotYet; m++) sim.EndMonth();
            Assert.Equal(ChallengeStatus.Open, sim.FindChallenge("standards")!.Status);
            Assert.Equal("allotment", sim.FindChallenge("standards")!.OpenedBy);
        }

        [Fact]
        public void GaiusIsIntroducedOnlyOnce()
        {
            var sim = At("fountain");
            Play(sim, "allotment");
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));
            for (int m = 0; m < 24; m++) sim.EndMonth();
            Assert.DoesNotContain("gaius-meet", sim.World.ScenesSeen);
        }

        [Fact]
        public void TheWorkshopKeepsItsOwnAdvantages()
        {
            var shop = At("workshop");
            var fountain = At("fountain");
            bool ordersCame = false;
            for (int m = 0; m < 14; m++)
            {
                shop.EndMonth();
                fountain.EndMonth();
                ordersCame |= shop.OrderBoard().Any();
                Assert.Empty(fountain.OrderBoard());                                       // craft orders are the workshop's
            }
            Assert.True(shop.OwnsWorkshop);
            Assert.False(fountain.OwnsWorkshop);
            Assert.True(ordersCame);
            Assert.Equal(CommissionStatus.NotYet, shop.FindCommission("allotment")!.Status);   // the neighborhood job is the fountain's
            Assert.NotEqual(CommissionStatus.NotYet, fountain.FindCommission("allotment")!.Status);
        }
    }
}
