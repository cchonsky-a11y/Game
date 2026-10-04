using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 institution access (decided 2026-10-02): the Merchants' Guild of Ostia takes members by invitation. Felix's
    /// referral after the cellar pump starts the path: guest, invited back, sponsored, admitted. Fame never counts.
    /// </summary>
    [Collection("Console")]
    public class P1InvitationTests
    {
        /// <summary>The cellar-pump commission done, so Felix knows your work.</summary>
        private static Simulation AfterThePump(ulong seed = 42)
        {
            var sim = P1Play.AfterThePump(seed);
            sim.World.Gold = 2000;
            return sim;
        }

        private static void UntilOffer(Simulation sim)
        {
            for (int guard = 0; guard < 24 && sim.InvitationState("guild")!.Pending == InvitationOffer.None; guard++) sim.EndMonth();
            Assert.NotEqual(InvitationOffer.None, sim.InvitationState("guild")!.Pending);
        }

        [Fact]
        public void WithoutTheWorkNoInvitationComes()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 5000;
            for (int m = 0; m < 36; m++) { if (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Offered) sim.DeclineCommission("cellarpump"); sim.EndMonth(); }
            Assert.Equal(InvitationOffer.None, sim.InvitationState("guild")!.Pending);
            Assert.True(sim.World.AccessTo("guild").Stage < InstitutionAccessStage.KnowsMember);
            Assert.False(sim.InvitationGate(sim.InvitationPathDefFor("guild")!).IsWarranted);
        }

        [Fact]
        public void TheWorkLeadsFromGuestToMemberThroughFelix()
        {
            var sim = AfterThePump();
            Assert.Equal(InstitutionAccessStage.KnowsMember, sim.World.AccessTo("guild").Stage);
            UntilOffer(sim);
            Assert.Equal(InvitationOffer.Guest, sim.InvitationState("guild")!.Pending);
            int att = sim.World.Attention;
            Assert.True(sim.AcceptInvitation("guild").Ok);
            Assert.Equal(att - 1, sim.World.Attention);
            Assert.Equal(InstitutionAccessStage.Guest, sim.World.AccessTo("guild").Stage);
            Assert.Contains(sim.World.Ledger.Entries, e => e.Kind == LedgerEntryKind.GiftOrFavor);   // your share of the wine

            UntilOffer(sim);
            Assert.Equal(InvitationOffer.Again, sim.InvitationState("guild")!.Pending);
            Assert.True(sim.AcceptInvitation("guild").Ok);
            Assert.Equal(InstitutionAccessStage.InvitedBack, sim.World.AccessTo("guild").Stage);

            UntilOffer(sim);
            Assert.Equal(InvitationOffer.Sponsor, sim.InvitationState("guild")!.Pending);
            Assert.True(sim.AcceptInvitation("guild").Ok);
            Assert.Equal("Felix", sim.World.AccessTo("guild").SponsorId);

            var guild = sim.World.Institution("guild");
            for (int m = 0; m < 3 && !guild.Backed; m++) sim.EndMonth();
            Assert.Equal(InstitutionAccessStage.Member, sim.World.AccessTo("guild").Stage);
            Assert.True(guild.Backed);
            Assert.Equal(10, sim.StakePercent(guild));                       // the standing P0 systems read (P1-03)
            Assert.True(sim.AnnualDues(guild) > 0);                            // obligations come with it
            Assert.Contains(sim.World.Ledger.Entries, e => e.Kind == LedgerEntryKind.InstitutionDues && e.Reason.Contains("guild takes you in"));
        }

        [Fact]
        public void TurningFelixDownMakesHimWaitBeforeAskingAgain()
        {
            var sim = AfterThePump();
            UntilOffer(sim);
            int declined = sim.Turn;
            Assert.True(sim.DeclineInvitation("guild").Ok);
            UntilOffer(sim);
            Assert.True(sim.Turn - declined >= sim.T.GetInt("invitations.inviterPatienceMonths"));
        }

        [Fact]
        public void TheGuildDoesNotSellSeats()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 5000;
            string output = ConsoleTests.Play(sim, "buy guild 5", "menu");
            Assert.Contains("doesn't sell seats: members bring you in", output);
            Assert.False(sim.World.Institution("guild").Backed);
            Assert.DoesNotContain("buy into guild", output);
        }
    }
}
