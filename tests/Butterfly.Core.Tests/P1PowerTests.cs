using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 Powered Workshops (Corey, 2026-10-04): Rome already has water power, gears, bearings and lathes; the leap is to
    /// combine, standardize and scale. Six chunky stages, people who can derail them, and a human consequence at the end.
    /// </summary>
    [Collection("Console")]
    public class P1PowerTests
    {
        /// <summary>A guild member who knows Gaius, Aulus and Marcus, with shared measures and gauges reproducible.</summary>
        private static Simulation Ready(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 20000;
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));
            Assert.True(sim.AdvanceCapability("gauges", CapabilityLevel.Reproducible, null, "test"));
            var access = sim.World.AccessTo("guild");
            access.RecordMemberRelationship("Felix");
            var ok = new InstitutionInvitationContext("guild", "Felix", true, true, true, true);
            Assert.True(access.TryAcceptGuestInvitation(ok));
            Assert.True(access.TryAcceptGuestInvitation(ok));
            Assert.True(access.TryBecomeSponsoredCandidate(ok));
            Assert.True(access.AdmitMember("Felix"));
            sim.World.ScenesSeen.AddRange(new[] { "gaius-meet", "aulus-meet", "marcus-meet" });
            return sim;
        }

        private static void Stage(Simulation sim)
        {
            while (sim.World.Attention < 2 || sim.ReservedInMonth(1) > 1) sim.EndMonth();
            var r = sim.StartChallengeStage("power");
            Assert.True(r.Ok, r.Message);
            while (sim.FindChallenge("power")!.Status == ChallengeStatus.Working) sim.EndMonth();
        }

        [Fact]
        public void TheNodesStartFromRomesBaseline()
        {
            var c = TestData.Load().Content;
            foreach (var id in new[] { "bearings", "transmission", "poweredboring", "lineshaft" })
            {
                var n = c.Capabilities.First(x => x.Id == id);
                Assert.False(string.IsNullOrWhiteSpace(n.Baseline));
                Assert.NotEqual("invent", n.Leap);
            }
            var ch = c.Challenges.First(x => x.Id == "power");
            Assert.Equal(6, ch.Stages.Count);
            Assert.Equal("nerius", ch.Consequence);
        }

        [Fact]
        public void ItOpensOnlyWithGaugesAndAMillwright()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.DoesNotContain("challenge:power", sim.SceneCandidateIds());
            var ready = Ready();
            Assert.Contains("challenge:power", ready.SceneCandidateIds());
        }

        [Fact]
        public void SixStagesMakeAPoweredWorkshopAndThenSomeoneAsksAboutHisMen()
        {
            var sim = Ready();
            var c = sim.FindChallenge("power")!;
            c.Status = ChallengeStatus.Open;
            for (int i = 0; i < 6; i++) Stage(sim);
            Assert.Equal(ChallengeStatus.Done, c.Status);
            foreach (var id in new[] { "lineshaft", "bearings", "transmission" })
                Assert.Equal(CapabilityLevel.Reproducible, sim.CapabilityLevelOf(id));
            Assert.Equal(CapabilityLevel.Prototype, sim.CapabilityLevelOf("poweredboring"));
            Assert.Contains(sim.Log.Events, e => e.Type == "challenge.complete" && e.Text.Contains("Aulus's shaft"));
            for (int m = 0; m < 6 && sim.PendingEvent?.Id != "nerius"; m++) sim.EndMonth();
            Assert.Equal("nerius", sim.PendingEvent?.Id);
            Assert.True(sim.Decide("workers").Ok);
            Assert.Contains("two of his own men", sim.PersonOf("Sextus")!.Status);
            Assert.Equal(2, sim.PersonOf("Sextus")!.Regard);
            // The answer leaves a mark in Rome, shown on one arrival and never repeated word for word on the next.
            string mark = sim.Data.Content.Events.First(e => e.Id == "nerius").Options.First(o => o.Id == "workers").Mark!;
            var first = string.Join(" ", sim.JumpForTests().Beats.Select(b => b.Text));
            var second = string.Join(" ", sim.JumpForTests().Beats.Select(b => b.Text));
            Assert.Contains(mark, first);
            Assert.DoesNotContain(mark, second);
        }

        [Fact]
        public void MarcusLeavingSlowsTheEnduranceRun()
        {
            var sim = Ready();
            var endurance = sim.Data.Content.Challenges.First(x => x.Id == "power").Stages.First(s => s.Id == "endurance");
            Assert.Equal("Marcus", sim.StagePerson(endurance));
            Assert.Equal(3, sim.StageMonths(endurance));
            sim.PersonOf("Marcus")!.Gone = true;
            sim.World.ScenesSeen.Add("gaius-meet");
            Assert.Equal("Gaius", sim.StagePerson(endurance));
            Assert.Equal(5, sim.StageMonths(endurance));
            Assert.Contains("Without Marcus", sim.StageLine(endurance) + endurance.SlowerText);
        }

        [Fact]
        public void MarcusChoosesFromWhatHappenedNotFromAScript()
        {
            // Low regard: he may leave for Priscus; high regard (he kept the tally): he may stay. Never both.
            int left = 0, stayed = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var sim = Ready(seed);
                sim.FindChallenge("power")!.StageIndex = 3;
                if (seed % 2 == 0) sim.PersonOf("Marcus")!.Regard = 2;
                for (int m = 0; m < 24; m++) sim.EndMonth();
                bool l = sim.World.LifeEventLog.ContainsKey("marcus-priscus") || sim.World.ReadyLife.Contains("marcus-priscus");
                bool s = sim.World.LifeEventLog.ContainsKey("marcus-stays") || sim.World.ReadyLife.Contains("marcus-stays");
                Assert.False(l && s);
                if (l) { left++; Assert.True(seed % 2 == 1); }
                if (s) { stayed++; Assert.True(seed % 2 == 0); }
            }
            Assert.True(left > 0 && stayed > 0);
        }

        [Fact]
        public void GaiusClosesTheShopForHisSonsNamingDay()
        {
            var sim = Ready();
            var c = sim.FindChallenge("power")!;
            c.Status = ChallengeStatus.Open;
            sim.PersonOf("Gaius")!.Regard = 2;
            c.StageIndex = 1;                                           // the bearings: a two-month stage
            Assert.True(sim.StartChallengeStage("power").Ok);
            int left = c.MonthsLeft;
            var naming = sim.Data.Content.Scenes.First(s => s.Id == "gaius-naming-day");
            Assert.True(naming.Requires.All(r => sim.Holds(r)));
            sim.World.ScenesSeen.AddRange(sim.Data.Content.Scenes.Where(s => s.Id != "gaius-naming-day").Select(s => s.Id));
            sim.World.ScenePacing.Reset();
            foreach (var cm in sim.World.Commissions) cm.Status = CommissionStatus.Declined;   // no work offers in the way
            sim.EndMonth();                                             // the only candidate: the naming day
            Assert.Contains("gaius-naming-day", sim.World.ScenesSeen);
            Assert.Contains(sim.Log.Events, e => e.Type == "challenge.delayed");
            Assert.Equal(left, c.MonthsLeft);                           // one month passed, one month added
        }

        [Fact]
        public void TheMachineMysteryInterruptsTheArc()
        {
            var sim = Ready();
            sim.FindChallenge("power")!.Status = ChallengeStatus.Open;
            sim.World.ScenesSeen.Add("r17-link");
            Assert.Contains("scene:r17-mill", sim.SceneCandidateIds());
        }
    }
}
