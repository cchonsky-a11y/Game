using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Snapshot test (BUILD_GUIDE §8): a saved reference playthrough, seed 42, through both jumps, must keep producing
    /// the same event log. If a deliberate rule or number change moves it, run this test, read the new hash from its
    /// output, update <see cref="ReferenceHash"/> and say so in the commit message and DECISIONS.md.
    /// </summary>
    public class SnapshotTests
    {
        private readonly ITestOutputHelper _out;
        public SnapshotTests(ITestOutputHelper output) => _out = output;

        /// <summary>The reference playthrough's log hash (set 2026-09-28; reset 2026-10-03 for P1 one-month turns, durations in months and no auto-end).</summary>
        public const string ReferenceHash = "e80c78dd43ac8b90fbd35e5ddefd0f8f7b175add6319578079d6a571a7fb55cc";

        /// <summary>A plain, fixed player: the workshop, the guild and the Circle, generous answers, the machine, then two jumps.</summary>
        public static Simulation ReferencePlaythrough()
        {
            var sim = ReferencePlaythroughBeforeJump();
            sim.Jump();
            sim.Jump();
            return sim;
        }

        /// <summary>The reference playthrough up to the moment it leaves Rome.</summary>
        public static Simulation ReferencePlaythroughBeforeJump()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            while (!sim.MachineReady || sim.Now.Year < 172)
            {
                var w = sim.World;
                if (sim.OutbreakAwaitingResponse && !sim.RespondToPlague("quarantine").Ok) sim.RespondToPlague("none");
                if (w.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(true);
                if (sim.PendingEvent is EventDef ev)
                {
                    var o = ev.Options.FirstOrDefault(x => x.Style == "generous" && sim.EventCost(x) <= w.Gold) ?? ev.Options.Last();
                    sim.Decide(o.Id);
                }
                foreach (var i in w.Institutions.Where(x => x.OfferedRank > 0).ToList()) sim.AnswerOffice(i.Key, sim.OfficeDuties() < 2);
                if (w.Aurei >= 1 && sim.MachineGoldRestored < 1 && sim.MachineStepsDone < sim.MachineStepsTotal) sim.SellAurei(w.Aurei);
                if (!sim.MachineAssessed) sim.Assess();
                foreach (var system in Simulation.MachineSystems)
                {
                    var step = sim.NextMachineStep(system);
                    if (step != null && sim.MachineStepGold(step) <= w.Gold) sim.Repair(system);
                }
                if (sim.MachineStepsDone >= sim.MachineStepsTotal && sim.MachineGoldRestored < sim.MachineGoldNeeded)
                {
                    double missing = sim.MachineGoldNeeded - sim.MachineGoldRestored - w.Aurei;
                    if (missing >= 1) sim.BuyAurei(System.Math.Min(missing, sim.AffordableAurei()));
                    if (w.Aurei >= 1) sim.RestoreGold(sim.MachineGoldNeeded - sim.MachineGoldRestored);
                }
                foreach (var id in new[] { "guild", "circle" })
                {
                    var i = w.Institution(id);
                    if (!i.Backed) { if (sim.JoinBlocker(i) == null && sim.BuyCost(i, 1) <= w.Gold / 2) sim.Buy(id, 1); }
                    else if (i.MeetingsThisYear < 2) sim.Attend(id, i.Def.DriftPaths[0].Id);
                }
                if (sim.OwnsWorkshop && sim.OrdersLeftThisSeason > 0 && sim.OrderBoard().FirstOrDefault(o => o.Attention <= w.Attention - 1) is OrderDef order) sim.TakeOrder(order.Id);
                foreach (var kind in Simulation.WorkKinds.Reverse())
                    if (sim.WorkAttention(kind) <= w.Attention && sim.Work(kind).Ok) break;
                sim.EndTurn();
                if (sim.Now.Year > 190) break;
            }
            return sim;
        }

        [Fact]
        public void TheReferencePlaythroughStillEndsTheSame()
        {
            var sim = ReferencePlaythrough();
            string hash = sim.Log.Hash();
            _out.WriteLine("Reference playthrough log hash: " + hash + " (arrived AD " + sim.Now.Year + ", Index " + sim.SphereIndex().ToString("0.0") + ")");
            Assert.Equal(ReferenceHash, hash);
        }
    }
}
