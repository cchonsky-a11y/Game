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
            Assert.Equal(5 * BatchRunner.Strategies().Length * 2, results.Count);
            string report = BatchRunner.Report(TestData.Load(), results, 5);
            foreach (var s in new[] { "Balanced (Pay-down)", "Specialized", "Neglectful", "Endow", "Split", "FreeMarket", "Interventionist" }) Assert.Contains("| " + s + " |", report);
            Assert.Contains("Attention (P0 pacing): demand", report);
            Assert.Contains("Audit charter check", report);
            Assert.Contains("Early", report);
            Assert.Contains("Late", report);
            var rates = BatchRunner.WinRates(results);
            foreach (var timing in BatchRunner.Timings.Select(t => t.Name))
                Assert.Equal(1.0, BatchRunner.Strategies().Sum(s => rates[(timing, s.Name)]), 6);
        }

        [Fact]
        public void EarlyJumpBreaksTheAcceptedPromiseAndLateJumpKeepsIt()
        {
            var early = BatchRunner.Play(TestData.Load(), new BalancedStrategy(), 3, "Early", BatchRunner.Timings[0].Year);
            var late = BatchRunner.Play(TestData.Load(), new BalancedStrategy(), 3, "Late", BatchRunner.Timings[1].Year);
            Assert.Contains(early.Promise, new[] { PromiseStatus.Broken, PromiseStatus.Refused });
            Assert.Equal(PromiseStatus.Kept, late.Promise);
        }

        [Fact]
        public void TimingWinRatesSumToOne()
        {
            var rates = BatchRunner.TimingWinRates(BatchRunner.RunAll(TestData.Load(), 5));
            Assert.Equal(1.0, rates.Values.Sum(), 6);
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
