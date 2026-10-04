using System;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Hardening pass (2026-10-04): the ledger under stress. Every payment and material cost is in the ledger; a profit share
    /// pays each of its shares once, on time, and stops at departure; favors pay nothing; a walked deal pays nothing; nothing
    /// pays twice; terms are priced when agreed; no negative gold; no money made by jumping; nothing stays "working" after
    /// departure. Rich (workshop) and poor (fountain) paths both reconcile; neither is equalized.
    /// </summary>
    [Collection("Console")]
    public class P1EconomyStressTests
    {
        private static Simulation At(string choice = "workshop", ulong seed = 42, double gold = 5000)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded(choice);
            sim.World.Gold = gold;
            return sim;
        }

        private static CommissionState Terms(Simulation sim, string id)
        {
            var c = sim.FindCommission(id)!;
            c.Status = CommissionStatus.Offered;
            while (sim.World.Attention < 1) sim.EndMonth();
            Assert.True(sim.LookAtCommission(id).Ok);
            return c;
        }

        private static void Finish(Simulation sim, string id)
        {
            var c = sim.FindCommission(id)!;
            while (sim.World.Attention < 1 || sim.ReservedInMonth(1) > 3 || sim.ReservedInMonth(2) > 3) sim.EndMonth();
            var r = sim.AcceptCommission(id);
            Assert.True(r.Ok, r.Message);
            while (c.Status == CommissionStatus.Working) sim.EndMonth();
            Assert.Equal(CommissionStatus.Done, c.Status);
        }

        private static bool Reconciles(Simulation sim, double start) =>
            Math.Abs(start + sim.World.Ledger.Income - sim.World.Ledger.Expenses - sim.World.Gold) < 0.01;

        [Fact]
        public void AProfitSharePaysEachShareOnceOnScheduleAndInTheLedger()
        {
            var sim = At();
            var d = sim.Data.Content.Commissions.First(x => x.Id == "drawings");
            Assert.Equal(ProjectFundingModel.ProfitShare, d.FundingModel);
            Assert.True(d.SharePayments > 0);
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));   // as its offer requires
            Terms(sim, "drawings");
            Finish(sim, "drawings");
            int done = sim.Turn;
            for (int m = 0; m < d.ShareEveryMonths * (d.SharePayments + 3); m++) sim.EndMonth();
            var shares = sim.Log.Events.Where(e => e.Type == "commission.share").ToList();
            Assert.Equal(d.SharePayments, shares.Count);                                   // each once, then it stops
            Assert.Equal(d.SharePayments, sim.World.Ledger.Entries.Count(e => e.Kind == LedgerEntryKind.ProfitShare && e.ProjectId == "commission:drawings"));
            Assert.All(sim.World.Ledger.Entries.Where(e => e.Kind == LedgerEntryKind.ProfitShare), e => Assert.True(e.Amount > 0));
            Assert.Equal(shares.Count, shares.Select(e => e.Text).Distinct().Count());     // never the same words twice
        }

        [Fact]
        public void AShareStillOwingStopsAtDeparture()
        {
            var sim = At();
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));   // as its offer requires
            Terms(sim, "drawings");
            Finish(sim, "drawings");
            Assert.True(sim.FindCommission("drawings")!.SharesLeft > 0);
            sim.JumpForTests();
            Assert.Equal(0, sim.FindCommission("drawings")!.SharesLeft);
            Assert.Contains(sim.Log.Events, e => e.Type == "commission.share.lapsed");
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "commission.share");
        }

        [Fact]
        public void AFavorPaysNoMoney()
        {
            var sim = At();
            var d = sim.Data.Content.Commissions.First(x => x.Id == "jars");
            Assert.Equal(ProjectFundingModel.Favor, d.FundingModel);
            Terms(sim, "jars");
            Finish(sim, "jars");
            Assert.DoesNotContain(sim.World.Ledger.ForProject("commission:jars"), e => e.Amount > 0);
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "commission.complete" && e.Target == "commission:jars" && e.Effects.Any(f => f.Key == "gold"));
        }

        [Fact]
        public void AWalkedDealPaysNothing()
        {
            // Philippus usually walks if pushed; find seeds where he does, and check no money moved for the job.
            int walked = 0;
            for (ulong seed = 1; seed <= 30 && walked < 3; seed++)
            {
                var sim = At("workshop", seed);
                var c = Terms(sim, "baths");
                sim.CounterCommission("baths");
                if (c.Status != CommissionStatus.Walked) continue;
                walked++;
                for (int m = 0; m < 6; m++) sim.EndMonth();
                Assert.Empty(sim.World.Ledger.ForProject("commission:baths").Where(e => e.Amount > 0));
                Assert.DoesNotContain(sim.Log.Events, e => e.Type == "commission.complete" && e.Target == "commission:baths");
                Assert.False(sim.AcceptCommission("baths").Ok);                            // can't take it back up
            }
            Assert.True(walked > 0);
        }

        [Fact]
        public void NoCommissionPaysTwice()
        {
            var sim = At();
            foreach (var id in new[] { "cellarpump", "fevers", "baths" })
            {
                Terms(sim, id);
                Finish(sim, id);
            }
            for (int m = 0; m < 24; m++) sim.EndMonth();
            foreach (var id in new[] { "cellarpump", "fevers", "baths" })
            {
                Assert.Equal(1, sim.Log.Events.Count(e => e.Type == "commission.complete" && e.Target == "commission:" + id));
                Assert.Equal(1, sim.Log.Events.Count(e => e.Type == "commission.agreed" && e.Target == "commission:" + id));
            }
        }

        [Fact]
        public void TermsArePricedWhenAgreedAndHeldToTheEnd()
        {
            var sim = At();
            for (int m = 0; m < 60; m++) sim.EndMonth();                                   // prices have risen with the coin
            var d = sim.Data.Content.Commissions.First(x => x.Id == "fevers");
            Terms(sim, "fevers");
            double agreedCompletion = sim.Priced(d.Completion);
            Assert.True(agreedCompletion > d.Completion);                                  // scaled by the price level
            Finish(sim, "fevers");
            var paid = sim.Log.Events.Single(e => e.Type == "commission.complete" && e.Target == "commission:fevers");
            double got = paid.Effects.Where(f => f.Key == "gold").Sum(f => f.After - f.Before);
            Assert.Equal(sim.World.Projects.First(p => p.Id == "commission:fevers").Terms.CompletionGold, got, 6);
            Assert.InRange(got, agreedCompletion * 0.98, agreedCompletion * 1.05);         // locked at agreement, not re-priced
        }

        [Fact]
        public void YouCannotSpendGoldYouDoNotHave()
        {
            var sim = At("fountain", 42, 0);
            sim.World.Aurei = 0;
            var c = sim.FindChallenge("standards")!;
            c.Status = ChallengeStatus.Open;
            Assert.False(sim.StartChallengeStage("standards").Ok);
            Assert.True(sim.World.Gold >= 0);
            for (int m = 0; m < 36; m++) { sim.EndMonth(); Assert.True(sim.World.Gold >= -1e-9, "gold went negative in month " + m); }
        }

        [Fact]
        public void JumpingMakesNoMoneyAndLeavesNoWorkUnderWay()
        {
            var sim = At();
            sim.World.Aurei = 40;
            Terms(sim, "cellarpump");
            while (sim.World.Attention < 1) sim.EndMonth();
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            Assert.Equal(CommissionStatus.Working, sim.FindCommission("cellarpump")!.Status);
            double aureiBefore = sim.World.Aurei + sim.World.DepositAurei + sim.World.HoardAurei;
            int ledgerBefore = sim.World.Ledger.Entries.Count;
            var a = sim.JumpForTests();
            Assert.DoesNotContain(sim.World.Commissions, x => x.Status == CommissionStatus.Working);
            Assert.DoesNotContain(sim.World.Challenges, x => x.Status == ChallengeStatus.Working);
            Assert.True(sim.World.Aurei <= aureiBefore + 1e-9);                            // nothing is created in transit
            Assert.True(a.AureiCarried + a.AureiLeft <= aureiBefore + 1e-9);
            Assert.DoesNotContain(sim.World.Ledger.Entries.Skip(ledgerBefore), e => e.ProjectId == "commission:cellarpump" && e.Amount > 0);
        }

        [Fact]
        public void CoinInHandStaysBehindOnAJump()
        {
            // SYSTEMS §9 and the briefing: only the machine's purse, the bank and the jar carry value across a jump.
            var sim = At("workshop", 42, 123);
            sim.World.Aurei = 3;
            var ledgerBefore = sim.World.Ledger.Expenses;
            sim.JumpForTests();
            var left = sim.Log.Events.Single(e => e.Type == "jump.coin.left");
            Assert.Equal(123, left.Effects.Single(f => f.Key == "gold").Before, 6);
            Assert.Equal(0, left.Effects.Single(f => f.Key == "gold").After, 6);
            Assert.True(sim.World.Ledger.Expenses >= ledgerBefore + 123 - 1e-6);          // in the ledger, not a silent reset
            Assert.True(sim.World.Gold < 1);
        }

        [Fact]
        public void BothOpeningsReconcileWithoutBeingEqualized()
        {
            foreach (var choice in new[] { "workshop", "fountain" })
            {
                var sim = new Simulation(TestData.Load(), 7);
                sim.ChooseSeeded(choice);
                double start = sim.World.Gold;
                for (int m = 0; m < 48; m++)
                {
                    foreach (var c in sim.World.Commissions.ToList())
                    {
                        if (c.Status == CommissionStatus.Offered) sim.LookAtCommission(c.Id);
                        if (c.Status == CommissionStatus.TermsOffered) sim.AcceptCommission(c.Id);
                    }
                    if (sim.World.Gold < 2 && sim.World.Aurei >= 5) sim.SellAurei(5);
                    if (sim.OwnsWorkshop && sim.OrdersLeftThisSeason > 0 && sim.OrderBoard().Any()) sim.TakeOrder(sim.OrderBoard().First().Id);
                    sim.EndMonth();
                    Assert.True(sim.World.Gold >= -1e-9);
                }
                Assert.True(Reconciles(sim, start), choice + " ledger doesn't reconcile");
                Assert.All(sim.World.Ledger.Entries.Where(e => e.Kind == LedgerEntryKind.Materials), e => Assert.True(e.Amount < 0));
            }
        }
    }
}
