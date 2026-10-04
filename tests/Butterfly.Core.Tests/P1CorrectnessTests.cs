using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 correctness pass (2026-10-04). A Grand Challenge is finished by its authored work, not by other work that already took
    /// its goal capability there; and authored work never finishes as if Rome had moved while the capability network refused it.
    /// </summary>
    [Collection("Console")]
    public class P1CorrectnessTests
    {
        /// <summary>A guild member who knows Gaius, Aulus and Marcus, with shared measures and gauges reproducible.</summary>
        private static Simulation PowerReady(ulong seed = 42)
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

        private static void Stage(Simulation sim, string id)
        {
            for (int m = 0; m < 24 && (sim.World.Attention < 2 || sim.ReservedInMonth(1) > 1); m++) sim.EndMonth();
            var r = sim.StartChallengeStage(id);
            Assert.True(r.Ok, r.Message);
            while (sim.FindChallenge(id)!.Status == ChallengeStatus.Working) sim.EndMonth();
        }

        private static int Count(Simulation sim, string type, string target) => sim.Log.Events.Count(e => e.Type == type && e.Target == target);

        [Fact]
        public void PoweredWorkshopsStillCompletesByItsSixStages()
        {
            var sim = PowerReady();
            var c = sim.FindChallenge("power")!;
            c.Status = ChallengeStatus.Open;
            for (int i = 0; i < 6; i++) Stage(sim, "power");
            Assert.Equal(ChallengeStatus.Done, c.Status);
            Assert.Equal(6, Count(sim, "challenge.stage", c.ProjectId));
            Assert.Equal(1, Count(sim, "challenge.complete", c.ProjectId));
            Assert.Equal(CapabilityLevel.Reproducible, sim.CapabilityLevelOf("lineshaft"));
        }

        [Fact]
        public void TheSluiceFirstDoesNotFinishOrShortenPoweredWorkshops()
        {
            // The Janiculum race (the sluice commission) takes line shafts to reproducible before the challenge begins: the old rule
            // (goal reached OR stages done) finished the challenge after its first stage.
            var sim = PowerReady();
            Assert.True(sim.AdvanceCapability("lineshaft", CapabilityLevel.Reproducible, null, "the sluice"));
            sim.FindCommission("sluice")!.Status = CommissionStatus.Done;
            var c = sim.FindChallenge("power")!;
            c.Status = ChallengeStatus.Open;
            var stages = sim.Data.Content.Challenges.First(x => x.Id == "power").Stages;
            for (int i = 0; i < 5; i++)
            {
                Stage(sim, "power");
                Assert.Equal(ChallengeStatus.Open, c.Status);                         // not done, whatever line shafts stand at
                Assert.Equal(i + 1, c.StageIndex);
                Assert.Equal(0, Count(sim, "challenge.complete", c.ProjectId));
            }
            Assert.Equal("endurance", sim.NextStage(c)!.Id);                           // the endurance run is still required
            Stage(sim, "power");
            Assert.Equal(ChallengeStatus.Done, c.Status);
            Assert.Equal(stages.Count, Count(sim, "challenge.stage", c.ProjectId));      // every authored stage ran
            Assert.Contains(sim.Log.Events, e => e.Type == "challenge.stage" && e.Text == stages.Last().Text);
            for (int m = 0; m < 6; m++) { if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options[0].Id); sim.EndMonth(); }
            Assert.Equal(1, Count(sim, "challenge.complete", c.ProjectId));             // exactly once
        }

        [Fact]
        public void ExternalProgressNeverTruncatesAnyChallenge()
        {
            // Every capability every challenge touches already at its highest stage level: one stage done is still one stage.
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 20000;
            var d = sim.Data.Content.Challenges.First(x => x.Id == "standards");
            foreach (var (id, to) in d.Stages.SelectMany(Simulation.StageAdvances).OrderBy(a => a.To))
                sim.AdvanceCapability(id, to, null, "elsewhere");
            Assert.True(sim.CapabilityLevelOf(d.GoalCapability) >= d.GoalLevel);
            var c = sim.FindChallenge("standards")!;
            c.Status = ChallengeStatus.Open;
            int advancesBefore = sim.Log.Events.Count(e => e.Type == "capability.advance");
            Stage(sim, "standards");
            Assert.Equal(ChallengeStatus.Open, c.Status);
            Assert.Equal(1, c.StageIndex);
            Assert.Equal(advancesBefore, sim.Log.Events.Count(e => e.Type == "capability.advance")); // already there: no advance claimed
        }

        [Fact]
        public void EveryChallengesStagesReachItsGoal()
        {
            var c = TestData.Load().Content;
            Assert.Empty(ContentValidation.Problems(c));
            foreach (var d in c.Challenges)
                Assert.Contains(d.Stages.SelectMany(Simulation.StageAdvances), a => a.Id == d.GoalCapability && a.To >= d.GoalLevel);
        }

        [Fact]
        public void WorkWhoseAdvanceRomeCantReachWaits()
        {
            // Gaius's singing fountain draws Hero's birds with sizes (dimensioned drawings), which needs shared measures. A fountain
            // player can know Gaius without them; the job used to run and finish while the drawings advance was refused.
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("fountain");
            sim.World.ScenesSeen.Add("gaius-meet");
            while (!sim.Holds("monthsIn:31")) sim.EndMonth();
            var fountainworks = sim.FindCommission("fountainworks")!;
            Assert.Equal(CapabilityLevel.None, sim.CapabilityLevelOf("metrology"));
            Assert.Equal(CommissionStatus.NotYet, fountainworks.Status);
            Assert.DoesNotContain("commission:fountainworks", sim.SceneCandidateIds());
            fountainworks.Status = CommissionStatus.TermsOffered;                         // even if it were offered, it can't start
            var refused = sim.AcceptCommission("fountainworks");
            Assert.False(refused.Ok);
            Assert.Contains("shared measures", refused.Message);
            fountainworks.Status = CommissionStatus.NotYet;
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Prototype, null, "test"));
            Assert.Contains("commission:fountainworks", sim.SceneCandidateIds());
        }

        [Fact]
        public void AStageIsCheckedWithEveryAdvanceItDeclares()
        {
            var sim = new Simulation(TestData.Load(), 42);
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));
            Assert.True(sim.AdvanceCapability("gauges", CapabilityLevel.Reproducible, null, "test"));
            Assert.True(sim.AdvanceCapability("lineshaft", CapabilityLevel.Prototype, null, "test"));
            // Controlled transmission reproducible needs line shafts reproducible: alone it is refused ...
            Assert.NotNull(sim.AuthoredAdvanceBlocker(new[] { ("transmission", CapabilityLevel.Reproducible) }));
            // ... but a stage that takes line shafts there first may (the endurance run's order).
            Assert.Null(sim.AuthoredAdvanceBlocker(new[] { ("lineshaft", CapabilityLevel.Reproducible), ("transmission", CapabilityLevel.Reproducible) }));
            var endurance = sim.Data.Content.Challenges.First(x => x.Id == "power").Stages.Last();
            Assert.Null(sim.AuthoredAdvanceBlocker(Simulation.StageAdvances(endurance)));
        }

        [Fact]
        public void BlockedAuthoredWorkFailsLoudlyInsteadOfFinishingSilently()
        {
            // The fever ward declares comparative case records (demonstrated), which needs recorded trials. Forced past its start
            // check, it must not finish as if Rome had moved.
            var sim = new Simulation(TestData.Load(), 42);
            var ward = sim.Data.Content.Inventions.First(i => i.Id == "ward");
            Assert.NotNull(sim.CapabilityBlocker(ward.Capability, ward.CapabilityTo));
            sim.World.ActiveInventions.Add(new ActiveInvention(ward, 0) { TurnsRemaining = 1 });
            var ex = Assert.Throws<InvalidOperationException>(() => sim.EndMonth());
            Assert.Contains("ward", ex.Message);
        }

        [Fact]
        public void WorkAlreadyAtItsLevelFinishesWithoutClaimingAnAdvance()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var ward = sim.Data.Content.Inventions.First(i => i.Id == "ward");
            Assert.True(sim.AdvanceCapability("records", CapabilityLevel.Reproducible, null, "test"));
            Assert.True(sim.AdvanceCapability(ward.Capability, CapabilityLevel.Prototype, null, "test"));
            int before = Count(sim, "capability.advance", "capability." + ward.Capability);
            sim.World.ActiveInventions.Add(new ActiveInvention(ward, 0) { TurnsRemaining = 1 });
            sim.EndMonth();
            Assert.Contains("ward", sim.World.Invented);
            Assert.Equal(before, Count(sim, "capability.advance", "capability." + ward.Capability));
        }

        /// <summary>The capabilities P1 content can move, and those no authored work reaches (future scope, kept hidden).</summary>
        [Fact]
        public void ShippedContentDeclaresNoImpossibleAdvance()
        {
            var c = TestData.Load().Content;
            var advances = new List<(string Id, CapabilityLevel To, string Source)>();
            foreach (var d in c.Commissions) advances.AddRange(Simulation.CommissionAdvances(d).Select(a => (a.Id, a.To, "commission " + d.Id)));
            foreach (var d in c.Challenges) foreach (var s in d.Stages) advances.AddRange(Simulation.StageAdvances(s).Select(a => (a.Id, a.To, "challenge " + d.Id)));
            foreach (var i in c.Inventions.Where(i => i.Capability.Length > 0)) advances.Add((i.Capability, i.CapabilityTo, "invention " + i.Id));

            // Fixpoint: the highest level each capability can reach from authored work whose prerequisites can themselves be reached.
            var reach = c.Capabilities.ToDictionary(x => x.Id, x => CapabilityLevel.None);
            for (bool changed = true; changed;)
            {
                changed = false;
                foreach (var (id, to, _) in advances.OrderBy(a => a.To))
                {
                    if (reach[id] >= to) continue;
                    var def = c.Capabilities.First(x => x.Id == id);
                    if (def.Edges.All(e => reach[e.Id] >= e.RequiredFor(to))) { reach[id] = to; changed = true; }
                }
            }
            foreach (var (id, to, source) in advances)
                Assert.True(reach[id] >= to, source + " declares " + id + " → " + to + ", which no P1 content can reach.");
            var unreachable = reach.Where(kv => kv.Value == CapabilityLevel.None).Select(kv => kv.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.Equal(new[] { "alloys", "copying", "electricity", "governor", "hydraulicpress", "interchange", "lathework", "steam" }, unreachable);
        }

        [Fact]
        public void FutureCapabilitiesDontShowAsCurrentGoals()
        {
            var sim = new Simulation(TestData.Load(), 42);
            foreach (var (id, to) in new[] { ("records", CapabilityLevel.Reproducible), ("metrology", CapabilityLevel.Reproducible), ("gauges", CapabilityLevel.Reproducible),
                                             ("valveseats", CapabilityLevel.Reproducible), ("lineshaft", CapabilityLevel.Reproducible) })
                Assert.True(sim.AdvanceCapability(id, to, null, "test"));
            var emerging = string.Join("\n", sim.ViewOf(MenuSection.Civilization).Emerging.Select(i => i.Label));
            foreach (var future in new[] { "alloys", "governor", "hydraulicpress", "lathework" })
                Assert.DoesNotContain(sim.CapabilityDefOf(future)!.Name, emerging, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(sim.CapabilityDefOf("drawings")!.Name, emerging, StringComparison.OrdinalIgnoreCase);
        }
    }
}
