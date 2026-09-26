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
            Assert.Equal(arrival.DepartureYear + sim.T.GetInt("jump.years"), arrival.ArrivalYear);
        }

        [Fact]
        public void StepsGoInOrderAndOnePerSystemAtATime()
        {
            var sim = Rich();
            Assert.Equal("bronze", sim.NextMachineStep("coil")!.Id);
            Assert.True(sim.Repair("coil").Ok);
            Assert.False(sim.Repair("coil").Ok);  // already under way
            sim.EndTurn();
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
            foreach (var id in new[] { "bronze", "casting" }) { sim.Repair("coil"); sim.EndTurn(); }
            // the casting (1 Attention a turn for 2 turns) is still under way this turn
            Assert.Equal(sim.AttentionPerTurn - 1, sim.World.Attention);
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
