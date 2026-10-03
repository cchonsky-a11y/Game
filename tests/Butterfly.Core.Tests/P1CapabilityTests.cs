using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 capability network (decided 2026-10-02): Rome's own baseline, the bottleneck and the inventor's leap for each node;
    /// a capability can't pass a level its prerequisites haven't reached, up to reproducible (PROPOSED P1-07).
    /// </summary>
    public class P1CapabilityTests
    {
        [Fact]
        public void EveryNodeStartsFromRomesBaselineWithAKnownLeap()
        {
            var caps = TestData.Load().Content.Capabilities;
            Assert.True(caps.Count >= 12);
            Assert.Equal(caps.Count, caps.Select(c => c.Id).Distinct().Count());
            foreach (var c in caps)
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Baseline), c.Id);
                Assert.False(string.IsNullOrWhiteSpace(c.Bottleneck), c.Id);
                Assert.Contains(c.Leap, CapabilityDef.Leaps);
                Assert.NotEqual("invent", c.Leap);
                foreach (var p in c.Prerequisites) Assert.Contains(caps, x => x.Id == p);
            }
        }

        [Fact]
        public void ThePrerequisitesHaveNoCycles()
        {
            var caps = TestData.Load().Content.Capabilities.ToDictionary(c => c.Id);
            var done = new HashSet<string>();
            void Visit(string id, HashSet<string> path)
            {
                if (done.Contains(id)) return;
                Assert.True(path.Add(id), "cycle through " + id);
                foreach (var p in caps[id].Prerequisites) Visit(p, path);
                path.Remove(id);
                done.Add(id);
            }
            foreach (var id in caps.Keys) Visit(id, new HashSet<string>());
        }

        [Fact]
        public void EverythingStartsAtNothingBeyondRomesBaseline()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.All(sim.World.Capabilities, c => Assert.Equal(CapabilityLevel.None, c.Level));
        }

        [Fact]
        public void KnowingIsNotMakingWithoutThePrerequisites()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.NotNull(sim.CapabilityBlocker("gauges", CapabilityLevel.Reproducible));
            Assert.Contains("shared measures", sim.CapabilityBlocker("gauges", CapabilityLevel.Theory));
            Assert.False(sim.AdvanceCapability("gauges", CapabilityLevel.Prototype, null, "test"));
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Prototype, null, "test"));
            Assert.Null(sim.CapabilityBlocker("gauges", CapabilityLevel.Prototype));
            Assert.NotNull(sim.CapabilityBlocker("gauges", CapabilityLevel.Reproducible));   // a prerequisite must be as far along
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Adopted, null, "test"));
            Assert.Null(sim.CapabilityBlocker("gauges", CapabilityLevel.Adopted));            // beyond reproducible, only reproducible is needed
            Assert.Contains(sim.Log.Events, e => e.Type == "capability.advance" && e.Target == "capability.metrology");
        }

        [Fact]
        public void TheCellarPumpTakesValveSeatsToReproducible()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            Assert.Equal(CapabilityLevel.None, sim.CapabilityLevelOf("valveseats"));
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Working) sim.EndMonth();
            Assert.True(sim.CapabilityLevelOf("valveseats") >= CapabilityLevel.Reproducible);   // others may copy it on (P1 people)
            var advances = sim.Log.Events.Where(e => e.Type == "capability.advance" && e.Actors.Contains("player")).ToList();
            Assert.Equal(2, advances.Count);                                    // prototype, then reproducible; never twice
            Assert.All(advances, e => Assert.NotEmpty(e.ImmediateCauses));      // each caused by a stage of the work
        }
    }
}
