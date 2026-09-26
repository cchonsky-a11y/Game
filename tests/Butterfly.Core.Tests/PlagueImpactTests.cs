using System.Collections.Generic;
using System.Linq;
using Butterfly.Batch;
using Xunit;
using Xunit.Abstractions;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// G4 (decided 2026-09-27): a plague must always lower the Index in the medium term. Counterfactual:
    /// the same seed and inputs with a plague that does nothing (no damage, no deaths, no gold loss, no
    /// debt clearing). Medium term = the first decade of the absence (about 10 years after the outbreak).
    /// </summary>
    public class PlagueImpactTests
    {
        private readonly ITestOutputHelper _out;
        public PlagueImpactTests(ITestOutputHelper output) { _out = output; }

        internal static GameData Harmless() => TestData.Load().WithTuning(new Dictionary<string, double>
        {
            { "plague.damage.medicine", 0 }, { "plague.damage.governance", 0 }, { "plague.damage.economy", 0 },
            { "plague.deathRatePerSeverity", 0 }, { "plague.goldLossPerSeverity", 0 }, { "plague.debtRelease", 0 },
        });

        [Fact]
        public void APlagueAlwaysLowersTheIndexTenYearsOn()
        {
            var real = TestData.Load();
            var harmless = Harmless();
            int runs = 0, lower = 0;
            double worst = double.MinValue;
            foreach (var make in new System.Func<Strategy>[] { () => new BalancedStrategy(), () => new SpecializedStrategy(), () => new NeglectfulStrategy(), () => new EndowStrategy() })
                for (ulong seed = 1; seed <= 50; seed++)
                {
                    double a = FirstDecadeIndex(real, make(), seed), b = FirstDecadeIndex(harmless, make(), seed);
                    runs++;
                    if (a < b - 1e-9) lower++;
                    worst = System.Math.Max(worst, a - b);
                }
            _out.WriteLine("plague lowered the Index in " + lower + " of " + runs + " runs; worst case with plague − without = " + worst.ToString("0.00"));
            Assert.Equal(runs, lower);
        }

        private static double FirstDecadeIndex(GameData data, Strategy s, ulong seed)
        {
            var sim = new Simulation(data, seed);
            while (sim.Now.Year < 165) { s.PlayTurn(sim); sim.EndTurn(); }
            s.BeforeJump(sim);
            return sim.JumpForTests().IndexByDecade[0];
        }
    }
}
