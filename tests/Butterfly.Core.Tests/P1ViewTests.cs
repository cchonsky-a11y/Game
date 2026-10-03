using System;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>P1 menu sections (decided 2026-10-02): eight views, each split into active, available now, blocked, emerging and archived.</summary>
    [Collection("Console")]
    public class P1ViewTests
    {
        private static Simulation AfterThePump()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            Assert.Contains(sim.ViewOf(MenuSection.Projects).Active, i => i.Label.Contains("Cassianus"));
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Working) sim.EndMonth();
            return sim;
        }

        [Fact]
        public void EverySectionHasAViewFromTheFirstMonth()
        {
            var sim = new Simulation(TestData.Load(), 42);
            foreach (MenuSection s in Enum.GetValues(typeof(MenuSection))) Assert.Equal(s, sim.ViewOf(s).Section);
            Assert.Contains(sim.ViewOf(MenuSection.Now).AvailableNow, i => i.Command == "end");
            Assert.Contains(sim.ViewOf(MenuSection.Institutions).Blocked, i => i.Label.Contains("by invitation only"));
            Assert.Contains(sim.ViewOf(MenuSection.Machine).AvailableNow, i => i.Command == "assess");
            Assert.Empty(sim.ViewOf(MenuSection.People).Active);
        }

        [Fact]
        public void TheWorkMovesThroughTheViews()
        {
            var sim = AfterThePump();
            Assert.Contains(sim.ViewOf(MenuSection.Projects).Archived, i => i.Label.Contains("(done)"));
            Assert.Contains(sim.ViewOf(MenuSection.Institutions).Emerging, i => i.Label.Contains("Ostia"));
            Assert.Contains(sim.ViewOf(MenuSection.People).Active, i => i.Label.StartsWith("Felix"));
            var civ = sim.ViewOf(MenuSection.Civilization);
            Assert.Contains(civ.Active, i => i.Label.StartsWith("True valve seats"));
            // The next step beside it shows its bottleneck, and what it still needs (knowing is not making).
            Assert.Contains(civ.Emerging, i => i.Label.StartsWith("The hydraulic press") && i.Label.Contains("recipes for bronze"));
        }

        [Fact]
        public void AReadyMachineSaysSoWithoutPushing()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.MachineDone.AddRange(sim.Data.Content.MachineSteps.Select(s => s.Id));
            if (sim.Data.Content.MachineAssessment != null) sim.World.MachineDone.Add(sim.Data.Content.MachineAssessment.Id);
            sim.World.MachineGoldRestored = sim.MachineGoldNeeded;
            Assert.True(sim.MachineReady);
            Assert.Contains(sim.ViewOf(MenuSection.Machine).AvailableNow,
                i => i.Label == "The machine is ready. You can leave now, or remain in Rome and continue your work." && i.Command == "jump");
        }

        [Fact]
        public void TheConsoleShowsTheSections()
        {
            var sim = AfterThePump();
            string output = ConsoleTests.Play(sim, "view", "view people", "view civilization");
            foreach (var s in new[] { "now (", "projects (", "people (", "institutions (", "knowledge (", "civilization (", "machine (", "journal (" })
                Assert.Contains(s, output);
            Assert.Contains("== People", output);
            Assert.Contains("Felix, a freedman", output);
            Assert.Contains("Emerging:", output);
        }
    }
}
