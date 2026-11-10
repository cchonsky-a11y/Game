using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// The inventor's own body (Stakes and Impact pass, slice 1a; SYSTEMS §3): age, catching the plague, the fever,
    /// leaving the city, and death.
    /// </summary>
    public class HealthTests
    {
        private static GameData With(params (string Key, double Value)[] overrides) =>
            TestData.Load().WithTuning(overrides.ToDictionary(o => o.Key, o => o.Value));

        private static readonly (string, double)[] NoFever = { ("health.fever.chance", 0), ("health.fever.perYearOver", 0) };

        private static (string, double)[] Plus((string, double)[] a, params (string, double)[] b) => a.Concat(b).ToArray();

        private static void PlayTo(Simulation sim, Func<bool> until)
        {
            int guard = 0;
            while (!until() && !sim.Dead && guard++ < 400)
            {
                if (sim.World.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(true);
                if (sim.OutbreakAwaitingResponse) sim.RespondToPlague("none");
                sim.EndTurn();
            }
        }

        [Fact]
        public void TheInventorStartsAt28With57HealthyYearsAndAgesAYearAYear()
        {
            var sim = new Simulation(With(NoFever), 1);
            Assert.Equal(28, sim.InventorAge);
            Assert.Equal(57, sim.HealthyYearsLeft);                 // 85 − 28 (SYSTEMS §3; Corey, 2026-11-10)
            Assert.Equal("Age 28 · about 57 healthy years left", sim.LifeLine);
            for (int m = 0; m < 11; m++) sim.EndTurn();
            Assert.Equal(28, sim.InventorAge);
            sim.EndTurn();
            Assert.Equal(29, sim.InventorAge);
            Assert.Equal(56, sim.HealthyYearsLeft);
            Assert.Equal("You are 29. You have spent 1 of your 57 healthy years in Rome.", sim.LifeSpentLine());
        }

        [Fact]
        public void IllnessChanceFollowsTheDeathShareAndThePrecautionsTaken()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.Equal(0.35, sim.PlagueIllnessChance(0.10), 9);   // 3.5 × history's 10% dead
            Assert.Equal(0.105, sim.PlagueIllnessChance(0.03), 9);  // a contained outbreak
            Assert.Equal(0.6, sim.PlagueIllnessChance(0.5), 9);     // capped
            sim.World.CleanWater = true;
            Assert.Equal(0.28, sim.PlagueIllnessChance(0.10), 9);   // one precaution takes a fifth off
            sim.World.Invented.Add("soap");
            sim.World.Invented.Add("ward");
            sim.World.CompletedProjects.Add("quarantine");
            Assert.Equal(4, sim.PlaguePrecautions().Count);
            Assert.Equal(0.07, sim.PlagueIllnessChance(0.10), 9);   // all four: × 0.2
            sim.World.Withdrawn = true;
            Assert.Equal(0.0175, sim.PlagueIllnessChance(0.10), 9); // in the country: × 0.25
        }

        [Fact]
        public void CareLowersTheChanceOfDyingButNeverBelowTheFloor()
        {
            var sim = new Simulation(TestData.Load(), 1);
            Assert.Equal(0.30, sim.PlagueDeathChance(), 9);
            sim.World.Invented.Add("spirits");
            Assert.Equal(0.26, sim.PlagueDeathChance(), 9);
            sim.GrantStake("circle", 0.1);
            sim.GrantStake("sanctuary", 0.1);
            Assert.Equal(0.18, sim.PlagueDeathChance(), 9);         // − 0.05 − 0.03
            var floored = new Simulation(With(("health.plague.minDeathChance", 0.25)), 1);
            floored.World.Invented.Add("spirits");
            floored.GrantStake("circle", 0.1);
            Assert.Equal(0.25, floored.PlagueDeathChance(), 9);
        }

        [Fact]
        public void AnIllnessTakesTwoMonthsInBedFourWeakAndThreeYearsForGood()
        {
            var data = With(Plus(NoFever, ("health.plague.illnessPerDeathShare", 100), ("health.plague.maxIllnessChance", 1),
                                 ("health.plague.deathChance", 0), ("health.plague.minDeathChance", 0)));
            var sim = new Simulation(data, 7);
            PlayTo(sim, () => sim.World.Plague.Stage == PlagueState.Passed);
            Assert.False(sim.Dead);
            Assert.Contains(sim.Log.Events, e => e.Type == "inventor.ill");
            Assert.Equal(3, sim.World.HealthyYearsLost);
            var free = new List<int>();
            for (int m = 0; m < 7; m++) { free.Add(sim.AttentionThisMonth); sim.EndTurn(); }
            Assert.Equal(new[] { 0, 0, 3, 3, 3, 3, 4 }, free);
            Assert.Equal(85 - sim.InventorAge - 3, sim.HealthyYearsLeft);
        }

        [Fact]
        public void WorkUnderWayWaitsWhileTheInventorIsInBed()
        {
            var sim = new Simulation(With(NoFever), 3);
            Assert.True(sim.Assess().Ok);                           // two months of work
            sim.World.BedriddenMonths = 2;
            sim.EndTurn();
            sim.EndTurn();
            Assert.False(sim.MachineAssessed);
            sim.EndTurn();
            sim.EndTurn();
            Assert.True(sim.MachineAssessed);
        }

        [Fact]
        public void DeathEndsTheRunAndSaysWhatRomeKept()
        {
            var data = With(Plus(NoFever, ("health.plague.illnessPerDeathShare", 100), ("health.plague.maxIllnessChance", 1),
                                 ("health.plague.deathChance", 1)));
            var sim = new Simulation(data, 7);
            PlayTo(sim, () => false);
            Assert.True(sim.Dead);
            Assert.StartsWith("You died in Rome in AD 166, aged 39, of the pestilence.", sim.Epitaph[0]);
            Assert.Contains("years later:", sim.Epitaph[1]);
            Assert.True(sim.Now.Year > 166 + 20);                   // the world went on for the absence a jump would have made
            Assert.Null(sim.World.Return);                          // a death is not an arrival
            var death = sim.Log.Events.Single(e => e.Type == "inventor.death");
            Assert.NotEmpty(death.ImmediateCauses);                 // logged with its cause (the illness)
            Assert.Throws<InvalidOperationException>(() => sim.EndTurn());
            Assert.Throws<InvalidOperationException>(() => sim.Jump());
        }

        [Fact]
        public void TheSameSeedLivesAndDiesTheSameWay()
        {
            string Play(ulong seed)
            {
                var sim = new Simulation(TestData.Load(), seed);
                PlayTo(sim, () => sim.Now.Year >= 168);
                return sim.Log.Hash() + (sim.Dead ? " dead" : " alive");
            }
            for (ulong seed = 1; seed <= 12; seed++) Assert.Equal(Play(seed), Play(seed));
        }

        [Fact]
        public void SomeUnpreparedPlayersDieOfThePlagueAndMostDoNot()
        {
            int dead = 0, ill = 0;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                var sim = new Simulation(With(NoFever), seed);
                PlayTo(sim, () => sim.World.Plague.Stage == PlagueState.Passed);
                if (sim.Log.Events.Any(e => e.Type == "inventor.ill")) ill++;
                if (sim.Dead) dead++;
            }
            // History's outbreak: about 35% fall ill and about 30% of those die (about 1 in 10 unprepared players).
            Assert.InRange(ill, 75, 135);
            Assert.InRange(dead, 15, 50);
        }

        [Fact]
        public void LookingAtTheRiskNeverChangesTheGame()
        {
            var a = new Simulation(TestData.Load(), 5);
            var b = new Simulation(TestData.Load(), 5);
            for (int m = 0; m < 150 && !a.Dead; m++)
            {
                _ = b.PlagueRiskBand(); _ = b.PlagueRiskLine; _ = b.LifeLine; _ = b.HealthLine; _ = b.DepartureBriefing().ToList();
                foreach (var s in new[] { a, b })
                {
                    if (s.World.Promise.Status == PromiseStatus.Offered) s.AnswerPromise(true);
                    if (s.OutbreakAwaitingResponse) s.RespondToPlague("none");
                    s.EndTurn();
                }
            }
            Assert.Equal(a.Log.Hash(), b.Log.Hash());
        }

        [Fact]
        public void TheRiskIsSaidPlainlyOnceItIsTrueAndOnlyAsABand()
        {
            var sim = new Simulation(With(NoFever), 2);
            Assert.Equal("", sim.PlagueRiskLine);
            PlayTo(sim, () => sim.World.Plague.Stage >= 1);
            Assert.Equal("You can catch this. Your risk of falling ill: high.", sim.PlagueRiskLine);
            Assert.Contains(sim.DepartureBriefing(), l => l.Contains("you can catch it (risk of falling ill: high)"));
            sim.World.CleanWater = true; sim.World.Invented.Add("soap"); sim.World.Invented.Add("ward"); sim.World.CompletedProjects.Add("quarantine");
            Assert.Equal("low", sim.PlagueRiskBand());
        }

        [Fact]
        public void LeavingTheCityCostsWorkStandingAndAPromiseAndCutsTheRisk()
        {
            var sim = new Simulation(With(NoFever), 4);
            Assert.False(sim.Withdraw().Ok);                        // nothing to flee from yet
            PlayTo(sim, () => sim.World.Plague.Stage >= 2);
            sim.World.Promise.Status = PromiseStatus.Active;       // a promise to stay, however it was given
            Assert.True(sim.CanWithdraw);
            sim.World.Gold = 100;
            var felix = sim.World.People.First(p => p.Id == "Felix");
            felix.Regard = 2;
            bool knowsFelix = sim.Knows("Felix");
            double before = sim.World.Gold;
            var r = sim.Withdraw();
            Assert.True(r.Ok, r.Message);
            Assert.Equal(before - sim.WithdrawGold, sim.World.Gold, 6);
            Assert.Equal(0, sim.World.Attention);
            Assert.Equal(PromiseStatus.Broken, sim.World.Promise.Status);
            Assert.Equal(knowsFelix ? 1 : 2, felix.Regard);
            Assert.False(sim.Withdraw().Ok);                        // already away
            int months = 0;
            while (sim.Withdrawn && months++ < 40) { Assert.Equal(0, sim.AttentionThisMonth); sim.EndTurn(); }
            Assert.False(sim.Withdrawn);
            Assert.Equal(PlagueState.Passed, sim.World.Plague.Stage);   // back only once it has passed
            Assert.True(sim.World.WithdrawnMonths >= 6);
            Assert.Contains(sim.Log.Events, e => e.Type == "inventor.return");
        }

        [Fact]
        public void TheLateSummerFeverCostsAMonthAndNeverKillsTheYoung()
        {
            var sim = new Simulation(With(("health.fever.chance", 1), ("health.plague.illnessPerDeathShare", 0)), 9);
            while (sim.Now.Month != 7) sim.EndTurn();
            Assert.True(sim.Bedridden);
            Assert.Equal(0, sim.AttentionThisMonth);
            sim.EndTurn();
            Assert.True(sim.Weak);
            Assert.Equal(3, sim.AttentionThisMonth);
            for (int m = 0; m < 120; m++) sim.EndTurn();
            Assert.False(sim.Dead);                                 // under 50, the fever doesn't kill
            Assert.Equal(0, sim.World.HealthyYearsLost);
        }
    }
}
