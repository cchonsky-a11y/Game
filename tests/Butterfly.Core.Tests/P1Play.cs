using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Shared P1 test setup: the moves many tests begin from, played through the real API.</summary>
    internal static class P1Play
    {
        /// <summary>A workshop game in which the cellar pump has been offered, looked at, agreed and finished.</summary>
        public static Simulation AfterThePump(ulong seed = 42, GameData? data = null)
        {
            var sim = new Simulation(data ?? TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.NotYet) sim.EndMonth();
            while (sim.World.ActiveProjects.Count > 0) sim.EndMonth();
            Assert.True(sim.LookAtCommission("cellarpump").Ok);
            Assert.True(sim.AcceptCommission("cellarpump").Ok);
            while (sim.FindCommission("cellarpump")!.Status == CommissionStatus.Working) sim.EndMonth();
            return sim;
        }

        /// <summary>Accepts Felix's invitations month by month until the guild access reaches <paramref name="until"/> (at most 60 months).</summary>
        public static void FollowFelix(Simulation sim, InstitutionAccessStage until, bool mustReach = false)
        {
            for (int m = 0; m < 60 && sim.World.AccessTo("guild").Stage < until; m++)
            {
                if (sim.InvitationState("guild")!.Pending != InvitationOffer.None) Assert.True(sim.AcceptInvitation("guild").Ok);
                if (sim.World.AccessTo("guild").Stage < until) sim.EndMonth();
            }
            if (mustReach) Assert.True(sim.World.AccessTo("guild").Stage >= until);
        }
    }
}
