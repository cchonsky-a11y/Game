using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 polish pass (2026-10-04): fallback odd jobs no longer print the same sentence every month. The lines rotate in a
    /// fixed order and grow with the people you know; pay, Attention and seeded results are unchanged.
    /// </summary>
    [Collection("Console")]
    public class P1OddJobTests
    {
        [Fact]
        public void OddJobsRotateTheirWordsWithoutChangingThePay()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            var texts = new System.Collections.Generic.List<string>();
            for (int m = 0; m < 8; m++)
            {
                double expected = sim.WorkPay("odd") * (1 - sim.WorkTaxRate());
                double before = sim.World.Gold;
                Assert.True(sim.Work("odd").Ok);
                Assert.Equal(expected, sim.World.Gold - before, 6);                    // the pay is the pay
                texts.Add(sim.Log.Events.Last(e => e.Type == "personal.work").Text);
                sim.EndMonth();
            }
            for (int i = 1; i < texts.Count; i++) Assert.NotEqual(texts[i - 1], texts[i]);   // never the same words twice running
            Assert.True(texts.Distinct().Count() >= 4);
            Assert.DoesNotContain(texts, t => t.Contains("{"));
        }

        [Fact]
        public void ThePeopleYouKnowSendYouWork()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("fountain");
            sim.FindCommission("cellarpump")!.Status = CommissionStatus.Offered;     // you know Felix
            var texts = Enumerable.Range(0, 12).Select(_ => { Assert.True(sim.Work("odd").Ok); var t = sim.Log.Events.Last(e => e.Type == "personal.work").Text; sim.EndMonth(); return t; }).ToList();
            Assert.Contains(texts, t => t.Contains("Felix"));
            Assert.DoesNotContain(texts, t => t.Contains("Gaius"));                     // not before you've met him
        }
    }
}
