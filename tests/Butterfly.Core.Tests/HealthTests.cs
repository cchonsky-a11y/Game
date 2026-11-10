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
        public void RomesMedicineMovesTheHealthyYearsAYearForEveryFivePoints()
        {
            var sim = new Simulation(With(NoFever), 1);
            double history = sim.World[Domain.Medicine].Level;      // Medicine as history had it: sub-score 100
            Assert.Equal(85, sim.HealthyYearsCeiling);
            sim.World[Domain.Medicine].Level = history * 1.15;      // sub-score 115: three years more
            Assert.Equal(88, sim.HealthyYearsCeiling);
            Assert.Equal("Age 28 · about 60 healthy years left (Rome's medicine: +3)", sim.LifeLine);
            sim.World[Domain.Medicine].Level = history * 0.80;      // sub-score 80: four years fewer
            Assert.Equal(81, sim.HealthyYearsCeiling);
            Assert.Equal("Age 28 · about 53 healthy years left (Rome's medicine: −4)", sim.LifeLine);
            sim.World[Domain.Medicine].Level = history * 3;         // never above the ceiling
            Assert.Equal(100, sim.HealthyYearsCeiling);
            sim.World[Domain.Medicine].Level = history * 0.01;      // or below the floor
            Assert.Equal(70, sim.HealthyYearsCeiling);
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
            Assert.Equal(sim.HealthyYearsCeiling - sim.InventorAge - 3, sim.HealthyYearsLeft);
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
        public void LeavingTheCityCostsWorkAndAPromiseAndLastsUntilItHasPassed()
        {
            var sim = new Simulation(With(NoFever), 4);
            Assert.False(sim.Withdraw().Ok);                        // nothing to flee from yet
            PlayTo(sim, () => sim.World.Plague.Stage >= 2);
            sim.World.Promise.Status = PromiseStatus.Active;       // a promise to stay, however it was given
            Assert.True(sim.CanWithdraw);
            sim.World.Gold = 100;
            double before = sim.World.Gold;
            var r = sim.Withdraw();
            Assert.True(r.Ok, r.Message);
            Assert.Equal(before - sim.WithdrawGold, sim.World.Gold, 6);
            Assert.Equal(0, sim.World.Attention);
            Assert.Equal(PromiseStatus.Broken, sim.World.Promise.Status);
            Assert.False(sim.Withdraw().Ok);                        // already away
            int months = 0;
            while (sim.Withdrawn && months++ < 40) { Assert.Equal(0, sim.AttentionThisMonth); sim.EndTurn(); }
            Assert.False(sim.Withdrawn);
            Assert.Equal(PlagueState.Passed, sim.World.Plague.Stage);   // back only once it has passed
            Assert.True(sim.World.WithdrawnMonths >= 6);
            Assert.Contains(sim.Log.Events, e => e.Type == "inventor.return");
        }

        [Fact]
        public void AStrangerWhoLeavesTheCityIsNotBlamedButAHealerOrAnOfficerIs()
        {
            // No duty: leaving was ordinary for anyone who could pay for it.
            var stranger = new Simulation(With(NoFever), 4);
            PlayTo(stranger, () => stranger.World.Plague.Stage >= 2);
            stranger.World.Gold = 100;
            var felix = stranger.World.People.First(p => p.Id == "Felix");
            felix.Regard = 3;
            Assert.Empty(stranger.WithdrawDuties());
            Assert.Equal("No one would hold it against you if you left the city.", stranger.WithdrawStigmaLine());
            Assert.Contains("No one expects it of you to stay", stranger.Withdraw().Message);
            Assert.Equal(3, felix.Regard);
            Assert.DoesNotContain(stranger.Log.Events, e => e.Type == "person.regard" && e.Text.Contains("You didn't"));

            // A member of the Physicians' Circle who also holds office in the guild: both are held against them.
            var healer = new Simulation(With(NoFever), 4);
            PlayTo(healer, () => healer.World.Plague.Stage >= 2);
            healer.World.Gold = 100;
            healer.GrantStake("circle", 0.1);
            healer.GrantStake("guild", 0.1);
            var guild = healer.World.Institution("guild");
            guild.Rank = Simulation.Officer;
            var circle = healer.World.Institution("circle");
            double circleBefore = circle.Loyalty, guildBefore = guild.Loyalty;
            Assert.Equal(2, healer.WithdrawDuties().Count);
            Assert.StartsWith("Leaving the city would be held against you:", healer.WithdrawStigmaLine());
            Assert.Contains("It is held against you", healer.Withdraw().Message);
            Assert.Equal(Math.Max(0, circleBefore - 10), circle.Loyalty, 6);
            Assert.Equal(Math.Max(0, guildBefore - 10), guild.Loyalty, 6);
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
