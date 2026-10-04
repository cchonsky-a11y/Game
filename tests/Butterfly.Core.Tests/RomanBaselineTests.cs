using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The mandatory Roman-baseline check (P1, master handoff §16): never "invent" what Rome had. Every invention and every
    /// capability says what Rome already did, the bottleneck, and the kind of leap; every commission names the capability it moves.
    /// </summary>
    [Collection("Console")]
    public class RomanBaselineTests
    {
        [Fact]
        public void EveryInventionStartsFromRomesBaseline()
        {
            foreach (var i in TestData.Load().Content.Inventions)
            {
                Assert.False(string.IsNullOrWhiteSpace(i.Baseline), i.Id + ": what did Rome already have?");
                Assert.False(string.IsNullOrWhiteSpace(i.Bottleneck), i.Id + ": what stops the next step?");
                Assert.Contains(i.Leap, CapabilityDef.Leaps);
            }
        }

        [Fact]
        public void EveryCommissionMovesANamedCapability()
        {
            // Not every job is technical (a favor, a bath-house pipe), but every capability a job names must exist, and most move one.
            var c = TestData.Load().Content;
            foreach (var x in c.Commissions)
            {
                if (x.Capability.Length > 0) Assert.Contains(c.Capabilities, cap => cap.Id == x.Capability);
                foreach (var w in x.Work.Where(w => w.Capability.Length > 0)) Assert.Contains(c.Capabilities, cap => cap.Id == w.Capability);
            }
            Assert.True(c.Commissions.Count(x => x.Capability.Length > 0 || x.Work.Any(w => w.Capability.Length > 0)) * 2 > c.Commissions.Count);
        }

        [Fact]
        public void TheInventionsListSaysWhatRomeAlreadyHas()
        {
            var sim = new Simulation(TestData.Load(), 42);
            string output = ConsoleTests.Play(sim, "inventions");
            Assert.Contains("Rome already: Bow and pole lathes", output);
        }
    }
}
