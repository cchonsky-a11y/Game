using Butterfly.Core;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The scripted-playtest validator (--checks) must follow the current rules, or every run reports false findings
    /// (found by 50 scripted runs, seeds 701–750, 2026-09-28).
    /// </summary>
    public class HarnessTests
    {
        private static Tuning T => TestData.Load().Tuning;

        [Theory]
        [InlineData(25)]
        [InlineData(40)]
        [InlineData(60)]
        public void AJumpOfTwentyFiveToSixtyYearsIsLegal(int years) => Assert.Null(Harness.JumpLengthFinding(years, years, T));

        [Theory]
        [InlineData(20)]
        [InlineData(65)]
        [InlineData(250)]
        [InlineData(33)]
        public void AJumpOutsideTheRangeOrOffTheStepsIsReported(int years) => Assert.NotNull(Harness.JumpLengthFinding(years, years, T));

        [Fact]
        public void AnAbsenceThatDiffersFromTheDrawIsReported() => Assert.NotNull(Harness.JumpLengthFinding(35, 30, T));

        [Fact]
        public void DecayOverAFinalHalfDecadeIsItsShareOfTheDecadesRate()
        {
            Assert.True(Harness.DecayMatches(0.10, 0.10));
            Assert.True(Harness.DecayMatches(1 - System.Math.Sqrt(0.9), 0.10));   // 5.13% over a half decade
            Assert.True(Harness.DecayMatches(1 - System.Math.Sqrt(0.99), 0.01));
            Assert.False(Harness.DecayMatches(0.05, 0.10));
        }

        [Fact]
        public void TheLastWarningMayShareTheOutbreaksYear()
        {
            // History's dates: AD 165, 166, 166 (month 6), outbreak AD 166 (month 9).
            var warnings = new[] { SimTime.FromYear(165, 6), SimTime.FromYear(166, 0), SimTime.FromYear(166, 6) };
            Assert.Null(Harness.WarningsFinding(warnings, SimTime.FromYear(166, 9)));
        }

        [Fact]
        public void WarningsOutOfOrderOrMissingAreReported()
        {
            var late = new[] { SimTime.FromYear(165, 6), SimTime.FromYear(166, 0), SimTime.FromYear(166, 9) };
            Assert.NotNull(Harness.WarningsFinding(late, SimTime.FromYear(166, 9)));
            Assert.NotNull(Harness.WarningsFinding(new[] { SimTime.FromYear(165, 6), SimTime.FromYear(166, 0) }, SimTime.FromYear(166, 9)));
            Assert.Null(Harness.WarningsFinding(new SimTime[0], null));    // no outbreak, nothing to check
        }

        [Fact]
        public void WarningsResolvedDuringAnAbsenceMayShareADate()
        {
            var warnings = new[] { SimTime.FromYear(166, 0), SimTime.FromYear(166, 0), SimTime.FromYear(166, 0) };
            Assert.Null(Harness.WarningsFinding(warnings, SimTime.FromYear(166, 0), departed: SimTime.FromYear(160, 8)));
            Assert.NotNull(Harness.WarningsFinding(warnings, SimTime.FromYear(166, 0)));   // lived through, they must be in order
        }

        [Fact]
        public void TheRealRunsWarningsAreInOrder()
        {
            var sim = new Simulation(TestData.Load(), 701);
            while (sim.World.Plague.OutbreakYear == 0) sim.EndTurn();
            var warnings = sim.Log.Events.Where(e => e.Type == "plague.warning").Select(e => e.Time).ToList();
            var outbreak = sim.Log.Events.First(e => e.Type == "plague.outbreak").Time;
            Assert.Null(Harness.WarningsFinding(warnings, outbreak));
        }
    }
}
