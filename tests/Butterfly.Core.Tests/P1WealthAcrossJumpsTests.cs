using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// What wealth reaches the future (SYSTEMS §9, P1 polish pass 2026-10-04). Some does, deliberately: the aurei the machine
    /// can carry, gold left with the banking house (interest, or lost if the house fails), gold buried in a jar (no
    /// interest, or found by someone), and money institutions hold, which stays with them in Rome. What doesn't: unconverted
    /// denarii in hand, and aurei beyond the machine's purse that were neither deposited nor buried.
    /// </summary>
    [Collection("Console")]
    public class P1WealthAcrossJumpsTests
    {
        private static Simulation At(double aurei, double denarii = 0, ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            sim.World.Aurei = aurei;
            sim.World.Gold = denarii;
            return sim;
        }

        [Fact]
        public void AureiTheMachineCanCarrySurvive()
        {
            var sim = At(6);
            Assert.True(6 <= sim.CarryAurei);
            var a = sim.JumpForTests();
            Assert.Equal(6, a.AureiCarried, 9);
            Assert.Equal(0, a.AureiLeft, 9);
            Assert.Equal(6, sim.World.Aurei, 9);
        }

        [Fact]
        public void AureiBeyondThePurseDoNotSurviveUnlessPutAway()
        {
            var sim = At(40);
            double cap = sim.CarryAurei;
            var a = sim.JumpForTests();
            Assert.Equal(cap, a.AureiCarried, 9);
            Assert.Equal(40 - cap, a.AureiLeft, 9);
            Assert.Equal(cap, sim.World.Aurei, 9);                                    // the rest stayed behind
        }

        [Fact]
        public void DepositedGoldFollowsTheBanksRules()
        {
            int kept = 0, lost = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var sim = At(40, 0, seed);
                Assert.True(sim.Deposit(30).Ok);
                Assert.Equal(10, sim.World.Aurei, 9);
                var a = sim.JumpForTests();
                Assert.Equal(30, a.AureiDeposited, 9);
                if (a.AureiDepositReturned > 0) { kept++; Assert.True(a.AureiDepositReturned >= 30); }   // with simple interest
                else lost++;                                                                         // the house failed or its head fled
                Assert.Equal(0, sim.World.DepositAurei, 9);
                Assert.Equal(a.AureiCarried + a.AureiDepositReturned, sim.World.Aurei, 9);
            }
            Assert.True(kept > 0);
            Assert.True(kept + lost == 30);
        }

        [Fact]
        public void BuriedGoldFollowsTheHoardsRules()
        {
            int found = 0, missing = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var sim = At(30, 0, seed);
                Assert.True(sim.Bury(20).Ok);
                var a = sim.JumpForTests();
                if (a.AureiHoardFound > 0) { found++; Assert.Equal(20, a.AureiHoardFound, 9); }   // no interest
                else missing++;                                                              // someone dug it up
                Assert.Equal(0, sim.World.HoardAurei, 9);
            }
            Assert.True(found > 0);
            Assert.Equal(30, found + missing);
        }

        [Fact]
        public void DenariiInHandStayBehind()
        {
            var sim = At(5, 123);
            sim.JumpForTests();
            var left = sim.Log.Events.Single(e => e.Type == "jump.coin.left");
            Assert.Equal(123, left.Effects.Single(f => f.Key == "gold").Before, 6);
            Assert.Equal(5, sim.World.Aurei, 9);                                       // the aurei were carried; the coin wasn't
        }

        [Fact]
        public void InstitutionalHoldingsAreNotPersonalWealth()
        {
            var sim = At(5, 0);
            sim.GrantStake("circle", 0.5);
            var circle = sim.World.Institution("circle");
            circle.Holdings = 200;
            Assert.Contains(sim.DepartureBriefing(), l => l.Contains("Money held by") && l.Contains("isn't part of your purse"));
            var a = sim.JumpForTests();
            Assert.Equal(5, a.AureiCarried, 9);
            Assert.Equal(5, sim.World.Aurei, 9);                                       // none of the circle's money came with you
            Assert.True(sim.World.Gold < 1);
            Assert.DoesNotContain(sim.Log.Events, e => e.Type.StartsWith("jump") && e.Effects.Any(f => f.Key == "gold" && f.After > f.Before));
        }

        [Fact]
        public void TheBriefingSaysWhatHappensToEachKindOfMoney()
        {
            var sim = At(40, 50);
            Assert.True(sim.Deposit(10).Ok);
            Assert.True(sim.Bury(5).Ok);
            var lines = sim.DepartureBriefing().ToList();
            Assert.Contains(lines, l => l.StartsWith("Your gold: the machine can carry"));         // carry
            Assert.Contains(lines, l => l.Contains("Too much to carry") && l.Contains("deposit <n>") && l.Contains("bury <n>") && l.Contains("stays behind and is lost"));
            Assert.Contains(lines, l => l.Contains("With the banking house"));                    // deposit (risk band, never the outcome)
            Assert.Contains(lines, l => l.StartsWith("  Buried: "));                              // bury
            Assert.Contains(lines, l => l.Contains("Coin in hand") && l.Contains("stay behind") && l.Contains("exchange"));   // denarii
            int first = lines.FindIndex(l => l.StartsWith("Your gold:"));
            Assert.True(lines.Skip(first).Take(5).Count(l => l.StartsWith("  ")) >= 4);           // one block, not scattered
            Assert.DoesNotContain(lines, l => l.Contains("endow"));                               // only offered to those who can
        }
    }
}
