using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 paid work beyond the first two commissions (2026-10-04): medicine through Serenus, a bath-house's water, a favor for
    /// Diodoros, shipwrights' drawings for a share, a mill bearing through Aulus. Terms always explicit; negotiation can lose
    /// the work; some jobs pay in regard, not money; one leaves a consequence nobody asked for.
    /// </summary>
    [Collection("Console")]
    public class P1WorkTests
    {
        private static readonly string[] NewWork = { "fevers", "baths", "jars", "drawings", "millbearing" };

        private static Simulation At(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 5000;
            return sim;
        }

        /// <summary>Opens a commission directly, looks, and returns it with terms on the table.</summary>
        private static CommissionState Terms(Simulation sim, string id)
        {
            var c = sim.FindCommission(id)!;
            c.Status = CommissionStatus.Offered;
            Assert.True(sim.LookAtCommission(id).Ok);
            Assert.Equal(CommissionStatus.TermsOffered, c.Status);
            return c;
        }

        private static void Finish(Simulation sim, string id)
        {
            while (sim.World.Attention < 1) sim.EndMonth();
            Assert.True(sim.AcceptCommission(id).Ok);
            while (sim.FindCommission(id)!.Status == CommissionStatus.Working) sim.EndMonth();
            Assert.Equal(CommissionStatus.Done, sim.FindCommission(id)!.Status);
        }

        [Fact]
        public void EveryNewJobStatesWhoPaysAndWhoBuysTheMaterials()
        {
            var sim = At();
            var defs = sim.Data.Content.Commissions.Where(d => NewWork.Contains(d.Id)).ToList();
            Assert.Equal(5, defs.Count);
            Assert.True(defs.Select(d => d.Encounter.Category).Distinct().Count() >= 4);   // not all engineering repair jobs
            foreach (var d in defs)
            {
                sim.World.Attention = 4;
                Assert.False(string.IsNullOrWhiteSpace(d.Payer), d.Id);
                Assert.False(string.IsNullOrWhiteSpace(d.MaterialsPayer), d.Id);
                foreach (var r in d.Requires) sim.Holds(r);                                // parses
                var line = sim.TermsLine(Terms(sim, d.Id));
                Assert.Contains("materials", line);
                Assert.True(line.Contains("pays") || line.Contains("no one pays"), d.Id);
            }
        }

        [Fact]
        public void APersonalFavorPaysInRegardNotMoney()
        {
            var sim = At();
            Assert.DoesNotContain("commission:jars", sim.SceneCandidateIds());      // only after Diodoros takes the deliveries
            var c = Terms(sim, "jars");
            Assert.Contains("a favor: no one pays you", sim.TermsLine(c));
            double gold = sim.World.Gold;
            int regard = sim.PersonOf("Diodoros")!.Regard;
            Finish(sim, "jars");
            Assert.Equal(regard + 2, sim.PersonOf("Diodoros")!.Regard);
            Assert.Contains("jar frames", sim.PersonOf("Diodoros")!.Status);
            Assert.True(sim.World.Gold <= gold + 1e-6);                                // no pay; income only from elsewhere
            Assert.DoesNotContain(sim.World.Ledger.Entries, e => e.ProjectId == "commission:jars" && e.Amount > 0);
        }

        [Fact]
        public void TheStingyBathKeeperUsuallyWalksIfYouPush()
        {
            int walked = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var sim = At(seed);
                Terms(sim, "baths");
                sim.CounterCommission("baths");
                if (sim.FindCommission("baths")!.Status == CommissionStatus.Walked) walked++;
            }
            Assert.InRange(walked, 6, 20);                                             // a lost negotiation is the likely outcome
        }

        [Fact]
        public void SerenussPatronComesThroughHimAndMovesRecordsAndCaseNotes()
        {
            var sim = At();
            Assert.DoesNotContain("commission:fevers", sim.SceneCandidateIds());
            sim.World.ScenesSeen.AddRange(new[] { "serenus-meet", "serenus-casebook" });
            Assert.Contains("commission:fevers", sim.SceneCandidateIds());           // a relationship, not fame
            Terms(sim, "fevers");
            Finish(sim, "fevers");
            Assert.Equal(CapabilityLevel.Demonstrated, sim.CapabilityLevelOf("records"));
            Assert.Equal(CapabilityLevel.Demonstrated, sim.CapabilityLevelOf("casenotes"));
            Assert.Contains("pomponia-cistern", sim.World.Flags);
            for (int m = 0; m < 12 && !sim.World.ScenesSeen.Contains("serenus-cistern"); m++) sim.EndMonth();
            Assert.Contains("serenus-cistern", sim.World.ScenesSeen);                  // and Serenus disagrees about why
        }

        [Fact]
        public void TheBathHouseWaterHasAConsequenceNobodyAskedFor()
        {
            var sim = At();
            Terms(sim, "baths");
            Finish(sim, "baths");
            Assert.Contains("baths-flow", sim.World.Flags);
            for (int m = 0; m < 40 && !sim.World.ScenesSeen.Contains("aqueduct-notice"); m++) sim.EndMonth();
            Assert.Contains("fullers-curse", sim.World.ScenesSeen);
            Assert.Contains("aqueduct-notice", sim.World.ScenesSeen);                  // the fuller's tap is cut; his men out of work
        }

        [Fact]
        public void TheDrawingsPayAShareAndMoveDimensionedDrawings()
        {
            var sim = At();
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));
            var c = Terms(sim, "drawings");
            Assert.Contains("a share of what it earns", sim.TermsLine(c));
            Finish(sim, "drawings");
            Assert.Equal(CapabilityLevel.Reproducible, sim.CapabilityLevelOf("drawings"));
        }

        [Fact]
        public void AulussMillPaysAndWinsHisRespect()
        {
            var sim = At();
            sim.World.ScenesSeen.Add("aulus-meet");
            Assert.True(sim.AdvanceCapability("metrology", CapabilityLevel.Reproducible, null, "test"));   // Aulus comes once gauges exist
            Assert.True(sim.AdvanceCapability("gauges", CapabilityLevel.Reproducible, null, "test"));
            Terms(sim, "millbearing");
            int regard = sim.PersonOf("Aulus")!.Regard;
            Finish(sim, "millbearing");
            Assert.Equal(regard + 1, sim.PersonOf("Aulus")!.Regard);
            Assert.Contains(sim.World.Ledger.Entries, e => e.ProjectId == "commission:millbearing" && e.Amount > 0);
        }

        [Fact]
        public void LosingTheFirstJobIsNotADeadEnd()
        {
            // Executable validation (seed 3) found that walking away from the pump closed the guild and all later work.
            var sim = At();
            Assert.DoesNotContain("commission:hoist", sim.SceneCandidateIds());
            sim.FindCommission("cellarpump")!.Status = CommissionStatus.Walked;
            for (int m = 0; m < 12 && sim.FindCommission("hoist")!.Status == CommissionStatus.NotYet; m++) sim.EndMonth();
            Assert.Equal(CommissionStatus.Offered, sim.FindCommission("hoist")!.Status);
            Terms(sim, "hoist");
            Finish(sim, "hoist");
            Assert.Equal(InstitutionAccessStage.KnowsMember, sim.World.AccessTo("guild").Stage);
            var gate = sim.InvitationGate(sim.InvitationPathDefFor("guild")!);
            Assert.True(gate.HasRelevantWork);
            Assert.True(gate.DemonstratedUsefulness);                                 // Felix saw it work (regard), not the same fact as the work
        }
    }
}