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
        internal static string Play(ulong seed, params string[] lines)
        {
            string path = Path.GetTempFileName();
            File.WriteAllLines(path, lines);
            var sim = new Simulation(TestData.Load(), seed);
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
    }
}
