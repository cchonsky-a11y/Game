using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The jump (SYSTEMS §2, §7, §11): the player chooses when to leave; the absence is simulated
    /// in decade steps with the same rules; three Echoes are surfaced in a four-beat arrival.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>True while the absence is being simulated.</summary>
        public bool IsAway { get; private set; }
        /// <summary>True once the inventor has arrived in the new era; the P0 scenario is over.</summary>
        public bool Arrived { get; private set; }
        public Arrival? Arrival { get; private set; }
        /// <summary>Year the inventor left (0 before the jump).</summary>
        public int DepartureYear { get; private set; }

        public Arrival Jump()
        {
            if (IsAway || Arrived) throw new InvalidOperationException("Already jumped.");
            var arrival = new Arrival { DepartureYear = Now.Year };
            DepartureYear = Now.Year;
            foreach (var d in DomainInfo.All) arrival.SubScoresBefore[d] = SubScore(d);
            arrival.IndexBefore = SphereIndex();

            var depart = Record("jump.depart", "machine", null, new[] { "player" }, null,
                "You start the machine and leave AD " + Now.Year + ".");
            SettlePromiseOnDeparture(depart.Id);
            TagEchoes(arrival, depart.Id);
            foreach (var i in Founded())
            {
                i.Quality = QualityAtDeparture(i);
                i.HoldingsAtDeparture = i.Holdings;
                i.DriftPath = ChooseDriftPath(i);
                Record("institution.departure", i.Key, new[] { depart.Id }, new[] { i.Leader }, null,
                    Cap(i.Def.Name) + " is left " + i.Quality + ", led by " + i.Leader + ".");
            }
            foreach (var p in World.ActiveProjects)
                Record("project.abandoned", p.Def.Id, new[] { p.StartEventId, depart.Id }, new[] { "player" }, null, p.Def.Name + " is abandoned unfinished.");
            World.ActiveProjects.Clear();
            World.Commitments.Clear();

            IsAway = true;
            if (OutbreakAwaitingResponse) ResolveOutbreak(AutomaticResponse(), new[] { "world" });
            int decades = T.GetInt("jump.years") / 10;
            for (int k = 1; k <= decades; k++) DecadeStep(k, arrival);
            IsAway = false;
            Arrived = true;

            arrival.ArrivalYear = Now.Year;
            foreach (var d in DomainInfo.All) arrival.SubScoresAfter[d] = SubScore(d);
            arrival.IndexAfter = SphereIndex();
            foreach (var i in Founded())
            {
                i.Outcome = OutcomeOf(i);
                arrival.Institutions.Add(new InstitutionReport(i.Def.Name, CurrentName(i), i.Outcome, i.Strength, i.Loyalty, i.Quality)
                {
                    HoldingsAtDeparture = i.HoldingsAtDeparture,
                    HoldingsNow = i.Holdings,
                    DebtPaidAway = i.DebtPaidAway,
                    GoldLostToCorruption = i.GoldLostToCorruption,
                    Corruption = i.Corruption,
                });
            }
            BuildBeats(arrival);
            Record("jump.arrive", "machine", new[] { depart.Id }, new[] { "player" }, null,
                "You arrive in AD " + Now.Year + ".");
            Arrival = arrival;
            return arrival;
        }

        /// <summary>
        /// What the player is leaving behind: exposures, never outcomes. Debts keep compounding during
        /// the absence; institutions decay at the rate their quality sets; an active promise would break.
        /// </summary>
        public IEnumerable<string> DepartureBriefing()
        {
            double rate = T.Get("debt.compoundRate");
            int cap = T.GetInt("debt.compoundingCapYearsAfterDeparture");
            foreach (var d in World.Domains.Where(x => x.Debt > 0))
                yield return d.Domain + " debt " + F(d.Debt) + " keeps growing 5% a year for " + cap + " years after you leave (about " +
                             F(d.Debt * Math.Pow(1 + rate, 10)) + " in a decade, " + F(d.Debt * Math.Pow(1 + rate, cap)) +
                             " after " + cap + " years) unless a crisis clears it. Paying it down now costs " + F(PaydownCost(d.Debt)) + " gold.";
            int window = T.GetInt("institutions.holdings.windowYears");
            foreach (var i in Founded())
            {
                var q = QualityAtDeparture(i);
                string why = q != InstitutionQuality.Bare ? "" : i.Chartered ? " (chartered but not endowed, so it counts as bare)"
                           : i.Endowed ? " (endowed but not chartered, so it counts as bare)" : "";
                yield return Cap(i.Def.Name) + " would be left " + (q == InstitutionQuality.Strong ? "strong" : q == InstitutionQuality.CharteredAndEndowed ? "chartered and endowed" : "bare") +
                             why + ", losing " + F(DecayRate(q) * 100) + "% of its strength each decade (now " + F(i.Strength) + ").";
                if (i.Holdings <= 0)
                {
                    yield return "  It holds no gold, so it can't pay down " + i.Def.Maintains + " debt while you're away. (endow " + i.Key + " <gold>)";
                    continue;
                }
                var domain = World[i.Def.Maintains];
                double share = PaymentShare(i);
                double coverable = i.Holdings / PaydownCost(1);
                yield return "  It holds " + F(i.Holdings) + " gold (" + (LargeHoldings(i) ? "large" : "small") + "), growing about " +
                             F(HoldingsGrowthRate() * 100) + "% a year with the economy for " + window + " years.";
                yield return "  It would pay " + (share >= 1 ? "in full" : share > 0 ? "partially" : "nothing") + " toward " + i.Def.Maintains +
                             " debt (now " + F(domain.Debt) + "); its gold covers about " + F(coverable) + " points at the 1.5× premium.";
                var w = CorruptionWeights(i);
                double sum = w[0] + w[1] + w[2];
                yield return "  Corruption risk: " + CorruptionRiskBand(i) + " for " + window + " years (" +
                             (LargeHoldings(i) ? "large holdings" : "small holdings") + ", " + (i.AuditCharter ? "audit charter" : "no audit charter") +
                             ", " + i.Leader + " is " + i.Def.LeaderIntegrity + "). If it happens, it could be minor (" + F(w[0] / sum * 100) + "%), major (" +
                             F(w[1] / sum * 100) + "%) or total (" + F(w[2] / sum * 100) + "%)." +
                             (i.AuditCharter ? "" : " (audit " + i.Key + ": " + F(T.Get("institutions.auditGold")) + " gold)");
            }
            if (!Founded().Any()) yield return "No institution will look after Rome while you're away.";
            if (LeavingBreaksPromise) yield return "You promised Demetria you would stay until the sickness has passed. Leaving now breaks that promise.";
            if (World.ActiveProjects.Count > 0) yield return "Unfinished work will be abandoned.";
            if (World.Gold >= 1) yield return "The " + F(Math.Floor(World.Gold)) + " gold in your hands stays behind and is lost unless you spend it, pay down debt or endow an institution.";
        }

        private void SettlePromiseOnDeparture(int departId)
        {
            var p = World.Promise;
            if (p.Status == PromiseStatus.Offered)
            {
                p.Status = PromiseStatus.Refused;
                p.Unanswered = true;
                var e = Record("promise.refuse", "promise", new[] { p.OfferEventId, departId }, new[] { "player" }, null,
                    "You leave without ever answering Demetria.");
                p.LastEventId = e.Id;
            }
            if (!LeavingBreaksPromise) return;
            p.Status = PromiseStatus.Broken;
            var leader = PromiseInstitution.Def.Leader;
            var broken = Record("promise.broken", "promise", new[] { p.LastEventId, departId }, new[] { "player", leader },
                new[] { new Effect("promise.status", (int)PromiseStatus.Active, (int)PromiseStatus.Broken) },
                "You leave before the sickness has passed. You promised " + leader + " you would stay.");
            p.LastEventId = broken.Id;
            if (PromiseInstitution.Founded)
                ChangeLoyalty(PromiseInstitution, -T.Get("promise.brokenLoyalty"), "institution.loyalty", new[] { broken.Id }, new[] { leader },
                    leader + " tells the Circle you broke your word.");
        }

        /// <summary>Tags the three P0 Echoes. The seeded choice is always one of them (SYSTEMS §11).</summary>
        private void TagEchoes(Arrival arrival, int departId)
        {
            string choice = World.SeededChoice ?? "neither";
            var echoes = new List<EchoRecord>
            {
                new EchoRecord("seeded", choice == "neither" ? "The fountain and the workshop" : choice == "fountain" ? "The district fountain" : "The smith's workshop",
                    "hour-one choice: " + choice),
            };
            var circle = World.Institution("circle");
            var faction = World.Institution("faction");
            var inst = circle.Founded ? circle : faction.Founded ? faction : null;
            echoes.Add(inst != null
                ? new EchoRecord("institution", Cap(inst.Def.Name), "founded, led by " + inst.Leader)
                : new EchoRecord("institution", "Demetria and Varro", "never backed"));
            echoes.Add(new EchoRecord("promise", "Your promise to Demetria", World.Promise.Status.ToString().ToLowerInvariant()));
            foreach (var e in echoes)
            {
                arrival.Echoes.Add(e);
                Record("echo.tag", e.Id, new[] { departId }, new[] { "world" }, null, "Echo: " + e.Name + " (" + e.AtDeparture + ").");
            }
        }

        // ---- coarse mode ----------------------------------------------------

        private void DecadeStep(int decade, Arrival arrival)
        {
            int startYear = Now.Year;
            bool antoninePassed = World.Plague.Stage == PlagueState.Passed;
            // Institution gold acts only in the 30 years after departure (decided 2026-09-26).
            bool window = startYear - DepartureYear < T.GetInt("institutions.holdings.windowYears");
            if (window) foreach (var i in Founded()) HoldingsDecadeStart(i, arrival);
            // The plague keeps its yearly rules while it is still on its way.
            for (int y = 1; y <= 10; y++)
            {
                Now = SimTime.FromYear(startYear + y);
                if (World.Plague.Stage != PlagueState.Passed) PlagueYearTick();
            }
            if (!antoninePassed && World.Plague.Stage == PlagueState.Passed)
                arrival.Crises.Add("AD " + World.Plague.OutbreakYear + ": the Antonine pestilence (" + World.Plague.SeverityLabel +
                                   ", about " + F(Math.Round(World.Plague.Deaths)) + " thousand dead; response: " +
                                   (World.Plague.Response == "none" ? "none" : World.Plague.Response + ", chosen by your institutions after you left") + ")");

            foreach (var d in DomainInfo.All) DomainDecadeStep(d, startYear);
            foreach (var i in Founded()) InstitutionDecadeStep(i, decade);
            if (window) foreach (var i in Founded()) HoldingsDecadeGrowth(i);
            FountainDecadeStep();
            if (antoninePassed) MaybeRecurrence(arrival);
            // Population recovers toward its old size as Medicine allows (flavor only; not in the Index).
            double popTarget = T.Get("plague.startPopulation") * Math.Min(1.5, SubScore(Domain.Medicine) / 100.0);
            World.Population += (popTarget - World.Population) * T.Get("jump.populationRecoveryPerDecade");

            arrival.IndexByDecade.Add(SphereIndex());
            Record("jump.decade", "world", null, new[] { "world" }, null,
                "AD " + Now.Year + ": Medicine " + F(World[Domain.Medicine].Level) + ", Governance " + F(World[Domain.Governance].Level) +
                ", Economy " + F(World[Domain.Economy].Level) + ".");
        }

        /// <summary>Institutions maintain the domain matching their type while the inventor is away (SYSTEMS §7).</summary>
        public double MaintainBonus(Domain d) =>
            Founded().Where(i => i.Def.Maintains == d && i.Strength >= T.Get("institutions.dissolvedBelow"))
                     .Sum(i => i.Strength * T.Get("jump.maintainPerStrength"));

        /// <summary>
        /// One decade for a domain: the level drifts toward the historical baseline (plus what institutions
        /// maintain), then ten years of the §6 debt rule run against the expectation.
        /// </summary>
        private void DomainDecadeStep(Domain d, int startYear)
        {
            var s = World[d];
            double levelBefore = s.Level, debtBefore = s.Debt;
            double target = Benchmark(d, Now.Year) + MaintainBonus(d);
            s.Level = Math.Max(Benchmark(d, Now.Year) * T.Get("domains.minLevelFraction"),
                Math.Min(T.Get("domains.maxLevel"), s.Level + (target - s.Level) * T.Get("jump.convergencePerDecade")));
            bool maintained = MaintainBonus(d) > 0;
            for (int y = 1; y <= 10; y++)
            {
                double expectation = Formulas.Expectation(Benchmark(d, startYear + y), s.Peak);
                // After the 30-year window, a domain no institution maintains accrues no new debt (decided 2026-09-27).
                bool accrues = maintained || startYear + y - DepartureYear <= T.GetInt("debt.unmaintainedAccrualStopsAfterYears");
                double accrual = accrues ? Formulas.DebtAccrual(expectation, s.Level, T.Get("debt.accrualRate")) : 0;
                // Debt compounds only for the first 30 years after departure (decided 2026-09-26).
                s.Debt = Formulas.DebtStep(s.Debt, accrual,
                    Formulas.AbsenceCompoundRate(startYear + y - DepartureYear, T.GetInt("debt.compoundingCapYearsAfterDeparture"), T.Get("debt.compoundRate")));
                s.Peak = Formulas.FadePeak(s.Peak, s.Level, T.Get("expectation.peakFadePerYear"));
            }
            Record("jump.domain", d.Key(), CausesOf(LevelKey(d), DebtKey(d)), new[] { "world" },
                new[] { new Effect(LevelKey(d), levelBefore, s.Level), new Effect(DebtKey(d), debtBefore, s.Debt) },
                d + " over the decade to AD " + Now.Year + ".");
            UpdateTier(d);
        }

        /// <summary>After the Antonine plague, the same crisis can recur; the chance per decade follows the region's tier.</summary>
        private void MaybeRecurrence(Arrival arrival)
        {
            // No new outbreak within 30 years of the last one (decided 2026-09-27).
            if (World.LastOutbreakYear > 0 && Now.Year - World.LastOutbreakYear < T.GetInt("plague.immunityYears")) return;
            double yearly = T.Get("jump.crisisChancePerYear." + PlagueTier().ToString().ToLowerInvariant());
            double chance = 1 - Math.Pow(1 - yearly, 10);
            if (!Rng.Chance(chance)) return;
            string response = AutomaticResponse();
            var start = Record("crisis.recurrence", "plague", CausesOf(TierKey(Domain.Medicine)),
                new[] { "world" }, null, "Pestilence returns to Rome (response: " + response + ").");
            double sev = Math.Max(0.01, PlagueSeverity(response));
            ApplyPlagueDamage(sev, new List<int> { start.Id }, out double deaths, out string label);
            arrival.Crises.Add("AD " + Now.Year + ": pestilence returns (" + label + ", about " + F(Math.Round(deaths)) + " thousand dead)");
        }

        private void FountainDecadeStep()
        {
            if (!World.CleanWater || World.FountainCondition <= 0) return;
            var circle = World.Institution("circle");
            bool tended = (circle.Founded && circle.Strength >= T.Get("jump.fountainTendedByStrength"))
                          || World[Domain.Medicine].Level >= Benchmark(Domain.Medicine, Now.Year);
            if (tended) return;
            double before = World.FountainCondition;
            World.FountainCondition = Math.Max(0, World.FountainCondition - T.Get("jump.fountainDecayPerDecade"));
            Record("fountain.decay", "fountain", null, new[] { "world" }, new[] { new Effect("fountain.condition", before, World.FountainCondition) },
                "No one maintains the district fountain.");
        }

        private string CurrentName(Institution i) => i.HasDrifted && i.DriftPath != null ? i.DriftPath.Name : i.Def.Name;

        // ---- the four beats -------------------------------------------------

        private void BuildBeats(Arrival arrival)
        {
            var text = Data.Content;
            var values = new Dictionary<string, string>
            {
                { "year", Now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "outbreakYear", World.Plague.OutbreakYear.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { "deaths", F(Math.Round(World.Plague.Deaths)) },
            };
            var seeded = arrival.Echoes.First(e => e.Id == "seeded");
            var institution = arrival.Echoes.First(e => e.Id == "institution");
            var promise = arrival.Echoes.First(e => e.Id == "promise");

            // 1. Recognition — the hour-one choice.
            string recognitionKey = RecognitionKey(out string? secondKey);
            string recognition = text.Template("recognition." + recognitionKey, values);
            if (secondKey != null) recognition += " " + text.Template("recognition." + secondKey, values);
            seeded.AtArrival = recognitionKey;
            seeded.Beat = "Recognition";
            arrival.Beats.Add(new ArrivalBeat("Recognition", recognition));

            // 2. Wrongness — the world is not the one history describes.
            arrival.Beats.Add(new ArrivalBeat("Wrongness", text.Template("wrongness." + WrongnessKey(arrival), values)));

            // 3. Personal echo — the promise.
            var circle = World.Institution("circle");
            bool keeperAlive = circle.Founded && OutcomeOf(circle) != InstitutionOutcome.Dissolved;
            values["keeper"] = keeperAlive ? CurrentName(circle) : "an old house in the Subura";
            string personalKey;
            switch (World.Promise.Status)
            {
                case PromiseStatus.Kept: personalKey = "kept"; break;
                case PromiseStatus.Broken: personalKey = keeperAlive ? "broken" : "brokenNoKeeper"; break;
                case PromiseStatus.Refused: personalKey = World.Promise.Unanswered ? "unanswered" : "refused"; break;
                default: personalKey = World.Plague.OutbreakYear > 0 ? "notOffered" : "notOfferedNoOutbreak"; break;
            }
            promise.AtArrival = personalKey;
            promise.Beat = "Personal echo";
            arrival.Beats.Add(new ArrivalBeat("Personal echo", text.Template("personal." + personalKey, values)));

            // 4. Discovery — what the institutions became.
            var parts = new List<string>();
            var keys = new List<string>();
            foreach (var i in World.Institutions.Where(i => i.Founded))
            {
                var outcome = OutcomeOf(i);
                string key = i.Key + "." + (i.ForcedOutcome == InstitutionOutcome.Captured && outcome == InstitutionOutcome.Captured ? "corrupted"
                    : outcome == InstitutionOutcome.Captured && i.Key == "circle" ? "drifted" : outcome.ToString().ToLowerInvariant());
                var v = new Dictionary<string, string>(values)
                {
                    { "leader", i.Leader },
                    { "pathName", i.DriftPath?.Name ?? i.Def.Name },
                    { "pathDescription", i.DriftPath?.Description ?? "" },
                };
                parts.Add(text.Template("discovery." + key, v));
                // The Discovery beat reveals any corruption and its level (decided 2026-09-26).
                if (i.Corruption != CorruptionLevel.None && key != i.Key + ".dissolved")
                    parts.Add(text.Template("discovery.corruption." + i.Corruption.ToString().ToLowerInvariant(), v));
                keys.Add(key);
            }
            if (parts.Count == 0)
            {
                parts.Add(text.Template("discovery.none", values));
                keys.Add("none");
            }
            institution.AtArrival = string.Join(", ", keys);
            institution.Beat = "Discovery";
            arrival.Beats.Add(new ArrivalBeat("Discovery", string.Join(" ", parts)));
        }

        private string RecognitionKey(out string? secondKey)
        {
            secondKey = null;
            string choice = World.SeededChoice ?? "neither";
            bool fountainRuns = World.CleanWater && World.FountainCondition >= T.Get("jump.fountainRunsAt");
            bool workshopDone = World.CompletedProjects.Contains("workshop");
            bool economyHeld = SubScore(Domain.Economy) >= 100;
            if (choice == "neither")
            {
                if (World.CleanWater) choice = "fountain";
                else if (workshopDone) choice = "workshop";
                else return "neither";
            }
            if (choice == "fountain")
            {
                secondKey = workshopDone ? "unchosenWorkshop.later" : "unchosenWorkshop.gone";
                return fountainRuns ? "fountain.runs" : "fountain.dry";
            }
            secondKey = fountainRuns ? "unchosenFountain.fixed" : "unchosenFountain.foul";
            return economyHeld ? "workshop.thrives" : "workshop.gone";
        }

        private string WrongnessKey(Arrival arrival)
        {
            var biggest = DomainInfo.All.OrderByDescending(d => Math.Abs(arrival.SubScoresAfter[d] - 100)).First();
            double sub = arrival.SubScoresAfter[biggest];
            if (Math.Abs(sub - 100) < T.Get("jump.asHistoryBand")) return "asHistory";
            return biggest.Key() + (sub > 100 ? ".high" : ".low");
        }
    }
}
