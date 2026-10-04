using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Hardening pass (2026-10-04): the echo audit found half the cast (Livia, Marcus, Lucan, Aulus) with no echo at all, the
    /// fountain's Gaius remembered only through powered workshops, and arrivals always led with Felix and Cassianus. Still at
    /// most two people an arrival.
    /// </summary>
    [Collection("Console")]
    public class P1EchoQualityTests
    {
        [Fact]
        public void EveryRecurringPersonCanBeRemembered()
        {
            var sim = new Simulation(TestData.Load(), 42);
            foreach (var p in sim.Data.Content.People.Where(p => p.Id != "Sextus"))   // Sextus is remembered through his choice's mark
            {
                Assert.True(p.Echoes.Count >= 2, p.Id + " has too few echo lines for two arrivals");
                foreach (var e in p.Echoes) foreach (var r in e.Requires) sim.Holds(r);
            }
            var all = sim.Data.Content.People.SelectMany(p => p.Echoes.Select(e => e.Text)).ToList();
            Assert.Equal(all.Count, all.Distinct().Count());
        }

        [Fact]
        public void TheFountainsGaiusIsRememberedWithoutPoweredWorkshops()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("fountain");
            sim.FindCommission("allotment")!.Status = CommissionStatus.Done;
            sim.PersonOf("Gaius")!.Regard = 5;                                          // the person you were closest to
            var a = sim.JumpForTests();
            Assert.Contains("person:Gaius", a.P1Echoes);
            Assert.True(a.P1Echoes.Count(e => e.StartsWith("person:")) <= sim.Data.Tuning.GetInt("echoes.peoplePerArrival"));
            Assert.Contains(a.Beats, b => b.Text.Contains("Crispi foundry"));
        }
    
        [Fact]
        public void UnintendedEchoesHaveSeveralSources()
        {
            // The strategy matrix found unintended echoes came only from Pollio's copy of the pump (missing in 37 of 300 first
            // arrivals). Bad copies now come from several threads, each with its own words on arrival.
            var c = TestData.Load().Content;
            var copies = c.Lives.Where(l => l.Capability.Length > 0 && l.Distorted).ToList();
            Assert.True(copies.Select(l => l.Capability).Distinct().Count() >= 3);
            Assert.True(copies.Select(l => l.Person).Distinct().Count() >= 3);
            foreach (var l in copies) Assert.True(c.Capabilities.First(n => n.Id == l.Capability).Echo.ContainsKey("distorted"), l.Id);
        }
    }
}
