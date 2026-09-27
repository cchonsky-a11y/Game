using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Rome's own debasement: near half its economic decline, tied to Governance (decided 2026-09-28).</summary>
    public class CoinTests
    {
        private static Simulation WithCoinage(int stance, ulong seed = 4)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 10000;
            sim.GrantStake("faction", 0.5);
            sim.World.Institution("faction").Strength = 100;             // full sway over Governance
            sim.World.Attention = 100;
            if (stance != 0) Assert.True(sim.SetPolicy(PolicyIssue.Coinage, stance).Ok);
            return sim;
        }

        /// <summary>Plays to AD 175 and leaves with the faction still standing (this test does no upkeep).</summary>
        private static Arrival LeaveIn175(Simulation sim)
        {
            while (sim.Now.Year < 175) sim.EndTurn();
            sim.World.Institution("faction").Strength = 100;
            sim.World.Institution("faction").Loyalty = 100;
            return sim.JumpForTests();
        }

        [Fact]
        public void DebasementFollowsTheHistoricalSilver()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.Equal(0, sim.DebasementProgress(155), 9);
            Assert.Equal(1, sim.DebasementProgress(268), 9);
            Assert.Equal(1, sim.DebasementProgress(300), 9);
            double last = 0;
            for (int y = 155; y <= 268; y++)
            {
                Assert.True(sim.DebasementProgress(y) >= last - 1e-12);
                last = sim.DebasementProgress(y);
            }
            // Mild in the P0 era, steep in the third century.
            Assert.True(sim.DebasementProgress(175) < 0.1);
            Assert.True(sim.DebasementProgress(260) - sim.DebasementProgress(250) > sim.DebasementProgress(175) - sim.DebasementProgress(165));
        }

        [Fact]
        public void TheCoinCarriesNearHalfTheEconomicDeclineAndSomeOfGovernance()
        {
            var sim = new Simulation(TestData.Load(), 1);
            double econ = sim.Benchmark(Domain.Economy, 155) - sim.Benchmark(Domain.Economy, 268) - sim.HistoricalPlagueDrop(Domain.Economy);
            double gov = sim.Benchmark(Domain.Governance, 155) - sim.Benchmark(Domain.Governance, 268) - sim.HistoricalPlagueDrop(Domain.Governance);
            Assert.Equal(0.5 * econ, sim.CoinDeclineSpan(Domain.Economy), 9);
            Assert.Equal(sim.T.Get("policy.coin.governanceShare") * gov, sim.CoinDeclineSpan(Domain.Governance), 9);
            Assert.Equal(0, sim.CoinDeclineSpan(Domain.Medicine), 9);
            Assert.Equal(sim.CoinDeclineSpan(Domain.Economy) * sim.DebasementProgress(215), sim.CoinDecline(Domain.Economy, 215), 9);
        }

        [Fact]
        public void OnlyADefendedCoinChangesRomesDebasement()
        {
            Assert.Equal(0, new Simulation(TestData.Load(), 1).CoinStanceFactor(), 9);   // no voice: history
            Assert.Equal(0, WithCoinage(0).CoinStanceFactor(), 9);                          // Rome's practice
            var sound = WithCoinage(1);
            Assert.Equal(sound.PolicySway(), sound.CoinStanceFactor(), 9);
            var debased = WithCoinage(-1);
            Assert.Equal(-debased.PolicySway() * debased.T.Get("policy.coin.debaseExtra"), debased.CoinStanceFactor(), 9);
        }

        [Fact]
        public void SoundCoinLeavesRomeStrongerOnArrivalAndDebasementWeaker()
        {
            double Arrive(int stance, Domain d)
            {
                var sim = WithCoinage(stance);
                LeaveIn175(sim);
                return sim.World[d].Level;
            }
            foreach (var d in new[] { Domain.Economy, Domain.Governance })
            {
                Assert.True(Arrive(1, d) > Arrive(0, d), d + ": sound coin should beat Rome's practice");
                Assert.True(Arrive(-1, d) < Arrive(0, d), d + ": debasement should fall behind Rome's practice");
            }
        }

        [Fact]
        public void TheArrivalShowsTheCoin()
        {
            var a = LeaveIn175(WithCoinage(1));
            Assert.Equal("sound", a.CoinKey);
            Assert.Contains("rings true", a.Beats.Single(b => b.Name == "Wrongness").Text);

            var plain = new Simulation(TestData.Load(), 4);
            while (plain.Now.Year < 175) plain.EndTurn();
            var b2 = plain.JumpForTests();
            Assert.Equal(plain.HistoricalSilver(plain.Now.Year) < 0.6 ? "historyDebased" : "historyMild", b2.CoinKey);
            Assert.DoesNotContain("{", b2.Beats.Single(b => b.Name == "Wrongness").Text);
        }
    }
}
