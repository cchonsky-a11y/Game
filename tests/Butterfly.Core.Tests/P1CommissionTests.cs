using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The first P1 commission, end to end (decided 2026-10-02): encounter → unpaid look → explicit terms (negotiation can
    /// fail) → staged work holding Attention → paid completion in the ledger → a referral into an institution's circle.
    /// </summary>
    public class P1CommissionTests
    {
        private static Simulation AtEncounter(ulong seed = 42)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();   // the hour-one workshop project is done
            return sim;
        }

        [Fact]
        public void FelixBringsTheWorkInTheFifthMonthAndSaysItIsUnpaid()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            for (int m = 0; m < 4; m++) { Assert.Equal(CommissionStatus.NotYet, sim.FindCommission("cellarpump")!.Status); sim.EndMonth(); }
            Assert.Equal(CommissionStatus.Offered, sim.FindCommission("cellarpump")!.Status);
            var e = sim.Log.Events.Last(x => x.Type == "commission.encounter");
            Assert.Contains("Pay: none initially · may lead to paid commission", e.Text);
            Assert.Equal(SceneCategory.WorkEconomy, sim.World.ScenePacing.LastCategory);
        }

        [Fact]
        public void LookingCostsAttentionAndTheTermsSayWhoPaysAndWhoBuysTheMaterials()
        {
            var sim = AtEncounter();
            int att = sim.World.Attention;
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            Assert.Equal(att - 1, sim.World.Attention);
            var c = sim.FindCommission("cellarpump")!;
            Assert.Equal(CommissionStatus.TermsOffered, c.Status);
            string terms = sim.TermsLine(c);
            Assert.Contains("Cassianus pays", terms);
            Assert.Contains("Cassianus buys the materials", terms);
            Assert.Contains("on completion", terms);
            Assert.Contains(sim.PendingDecisions(), r => r.Contains("Cassianus"));   // fast-forward stops for the offer
        }

        [Fact]
        public void AcceptedWorkRunsItsStagesPaysThroughTheLedgerAndLeadsToFelixsCircle()
        {
            var sim = AtEncounter();
            sim.LookAtCommission("cellarpump");
            double gold = sim.World.Gold;
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            var project = sim.World.Projects.Single(p => p.Id == "commission:cellarpump");
            Assert.Equal(ProjectFundingModel.ClientPaid, project.Terms.FundingModel);
            Assert.Equal("Cassianus", project.Terms.Payer);
            Assert.Equal(gold + project.Terms.UpfrontGold, sim.World.Gold, 6);   // the money now arrives

            var def = sim.CommissionDefOf(sim.FindCommission("cellarpump")!);
            for (int m = 0; m < def.Work.Sum(w => w.DurationMonths); m++)
            {
                sim.EndMonth();
                if (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Working)
                    Assert.Contains(sim.ReservedAttentionParts(), p => p.What == def.Title);   // each stage holds its Attention
            }
            Assert.Equal(CommissionStatus.Done, sim.FindCommission("cellarpump")!.Status);
            Assert.Equal(ProjectStage.Complete, project.Stage);
            var entries = sim.World.Ledger.ForProject("commission:cellarpump");
            Assert.Equal(project.Terms.UpfrontGold + project.Terms.CompletionGold, entries.Sum(e => e.Amount), 6);
            Assert.All(entries, e => Assert.Equal("Cassianus", e.Counterparty));
            Assert.Equal(InstitutionAccessStage.KnowsMember, sim.World.AccessTo("guild").Stage);
            Assert.Equal("Felix", sim.World.AccessTo("guild").KnownMemberId);
        }

        [Fact]
        public void PushingForMoreCanWinStandFirmOrLoseTheWorkAndOnlyOnce()
        {
            var outcomes = new System.Collections.Generic.HashSet<string>();
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var sim = AtEncounter(seed);
                sim.LookAtCommission("cellarpump");
                var c = sim.FindCommission("cellarpump")!;
                double before = c.CompletionPay;
                Assert.True(sim.CounterCommission("cellarpump").Ok);
                outcomes.Add(c.Status == CommissionStatus.Walked ? "walked" : c.CompletionPay > before ? "raised" : "firm");
                Assert.False(sim.CounterCommission("cellarpump").Ok);         // he remembers being pushed
            }
            Assert.Equal(new[] { "firm", "raised", "walked" }, outcomes.OrderBy(x => x));
        }

        [Fact]
        public void LeavingRomeAbandonsUnfinishedWork()
        {
            var sim = AtEncounter();
            sim.LookAtCommission("cellarpump");
            sim.AcceptCommission("cellarpump");
            sim.JumpForTests();
            Assert.Equal(CommissionStatus.Abandoned, sim.FindCommission("cellarpump")!.Status);
            Assert.Equal(ProjectStage.Abandoned, sim.World.Projects.Single().Stage);
        }

        [Fact]
        public void CommissionTextAvoidsTheBannedNarratorPatterns()
        {
            // Master handoff §7: say it once; no narrator commentary after a line.
            var banned = new[] { "That lands", "That gets his attention", "That's a shift", "There it is", "That changes things", "That matters",
                                 "This matters", "That tells you something", "The real issue is", "Not a question", "He understands",
                                 "You can tell he means it", "The room changes", "He looks at you differently" };
            foreach (var d in TestData.Load().Content.Commissions)
            {
                var texts = new[] { d.Encounter.Text, d.Diagnosis.Text, d.TermsScene.Text, d.AcceptText, d.StandFirmText, d.WalkText, d.DeclineText, d.ReferralText }
                    .Concat(d.Work.Select(w => w.Text));
                foreach (var t in texts) foreach (var b in banned) Assert.DoesNotContain(b, t);
            }
        }
    }
}
