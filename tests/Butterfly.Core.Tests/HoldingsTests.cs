using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>Institution gold and corruption (decided 2026-09-26).</summary>
    public class HoldingsTests
    {
        private static Simulation WithCircle(ulong seed = 5)
        {
            var sim = new Simulation(TestData.Load(), seed);
            sim.World.Gold = 2000;
            sim.World.Attention = 100;
            sim.GrantStake("circle", 0.5);
            return sim;
        }

        [Fact]
        public void EndowingAddsHoldingsAndTheMinimumMakesItEndowed()
        {
            var sim = WithCircle();
            var c = sim.World.Institution("circle");
            Assert.True(sim.Endow("circle", 30).Ok);
            Assert.False(c.Endowed);
            Assert.True(sim.Endow("circle", 30).Ok);
            Assert.True(c.Endowed);
            Assert.Equal(60, c.Holdings);
            Assert.True(sim.Endow("circle", 100).Ok);
            Assert.Equal(160, c.Holdings);
        }

        [Fact]
        public void GrowthRateRunsFromZeroToOnePointFivePercentWithEconomy()
        {
            var sim = WithCircle();
            sim.World[Domain.Economy].Level = 0;
            Assert.Equal(0, sim.HoldingsGrowthRate(), 9);
            sim.World[Domain.Economy].Level = 100;
            Assert.Equal(0.015, sim.HoldingsGrowthRate(), 9);
            sim.World[Domain.Economy].Level = 50;
            Assert.Equal(0.0075, sim.HoldingsGrowthRate(), 9);
        }

        [Fact]
        public void CorruptionChanceFollowsTheFormula()
        {
            var sim = WithCircle();
            var c = sim.World.Institution("circle"); // Demetria is honest
            sim.Endow("circle", 60);
            Assert.Equal(0.05 * 1 * 1 * (1 - 0.3), sim.CorruptionChance(c), 9);
            sim.Endow("circle", 200); // large holdings
            Assert.Equal(0.05 * 2 * 1 * (1 - 0.3), sim.CorruptionChance(c), 9);
            sim.Audit("circle");
            Assert.Equal(0.05 * 2 * 0.5 * (1 - 0.3), sim.CorruptionChance(c), 9);

            sim.GrantStake("faction", 0.5); // Varro is venal
            var f = sim.World.Institution("faction");
            sim.Endow("faction", 60);
            Assert.Equal(0.05 * 1 * 1 * 1.3, sim.CorruptionChance(f), 9);
        }

        [Fact]
        public void SeverityWeightsShiftWithAuditAndIntegrity()
        {
            var sim = WithCircle();
            sim.GrantStake("faction", 0.5);
            var honest = sim.World.Institution("circle");
            var venal = sim.World.Institution("faction");
            Assert.Equal(new double[] { 50, 40, 10 }, sim.CorruptionWeights(honest));
            Assert.Equal(new double[] { 30, 40, 30 }, sim.CorruptionWeights(venal));
            sim.Audit("faction");
            Assert.Equal(new double[] { 60, 25, 15 }, sim.CorruptionWeights(venal));
        }

        [Fact]
        public void PaymentShareDependsOnLoyaltyAndCorruption()
        {
            var sim = WithCircle();
            var c = sim.World.Institution("circle");
            c.Loyalty = 80;
            Assert.Equal(1, sim.PaymentShare(c));
            c.HasDrifted = true;
            Assert.Equal(0.5, sim.PaymentShare(c));
            c.HasDrifted = false;
            c.Corruption = CorruptionLevel.Minor;
            Assert.Equal(0.5, sim.PaymentShare(c));
            c.Corruption = CorruptionLevel.Major;
            Assert.Equal(0, sim.PaymentShare(c));
            c.Corruption = CorruptionLevel.None;
            c.Loyalty = 5;
            Assert.Equal(0, sim.PaymentShare(c));
        }

        [Fact]
        public void InstitutionsPayTheirDomainDebtOnlyInTheThirtyYearWindow()
        {
            var sim = WithCircle();
            var c = sim.World.Institution("circle");
            sim.Charter("circle");
            sim.Endow("circle", 300);
            sim.Audit("circle");
            c.Loyalty = 90;
            sim.World[Domain.Medicine].Debt = 40;
            sim.World[Domain.Economy].Debt = 40;
            sim.JumpForTests();
            var payments = sim.Log.Events.Where(e => e.Type == "debt.paidByInstitution").ToList();
            Assert.NotEmpty(payments);
            Assert.All(payments, e => Assert.Equal("medicine", e.Target));              // only its own domain
            Assert.All(payments, e => Assert.True(e.Time.Year - sim.DepartureYear <= 30)); // only in the window
            Assert.DoesNotContain(sim.Log.Events, e => e.Type == "holdings.grow" && e.Time.Year - sim.DepartureYear > 30);
            Assert.True(c.DebtPaidAway > 0);
        }

        [Fact]
        public void CorruptionIsCheckedOnlyInTheWindowAndCanCaptureTheInstitution()
        {
            int corrupted = 0, captured = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.World.Gold = 2000;
                sim.World.Attention = 100;
                sim.GrantStake("faction", 0.5); // venal leader, large holdings, no audit: the riskiest case
                sim.Endow("faction", 500);
                sim.JumpForTests();
                var events = sim.Log.Events.Where(e => e.Type == "institution.corruption").ToList();
                Assert.All(events, e => Assert.True(e.Time.Year - sim.DepartureYear <= 30));
                if (events.Count > 0) corrupted++;
                var f = sim.World.Institution("faction");
                if (f.Corruption == CorruptionLevel.Total)
                {
                    Assert.NotNull(f.ForcedOutcome);
                    captured++;
                }
                if (events.Count > 0) Assert.Contains(sim.Log.Events, e => e.Type == "debt.corruption");
            }
            Assert.InRange(corrupted, 40, 140);
            // The Discovery beat reveals corruption and its level.
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var sim = new Simulation(TestData.Load(), seed);
                sim.World.Gold = 2000;
                sim.World.Attention = 100;
                sim.GrantStake("faction", 0.5);
                sim.Endow("faction", 500);
                var arrival = sim.JumpForTests();
                var f = sim.World.Institution("faction");
                if (f.Corruption == CorruptionLevel.None || sim.OutcomeOf(f) == InstitutionOutcome.Dissolved) continue;
                Assert.Contains("(" + f.Corruption.ToString().ToLowerInvariant() + " corruption)", arrival.Beats.First(b => b.Name == "Discovery").Text);
                break;
            } // ≈ 1 − (1 − 0.13)^3 ≈ 34% of runs
            Assert.True(captured > 0);
        }

        [Fact]
        public void RiskBandsFollowTheChance()
        {
            var sim = WithCircle();
            sim.GrantStake("faction", 0.5);
            var c = sim.World.Institution("circle");
            var f = sim.World.Institution("faction");
            sim.Endow("circle", 60);   // honest, small: 3.5%
            sim.Endow("faction", 60);  // venal, small: 6.5%
            Assert.Equal("Low", sim.CorruptionRiskBand(c));
            Assert.Equal("Medium", sim.CorruptionRiskBand(f));
            sim.Endow("faction", 200); // venal, large: 13%
            Assert.Equal("High", sim.CorruptionRiskBand(f));
        }

        [Fact]
        public void BriefShowsHoldingsCorruptionRiskAndPayments()
        {
            var sim = WithCircle();
            sim.Endow("circle", 100);
            var lines = sim.DepartureBriefing().ToList();
            Assert.Contains(lines, l => l.Contains("holds 100 gold"));
            Assert.Contains(lines, l => l.StartsWith("  Corruption risk: Low") && l.Contains("minor (50%)") && l.Contains("total (10%)"));
            Assert.DoesNotContain(lines, l => l.Contains("per decade")); // a band, never a number or an outcome
            Assert.Contains(lines, l => l.Contains("toward Medicine debt"));
        }
    }

    public class PacingTests
    {
        [Fact]
        public void TwentySixMonthTurnsMakeATenYearEra()
        {
            var t = TestData.Load().Tuning;
            Assert.Equal(20, t.GetInt("time.eraTurns") * t.GetInt("time.monthsPerTurn") / 12);
        }

        [Fact]
        public void QuietTurnsAdvanceUntilSomethingNeedsThePlayer()
        {
            var sim = new Simulation(TestData.Load(), 8);
            sim.ChooseSeeded("fountain");
            int advanced = sim.AdvanceUntilDecision();
            Assert.True(advanced >= 1);
            Assert.NotEmpty(sim.PendingDecisions());
        }

        [Fact]
        public void OpenPromptsStopAutoAdvance()
        {
            var sim = new Simulation(TestData.Load(), 8);
            Assert.Contains(sim.PendingDecisions(), r => r.Contains("fountain or workshop"));
            while (sim.World.Promise.Status == PromiseStatus.NotOffered) sim.AdvanceUntilDecision();
            Assert.Contains(sim.PendingDecisions(), r => r.Contains("Demetria"));
        }

        [Fact]
        public void EraEndStopsAutoAdvance()
        {
            var sim = new Simulation(TestData.Load(), 8);
            sim.ChooseSeeded("workshop");
            while (!sim.EraOver)
            {
                if (sim.World.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(false);
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                sim.AdvanceUntilDecision();
            }
            Assert.Equal(sim.EraTurns + 1, sim.Turn);
            Assert.Contains(sim.PendingDecisions(), r => r.Contains("turns are over"));
        }
    }
}
