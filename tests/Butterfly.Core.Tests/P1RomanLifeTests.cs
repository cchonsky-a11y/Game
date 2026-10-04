using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Roman life as a pillar (2026-10-04): reusable, conditional texture tied to calendar, people and place; never the same
    /// words twice; and in a plain run through AD 164 Rome keeps showing up every year while engineering doesn't take over.
    /// </summary>
    [Collection("Console")]
    public class P1RomanLifeTests
    {
        [Fact]
        public void NoAuthoredTextRepeatsWordForWord()
        {
            var c = TestData.Load().Content;
            var texts = c.Scenes.SelectMany(s => new[] { s.Text }.Concat(s.Variants)).ToList();
            Assert.Equal(texts.Count, texts.Distinct().Count());
            Assert.True(c.Scenes.Count(s => s.Category == SceneCategory.RomanLife) >= 25);
            Assert.Contains(c.Scenes, s => s.CooldownMonths > 0 && s.Variants.Count >= 2);
            Assert.Contains(c.Scenes, s => s.Triggers.Length > 0 && s.Category == SceneCategory.RomanLife);  // some ask a choice
            Assert.Contains(c.Scenes, s => s.Gold != 0);                                                      // some cost money
        }

        [Fact]
        public void AReusableSceneComesBackWithNewWordsAfterItsCooldown()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.World.ScenesSeen.AddRange(sim.Data.Content.Scenes.Where(s => s.Id != "market-day").Select(s => s.Id));
            foreach (var cm in sim.World.Commissions) cm.Status = CommissionStatus.Declined;
            var texts = new List<string>();
            for (int m = 0; m < 40; m++)
            {
                int from = sim.Log.Events.Count;
                sim.EndMonth();
                texts.AddRange(sim.Log.Events.Skip(from).Where(e => e.Target == "scene:market-day").Select(e => e.Text));
            }
            var def = sim.Data.Content.Scenes.First(s => s.Id == "market-day");
            Assert.Equal(1 + def.Variants.Count, texts.Count);                  // every text once, then no more
            Assert.Equal(texts.Count, texts.Distinct().Count());
            var turns = sim.World.RoutedScenes.Where(r => r.Id == "scene:market-day").Select(r => r.Turn).ToList();
            for (int k = 1; k < turns.Count; k++) Assert.True(turns[k] - turns[k - 1] >= def.CooldownMonths);
        }

        /// <summary>A plain game through AD 164: the workshop, every offer looked at and accepted, invitations accepted.</summary>
        private static Simulation PlainRun(ulong seed)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.ChooseSeeded("workshop");
            while (sim.Now.Year < 164)
            {
                foreach (var c in sim.World.Commissions.ToList())
                {
                    if (c.Status == CommissionStatus.Offered) sim.LookAtCommission(c.Id);
                    if (c.Status == CommissionStatus.TermsOffered) sim.AcceptCommission(c.Id);
                }
                foreach (var p in sim.World.Invitations.Where(p => p.Pending != InvitationOffer.None).ToList()) sim.AcceptInvitation(p.Institution);
                if (sim.PendingEvent != null) sim.Decide(sim.PendingEvent.Options[0].Id);
                sim.EndMonth();
            }
            return sim;
        }

        [Fact]
        public void RomeShowsUpEveryYearAndEngineeringDoesNotTakeOver()
        {
            foreach (ulong seed in new ulong[] { 1, 2, 3 })
            {
                var sim = PlainRun(seed);
                var picks = sim.World.RoutedScenes;
                for (int year = 1; year <= 8; year++)
                    Assert.True(picks.Any(p => p.Category == SceneCategory.RomanLife && (p.Turn - 1) / 12 == year), "seed " + seed + ": no Roman life in year " + year);
                double engineering = picks.Count(p => p.Category == SceneCategory.Engineering) / (double)picks.Count;
                Assert.True(engineering < 0.35, "seed " + seed + ": engineering " + engineering.ToString("0.00"));
                Assert.True(picks.Select(p => p.Category).Distinct().Count() >= 6, "seed " + seed + ": too few kinds of scene");
            }
        }
    }
}
