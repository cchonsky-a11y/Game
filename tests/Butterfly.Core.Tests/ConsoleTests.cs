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
    }
}
