using System;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>The workshop (decided 2026-09-28, P0-34): orders, apprentices, the smith, and what they leave behind.</summary>
    public class WorkshopTests
    {
        private static Simulation WithWorkshop(ulong seed = 61)
        {
            var sim = new Simulation(TestData.Load(), seed);
            Assert.True(sim.ChooseSeeded("workshop").Ok);
            while (!sim.OwnsWorkshop) sim.EndTurn();
            while (!sim.OrderBoard().Any()) sim.EndTurn();
            return sim;
        }

        [Fact]
        public void NoOrdersWithoutAWorkshop()
        {
            var sim = new Simulation(TestData.Load(), 61);
            sim.ChooseSeeded("fountain");
            for (int t = 0; t < 12; t++) sim.EndTurn();
            Assert.Empty(sim.OrderBoard());
            Assert.False(sim.TakeOrder("tools").Ok);
            Assert.False(sim.HireApprentice().Ok);
        }

        [Fact]
        public void EachSeasonBringsOrdersAndTheWorkshopCanTakeOnlySome()
        {
            var sim = WithWorkshop();
            Assert.Equal(sim.CurrentSize.Offered, sim.OrderBoard().Count());
            Assert.Equal(1, sim.OrdersPerSeason());
            var first = sim.OrderBoard().First();
            double gold = sim.World.Gold;
            Assert.True(sim.TakeOrder(first.Id).Ok);
            Assert.True(sim.World.Gold > gold);
            Assert.Equal(1, sim.World.OrdersTaken);
            Assert.False(sim.TakeOrder(sim.OrderBoard().First().Id).Ok);         // one a season
        }

        [Fact]
        public void OrderPayIsTaxedAndGrowsWithApprentices()
        {
            var sim = WithWorkshop();
            var o = sim.OrderBoard().First();
            double bare = sim.OrderPay(o);
            Assert.Equal(o.Pay * sim.World.PriceLevel * (1 + sim.World.WorkshopBonus), bare, 6);
            sim.World.Apprentices = 2;
            Assert.Equal(bare / (1 + sim.World.WorkshopBonus) * (1 + sim.World.WorkshopBonus + 2 * sim.T.Get("workshop.apprentices.outputEach")), sim.OrderPay(o), 6);
            Assert.Equal(2, sim.OrdersPerSeason());                             // two apprentices: two orders a season
            double gold = sim.World.Gold, pay = sim.OrderPay(o);
            sim.TakeOrder(o.Id);
            Assert.Equal(gold + pay * (1 - sim.WorkTaxRate()), sim.World.Gold, 6);
        }

        [Fact]
        public void ApprenticesRaiseTheWorkshopsIncomeAndArePaidEachYear()
        {
            var sim = WithWorkshop();
            double income = sim.OwnedIncome();
            Assert.True(sim.HireApprentice().Ok);
            Assert.Equal(1, sim.World.Apprentices);
            Assert.True(sim.OwnedIncome() > income);
            sim.World.Gold = 1000;
            int year = sim.Now.Year;
            while (sim.Now.Year == year) sim.EndTurn();
            Assert.Contains(sim.Log.Events, e => e.Type == "workshop.wages");
        }

        [Fact]
        public void AnApprenticeYouCantPayLeaves()
        {
            var sim = WithWorkshop();
            sim.World.Apprentices = 3;
            int year = sim.Now.Year;
            while (sim.Now.Year == year) { sim.World.Gold = 0; sim.EndTurn(); }
            Assert.Equal(0, sim.World.Apprentices);
            Assert.Contains(sim.Log.Events, e => e.Type == "workshop.apprentice" && e.Text.Contains("can't pay"));
        }

        [Fact]
        public void TheApprenticesCarryYourTechniquesAfterYouLeave()
        {
            var sim = WithWorkshop();
            Assert.Equal(0, sim.WorkshopCarry(sim.Now.Year));                  // not before you leave
            sim.World.Apprentices = 4;
            var arrival = sim.JumpForTests();
            double carry = 4 * sim.T.Get("workshop.carry.perApprentice");
            Assert.Equal(carry, sim.WorkshopCarry(arrival.DepartureYear), 6);
            Assert.Equal(0, sim.WorkshopCarry(arrival.DepartureYear + sim.T.GetInt("workshop.carry.years")));
        }

        [Fact]
        public void TheWorkshopsFateFollowsItsApprenticesAndTheSmith()
        {
            var many = WithWorkshop();
            many.World.Apprentices = 4;
            many.World.SmithRegard = 80;
            var a = many.JumpForTests();
            Assert.Equal("street", many.WorkshopFate());
            Assert.Contains("street of forges", a.Beats[0].Text);
            Assert.Contains("learned from your apprentices", many.Visit("forges"));

            var none = WithWorkshop();
            none.World.SmithRegard = 10;
            none.JumpForTests();
            Assert.Equal("gone", none.WorkshopFate());
        }

        [Fact]
        public void TheSmithAsksToSellYourDesigns()
        {
            var sim = WithWorkshop();
            while (sim.PendingEvent?.Id != "designs" && sim.Now.Year < 164)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            Assert.Equal("designs", sim.PendingEvent!.Id);
            double bonus = sim.World.WorkshopBonus, regard = sim.World.SmithRegard;
            Assert.True(sim.Decide("sell").Ok);
            Assert.True(sim.World.WorkshopBonus < bonus);                       // the workshop loses its edge
            Assert.True(sim.World.SmithRegard > regard);
        }

        [Fact]
        public void TheSmithOnlyAsksAPartner()
        {
            var sim = new Simulation(TestData.Load(), 62);
            sim.ChooseSeeded("fountain");
            while (sim.Now.Year < 169)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "event.offer" && (e.Target == "designs" || e.Target == "riverForge"));
        }
    
        private static void Finish(Simulation sim) { while (sim.World.WorkshopBuildTurns > 0) sim.EndTurn(); }

        [Fact]
        public void TheWorkshopGrowsOneSizeAtATimeWhenRomeAllows()
        {
            var sim = WithWorkshop();
            Assert.Equal(1, sim.WorkshopSize);
            sim.World.Gold = 10000;
            Assert.False(sim.Expand().Ok);                                      // the yard needs the guild or the bank
            sim.World.Institution("guild").Stake = 0.02;
            double gold = sim.World.Gold;
            Assert.True(sim.Expand().Ok);
            Assert.Equal(gold - sim.ExpandCost(sim.NextSize!), sim.World.Gold, 6);
            Assert.False(sim.Expand().Ok);                                      // one at a time
            Finish(sim);
            Assert.Equal(2, sim.WorkshopSize);
            Assert.Equal(6, sim.CurrentSize.ApprenticeMax);
            Assert.False(sim.Expand().Ok);                                      // the works needs a water right
            sim.World.Institution("faction").Stake = 0.10;
            Assert.True(sim.Expand().Ok);
            Finish(sim);
            Assert.Equal(3, sim.WorkshopSize);
            Assert.False(sim.Expand().Ok);                                      // the foundry needs the water-powered bellows
            sim.World.Invented.Add("bellows");
            Assert.True(sim.Expand().Ok);
            Finish(sim);
            Assert.Equal(4, sim.WorkshopSize);
            Assert.Null(sim.NextSize);
        }

        [Fact]
        public void ABiggerWorkshopTakesMoreAndBetterOrders()
        {
            var sim = WithWorkshop();
            double output = sim.WorkshopOutput(), income = sim.OwnedIncome();
            sim.World.WorkshopSize = 4;
            Assert.True(sim.WorkshopOutput() > output);
            Assert.True(sim.OwnedIncome() > income);
            Assert.Equal(3, sim.OrdersPerSeason());
            // Only a foundry is offered public-works contracts.
            bool contract = false;
            for (int t = 0; t < 60 && !contract; t++) { sim.EndTurn(); contract = sim.OrderBoard().Any(o => o.Id == "contract"); if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id); if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); }
            Assert.True(contract);
            var small = WithWorkshop(63);
            for (int t = 0; t < 30; t++) { Assert.DoesNotContain(small.OrderBoard(), o => o.Id == "contract"); small.EndTurn(); if (small.PendingEvent != null) small.Decide(small.PendingEvent.Options.Last().Id); }
        }

        [Fact]
        public void AnUpkeepYouCantPayShrinksTheWorkshop()
        {
            var sim = WithWorkshop();
            sim.World.WorkshopSize = 3;
            sim.World.Apprentices = 6;
            int year = sim.Now.Year;
            while (sim.Now.Year == year) { sim.World.Gold = 0; sim.EndTurn(); }
            Assert.Equal(2, sim.WorkshopSize);
            Assert.Contains(sim.Log.Events, e => e.Type == "workshop.size" && e.Text.Contains("can't pay the upkeep"));
        }

        [Fact]
        public void BackingTheSmithsRiverForgeBuildsTheWorks()
        {
            var sim = WithWorkshop();
            while (sim.PendingEvent?.Id != "riverForge" && sim.Now.Year < 169)
            {
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.World.Gold = Math.Max(sim.World.Gold, 50);                  // keep up the upkeep of nothing
                sim.EndTurn();
            }
            Assert.Equal("riverForge", sim.PendingEvent!.Id);
            sim.World.Gold = 1000;
            Assert.True(sim.Decide("fund").Ok);
            Assert.Equal(3, sim.WorkshopSize);
        }

        [Fact]
        public void TheSmithDoesntAskForWhatYouAlreadyBuilt()
        {
            var sim = WithWorkshop();
            sim.World.WorkshopSize = 3;
            while (sim.Now.Year < 169)
            {
                sim.World.Gold = 1000;
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options.Last().Id);
                sim.EndTurn();
            }
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "event.offer" && e.Target == "riverForge");
        }

        [Fact]
        public void TheWorksShowsAtTheForgesOnArrival()
        {
            var sim = WithWorkshop();
            sim.World.WorkshopSize = 3;
            sim.World.Apprentices = 4;
            sim.World.SmithRegard = 70;
            sim.JumpForTests();
            Assert.Equal("street", sim.WorkshopFate());
            Assert.Contains("water wheel you paid for", sim.Visit("forges"));
        }
}
}
