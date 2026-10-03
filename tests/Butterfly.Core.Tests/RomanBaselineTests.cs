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
            var c = TestData.Load().Content;
            Assert.All(c.Commissions, x => Assert.Contains(c.Capabilities, cap => cap.Id == x.Capability));
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
