using System.Linq;
using Butterfly.Batch;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class BatchTests
    {
        [Fact]
        public void EveryStrategyPlaysToArrivalDeterministically()
        {
            foreach (var strategy in BatchRunner.Strategies())
                foreach (var (timing, year) in BatchRunner.Timings)
                {
                    var a = BatchRunner.Play(TestData.Load(), strategy, 42, timing, year);
                    var b = BatchRunner.Play(TestData.Load(), strategy, 42, timing, year);
                    Assert.Equal(a.LogHash, b.LogHash);
                    Assert.Equal(a.IndexAfter, b.IndexAfter);
                }
        }

        [Fact]
        public void ReportHasSummaryTableForAllStrategiesAndTimings()
        {
            var results = BatchRunner.RunAll(TestData.Load(), 5);
            Assert.Equal(5 * 3 * 2, results.Count);
            string report = BatchRunner.Report(results, 5);
            foreach (var s in new[] { "Balanced", "Specialized", "Neglectful" }) Assert.Contains("| " + s + " |", report);
            Assert.Contains("Early", report);
            Assert.Contains("Late", report);
            var rates = BatchRunner.WinRates(results);
            foreach (var timing in BatchRunner.Timings.Select(t => t.Name))
                Assert.Equal(1.0, BatchRunner.Strategies().Sum(s => rates[(timing, s.Name)]), 6);
        }

        [Fact]
        public void EarlyJumpBreaksTheAcceptedPromiseAndLateJumpKeepsIt()
        {
            var early = BatchRunner.Play(TestData.Load(), new BalancedStrategy(), 3, "Early", 162);
            var late = BatchRunner.Play(TestData.Load(), new BalancedStrategy(), 3, "Late", 169);
            Assert.Contains(early.Promise, new[] { PromiseStatus.Broken, PromiseStatus.Refused });
            Assert.Equal(PromiseStatus.Kept, late.Promise);
        }

        [Fact]
        public void NeglectIsNeverTheBestStrategyOnAverage()
        {
            var results = BatchRunner.RunAll(TestData.Load(), 20);
            var rates = BatchRunner.WinRates(results);
            foreach (var timing in BatchRunner.Timings.Select(t => t.Name))
                Assert.True(rates[(timing, "Neglectful")] < 0.2);
        }
    }
}
