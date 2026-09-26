using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The plague: the P0 crisis (SYSTEMS §6). It always passes through three visible warning stages,
    /// at most one per year, before it can break out. Debt tiers speed it up; debt makes it worse;
    /// Medicine, Governance and preparations make the region resilient. Outcomes branch.
    /// </summary>
    public sealed partial class Simulation
    {
        public static readonly string[] PlagueResponses = { "quarantine", "hospice", "none" };

        private void InitPlague()
        {
            var p = World.Plague;
            World.Population = T.Get("plague.startPopulation");
            p.FirstWarningYear = T.GetInt("plague.firstWarningYear") + Rng.NextInt(0, T.GetInt("plague.firstWarningJitterYears") + 1);
        }

        /// <summary>Medicine's debt tier alone drives the plague's odds (decided 2026-09-26).</summary>
        public DebtTier PlagueTier() => World[Domain.Medicine].Tier;

        /// <summary>Chance per year that a visible warning stage advances (SYSTEMS §6: tiers raise crisis chance).</summary>
        public double PlagueAdvanceChance()
        {
            double chance = T.Get("plague.advanceChance." + PlagueTier().ToString().ToLowerInvariant());
            if (!World.CleanWater) chance += T.Get("plague.foulWaterAdvanceBonus");
            if (Now.Year >= T.GetInt("plague.forceAdvanceFromYear")) chance = 1;
            return Math.Min(1, chance);
        }

        private void PlagueYearTick()
        {
            var p = World.Plague;
            if (p.Stage == PlagueState.Quiet)
            {
                if (Now.Year >= p.FirstWarningYear) AdvancePlague();
                return;
            }
            if (!p.IsWarning || p.StageEnteredYear >= Now.Year) return;
            if (Rng.Chance(PlagueAdvanceChance())) AdvancePlague();
        }

        private void AdvancePlague()
        {
            var p = World.Plague;
            int before = p.Stage;
            p.Stage++;
            p.StageEnteredYear = Now.Year;
            var causes = new List<int> { p.LastStageEventId, CauseOf(TierKey(Domain.Medicine)) };
            if (!World.CleanWater) causes.Add(CauseOf("fountain.clean"));
            string text = PlagueStageText(p.Stage);
            var e = Record(p.Stage == PlagueState.Outbreak ? "plague.outbreak" : "plague.warning", "plague", causes,
                new[] { "world" }, new[] { new Effect("plague.stage", before, p.Stage) }, text);
            p.LastStageEventId = e.Id;
            if (p.Stage == 1) OfferPromise(e.Id);
            if (p.Stage == PlagueState.Outbreak)
            {
                p.OutbreakYear = Now.Year;
                if (IsAway) ResolveOutbreak(AutomaticResponse(), new[] { "world" });
            }
        }

        public static string PlagueStageText(int stage)
        {
            switch (stage)
            {
                case 1: return "Warning 1 of 3 — Rumors from the East: soldiers back from the Parthian war speak of a pestilence in Seleucia.";
                case 2: return "Warning 2 of 3 — Fever at Ostia: dockworkers fall sick after the grain fleet comes in.";
                case 3: return "Warning 3 of 3 — The sick fill the Subura: physicians report fever, rash and black stools among the poor.";
                case 4: return "Outbreak — the pestilence breaks out across Rome.";
                default: return "The pestilence has passed.";
            }
        }

        public bool OutbreakAwaitingResponse => World.Plague.Stage == PlagueState.Outbreak && World.Plague.Response == null;

        public IEnumerable<string> AvailablePlagueResponses() =>
            PlagueResponses.Where(r => r != "hospice" || HospiceAvailable());

        /// <summary>Player decision when the outbreak begins (the plague's branch point).</summary>
        public CommandResult RespondToPlague(string response)
        {
            if (!OutbreakAwaitingResponse) return CommandResult.Fail("There is no outbreak to respond to.");
            if (!AvailablePlagueResponses().Contains(response)) return CommandResult.Fail("You can't choose '" + response + "' now.");
            double cost = T.Get("plague.response." + response + ".gold");
            if (World.Gold < cost) return CommandResult.Fail("That response costs " + F(cost) + " gold.");
            var attention = CheckAttention(T.GetInt("plague.response." + response + ".attention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("plague.response." + response + ".attention"));
            ChooseResponse(response, new[] { "player" });
            return CommandResult.Success("Response chosen: " + response + ". It takes effect at the end of the turn.");
        }

        private void ChooseResponse(string response, IEnumerable<string> actors)
        {
            var p = World.Plague;
            double cost = T.Get("plague.response." + response + ".gold");
            double before = World.Gold;
            SpendGold(Math.Min(cost, World.Gold));
            p.Response = response;
            string text;
            switch (response)
            {
                case "quarantine": text = "The district is closed: sick houses are chalked and the docks held."; break;
                case "hospice": text = "The physicians' circle opens a hospice beside the fountain."; break;
                default: text = "No organized response: each household fends for itself."; break;
            }
            var e = Record("plague.response", "plague", new[] { p.LastStageEventId }, actors,
                new[] { new Effect(GoldKey, before, World.Gold) }, text);
            p.ResponseEventId = e.Id;
        }

        private void ResolvePendingOutbreak()
        {
            var p = World.Plague;
            if (p.Stage != PlagueState.Outbreak || p.Severity > 0) return;
            if (p.Response == null) ChooseResponse("none", new[] { "world" });
            ResolveOutbreakDamage();
        }

        private void ResolveOutbreak(string response, IEnumerable<string> actors)
        {
            ChooseResponse(response, actors);
            ResolveOutbreakDamage();
        }

        /// <summary>Hazard: base plus Medicine debt (severity scales with debt), plus foul water.</summary>
        public double PlagueHazard()
        {
            double hazard = T.Get("plague.baseHazard") + World[Domain.Medicine].Debt * T.Get("plague.hazardPerMedicineDebt");
            if (!World.CleanWater) hazard += T.Get("plague.foulWaterHazard");
            return Math.Min(T.Get("plague.maxHazard"), hazard);
        }

        /// <summary>Resilience from Medicine and Governance levels, preparations, response and institutions.</summary>
        public double PlagueResilience(string? response)
        {
            double r = World[Domain.Medicine].Level / 100.0 * T.Get("plague.resiliencePerMedicine")
                     + World[Domain.Governance].Level / 100.0 * T.Get("plague.resiliencePerGovernance")
                     + World.PlagueResilienceBonus
                     + InstitutionPlagueResilience();
            if (response != null) r += T.Get("plague.response." + response + ".resilience");
            return Math.Min(T.Get("plague.maxResilience"), r);
        }

        /// <summary>Governance and Economy debt tiers make the plague worse (decided 2026-09-26).</summary>
        public double PlagueSeverityMultiplier() =>
            1 + T.Get("plague.severityPerTier." + World[Domain.Governance].Tier.ToString().ToLowerInvariant())
              + T.Get("plague.severityPerTier." + World[Domain.Economy].Tier.ToString().ToLowerInvariant());

        /// <summary>Severity = Hazard × Exposure × (1 − Resilience) (the SYSTEMS §9 loss form), raised by Governance and Economy debt.</summary>
        public double PlagueSeverity(string? response) =>
            PlagueHazard() * T.Get("plague.exposure") * (1 - PlagueResilience(response)) * PlagueSeverityMultiplier();

        private void ResolveOutbreakDamage()
        {
            var p = World.Plague;
            p.Severity = Math.Max(0.01, PlagueSeverity(p.Response));
            p.StruckInAbsence = IsAway;
            var toll = ApplyPlagueDamage(p.Severity, new List<int> { p.LastStageEventId, p.ResponseEventId }, out double deaths);
            p.Deaths = deaths;

            OnPlagueResolved(toll.Id);
            ChooseOpening(toll.Id);

            p.Stage = PlagueState.Passed;
            var passed = Record("plague.passed", "plague", new[] { toll.Id }, new[] { "world" },
                new[] { new Effect("plague.stage", PlagueState.Outbreak, PlagueState.Passed) }, "The worst of the pestilence has passed.");
            p.LastStageEventId = passed.Id;
            OnPlaguePassedForPromise(passed.Id);
        }

        /// <summary>Tolls, level damage and debt release shared by the plague and its later recurrences.</summary>
        internal GameEvent ApplyPlagueDamage(double sev, List<int> causes, out double deaths)
        {
            causes.AddRange(new[] { CauseOf(DebtKey(Domain.Medicine)), CauseOf(LevelKey(Domain.Medicine)), CauseOf(LevelKey(Domain.Governance)),
                CauseOf(TierKey(Domain.Governance)), CauseOf(TierKey(Domain.Economy)),
                CauseOf("plague.resilience"), CauseOf("fountain.clean") });

            double popBefore = World.Population;
            deaths = World.Population * sev * T.Get("plague.deathRatePerSeverity");
            World.Population -= deaths;
            double goldBefore = World.Gold;
            World.Gold = Math.Max(0, World.Gold - sev * T.Get("plague.goldLossPerSeverity"));
            string label = sev < T.Get("plague.severity.mild") ? "mild" : sev < T.Get("plague.severity.grave") ? "grave" : "catastrophic";
            var toll = Record("plague.toll", "plague", causes, new[] { "world" },
                new[] { new Effect("population", popBefore, World.Population), new Effect(GoldKey, goldBefore, World.Gold),
                        new Effect("plague.severity", 0, sev) },
                "The pestilence is " + label + " (severity " + F(sev) + "): about " + F(deaths) + " thousand dead in Rome.");

            foreach (var d in DomainInfo.All)
                ChangeLevel(d, -sev * T.Get("plague.damage." + d.Key()), "plague.damage", new[] { toll.Id }, new[] { "world" },
                    "The pestilence strikes " + d + ".");

            // A crisis releases debt (SYSTEMS §6), violently, and resets what people expect.
            foreach (var d in DomainInfo.All)
            {
                var s = World[d];
                s.Peak = s.Level;
                if (s.Debt <= 0) continue;
                double before = s.Debt;
                s.Debt *= 1 - T.Get("plague.debtRelease");
                Record("debt.release", d.Key(), new[] { toll.Id }, new[] { "world" },
                    new[] { new Effect(DebtKey(d), before, s.Debt) }, "The crisis releases " + d + " debt.");
                UpdateTier(d);
            }
            return toll;
        }

        /// <summary>SYSTEMS §6: crises also create openings. One is chosen by weighted seeded draw.</summary>
        private void ChooseOpening(int tollId)
        {
            var p = World.Plague;
            var options = new List<KeyValuePair<string, double>>
            {
                new KeyValuePair<string, double>("orphanedTalent", 1 + (World.CompletedProjects.Contains("physician") ? 1 : 0)),
                new KeyValuePair<string, double>("laborScarcity", 1 + p.Severity / 25.0),
                new KeyValuePair<string, double>("newMovement", 1 + (p.Response == "none" ? 1 : 0)),
                new KeyValuePair<string, double>("experimentation", p.Response == "hospice" ? 2 : 0.25),
            };
            double total = options.Sum(o => o.Value);
            double roll = Rng.NextDouble() * total;
            string chosen = options[options.Count - 1].Key;
            foreach (var o in options)
            {
                if (roll < o.Value) { chosen = o.Key; break; }
                roll -= o.Value;
            }
            p.Opening = chosen;

            switch (chosen)
            {
                case "orphanedTalent":
                    ChangeLevel(Domain.Medicine, T.Get("plague.opening.orphanedTalent"), "plague.opening", new[] { tollId }, new[] { "world" },
                        "Opening: an orphaned apprentice physician, Iulia, begins treating the district's sick.");
                    break;
                case "laborScarcity":
                    ChangeLevel(Domain.Economy, T.Get("plague.opening.laborScarcity"), "plague.opening", new[] { tollId }, new[] { "world" },
                        "Opening: with so many dead, surviving laborers command higher wages; the district's workshops reorganize.");
                    break;
                case "newMovement":
                    ChangeLevel(Domain.Governance, T.Get("plague.opening.newMovement"), "plague.opening", new[] { tollId }, new[] { "world" },
                        "Opening: a burial brotherhood forms among the survivors and starts settling disputes in the district.");
                    break;
                default:
                    ChangeLevel(Domain.Medicine, T.Get("plague.opening.experimentation"), "plague.opening", new[] { tollId }, new[] { "world" },
                        "Opening: in the hospice, physicians compare treatments and record what works.");
                    break;
            }
        }
    }
}
