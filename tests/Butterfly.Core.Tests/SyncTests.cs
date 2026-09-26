using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// BUILD_GUIDE §2.3: tuning.json must agree with the values SYSTEMS.md fixes, and every key
    /// must trace to SYSTEMS.md or a pending proposal. A failure here means documents and code drifted.
    /// </summary>
    public class SyncTests
    {
        private static Tuning T => TestData.Load().Tuning;

        [Theory]
        [InlineData("debt.compoundRate", 0.05)]          // SYSTEMS §6
        [InlineData("debt.accrualRate", 1)]              // SYSTEMS §6
        [InlineData("debt.paydownMultiplier", 1.5)]      // SYSTEMS §6
        [InlineData("institutions.decayPerDecade.bare", 0.10)]      // SYSTEMS §7
        [InlineData("institutions.decayPerDecade.chartered", 0.03)] // SYSTEMS §7
        [InlineData("institutions.decayPerDecade.strong", 0.01)]    // SYSTEMS §7
        [InlineData("attention.perTurn", 4)]             // SYSTEMS §3
        [InlineData("time.monthsPerTurn", 3)]            // SYSTEMS §2, Stage 3
        [InlineData("time.startYear", 155)]              // VISION premise
        [InlineData("jump.years", 250)]                  // PROTOTYPE_SCOPE
        public void CanonicalValuesMatchSystems(string key, double expected)
        {
            Assert.Equal(expected, T.Get(key), 9);
        }

        [Fact]
        public void EveryTuningKeyTracesToASource()
        {
            foreach (var key in T.Keys)
            {
                string r = T.Ref(key);
                Assert.False(string.IsNullOrWhiteSpace(r), key + " has no ref");
                Assert.True(r.Contains("SYSTEMS") || r.Contains("PROPOSED") || r.Contains("SCOPE") || r.Contains("VISION") || r.Contains("GDD"),
                    key + " ref '" + r + "' names no source document");
            }
        }
    }
}
