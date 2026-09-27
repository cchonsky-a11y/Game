using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>The time machine repair track, joining benefits and costlier projects (decided 2026-09-28).</summary>
    public class MachineTests
    {
        private static Simulation Rich(ulong seed = 7)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 1000;
            sim.MarkAssessedForTests();
            return sim;
        }

        private static void FinishMachine(Simulation sim)
        {
            for (int guard = 0; guard < 60 && !sim.MachineReady; guard++)
            {
                foreach (var system in Simulation.MachineSystems) sim.Repair(system);
                if (sim.MachineStepsDone >= sim.MachineStepsTotal) sim.RestoreGold(sim.MachineGoldNeeded);
                sim.EndTurn();
            }
        }

        [Fact]
        public void NothingCanBeRepairedBeforeTheMachineIsAssessed()
        {
            var sim = new Simulation(TestData.Load(), 7);
            sim.World.Gold = 1000;
            Assert.False(sim.MachineAssessed);
            Assert.Null(sim.NextMachineStep("coil"));
            Assert.False(sim.Repair("coil").Ok);
            Assert.False(sim.Upgrade("lens").Ok);
            Assert.Contains("Not yet assessed", sim.MachineStatus().First());
            var a = sim.Data.Content.MachineAssessment!;
            Assert.True(sim.Assess().Ok);
            Assert.False(sim.Assess().Ok);                                   // already under way
            for (int t = 0; t < a.Turns; t++) { Assert.False(sim.MachineAssessed); sim.EndTurn(); }
            Assert.True(sim.MachineAssessed);
            Assert.Contains(sim.Log.Events, e => e.Type == "machine.assessed" && e.ImmediateCauses.Count > 0);
            Assert.Equal("bronze", sim.NextMachineStep("coil")!.Id);
            Assert.True(sim.Repair("coil").Ok);
            Assert.Equal(sim.MachineStepsTotal, sim.Data.Content.MachineSteps.Count);   // the assessment isn't a repair step
        }

        [Fact]
        public void AllTheScavengedGoldMustGoBackAndItIsNotDebased()
        {
            var sim = Rich();
            Assert.Equal(sim.T.Get("gold.start"), sim.MachineGoldNeeded, 9);
            for (int guard = 0; guard < 60 && sim.MachineStepsDone < sim.MachineStepsTotal; guard++)
            {
                foreach (var system in Simulation.MachineSystems) sim.Repair(system);
                sim.EndTurn();
            }
            Assert.False(sim.MachineReady);                                   // repaired, but the gold is still out
            Assert.Throws<System.InvalidOperationException>(() => sim.Jump());
            sim.World.Aurei = 100;
            sim.World.PriceLevel = 2;                                         // prices don't change the machine's gold
            double denarii = sim.World.Gold;
            Assert.True(sim.RestoreGold(25).Ok);
            Assert.Equal(75, sim.World.Aurei, 9);                             // it takes aurei, not denarii
            Assert.Equal(denarii, sim.World.Gold, 9);
            Assert.False(sim.MachineReady);
            Assert.True(sim.RestoreGold(1000).Ok);                            // only what's missing is taken
            Assert.Equal(100 - sim.MachineGoldNeeded, sim.World.Aurei, 9);
            Assert.True(sim.MachineReady);
            Assert.False(sim.RestoreGold(1).Ok);
            Assert.Contains(sim.Log.Events, e => e.Type == "machine.gold");
        }

        [Fact]
        public void TheRepairTakesACoupleOfYears()
        {
            // Even with every system worked in parallel, the longest system takes at least two years (decided 2026-09-28).
            var steps = TestData.Load().Content.MachineSteps;
            int longest = Simulation.MachineSystems.Max(sys => steps.Where(s => s.System == sys).Sum(s => s.Turns));
            Assert.True(longest * TestData.Load().Tuning.GetInt("time.monthsPerTurn") >= 24);
        }

        [Fact]
        public void AnEarlyBareJumpGoes25To40Years()
        {
            var sim = Rich();
            Assert.Equal((25, 40), sim.JumpRange());
            var seen = new System.Collections.Generic.HashSet<int>();
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var s = Rich(seed);
                seen.Add(s.JumpForTests().JumpYears);
            }
            Assert.Contains(25, seen);                        // an early, bare machine can fall short
            Assert.All(seen, y => Assert.InRange(y, 25, 40));
            Assert.All(seen, y => Assert.Equal(0, y % 5));
        }

        [Fact]
        public void UpgradesAndTimeInTheEraLengthenTheJump()
        {
            var sim = Rich();
            foreach (var u in sim.Data.Content.MachineUpgrades) sim.World.MachineDone.Add(u.Id);
            Assert.Equal((40, 55), sim.JumpRange());                // three upgrades: +15
            while (sim.Now.Year < 170) sim.EndTurn();                 // 15 years in the era: +10
            Assert.Equal((50, 60), sim.JumpRange());                // capped at 60
        }

        [Fact]
        public void UpgradesAreOptionalAndNeedRome()
        {
            var sim = Rich();
            sim.World.Attention = 100;
            Assert.True(sim.Upgrade("flywheel").Ok);
            Assert.False(sim.Upgrade("flywheel").Ok);
            Assert.False(sim.Upgrade("warpdrive").Ok);
            var lens = sim.Data.Content.MachineUpgrades.First(u => u.Id == "lens");
            Assert.Equal(lens.AltGold, sim.MachineStepGold(lens)); // no medicine work yet: the glassblower's price
            FinishMachine(sim);
            Assert.True(sim.MachineReady);                         // the 9 required steps are enough to jump
        }

        [Fact]
        public void AHalfDecadeJumpScalesTheAbsence()
        {
            var a = Rich(3);
            var arrival = a.JumpForTests();
            Assert.InRange(arrival.ArrivalYear - arrival.DepartureYear, 25, 40);
        }

        [Fact]
        public void NineStepsInThreeSystems()
        {
            var steps = TestData.Load().Content.MachineSteps;
            Assert.Equal(9, steps.Count);
            foreach (var system in Simulation.MachineSystems) Assert.Equal(3, steps.Count(s => s.System == system));
        }

        [Fact]
        public void TheMachineCantJumpUntilAllNineStepsAreDone()
        {
            var sim = Rich();
            Assert.Throws<System.InvalidOperationException>(() => sim.Jump());
            FinishMachine(sim);
            Assert.True(sim.MachineReady);
            var arrival = sim.Jump();
            Assert.Equal(arrival.DepartureYear + arrival.JumpYears, arrival.ArrivalYear);
        }

        [Fact]
        public void StepsGoInOrderAndOnePerSystemAtATime()
        {
            var sim = Rich();
            Assert.Equal("bronze", sim.NextMachineStep("coil")!.Id);
            Assert.True(sim.Repair("coil").Ok);
            Assert.False(sim.Repair("coil").Ok);  // already under way
            for (int t = 0; t < sim.Data.Content.MachineSteps.First(m => m.Id == "bronze").Turns; t++) sim.EndTurn();
            Assert.Equal("casting", sim.NextMachineStep("coil")!.Id);
            var done = sim.Log.Events.Single(e => e.Type == "machine.step");
            Assert.NotEmpty(done.ImmediateCauses);
        }

        [Fact]
        public void RomeMakesStepsCheaper()
        {
            var sim = Rich();
            var bronze = sim.NextMachineStep("coil")!;
            Assert.Equal(bronze.AltGold, sim.MachineStepGold(bronze)); // no trade contacts: the open market
            sim.World.CompletedProjects.Add("workshop");
            sim.World.Attention = 100;
            Assert.True(sim.Buy("guild", 1).Ok);
            Assert.Equal(bronze.Gold, sim.MachineStepGold(bronze));
        }

        [Fact]
        public void MultiTurnStepsReserveAttention()
        {
            var sim = Rich();
            sim.World.Attention = 100;
            Assert.True(sim.Repair("coil").Ok);       // bronze: 1 Attention a turn for more than one turn
            sim.EndTurn();
            Assert.Equal(sim.AttentionPerTurn - sim.Data.Content.MachineSteps.First(m => m.Id == "bronze").AttentionPerTurn, sim.World.Attention);
        }

        [Fact]
        public void AVoiceMakesAnInstitutionPayPartOfAProject()
        {
            var sim = Rich();
            var market = sim.Data.Content.Project("market")!;
            Assert.Equal(market.Gold, sim.ProjectGold(market));
            sim.GrantStake("guild", 0.25);
            Assert.Equal((int)System.Math.Round(market.Gold * (1 - sim.T.Get("stakes.voiceProjectShare"))), sim.ProjectGold(market));
            double gold = sim.World.Gold;
            Assert.True(sim.StartProject("market").Ok);
            Assert.Equal(sim.ProjectGold(market), gold - sim.World.Gold, 6);
        }

        [Fact]
        public void MembershipsRaiseWorkPay()
        {
            var sim = Rich();
            double plain = sim.WorkPay("craft");
            sim.GrantStake("guild", 0.01);
            sim.GrantStake("faction", 0.01);
            Assert.Equal(plain * (1 + 2 * sim.T.Get("joining.workBonusPerMembership")), sim.WorkPay("craft"), 6);
            sim.Found("school");
            Assert.Equal(2, sim.Memberships()); // your own institutions aren't memberships
        }
    }
}

namespace Butterfly.Core.Tests
{
    /// <summary>Inventions (decided 2026-09-28, first version).</summary>
    public class InventionTests
    {
        private static Simulation Rich()
        {
            var sim = new Simulation(TestData.Load(), 9);
            sim.World.Gold = 1000;
            sim.World.Attention = 100;
            return sim;
        }

        private static void Finish(Simulation sim, string id)
        {
            Xunit.Assert.True(sim.Invent(id).Ok);
            for (int t = 0; t < 10 && !sim.World.Invented.Contains(id); t++) { sim.EndTurn(); sim.World.Attention = 100; }
            Xunit.Assert.Contains(id, sim.World.Invented);
        }

        [Xunit.Fact]
        public void KnowingIsNotMaking()
        {
            var sim = Rich();
            Xunit.Assert.False(sim.Invent("wheelbarrow").Ok); // no workshop
            sim.World.CompletedProjects.Add("workshop");
            Xunit.Assert.True(sim.Invent("wheelbarrow").Ok);
            Xunit.Assert.False(sim.Invent("wheelbarrow").Ok); // once
        }

        [Xunit.Fact]
        public void TheWheelbarrowPaysIncome()
        {
            var sim = Rich();
            sim.World.CompletedProjects.Add("workshop");
            double before = sim.YearlyIncome();
            Finish(sim, "wheelbarrow");
            Xunit.Assert.True(sim.YearlyIncome() >= before + 3 - 1e-9);
        }

        [Xunit.Fact]
        public void BookkeepingRaisesStandingInfluenceAndConsultingPay()
        {
            var sim = Rich();
            sim.GrantStake("guild", 0.02);
            var guild = sim.World.Institution("guild");
            double loyalty = guild.Loyalty, pay = sim.WorkPay("consult");
            Finish(sim, "bookkeeping");
            Xunit.Assert.Equal(5, sim.StakePercent(guild));
            Xunit.Assert.True(guild.Loyalty > loyalty);
            Xunit.Assert.True(sim.WorkPay("consult") > pay);
            var stake = System.Linq.Enumerable.Single(sim.Log.Events, e => e.Type == "institution.stake");
            Xunit.Assert.NotEmpty(stake.ImmediateCauses);
        }
    
        [Fact]
        public void PricesRiseWithTheDebasedCoin()
        {
            var history = Rich();
            var market = history.Data.Content.Project("market")!;
            int before = history.ProjectGold(market);
            while (history.Now.Year < 157) history.EndTurn();                   // two years of Rome's own slow debasement
            Assert.True(history.ProjectGold(market) > before);
            Assert.True(history.World.PriceLevel > 1.02 && history.World.PriceLevel < 1.04);
            Assert.Contains(history.Log.Events, e => e.Type == "prices.rise");

            var debased = Rich();
            debased.GrantStake("faction", 0.5);
            debased.World.Institution("faction").Strength = 100;             // full sway over Governance
            debased.World.Attention = 100;
            Assert.True(debased.SetPolicy(PolicyIssue.Coinage, -1).Ok);
            while (debased.Now.Year < 157) debased.EndTurn();
            Assert.True(debased.World.PriceLevel > history.World.PriceLevel);

            var sound = Rich();
            sound.GrantStake("faction", 0.5);
            sound.World.Institution("faction").Strength = 100;
            sound.World.Attention = 100;
            Assert.True(sound.SetPolicy(PolicyIssue.Coinage, 1).Ok);
            while (sound.Now.Year < 157) sound.EndTurn();
            Assert.True(sound.World.PriceLevel < history.World.PriceLevel);
        }

        [Fact]
        public void PayCatchesUpWithPricesOnlyPartly()
        {
            var sim = Rich();
            double pay = sim.WorkPay("craft");
            sim.World.PriceLevel = 1.2;
            Assert.Equal(pay * (1 + 0.2 * sim.T.Get("prices.wageCatchUp")), sim.WorkPay("craft"), 6);
            Assert.True(sim.WorkPay("craft") / pay < 1.2);
        }
    }
}

namespace Butterfly.Core.Tests
{
    /// <summary>The invention tree: three branches of three tiers (decided 2026-09-28).</summary>
    public class InventionTreeTests
    {
        [Fact]
        public void FourBranchesOfThreeWithEachTierNeedingTheOneBefore()
        {
            var inventions = TestData.Load().Content.Inventions;
            Assert.Equal(12, inventions.Count);
            foreach (var branch in Simulation.InventionBranches)
            {
                var chain = inventions.Where(i => i.Branch == branch).ToList();
                Assert.Equal(3, chain.Count);
                Assert.Null(chain[0].Prerequisite);
                Assert.Equal(chain[0].Id, chain[1].Prerequisite);
                Assert.Equal(chain[1].Id, chain[2].Prerequisite);
                Assert.True(chain[2].Gold > chain[1].Gold && chain[1].Gold > chain[0].Gold);   // each tier costs more
            }
            Assert.Empty(ContentChecks.Check(TestData.Load().Content).Where(p => p.StartsWith("invention")));
        }

        [Fact]
        public void EveryEstablishedInstitutionCanGainStandingAndStakesVary()
        {
            var sim = new Simulation(TestData.Load(), 1);
            var groups = new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "trade", new[] { "guild", "bank" } }, { "medicine", new[] { "circle", "sanctuary" } }, { "faction", new[] { "faction", "junian" } },
                { "guild", new[] { "guild" } }, { "junian", new[] { "junian" } }, { "bank", new[] { "bank" } },
                { "circle", new[] { "circle" } }, { "sanctuary", new[] { "sanctuary" } },
            };
            var covered = sim.Data.Content.Inventions.SelectMany(i => i.Effects).Where(e => e.Type == "stake").SelectMany(e => groups[e.Group!]).ToHashSet();
            foreach (var id in new[] { "circle", "sanctuary", "faction", "junian", "guild", "bank" }) Assert.Contains(id, covered);
            var stakes = sim.Data.Content.Inventions.SelectMany(i => i.Effects).Where(e => e.Type == "stake").Select(e => e.Value).Distinct().Count();
            var loyalties = sim.Data.Content.Inventions.SelectMany(i => i.Effects).Where(e => e.Type == "loyalty").Select(e => e.Value).Distinct().Count();
            Assert.True(stakes >= 4 && loyalties >= 4);   // standing varies by invention
            Assert.Contains("stake", sim.InventionPayoffText(sim.InventionById("bills")!));
        }

        [Fact]
        public void WorkshopInventionsRaiseTheWorkshopsIncomeModestly()
        {
            var sim = new Simulation(TestData.Load(), 9);
            sim.World.Gold = 1000;
            sim.World.Attention = 100;
            sim.World.CompletedProjects.Add("workshop");
            sim.World.IncomeBonus += sim.WorkshopRate();
            double before = sim.OwnedIncome();
            Assert.True(sim.Invent("lathe").Ok);
            for (int t = 0; t < sim.InventionById("lathe")!.Turns; t++) sim.EndTurn();
            Assert.Equal(before * 1.10, sim.OwnedIncome(), 6);
            // All three together add less than half again to the workshop.
            Assert.True(sim.Data.Content.Inventions.Where(i => i.Branch == "workshop").SelectMany(i => i.Effects).Where(e => e.Type == "workshop").Sum(e => e.Value) < 0.5);
        }

        [Fact]
        public void ALockedInventionShowsWhatItNeedsAndCantBeStarted()
        {
            var sim = new Simulation(TestData.Load(), 9);
            sim.World.Gold = 1000;
            sim.World.Attention = 100;
            sim.World.CompletedProjects.Add("fountain");
            sim.GrantStake("sanctuary", 0.01);
            var spirits = sim.InventionById("spirits")!;
            Assert.StartsWith("locked: first make Soap", sim.InventionState(spirits));
            Assert.False(sim.Invent("spirits").Ok);
            Assert.Equal("ready", sim.InventionState(sim.InventionById("soap")!));
            Assert.True(sim.Invent("soap").Ok);
            for (int t = 0; t < sim.InventionById("soap")!.Turns; t++) sim.EndTurn();
            sim.World.Attention = 100;
            Assert.Equal("ready", sim.InventionState(spirits));
            Assert.True(sim.Invent("spirits").Ok);
            // The top tier needs both its predecessor and more from Rome (10% of a Medicine house).
            Assert.StartsWith("locked", sim.InventionState(sim.InventionById("ward")!));
        }
    }
}

namespace Butterfly.Core.Tests
{
    /// <summary>Turn length: 2-month turns by default, and the player may change it (decided 2026-09-28; SYSTEMS §2).</summary>
    public class TurnLengthTests
    {
        [Fact]
        public void TurnsAreTwoMonthsAndThePlayerCanChangeThemUpToTheCap()
        {
            var sim = new Simulation(TestData.Load(), 3);
            Assert.Equal(2, sim.MonthsPerTurn);
            sim.EndTurn();
            Assert.Equal(2, sim.Now.Month);
            Assert.False(sim.SetTurnLength(4).Ok);            // never beyond the Stage 3 cap
            Assert.False(sim.SetTurnLength(0).Ok);
            Assert.True(sim.SetTurnLength(3).Ok);
            Assert.Contains(sim.Log.Events, e => e.Type == "time.turnLength");
            sim.EndTurn();
            Assert.Equal(5, sim.Now.Month);
            Assert.True(sim.SetTurnLength(1).Ok);
            sim.EndTurn();
            Assert.Equal(6, sim.Now.Month);
            Assert.Equal(4, sim.World.Attention);             // Attention stays 4 a turn
        }

        [Fact]
        public void TheEraEndsByTheCalendarAndThePlagueByItsDatesWhateverTheTurnLength()
        {
            foreach (int months in new[] { 1, 2, 3 })
            {
                var sim = new Simulation(TestData.Load(), 3);
                if (months != sim.MonthsPerTurn) Assert.True(sim.SetTurnLength(months).Ok);
                while (!sim.EraOver)
                {
                    if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                    sim.EndTurn();
                }
                Assert.Equal(175, sim.Now.Year);
                Assert.Equal(0, sim.Now.Month);
                var outbreak = sim.Log.Events.Single(e => e.Type == "plague.outbreak");
                // The outbreak shows on the turn that covers October 166.
                Assert.Equal(166, outbreak.Time.Year);
                Assert.InRange(outbreak.Time.Month, 9 - months + 1, 9);
            }
        }
    }
}

namespace Butterfly.Core.Tests
{
    /// <summary>Denarii and gold aurei (decided 2026-09-28).</summary>
    public class CurrencyTests
    {
        [Fact]
        public void YouArriveWithGoldAndMustChangeItToSpend()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.Equal(sim.T.Get("gold.start"), sim.World.Aurei, 9);
            Assert.Equal(0, sim.World.Gold, 9);
            Assert.False(sim.StartProject("fountain").Ok);                   // no denarii yet
            Assert.True(sim.SellAurei(10).Ok);
            Assert.Equal(10 * (1 - sim.T.Get("currency.exchangeFee")), sim.World.Gold, 9);
            Assert.Equal(sim.AttentionPerTurn - sim.T.GetInt("currency.exchangeAttention"), sim.World.Attention);
            Assert.Equal("250 denarii", sim.Money(10));                      // 1 aureus = 25 denarii in AD 155
        }

        [Fact]
        public void GoldHoldsItsValueWhileTheDenariusIsDebased()
        {
            var sim = new Simulation(TestData.Load(), 1);
            double before = sim.AureusInDenarii;
            while (sim.Now.Year < 165) sim.EndTurn();
            Assert.True(sim.World.PriceLevel > 1);
            Assert.Equal(before * sim.World.PriceLevel, sim.AureusInDenarii, 6);   // an aureus buys what it bought
            sim.World.Gold = 1000;
            sim.World.Attention = 4;
            double aurei = sim.World.Aurei;
            Assert.True(sim.BuyAurei(5).Ok);
            Assert.Equal(aurei + 5, sim.World.Aurei, 9);
            Assert.Equal(1000 - 5 * sim.World.PriceLevel * (1 + sim.T.Get("currency.exchangeFee")), sim.World.Gold, 6);
        }

        [Fact]
        public void TheHourOneChoiceIsPaidInGold()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.True(sim.ChooseSeeded("workshop").Ok);
            Assert.Equal(sim.T.Get("gold.start") - sim.ProjectGold(sim.Data.Content.Project("workshop")!), sim.World.Aurei, 9);
        }
    }
}

namespace Butterfly.Core.Tests
{
    /// <summary>Gold across the jump (decided 2026-09-28).</summary>
    public class SavingsTests
    {
        private static Simulation Ready(ulong seed)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Aurei = 100;
            sim.World.Attention = 4;
            return sim;
        }

        [Fact]
        public void TheMachineCarriesASmallPurseAndTheRestIsLost()
        {
            var sim = Ready(1);
            var a = sim.JumpForTests();
            Assert.Equal(sim.CarryAurei, a.AureiCarried, 9);
            Assert.Equal(100 - sim.CarryAurei, a.AureiLeft, 9);
            Assert.Equal(sim.CarryAurei, sim.World.Aurei, 9);
            Assert.Contains("long gone", a.Beats.Single(b => b.Name == "Discovery").Text);
        }

        [Fact]
        public void ADepositEarnsInterestUnlessTheHouseFailsAndAHoardMayBeFound()
        {
            int kept = 0, lost = 0, found = 0, missing = 0;
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var sim = Ready(seed);
                Assert.True(sim.Deposit(40).Ok);
                Assert.True(sim.Bury(40).Ok);
                Assert.Equal(20, sim.World.Aurei, 9);
                var a = sim.JumpForTests();
                if (a.AureiDepositReturned > 0) { kept++; Assert.True(a.AureiDepositReturned > 40); } else lost++;
                if (a.AureiHoardFound > 0) { found++; Assert.Equal(40, a.AureiHoardFound, 9); } else missing++;
                Assert.Equal(a.AureiCarried + a.AureiDepositReturned + a.AureiHoardFound, sim.World.Aurei, 9);
                Assert.DoesNotContain("{", a.Beats.Single(b => b.Name == "Discovery").Text);
            }
            Assert.True(kept > 0 && lost > 0 && found > 0 && missing > 0);   // both risks are real, and neither is certain
        }

        [Fact]
        public void TheBriefingShowsRiskBandsNeverOutcomes()
        {
            var sim = Ready(2);
            sim.Deposit(30);
            var lines = sim.DepartureBriefing().ToList();
            Assert.Contains(lines, l => l.StartsWith("Your gold: the machine can carry"));
            Assert.Contains(lines, l => l.Contains("risk the house fails or embezzles: "));
        }
    }
}

namespace Butterfly.Core.Tests
{
    /// <summary>Walking around Rome at every arrival, including the first (decided 2026-09-28).</summary>
    public class WalkTests
    {
        [Fact]
        public void EveryPlaceCanBeVisitedAtTheStartAndAfterAJump()
        {
            var sim = new Simulation(TestData.Load(), 3);
            foreach (var p in Simulation.WalkPlaces)
            {
                string text = sim.Visit(p);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("{", text);
            }
            Assert.Contains("25 denarii", sim.Visit("changers"));
            Assert.Contains("78% silver", sim.Visit("changers"));
            Assert.DoesNotContain("when you left", sim.Visit("market"));           // the first walk is about where you are, not what changed
            sim.ChooseSeeded("workshop");
            while (sim.Now.Year < 170) { if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); sim.EndTurn(); }
            sim.JumpForTests();
            foreach (var p in Simulation.WalkPlaces)
            {
                string text = sim.Visit(p);
                Assert.DoesNotContain("{", text);
            }
            Assert.Contains("when you left", sim.Visit("market"));
            Assert.Contains("when you left", sim.Visit("changers"));
            Assert.Contains("workshop you funded", sim.Visit("forges"));
            Assert.StartsWith("Visit where?", sim.Visit("moon"));
        }

        [Fact]
        public void PricesKeepRisingWhileYouAreAway()
        {
            var sim = new Simulation(TestData.Load(), 3);
            double before = sim.World.PriceLevel;
            var a = sim.JumpForTests();
            Assert.Equal(before * System.Math.Pow(1 + sim.T.Get("prices.inflationAsHistory"), a.JumpYears), sim.World.PriceLevel, 6);
            Assert.True(sim.CoinSilverNow() < 0.78);   // the coin followed history's debasement
        }
    }
}


namespace Butterfly.Core.Tests
{
    /// <summary>Option A: one more jump from the arrival (decided 2026-09-28).</summary>
    public class SecondJumpTests
    {
        [Fact]
        public void YouCanJumpOnceMoreFromTheArrivalAndThenNoMore()
        {
            var sim = new Simulation(TestData.Load(), 5);
            sim.ChooseSeeded("fountain");
            while (sim.Now.Year < 168) { if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); sim.EndTurn(); }
            var first = sim.JumpForTests();
            Assert.True(sim.CanJumpAgain);
            var (lo, hi) = sim.JumpRange();
            Assert.Equal((25, 40), (lo, hi));                  // no time in the new era yet, no upgrades
            string before = sim.Visit("market");
            var second = sim.JumpForTests();
            Assert.Equal(first.ArrivalYear, second.DepartureYear);
            Assert.InRange(second.JumpYears, 25, 40);
            Assert.Equal(second.DepartureYear + second.JumpYears, second.ArrivalYear);
            Assert.Equal(4, second.Beats.Count);
            Assert.All(second.Beats, b => Assert.DoesNotContain("{", b.Text));
            Assert.Contains("when you left", sim.Visit("market"));
            Assert.NotEqual(before, sim.Visit("market"));
            Assert.False(sim.CanJumpAgain);
            Assert.Throws<System.InvalidOperationException>(() => sim.JumpForTests());
        }
    }

    public class NewsTests
    {
        [Fact]
        public void NewsIsDatedInsideTheEraAndInOrder()
        {
            var data = TestData.Load();
            var news = data.Content.News;
            Assert.True(news.Count >= 15);
            for (int k = 0; k < news.Count; k++)
            {
                Assert.InRange(news[k].Month, 1, 12);
                Assert.InRange(news[k].Year, 155, 174);   // the era runs AD 155-175
                Assert.False(string.IsNullOrWhiteSpace(news[k].Text));
                if (k > 0) Assert.True(news[k - 1].Time.TotalMonths <= news[k].Time.TotalMonths);
            }
        }

        [Fact]
        public void ReadingTheNewsIsFreeAndChangesNothing()
        {
            var a = new Simulation(TestData.Load(), 7);
            var b = new Simulation(TestData.Load(), 7);
            a.ChooseSeeded("workshop");
            b.ChooseSeeded("workshop");
            for (int t = 0; t < 40; t++)
            {
                int attention = a.World.Attention;
                Assert.NotEmpty(a.News());
                Assert.Equal(attention, a.World.Attention);
                if (a.OutbreakAwaitingResponse) { a.RespondToPlague("none"); b.RespondToPlague("none"); }
                a.EndTurn();
                b.EndTurn();
            }
            Assert.Equal(b.Log.Hash(), a.Log.Hash());
        }

        [Fact]
        public void HistoryArrivesOnTheTurnThatCoversItsDate()
        {
            var sim = new Simulation(TestData.Load(), 3);
            sim.ChooseSeeded("workshop");
            Assert.Contains(sim.HistoryNewsThisTurn(), n => n.Text.Contains("Antoninus Pius has ruled"));
            while (sim.Now.TotalMonths + sim.MonthsPerTurn < SimTime.FromYear(161, 2).TotalMonths) sim.EndTurn();
            Assert.DoesNotContain(sim.HistoryNewsSoFar(), n => n.Text.Contains("dead at his villa"));
            sim.EndTurn();
            Assert.Contains(sim.HistoryNewsThisTurn(), n => n.Text.Contains("dead at his villa"));
            Assert.Contains(sim.News(), l => l.Contains("dead at his villa"));
        }

        [Fact]
        public void TheMarketLineShowsTheCoinAndAfterAJumpHistoryIsSilent()
        {
            var sim = new Simulation(TestData.Load(), 3);
            Assert.Contains(sim.News(), l => l.StartsWith("At the market") && l.Contains("25 denarii") && l.Contains("78% silver"));
            sim.ChooseSeeded("workshop");
            while (sim.Now.Year < 168) { if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none"); sim.EndTurn(); }
            Assert.Contains(sim.News(), l => l.Contains("pestilence", System.StringComparison.OrdinalIgnoreCase) || l.Contains("sickness", System.StringComparison.OrdinalIgnoreCase));
            sim.JumpForTests();
            Assert.Empty(sim.HistoryNewsSoFar());
            Assert.Contains(sim.News(), l => l.StartsWith("At the market"));
        }
    }
}
