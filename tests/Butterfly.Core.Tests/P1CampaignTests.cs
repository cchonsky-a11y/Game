using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>The executable two-jump validation runs end to end on the real build (scripted, not human).</summary>
    public class P1CampaignTests
    {
        [Fact]
        public void AScriptedCampaignReachesTwoArrivalsWithHumanEchoesAndABalancedLedger()
        {
            var r = Butterfly.Batch.P1Campaign.Play(TestData.Load(), 5);
            Assert.True(r.Arrive1 > r.Depart1 && r.Arrive2 > r.Arrive1);
            Assert.Contains(r.Echoes1, e => e.StartsWith("person:"));
            Assert.Contains(r.Echoes2, e => e.StartsWith("person:"));
            Assert.True(r.LedgerReconciles);
            Assert.Equal(0, r.AttentionConflicts);
            Assert.Equal(0, r.DuplicateTexts);
            Assert.Equal(0, r.RepeatedArrivalSentences);
            Assert.True(r.CommissionsDone >= 6);
        }
    }
}
