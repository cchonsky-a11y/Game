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
            return sim;
        }

        private static void FinishMachine(Simulation sim)
        {
            for (int guard = 0; guard < 60 && !sim.MachineReady; guard++)
            {
                foreach (var system in Simulation.MachineSystems) sim.Repair(system);
                sim.EndTurn();
            }
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
    }
}
