using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Hardening pass (2026-10-04): a Grand Challenge must not hang on one client. Measurement and standards comes up through
    /// any of several real encounters with the reproducibility problem (the pump's seat, the hoist's pawl, the bath-house
    /// stopcock, a fountain allotment's two calices); they converge on one challenge, which opens once.
    /// </summary>
    [Collection("Console")]
    public class P1ChallengeRouteTests
    {
        private static Simulation At(string choice = "workshop", ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded(choice);
            sim.World.Gold = 5000;
            return sim;
        }

        private static void Close(Simulation sim, string commission, CommissionStatus status) => sim.FindCommission(commission)!.Status = status;

        /// <summary>Ends months until the router has opened the challenge (or the months run out).</summary>
        private static ChallengeState RunUntilOpen(Simulation sim, string id = "standards", int months = 30)
        {
            for (int m = 0; m < months && sim.FindChallenge(id)!.Status == ChallengeStatus.NotYet; m++) sim.EndMonth();
            return sim.FindChallenge(id)!;
        }

        [Fact]
        public void EveryRequirementInTheContentParses()
        {
            var sim = At();
            var c = sim.Data.Content;
            foreach (var ch in c.Challenges)
            {
                Assert.NotEmpty(ch.Routes);
                Assert.Equal(ch.Routes.Count, ch.Routes.Select(r => r.Id).Distinct().Count());
                foreach (var r in ch.Routes.SelectMany(r => r.Requires)) sim.Holds(r);
                foreach (var s in ch.Stages) foreach (var r in s.Needs.Concat(s.Resource?.Source ?? new string[0])) sim.Holds(r);
            }
            foreach (var r in c.Commissions.SelectMany(d => d.Requires)) sim.Holds(r);
            foreach (var r in c.Scenes.SelectMany(d => d.Requires)) sim.Holds(r);
            foreach (var p in c.InvitationPaths)
                foreach (var r in p.RelationshipEvidence.Concat(p.WorkEvidence).Concat(p.UsefulnessEvidence)) sim.Holds(r);
        }

        [Fact]
        public void MeasurementAndStandardsHasSeveralIndependentRoutes()
        {
            var d = At().Data.Content.Challenges.First(x => x.Id == "standards");
            Assert.True(d.Routes.Count >= 2);
            // No route needs the pump to have been done, except the pump's own.
            Assert.True(d.Routes.Count(r => r.Requires.Any(q => q.Contains("cellarpump:Done"))) <= 1);
            Assert.All(d.Routes, r => Assert.False(string.IsNullOrWhiteSpace(r.Text)));
        }

        [Fact]
        public void ThePumpRouteOpensIt()
        {
            var sim = At();
            Close(sim, "cellarpump", CommissionStatus.Done);
            Assert.True(sim.AdvanceCapability("valveseats", CapabilityLevel.Reproducible, null, "test"));
            var c = RunUntilOpen(sim);
            Assert.Equal(ChallengeStatus.Open, c.Status);
            Assert.Equal("pump", c.OpenedBy);
            Assert.Contains(sim.Log.Events, e => e.Type == "challenge.open" && e.Text.Contains("Cassianus's pump"));
        }

        [Fact]
        public void TheHoistRouteOpensItWithoutThePump()
        {
            var sim = At();
            Close(sim, "cellarpump", CommissionStatus.Walked);
            Close(sim, "hoist", CommissionStatus.Done);
            var c = RunUntilOpen(sim);
            Assert.Equal(ChallengeStatus.Open, c.Status);
            Assert.Equal("hoist", c.OpenedBy);
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "challenge.open" && e.Text.Contains("Cassianus"));
        }

        [Fact]
        public void TheBathHouseRouteOpensItWithoutThePumpOrTheHoist()
        {
            var sim = At("fountain");
            Close(sim, "cellarpump", CommissionStatus.Declined);
            Close(sim, "hoist", CommissionStatus.Declined);
            Close(sim, "baths", CommissionStatus.Done);
            var c = RunUntilOpen(sim);
            Assert.Equal(ChallengeStatus.Open, c.Status);
            Assert.Equal("baths", c.OpenedBy);
        }

        [Fact]
        public void LosingThePumpDoesNotLoseTheChallenge()
        {
            // Played out, not set: the pump walks, the hoist comes through Felix, and its finished work raises the question.
            var sim = At();
            Close(sim, "cellarpump", CommissionStatus.Walked);
            for (int m = 0; m < 12 && sim.FindCommission("hoist")!.Status == CommissionStatus.NotYet; m++) sim.EndMonth();
            var hoist = sim.FindCommission("hoist")!;
            Assert.Equal(CommissionStatus.Offered, hoist.Status);
            while (sim.World.Attention < 1) sim.EndMonth();
            Assert.True(sim.LookAtCommission("hoist").Ok);
            while (sim.World.Attention < 1) sim.EndMonth();
            Assert.True(sim.AcceptCommission("hoist").Ok);
            while (hoist.Status == CommissionStatus.Working) sim.EndMonth();
            Assert.Equal(CommissionStatus.Done, hoist.Status);
            var c = RunUntilOpen(sim);
            Assert.Equal(ChallengeStatus.Open, c.Status);
        }

        [Fact]
        public void WithNoRouteItStaysClosed()
        {
            var sim = At();
            Close(sim, "cellarpump", CommissionStatus.Walked);
            Close(sim, "hoist", CommissionStatus.Declined);
            Close(sim, "baths", CommissionStatus.Declined);
            for (int m = 0; m < 30; m++) sim.EndMonth();
            Assert.Equal(ChallengeStatus.NotYet, sim.FindChallenge("standards")!.Status);
            Assert.Null(sim.OpeningRoute(sim.FindChallenge("standards")!));
        }

        [Fact]
        public void ItOpensOnlyOnceWhenSeveralRoutesHold()
        {
            var sim = At();
            Close(sim, "cellarpump", CommissionStatus.Done);
            Assert.True(sim.AdvanceCapability("valveseats", CapabilityLevel.Reproducible, null, "test"));
            Close(sim, "hoist", CommissionStatus.Done);
            Close(sim, "baths", CommissionStatus.Done);
            var c = RunUntilOpen(sim);
            for (int m = 0; m < 12; m++) sim.EndMonth();
            Assert.Equal(1, sim.Log.Events.Count(e => e.Type == "challenge.open" && e.Target == c.ProjectId));
            Assert.Equal("pump", c.OpenedBy);                                           // the first route that holds, in authored order
            Assert.DoesNotContain("challenge:standards", sim.SceneCandidateIds());
        }
    }
}
