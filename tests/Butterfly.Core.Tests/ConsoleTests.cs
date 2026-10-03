using System.IO;
using Butterfly.Core;

namespace Butterfly.Core.Tests
{
    [CollectionDefinition("Console", DisableParallelization = true)]
    public sealed class ConsoleCollection { }

    /// <summary>
    /// The console game driven by a script, as the automated playtests run it (--inputs): bugs found by the live
    /// blind testers (playtests/ai/live) in the menu and the jump flow.
    /// </summary>
    [Collection("Console")]
    public class ConsoleTests
    {
        /// <summary>Plays the console with these lines (the menu on) and returns everything it printed.</summary>
        internal static string Play(ulong seed, params string[] lines) => Play(new Simulation(TestData.Load(), seed), lines);

        internal static string Play(Simulation sim, params string[] lines)
        {
            string path = Path.GetTempFileName();
            File.WriteAllLines(path, lines);
            var old = Console.Out;
            var sw = new StringWriter();
            Console.SetOut(sw);
            try { new ConsoleGame(sim, new ScriptInput(path, sim), null, false, true).Run(); }
            finally { Console.SetOut(old); File.Delete(path); }
            return sw.ToString();
        }

        [Fact]
        public void InstitutionMenuLabelsNameTheCommandTheyRun()
        {
            // Tester 7: the menu said "join sanctuary", but typing "join sanctuary" was an unknown command.
            string output = Play(42, "choose workshop", "exchange 20 aurei", "menu");
            Assert.Contains("buy into sanctuary, 1%", output);
            Assert.DoesNotContain("] join ", output);
            string typed = Play(42, "choose workshop", "exchange 20 aurei", "buy sanctuary 1");
            Assert.DoesNotContain("Unknown command", typed);
        }
    
        /// <summary>A real first jump (the machine repaired), arriving with more gold than the machine can carry.</summary>
        private static Simulation Arrived(ulong seed = 7)
        {
            var sim = Ready(seed);
            sim.Jump();
            sim.World.Aurei = sim.CarryAurei + 15;
            return sim;
        }

        private static Simulation Ready(ulong seed)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 1000;
            sim.MarkAssessedForTests();
            for (int guard = 0; guard < 60 && !sim.MachineReady; guard++)
            {
                foreach (var system in Simulation.MachineSystems) sim.Repair(system);
                if (sim.MachineStepsDone >= sim.MachineStepsTotal) sim.RestoreGold(sim.MachineGoldNeeded);
                sim.EndTurn();
            }
            Assert.True(sim.MachineReady);
            return sim;
        }

        [Theory]
        [InlineData("visit market")]
        [InlineData("learn more")]
        [InlineData("visit market|learn more")]
        public void AJumpAtAnArrivalPreparesTheNextJumpInsteadOfLeavingAtOnce(string before)
        {
            // Testers 2 and 6: one 'jump' at the first arrival jumped again at once, so they never walked around it.
            // And whether it did depended on what they had typed in between ('visit' kept the jump armed, 'learn more' didn't).
            var sim = Ready(7);
            var lines = new List<string> { "jump", "jump" };         // the first jump, through the console as a player makes it
            lines.AddRange(before.Split('|'));
            lines.Add("jump");
            string output = Play(sim, lines.ToArray());
            Assert.Equal(1, sim.JumpsMade);
            Assert.True(sim.Arrived);
            Assert.Contains("Prepare: deposit <aurei> · bury <aurei>", output);
        }

        [Fact]
        public void TheSecondJumpOffersOnlyWhatCanStillBeDoneAndItWorks()
        {
            // Tester 7: the second jump's briefing offered paydown, endow, audit and exchange and showed Rome's debts,
            // none of which could be acted on, and 'bury 1' was refused, so the gold was lost.
            var sim = Arrived();
            string output = Play(sim, "jump", "bury 15", "jump");
            int from = output.IndexOf("(after arrival) > jump");
            string briefing = output.Substring(from, output.IndexOf("Type 'jump' again", from) - from);
            Assert.DoesNotContain("paydown", briefing);
            Assert.DoesNotContain("endow", briefing);
            Assert.DoesNotContain("audit", briefing);
            Assert.DoesNotContain(" debt ", briefing);
            Assert.Contains("Buried 15 aurei", output);
            Assert.Equal(2, sim.JumpsMade);
            Assert.Equal(15, sim.Arrival!.AureiBuried);
            Assert.Equal(0, sim.Arrival.AureiLeft);
        }
    
        [Fact]
        public void JumpingTwiceWithAnUnreadyMachineDoesNotCrash()
        {
            // Found by the scripted playtests: 'jump', 'jump' before the repairs threw and ended the game.
            var sim = new Simulation(TestData.Load(), 42);
            string output = Play(sim, "jump", "jump", "status");
            Assert.False(sim.Arrived);
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(output, "The machine isn't ready").Count);
        }
    
        [Fact]
        public void NothingButEndMonthEndsTheMonth()
        {
            // P1 (decided 2026-10-02): spending the last Attention never ends the month (L2, testers 2 and 6: a paydown or the
            // last Attention ended the turn, and their next 'end' skipped one).
            var sim = new Simulation(TestData.Load(), 3);
            sim.ChooseSeeded("workshop");
            sim.World[Domain.Economy].Debt = 10;
            sim.World.Gold = 5000;
            string output = Play(sim, "@autoend on", "work craft", "exchange 1 aurei", "assess", "paydown economy 10");
            Assert.Equal(0, sim.World.Attention);
            Assert.Equal(1, sim.Turn);                                  // still the first month
            Assert.Contains("No Attention left this month. Type 'end' to end the month.", output);
            Assert.Equal(0, sim.World[Domain.Economy].Debt, 6);
            Play(sim, "end");
            Assert.Equal(2, sim.Turn);                                  // 'end' moves exactly one month
            Assert.Contains("Attention: ", Play(sim, "status"));
        }
    
        [Fact]
        public void PledgingAllOfNextTurnsAttentionIsAnnounced()
        {
            // L3 (tester 2): a commitment that took all Attention passed four turns at once, with no word beforehand.
            var sim = new Simulation(TestData.Load(), 3);
            sim.World.Gold = 5000;
            sim.ChooseSeeded("fountain");
            sim.GrantStake("circle", 0.5);
            string output = Play(sim, "start warehouses", "mentor circle");
            Assert.Contains("All your Attention is pledged for the months ahead", output);
            Assert.True(sim.AttentionCommittedNextTurn() >= sim.AttentionPerTurn);
            sim.EndTurn();
            Assert.Equal(0, sim.World.Attention);                     // the prediction holds

            var light = new Simulation(TestData.Load(), 3);
            light.World.Gold = 5000;
            light.ChooseSeeded("fountain");
            Assert.DoesNotContain("pledged for the months ahead", Play(light, "work craft"));   // work is this month only
        }
    
        [Fact]
        public void ApprenticesLeavingOverUnpaidWagesAppearInTheTurnSummary()
        {
            // L4 (tester 2): "My apprentices vanished and nobody told me. I only found out in the log."
            var sim = new Simulation(TestData.Load(), 61);
            sim.ChooseSeeded("workshop");
            while (!sim.OwnsWorkshop) sim.EndTurn();
            sim.World.Apprentices = 3;
            sim.World.Gold = 0;
            var lines = Enumerable.Repeat("end", 8).ToArray();
            string output = Play(sim, lines);
            Assert.True(sim.World.Apprentices < 3);
            Assert.Contains("• You can't pay all the apprentices' wages", output);
        }
    
        [Fact]
        public void BuyingIntoTheBankWithoutAPercentBuysTheFirstPurchaseItRequires()
        {
            // L8 (tester 6): 'buy bank' failed, because the bank's first purchase is 5% and the default was 1%.
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            sim.World.Gold = 20000;
            Play(sim, "buy bank");
            Assert.Equal(sim.T.GetInt("joining.bankMinFirstPercent"), sim.StakePercent(sim.World.Institution("bank")));

            var small = new Simulation(TestData.Load(), 42);
            small.ChooseSeeded("workshop");
            small.World.Gold = 20000;
            string output = Play(small, "buy bank 1");
            Assert.Equal(0, small.StakePercent(small.World.Institution("bank")));   // an explicit 1% is still refused, with the reason
            Assert.Contains("5%", output);
        }
    
        [Fact]
        public void TheHeaderShowsFreeTotalAndWhatHoldsTheReservedAttention()
        {
            // P1 header (decided 2026-10-02): "Attention: 2 free / 4 total · 2 reserved — Machine Assessment".
            var sim = new Simulation(TestData.Load(), 3);
            string output = Play(sim, "choose fountain", "end", "status");
            Assert.Contains("Attention: 2 free / 4 total · 2 reserved — Repair the district fountain", output);
            Assert.Contains("== Month 2 · ", output);
        }
    }
}
