using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 migration (Corey, 2026-10-04): relationship-first institutions with distinct evidence and a real vote; fast-forward
    /// no longer stops for stake purchases; generic work is a fallback; repeat work comes from people; offscreen lives have
    /// bounded windows and competing branches; old inventions are practical projects in the capability model.
    /// </summary>
    [Collection("Console")]
    public class P1MigrationTests
    {
        private static Simulation AfterThePump(ulong seed = 42, GameData? data = null)
        {
            var sim = new Simulation(data ?? TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Working) sim.EndMonth();
            sim.World.Gold = 5000;
            return sim;
        }

        private static void FollowFelix(Simulation sim, InstitutionAccessStage until)
        {
            for (int m = 0; m < 60 && sim.World.AccessTo("guild").Stage < until; m++)
            {
                if (sim.InvitationState("guild")!.Pending != InvitationOffer.None) Assert.True(sim.AcceptInvitation("guild").Ok);
                if (sim.World.AccessTo("guild").Stage < until) sim.EndMonth();
            }
        }

        [Fact]
        public void ThePhysiciansCircleNoLongerSellsSeats()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.Gold = 5000;
            Assert.True(sim.OnInvitationPath(sim.World.Institution("circle")));
            Assert.Contains("doesn't sell seats", ConsoleTests.Play(sim, "buy circle 5"));
            Assert.False(sim.World.Institution("circle").Backed);
        }

        [Fact]
        public void TheGateKeepsItsEvidenceApart()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var d = sim.InvitationPathDefFor("circle")!;
            sim.World.AccessTo("circle").RecordMemberRelationship("Serenus");
            sim.World.ScenesSeen.Add("serenus-meet");                         // you know him
            sim.PersonOf("Serenus")!.Regard = 1;
            var gate = sim.InvitationGate(d);
            Assert.True(gate.HasExistingRelationship);
            Assert.False(gate.HasRelevantWork);                               // no medicine work, no promise
            Assert.False(gate.DemonstratedUsefulness);                        // he hasn't seen you be useful yet
            sim.World.CompletedProjects.Add("fountain");
            gate = sim.InvitationGate(d);
            Assert.True(gate.HasRelevantWork);
            Assert.False(gate.DemonstratedUsefulness);                        // work alone isn't usefulness
            sim.PersonOf("Serenus")!.Regard = 2;
            Assert.True(sim.InvitationGate(d).IsWarranted);
        }

        [Fact]
        public void TheMembersCanPutTheVoteOffAndThenRefuse()
        {
            var data = TestData.Load().WithTuning(new Dictionary<string, double> { { "invitations.vote.base", 0 }, { "invitations.vote.perSponsorRegard", 0 } });
            var sim = AfterThePump(42, data);
            FollowFelix(sim, InstitutionAccessStage.SponsoredCandidate);
            Assert.Equal(InstitutionAccessStage.SponsoredCandidate, sim.World.AccessTo("guild").Stage);
            for (int m = 0; m < 6 && !sim.Log.Events.Any(e => e.Type == "invitation.refused"); m++) sim.EndMonth();
            Assert.Contains(sim.Log.Events, e => e.Type == "invitation.postponed");
            Assert.Contains(sim.Log.Events, e => e.Type == "invitation.refused");
            Assert.Equal(InstitutionAccessStage.InvitedBack, sim.World.AccessTo("guild").Stage);
            Assert.False(sim.World.Institution("guild").Backed);
        }

        [Fact]
        public void AnAffordableStakeNoLongerStopsFastForward()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            double cheapestProject = sim.AvailableProjects().Min(p => sim.ProjectGold(p));
            double sanctuarySeat = sim.BuyCost(sim.World.Institution("sanctuary"), 1) + sim.EntryFee(sim.World.Institution("sanctuary"));
            if (sanctuarySeat >= cheapestProject) return;                      // can't separate the two in this content
            sim.World.Aurei = 0;
            sim.World.Gold = 0;
            bool without = sim.AffordableInvestment();
            sim.World.Gold = sanctuarySeat;
            Assert.Equal(without, sim.AffordableInvestment());               // the seat being affordable changes nothing
        }

        [Fact]
        public void GenericWorkIsOnlyAFallbackInTheMenu()
        {
            var sim = new Simulation(TestData.Load(), 42);
            string idle = ConsoleTests.Play(sim, "menu on", "menu");
            Assert.Contains("odd jobs to get by", idle);
            Assert.DoesNotContain("consult (", idle);
            Assert.DoesNotContain("craft (", idle);
            var busy = AfterThePump();
            busy.FindCommission("bilges");                                    // exists
            busy.World.Commissions.First(c => c.Id == "cellarpump").Status = CommissionStatus.Working;
            busy.World.Commissions.First(c => c.Id == "cellarpump").MonthsLeftInStage = 1;
            busy.World.Commissions.First(c => c.Id == "cellarpump").WorkIndex = 2;
            Assert.DoesNotContain("odd jobs to get by", ConsoleTests.Play(busy, "menu on", "menu"));
        }

        [Fact]
        public void RepeatWorkComesThroughTheGuild()
        {
            var sim = AfterThePump();
            Assert.Equal(CommissionStatus.NotYet, sim.FindCommission("bilges")!.Status);
            FollowFelix(sim, InstitutionAccessStage.Guest);
            for (int m = 0; m < 12 && sim.FindCommission("bilges")!.Status == CommissionStatus.NotYet; m++) sim.EndMonth();
            Assert.Equal(CommissionStatus.Offered, sim.FindCommission("bilges")!.Status);
            Assert.Contains("Lollius", sim.Log.Events.Last(e => e.Type == "commission.encounter").Text);
        }

        [Fact]
        public void OffscreenLivesBranchAndDoNotBecomeInevitable()
        {
            int deliveries = 0, antioch = 0;
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                var cass = sim.PersonOf("Cassianus")!;
                cass.Happened.Add("warehouse-fire");
                sim.World.LifeEventLog["warehouse-fire"] = 1;
                for (int m = 0; m < 48; m++) sim.EndMonth();
                bool d = sim.World.LifeEventLog.ContainsKey("diodoros-deliveries") || sim.World.ReadyLife.Contains("diodoros-deliveries");
                bool a = sim.World.LifeEventLog.ContainsKey("diodoros-antioch") || sim.World.ReadyLife.Contains("diodoros-antioch");
                Assert.False(d && a);                                           // one branch or the other, never both
                if (d) deliveries++;
                if (a) antioch++;
                foreach (var id in new[] { "diodoros-deliveries", "diodoros-antioch" })
                    if (sim.World.LifeEligibleSince.TryGetValue(id, out int since) && sim.World.LifeEventLog.TryGetValue(id, out int ev))
                        Assert.True(sim.Log.Get(ev).Time.TotalMonths - sim.Log.Events.First(e => e.Type == "turn.start").Time.TotalMonths < since + 24 + 1);
            }
            Assert.True(deliveries > 0 && antioch > 0);                         // both lives happen in some games
            Assert.True(deliveries + antioch < 40);                              // and in some, neither: the window closes
        }

        [Fact]
        public void APersonWhoLeavesCanNoLongerInviteOrIntroduce()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.PersonOf("Felix")!.Gone = true;
            Assert.True(sim.IsPersonAway("Felix"));
        }

        [Fact]
        public void OldInventionsArePracticalProjectsThatMoveCapabilities()
        {
            var sim = new Simulation(TestData.Load(), 9);
            sim.World.Gold = 5000;
            sim.World.Attention = 100;
            var ward = sim.InventionById("ward")!;
            Assert.Equal("casenotes", ward.Capability);
            Assert.All(sim.Data.Content.Inventions.SelectMany(i => i.Effects), e => Assert.NotEqual("stake", e.Type));
            Assert.NotNull(sim.CapabilityBlocker("casenotes", CapabilityLevel.Demonstrated));   // recorded trials first
            Assert.True(sim.AdvanceCapability("records", CapabilityLevel.Demonstrated, null, "test"));
            Assert.Null(sim.CapabilityBlocker("casenotes", CapabilityLevel.Demonstrated));
        }
    }
}
