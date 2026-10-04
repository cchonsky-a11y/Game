using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The approved R-17 thread: the crash log's handshake, LINK QUALITY: DEGRADED, REMOTE REFERENCE ACKNOWLEDGED, R-17 ACTIVE,
    /// and then, only after the inventor reopens the channel, REQUEST RECEIVED / SOURCE: R-17 / DO NOT JUMP. Never explained,
    /// never a lock on the jump.
    /// </summary>
    [Collection("Console")]
    public class P1MysteryTests
    {
        private static Simulation Repaired(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 20000;
            sim.MarkAssessedForTests();
            for (int guard = 0; guard < 120 && sim.MachineStepsDone < sim.MachineStepsTotal; guard++)
            {
                foreach (var system in Simulation.MachineSystems) sim.Repair(system);
                sim.EndMonth();
            }
            Assert.Equal(sim.MachineStepsTotal, sim.MachineStepsDone);
            for (int m = 0; m < 48 && !sim.World.ScenesSeen.Contains("r17-active"); m++) sim.EndMonth();
            return sim;
        }

        private static bool LogSays(Simulation sim, string s) => sim.Log.Events.Any(e => e.Text.Contains(s));

        [Fact]
        public void TheSequenceUnfoldsWithTheMachine()
        {
            var sim = Repaired();
            var order = sim.World.ScenesSeen.Where(id => id.StartsWith("r17-")).ToList();
            Assert.Equal(new[] { "r17-request", "r17-link", "r17-acknowledged", "r17-active" }, order.Where(id => id != "r17-mill"));
            foreach (var line in new[] { "REFERENCE REQUEST ACCEPTED — R-17", "LINK QUALITY: DEGRADED", "REMOTE REFERENCE ACKNOWLEDGED", "R-17 ACTIVE" })
                Assert.True(LogSays(sim, line), line);
            Assert.False(LogSays(sim, "DO NOT JUMP"));                     // not until the channel is reopened
        }

        [Fact]
        public void TheWarningComesOnlyAfterTheChannelIsReopened()
        {
            var fresh = new Simulation(TestData.Load(), 42);
            Assert.False(fresh.CanListen);
            Assert.False(fresh.Listen().Ok);

            var sim = Repaired();
            for (int m = 0; m < 6; m++) sim.EndMonth();
            Assert.False(LogSays(sim, "DO NOT JUMP"));                     // listening is the player's choice
            Assert.True(sim.CanListen);
            Assert.True(sim.Listen().Ok);
            Assert.False(sim.CanListen);
            for (int m = 0; m < 6 && sim.PendingEvent?.Id != "r17-warning"; m++) sim.EndMonth();
            Assert.Equal("r17-warning", sim.PendingEvent?.Id);
            var offer = sim.Log.Events.Last(e => e.Type == "event.offer").Text;
            Assert.Contains("REQUEST RECEIVED. SOURCE: R-17", offer);
            Assert.Contains("DO NOT JUMP", offer);
            Assert.Contains("nearly the same interval", offer);           // compared with the AD 155 handshake, not explained
        }

        [Fact]
        public void TheWarningNeverLocksTheJump()
        {
            foreach (var answer in new[] { "shutdown", "listen", "carryon" })
            {
                var sim = Repaired();
                sim.RestoreGold(sim.MachineGoldNeeded);
                sim.World.Aurei = 100;
                sim.RestoreGold(sim.MachineGoldNeeded);
                Assert.True(sim.Listen().Ok);
                for (int m = 0; m < 6 && sim.PendingEvent?.Id != "r17-warning"; m++) sim.EndMonth();
                Assert.True(sim.Decide(answer).Ok);
                Assert.Contains("r17-warned", sim.World.Flags);
                Assert.True(sim.MachineReady, answer);
                Assert.Contains(sim.DepartureBriefing(), l => l.Contains("DO NOT JUMP"));
                var arrival = sim.Jump();                                  // allowed
                Assert.True(sim.Arrived);
                Assert.True(arrival.ArrivalYear > arrival.DepartureYear);
            }
        }

        [Fact]
        public void TheNotebookPointsAtReciprocalLockAndNothingMore()
        {
            var sim = Repaired();
            Assert.True(sim.Listen().Ok);
            for (int m = 0; m < 6 && sim.PendingEvent?.Id != "r17-warning"; m++) sim.EndMonth();
            Assert.True(sim.Decide("carryon").Ok);
            for (int m = 0; m < 24 && !sim.World.ScenesSeen.Contains("r17-notebook"); m++) sim.EndMonth();
            Assert.Contains("r17-notebook", sim.World.ScenesSeen);
            var text = sim.Log.Events.Last(e => e.Target == "scene:r17-notebook").Text;
            Assert.Contains("reciprocal lock", text);
            Assert.DoesNotContain("because", text);                         // no explanation
        }

        [Fact]
        public void TheThreadReplaysExactly()
        {
            string Run()
            {
                var sim = Repaired(7);
                sim.Listen();
                for (int m = 0; m < 4; m++) sim.EndMonth();
                if (sim.PendingEvent?.Id == "r17-warning") sim.Decide("listen");
                for (int m = 0; m < 12; m++) sim.EndMonth();
                return sim.Log.Hash();
            }
            Assert.Equal(Run(), Run());
        }
    }
}
