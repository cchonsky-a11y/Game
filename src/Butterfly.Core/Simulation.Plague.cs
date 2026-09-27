using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The plague: the P0 crisis (SYSTEMS §6). It follows history (decided 2026-09-28): three visible warnings and
    /// the outbreak come on their historical dates, and with nothing done it strikes as hard as it did. The player
    /// changes only how hard it hits: Medicine, Governance, debt, clean water, preparations and the response.
    /// </summary>
    public sealed partial class Simulation
    {
        public static readonly string[] PlagueResponses = { "quarantine", "hospice", "none" };

        private void InitPlague()
        {
            World.Population = T.Get("plague.startPopulation");
        }

        /// <summary>When plague stage 1–4 arrives (the historical dates).</summary>
        public SimTime PlagueStageDate(int stage) =>
            SimTime.FromYear((int)T.GetArray("plague.historical.stageYears")[stage - 1], (int)T.GetArray("plague.historical.stageMonths")[stage - 1]);

        public int HistoricalOutbreakYear => PlagueStageDate(PlagueState.Outbreak).Year;

        /// <summary>
        /// Advances every plague stage whose historical date falls by the end of the turn now starting (turn starts, and
        /// each year of an absence), so a stage shows on the turn that covers its date whatever the turn length.
        /// </summary>
        private void AdvancePlagueToDate()
        {
            var p = World.Plague;
            int horizon = Now.TotalMonths + (IsAway ? 0 : MonthsPerTurn - 1);
            while (p.Stage < PlagueState.Outbreak && PlagueStageDate(p.Stage + 1).TotalMonths <= horizon)
                AdvancePlague();
        }

        private void AdvancePlague()
        {
            var p = World.Plague;
            int before = p.Stage;
            p.Stage++;
            p.StageEnteredYear = Now.Year;
            var causes = new List<int> { p.LastStageEventId };
            string text = PlagueStageText(p.Stage);
            var e = Record(p.Stage == PlagueState.Outbreak ? "plague.outbreak" : "plague.warning", "plague", causes,
                new[] { "world" }, new[] { new Effect("plague.stage", before, p.Stage) }, text);
            p.LastStageEventId = e.Id;
            // Demetria asks during the warnings, once she has a reason to ask you (decided 2026-09-28).
            if (p.IsWarning) OfferPromise(e.Id);
            if (p.Stage == PlagueState.Outbreak)
            {
                p.OutbreakYear = PlagueStageDate(PlagueState.Outbreak).Year;
                if (IsAway) ResolveOutbreak(AutomaticResponse(), new[] { "world" });
            }
        }

        public static string PlagueStageText(int stage)
        {
            switch (stage)
            {
                case 1: return "Warning 1 of 3 — Rumors from the East: letters from the legions besieging Seleucia speak of a pestilence in the camps.";
                case 2: return "Warning 2 of 3 — The army comes home: the legions march back from the East, and towns along their road bury their dead.";
                case 3: return "Warning 3 of 3 — Fever at Ostia and in the Subura: physicians report fever, rash and black stools among the poor.";
                case 4: return "Outbreak — the pestilence breaks out across Rome as the city celebrates the army's triumph.";
                default: return "The pestilence has passed.";
            }
        }

        public bool OutbreakAwaitingResponse => World.Plague.Stage == PlagueState.Outbreak && World.Plague.Response == null;

        /// <summary>
        /// Standing to direct a response (SYSTEMS §7: a voice; decided 2026-09-28: the warnings, having proved you right,
        /// lower it to influence in a Medicine or Governance institution). Without it, Rome responds as history did.
        /// </summary>
        public bool CanDirectPlagueResponse() =>
            World.Institutions.Any(i => (i.Def.Maintains == Domain.Medicine || i.Def.Maintains == Domain.Governance) && i.Backed &&
                                        i.Stake >= T.Get("authority.responseStake") - 1e-9 && i.Strength >= T.Get("institutions.dissolvedBelow"));

        public IEnumerable<string> AvailablePlagueResponses() =>
            PlagueResponses.Where(r => r == "none" || (CanDirectPlagueResponse() && (r != "hospice" || HospiceAvailable())));

        /// <summary>Player decision when the outbreak begins (the plague's branch point).</summary>
        public CommandResult RespondToPlague(string response)
        {
            if (!OutbreakAwaitingResponse) return CommandResult.Fail("There is no outbreak to respond to.");
            if (!AvailablePlagueResponses().Contains(response))
                return CommandResult.Fail(!CanDirectPlagueResponse() && response != "hospice"
                    ? "No one will take orders from you: you need " + F(T.Get("authority.responseStake") * 100) + "% of a Medicine or Governance institution. Rome will respond as it did in history (respond none)."
                    : "You can't choose '" + response + "' now.");
            double cost = T.Get("plague.response." + response + ".gold");
            if (World.Gold < cost) return CommandResult.Fail("That response costs " + Money(cost) + ".");
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
                case "hospice": text = Cap(HospiceInstitution()?.Def.Name ?? "the physicians") + " opens a hospice in the Subura."; break;
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

        /// <summary>Medicine's debt tier: it sets the odds of a later recurrence during an absence.</summary>
        public DebtTier PlagueTier() => World[Domain.Medicine].Tier;

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

        /// <summary>
        /// Severity of the plague as history had it: the one that killed the historical share of Rome
        /// (decided 2026-09-28: about 10% unless the player mitigates it).
        /// </summary>
        public double HistoricalPlagueSeverity => T.Get("plague.historical.deathShare") / T.Get("plague.deathRatePerSeverity");

        /// <summary>Hazard in Rome as history had it: the district fountain still foul, no debt of your making.</summary>
        public double HistoricalPlagueHazard =>
            Math.Min(T.Get("plague.maxHazard"), T.Get("plague.baseHazard") + T.Get("plague.foulWaterHazard"));

        /// <summary>Resilience as history had it: Medicine and Governance at their historical levels, no preparations, no response.</summary>
        public double HistoricalPlagueResilience
        {
            get
            {
                int y = HistoricalOutbreakYear;
                double r = Benchmark(Domain.Medicine, y) / 100.0 * T.Get("plague.resiliencePerMedicine")
                         + Benchmark(Domain.Governance, y) / 100.0 * T.Get("plague.resiliencePerGovernance")
                         + T.Get("plague.response.none.resilience");
                return Math.Min(T.Get("plague.maxResilience"), r);
            }
        }

        /// <summary>
        /// Severity = the historical plague, scaled by how Rome's Hazard × (1 − Resilience) now compares with history's
        /// (the SYSTEMS §9 loss form), raised by Governance and Economy debt. With nothing changed, exactly history.
        /// </summary>
        public double PlagueSeverity(string? response) =>
            HistoricalPlagueSeverity * T.Get("plague.exposure")
            * (PlagueHazard() / HistoricalPlagueHazard)
            * ((1 - PlagueResilience(response)) / (1 - HistoricalPlagueResilience))
            * PlagueSeverityMultiplier();

        /// <summary>What the historical plague cost a domain: the history curve's step across the outbreak year.</summary>
        public double HistoricalPlagueDrop(Domain d) =>
            Math.Max(0, Benchmark(d, HistoricalOutbreakYear) - Benchmark(d, HistoricalOutbreakYear + 1));

        private void ResolveOutbreakDamage()
        {
            var p = World.Plague;
            p.Severity = Math.Max(0.01, PlagueSeverity(p.Response));
            p.StruckInAbsence = IsAway;
            var toll = ApplyPlagueDamage(p.Severity, new List<int> { p.LastStageEventId, p.ResponseEventId }, out double deaths, out string label);
            p.Deaths = deaths;
            p.SeverityLabel = label;

            OnPlagueResolved(toll.Id);
            ChooseOpening(toll.Id);

            p.Stage = PlagueState.Passed;
            var passed = Record("plague.passed", "plague", new[] { toll.Id }, new[] { "world" },
                new[] { new Effect("plague.stage", PlagueState.Outbreak, PlagueState.Passed) },
                "The sickness burns itself out. Nothing is fixed; there are simply fewer people left, and the survivors expect less.");
            p.LastStageEventId = passed.Id;
            OnPlaguePassedForPromise(passed.Id);
        }

        /// <summary>Tolls, level damage and debt release shared by the plague and its later recurrences.</summary>
        /// <summary>Severity label by share of the population dead (decided 2026-09-27).</summary>
        public string SeverityLabel(double deathShare) =>
            deathShare >= T.Get("plague.severityLabels.catastrophicFromDeathShare") ? "catastrophic"
            : deathShare >= T.Get("plague.severityLabels.severeFromDeathShare") ? "severe" : "contained";

        internal GameEvent ApplyPlagueDamage(double sev, List<int> causes, out double deaths) => ApplyPlagueDamage(sev, causes, out deaths, out _);

        internal GameEvent ApplyPlagueDamage(double sev, List<int> causes, out double deaths, out string label)
        {
            World.LastOutbreakYear = Now.Year;
            causes.AddRange(new[] { CauseOf(DebtKey(Domain.Medicine)), CauseOf(LevelKey(Domain.Medicine)), CauseOf(LevelKey(Domain.Governance)),
                CauseOf(TierKey(Domain.Governance)), CauseOf(TierKey(Domain.Economy)),
                CauseOf("plague.resilience"), CauseOf("fountain.clean") });

            double popBefore = World.Population;
            deaths = World.Population * sev * T.Get("plague.deathRatePerSeverity");
            World.Population -= deaths;
            double goldBefore = World.Gold;
            World.Gold = Math.Max(0, World.Gold - sev * T.Get("plague.goldLossPerSeverity"));
            double share = popBefore > 0 ? deaths / popBefore : 0;
            label = SeverityLabel(share);
            var toll = Record("plague.toll", "plague", causes, new[] { "world" },
                new[] { new Effect("population", popBefore, World.Population), new Effect(GoldKey, goldBefore, World.Gold),
                        new Effect("plague.severity", 0, sev) },
                "The pestilence is " + label + ": about " + F(Math.Round(deaths)) + " thousand dead, " + F(Math.Round(share * 100)) + "% of Rome.");

            foreach (var d in DomainInfo.All)
                ChangeLevel(d, -HistoricalPlagueDrop(d) * T.Get("plague.damage." + d.Key()) * sev / HistoricalPlagueSeverity, "plague.damage", new[] { toll.Id }, new[] { "world" },
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
