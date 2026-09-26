using System.Collections.Generic;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class RngTests
    {
        [Fact]
        public void SameSeedGivesSameSequence()
        {
            var a = new Rng(42);
            var b = new Rng(42);
            for (int i = 0; i < 1000; i++) Assert.Equal(a.NextULong(), b.NextULong());
        }

        [Fact]
        public void KnownSplitMix64Values()
        {
            // Reference outputs of SplitMix64 for seed 0 — guards against accidental algorithm changes.
            var r = new Rng(0);
            Assert.Equal(0xE220A8397B1DCDAFUL, r.NextULong());
            Assert.Equal(0x6E789E6AA1B965F4UL, r.NextULong());
            Assert.Equal(0x06C45D188009454FUL, r.NextULong());
        }

        [Fact]
        public void DifferentSeedsDiffer()
        {
            Assert.NotEqual(new Rng(1).NextULong(), new Rng(2).NextULong());
        }

        [Fact]
        public void NextDoubleAndIntStayInRange()
        {
            var r = new Rng(7);
            for (int i = 0; i < 10000; i++)
            {
                double d = r.NextDouble();
                Assert.InRange(d, 0.0, 0.9999999999);
                Assert.InRange(r.NextInt(3, 9), 3, 8);
            }
        }
    }

    public class TimeTests
    {
        [Fact]
        public void TurnsAdvanceByTuningMonthsAndTickYears()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.Equal(155, sim.Now.Year);
            for (int i = 0; i < 4; i++) sim.EndTurn();
            Assert.Equal(156, sim.Now.Year);
            Assert.Equal(0, sim.Now.Month);
            Assert.Contains(sim.Log.Events, e => e.Type == "year.start" && e.Time.Year == 156);
        }

        [Fact]
        public void StampIsStable()
        {
            Assert.Equal("AD 155-04", SimTime.FromYear(155, 3).Stamp);
        }
    }

    public class EventLogTests
    {
        [Fact]
        public void EventsCarryCausesActorsAndEffects()
        {
            var log = new EventLog();
            var a = log.Record(SimTime.FromYear(155), "test.a", "x", null, new[] { "player" }, null, "A");
            var b = log.Record(SimTime.FromYear(155), "test.b", "x", new[] { a.Id, a.Id, 0 }, new[] { "world" },
                new[] { new Effect("x", 1, 2) }, "B");
            Assert.Equal(new List<int> { 1 }, b.ImmediateCauses);
            Assert.Equal(1, b.Effects[0].Delta);
            Assert.Equal("world", b.Actors[0]);
        }

        [Fact]
        public void SameSeedProducesIdenticalLogHash()
        {
            string Run()
            {
                var sim = new Simulation(TestData.Load(), 42);
                for (int i = 0; i < 12; i++) sim.EndTurn();
                return sim.Log.Hash();
            }
            Assert.Equal(Run(), Run());
        }
    }
}
