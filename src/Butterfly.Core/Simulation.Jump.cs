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

        /// <summary>Leaves for the future. The machine must be repaired first (all 9 steps, decided 2026-09-28).</summary>
        public Arrival Jump() => Jump(false);

        /// <summary>Test setup: jumps even if the machine isn't repaired (tests of the absence, not of the machine).</summary>
        internal Arrival JumpForTests() => Jump(true);

        /// <summary>True if the plague's warnings had begun before you left (so Demetria could have asked).</summary>
        private bool _warningsBeforeDeparture;

        /// <summary>When the current era began for the jump range's time bonus: AD 155, or the year you last arrived.</summary>
        private double? _eraStart;

        public int JumpsMade { get; private set; }

        /// <summary>
        /// Option A (decided 2026-09-28): after an arrival you may jump again at once, without a second era to play, up to
        /// the P0 limit. The machine stays repaired; the range starts over from the repairs and upgrades.
        /// </summary>
        public bool CanJumpAgain => Arrived && JumpsMade < T.GetInt("jump.maxJumps");

        private Arrival Jump(bool ignoreMachine)
        {
            if (IsAway || (Arrived && !CanJumpAgain)) throw new InvalidOperationException("Already jumped.");
            Arrived = false;
            if (!ignoreMachine && !MachineReady)
                throw new InvalidOperationException("The machine isn't ready (" + MachineStepsDone + "/" + MachineStepsTotal + " steps, " +
                                                    F(MachineGoldRestored) + "/" + F(MachineGoldNeeded) + " aurei restored).");
            var arrival = new Arrival { DepartureYear = Now.Year, DenariiPerUnit = DenariiPerUnit };
            DepartureYear = Now.Year;
            NoteFirstDeparture();
            _absenceBusts = 0;
            // Whether Demetria could have asked is settled when you first leave Rome; by a later jump the plague has come
            // and gone without you (tester 7 left in AD 159 and was told on the second arrival that she never asked).
            if (JumpsMade == 0) _warningsBeforeDeparture = World.Plague.Stage >= 1;
            foreach (var d in DomainInfo.All) World.DepartureDeviation[(int)d] = World[d].Level - Benchmark(d, Now.Year);
            foreach (var i in Issues.Where(i => Stance(i) != 0)) arrival.PolicyAtDeparture.Add(i.ToString().ToLowerInvariant() + " " + StanceWord(i, Stance(i)));
            foreach (var d in DomainInfo.All) arrival.SubScoresBefore[d] = SubScore(d);
            arrival.IndexBefore = SphereIndex();

            var depart = Record("jump.depart", "machine", null, new[] { "player" }, null,
                "You start the machine and leave AD " + Now.Year + ".");
            _leftRome = TakeSnapshot();
            SettlePromiseOnDeparture(depart.Id);
            SavingsAtDeparture(arrival);
            TagEchoes(arrival, depart.Id);
            // Last orders (P0-32): given once, when you first leave Rome; on a later jump they are older and weigh less.
            LapsePendingEvent();
            if (JumpsMade == 0) ApplyLastOrders(depart.Id);
            else foreach (var i in World.Institutions) i.OrderForce *= T.Get("offices.laterJumpOrderShare");
            if (JumpsMade == 0)
                foreach (var i in World.Institutions.Where(x => x.Rank >= Officer)) i.DepartureOffice = OfficeTitle(i, i.Rank);
            foreach (var i in World.Institutions.Where(x => !x.Def.IsOwn && x.Rank > Member)) i.Rank = Member;   // your offices end when you go
            foreach (var i in Influential())
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
            World.ActiveMachineSteps.Clear();
            World.ActiveInventions.Clear();
            AbandonCommissionsOnDeparture(depart.Id);
            AbandonChallengesOnDeparture(depart.Id);
            World.Commitments.Clear();

            IsAway = true;
            if (OutbreakAwaitingResponse) ResolveOutbreak(AutomaticResponse(), new[] { "world" });
            // How far the machine carries you is drawn now, within the range its repairs allow (decided 2026-09-28).
            int years = DrawJumpYears();
            arrival.JumpYears = years;
            for (int k = 1; k * 10 <= years; k++) DecadeStep(k, arrival);
            if (years % 10 != 0)
            {
                _stepYears = years % 10; // a final half-decade step, with every per-decade rate scaled to it
                DecadeStep(years / 10 + 1, arrival);
                _stepYears = 10;
            }
            IsAway = false;
            Arrived = true;
            JumpsMade++;
            _eraStart = Now.YearFraction;
            SavingsOnArrival(arrival);
            CarryCapabilities(years, depart.Id);

            arrival.ArrivalYear = Now.Year;
            foreach (var d in DomainInfo.All) arrival.SubScoresAfter[d] = SubScore(d);
            arrival.IndexAfter = SphereIndex();
            foreach (var i in Influential())
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
            // After an arrival nothing in Rome can be changed any more: show only what the player can still act on.
            if (Arrived)
            {
                yield return "The machine will carry you " + JumpRangeText() + "; exactly how far, you'll know when you arrive.";
                foreach (var line in SavingsBriefing()) yield return line;
                yield break;
            }
            double rate = T.Get("debt.compoundRate");
            int cap = T.GetInt("debt.compoundingCapYearsAfterDeparture");
            foreach (var d in World.Domains.Where(x => x.Debt > 0))
                yield return d.Domain + " debt " + F(d.Debt) + " keeps growing 5% a year for " + cap + " years after you leave (about " +
                             F(d.Debt * Math.Pow(1 + rate, 10)) + " in a decade, " + F(d.Debt * Math.Pow(1 + rate, cap)) +
                             " after " + cap + " years) unless a crisis clears it. Paying it down now costs " + Money(PaydownCost(d.Debt)) + ".";
            // Last orders (P0-32): what you will leave each institution you belong to.
            if (JumpsMade == 0)
                foreach (var i in World.Institutions.Where(x => x.Exists && x.Def.DriftPaths.Count >= 2 && (x.Def.IsOwn ? x.Stake > 0 : x.Backed)))
                {
                    var lead = LeadingCamp(i);
                    yield return i.OrderCamp >= 0
                        ? "Your last orders for " + i.Def.ShortName + ": back " + CampName(i, i.OrderCamp) + (i.OrderSuccessor >= 0 ? ", " + i.Def.Successors[i.OrderSuccessor].Name + " to lead it" : "") +
                          " — " + OrdersBand(LastOrderForce(i)) + " weight (" + OrdersWhy(i) + ")."
                        : "No last orders for " + i.Def.ShortName + " (" + (lead == null ? "neither camp leads" : "leading now: " + CampName(i, lead.Value)) + "): orders " + i.Key + " " +
                          string.Join("|", i.Def.DriftPaths.Select(p => p.Id)) + ((i.Def.IsOwn || i.Rank >= Head) && i.Def.Successors.Count > 0 ? " [1|2 to name who succeeds you: " + SuccessorList(i) + "]" : "") + ".";
                }
            int window = T.GetInt("institutions.holdings.windowYears");
            foreach (var i in Influential())
            {
                var q = QualityAtDeparture(i);
                string why = q != InstitutionQuality.Bare ? "" : i.Chartered ? " (chartered but not endowed, so it counts as bare)"
                           : i.Endowed ? " (endowed but not chartered, so it counts as bare)" : "";
                yield return Cap(i.Def.Name) + " would be left " + (q == InstitutionQuality.Strong ? "strong" : q == InstitutionQuality.CharteredAndEndowed ? "chartered and endowed" : "bare") +
                             why + ", losing " + F(DecayRate(q) * 100) + "% of its strength each decade (now " + F(i.Strength) + ").";
                if (i.Holdings <= 0)
                {
                    // L11 (tester 2): endowing needs control; don't suggest it to a player who can't.
                    yield return "  It holds no money, so it can't pay down " + i.Def.Maintains + " debt while you're away. " +
                                 (Controls(i) ? "(endow " + i.Key + " <denarii>)" : "(Only someone who controls it, with " + F(ControlAt * 100) + "%, can endow it.)");
                    continue;
                }
                var domain = World[i.Def.Maintains];
                double share = PaymentShare(i);
                double coverable = i.Holdings / PaydownCost(1);
                yield return "  It holds " + Money(i.Holdings) + " (" + (LargeHoldings(i) ? "large" : "small") + "), growing about " +
                             F(HoldingsGrowthRate() * 100) + "% a year with the economy for " + window + " years.";
                yield return "  It would pay " + (share >= 1 ? "in full" : share > 0 ? "partially" : "nothing") + " toward " + i.Def.Maintains +
                             " debt (now " + F(domain.Debt) + "); its money covers about " + F(coverable) + " points at the 1.5× premium.";
                var w = CorruptionWeights(i);
                double sum = w[0] + w[1] + w[2];
                yield return "  Corruption risk: " + CorruptionRiskBand(i) + " for " + window + " years (" +
                             (LargeHoldings(i) ? "large holdings" : "small holdings") + ", " + (i.AuditCharter ? "audit charter" : "no audit charter") +
                             ", " + (YouLead(i) ? "you lead it" : i.Leader + " is " + i.Integrity) + "). If it happens, it could be minor (" + F(w[0] / sum * 100) + "%), major (" +
                             F(w[1] / sum * 100) + "%) or total (" + F(w[2] / sum * 100) + "%)." +
                             (i.AuditCharter ? "" : " (audit " + i.Key + ": " + Money(T.Get("institutions.auditGold")) + ")");
            }
            if (!Influential().Any()) yield return "No institution you hold " + F(InfluenceAt * 100) + "%+ of will look after Rome while you're away.";
            yield return "The machine will carry you " + JumpRangeText() + "; exactly how far, you'll know when you arrive." +
                         (MachineUpgradesDone < Data.Content.MachineUpgrades.Count ? " Upgrades and more time here would lengthen it." : "");
            if (CoinStanceFactor() > 0)
                yield return "Sound coin: while " + PolicyInstitution!.Def.Name + " stands, Rome's mint won't cut the silver, sparing Rome part of the decline history had in store.";
            else if (CoinStanceFactor() < 0)
                yield return "Debased coin: while " + PolicyInstitution!.Def.Name + " stands, the mint keeps cutting the silver faster than Rome ever did.";
            if (World.Promise.Status == PromiseStatus.Offered) yield return "Demetria asked you to stay until the sickness has passed. If you leave now, she will never have an answer.";
            if (LeavingBreaksPromise) yield return "You promised Demetria you would stay until the sickness has passed. Leaving now breaks that promise.";
            if (World.ActiveProjects.Count > 0) yield return "Unfinished work will be abandoned.";
            if (World.Gold >= 1) yield return "The " + Money(World.Gold) + " in your hands stay behind and are lost unless you spend them, pay down debt or endow an institution.";
            foreach (var line in SavingsBriefing()) yield return line;
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
            if (PromiseInstitution.Backed)
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
            // The institution Echo is the one you hold the largest influential stake in.
            var inst = Influential().OrderByDescending(i => i.Stake).ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();
            echoes.Add(inst != null
                ? new EchoRecord("institution", Cap(inst.Def.Name), (inst.Def.IsOwn ? "founded" : StakePercent(inst) + "% stake") + ", led by " + inst.Leader)
                : new EchoRecord("institution", "Demetria and Varro", "never backed"));
            echoes.Add(new EchoRecord("promise", "Your promise to Demetria", World.Promise.Status.ToString().ToLowerInvariant()));
            foreach (var e in echoes)
            {
                arrival.Echoes.Add(e);
                Record("echo.tag", e.Id, new[] { departId }, new[] { "world" }, null, "Echo: " + e.Name + " (" + e.AtDeparture + ").");
            }
        }

        // ---- coarse mode ----------------------------------------------------

        /// <summary>Years in the current coarse step: 10, or 5 for a final half-decade.</summary>
        private int _stepYears = 10;
        /// <summary>The current step as a fraction of a decade (per-decade rates are scaled by it).</summary>
        private double StepFraction => _stepYears / 10.0;
        /// <summary>A per-decade share (drift, recovery) scaled to the current step.</summary>
        private double ScaleShare(double perDecade) => 1 - Math.Pow(1 - perDecade, StepFraction);

        private void DecadeStep(int decade, Arrival arrival)
        {
            int startYear = Now.Year;
            bool antoninePassed = World.Plague.Stage == PlagueState.Passed;
            // Institution gold acts only in the 30 years after departure (decided 2026-09-26).
            bool window = startYear - DepartureYear < T.GetInt("institutions.holdings.windowYears");
            if (window) foreach (var i in Influential()) HoldingsDecadeStart(i, arrival);
            // The plague still comes on its historical dates while you are away.
            for (int y = 1; y <= _stepYears; y++)
            {
                Now = SimTime.FromYear(startYear + y);
                if (World.Plague.Stage != PlagueState.Passed) AdvancePlagueToDate();
                HistoricalRecurrences(arrival);
            }
            if (!antoninePassed && World.Plague.Stage == PlagueState.Passed)
                arrival.Crises.Add("AD " + World.Plague.OutbreakYear + ": the Antonine pestilence (" + World.Plague.SeverityLabel +
                                   ", about " + F(Math.Round(World.Plague.Deaths)) + " thousand dead; response: " +
                                   (World.Plague.Response == "none" ? "none" : World.Plague.Response + ", chosen by your institutions after you left") + ")");

            foreach (var d in DomainInfo.All) DomainDecadeStep(d, startYear);
            foreach (var i in Influential()) InstitutionDecadeStep(i, decade);
            if (window) foreach (var i in Influential()) HoldingsDecadeGrowth(i);
            FountainDecadeStep();
            SavingsDecadeStep();
            // Prices keep rising with the coin's debasement while you're away (history's, or your policy's while it stands).
            World.PriceLevel *= Math.Pow(1 + InflationRate(), _stepYears);
            if (antoninePassed) MaybeRecurrence(arrival);
            PolicyDecadeStep(arrival);
            // Population recovers toward its old size as Medicine allows (flavor only; not in the Index).
            double popTarget = T.Get("plague.startPopulation") * Math.Min(1.5, SubScore(Domain.Medicine) / 100.0);
            World.Population += (popTarget - World.Population) * ScaleShare(T.Get("jump.populationRecoveryPerDecade"));

            arrival.IndexByDecade.Add(SphereIndex());
            Record("jump.decade", "world", null, new[] { "world" }, null,
                "AD " + Now.Year + ": Medicine " + F(World[Domain.Medicine].Level) + ", Governance " + F(World[Domain.Governance].Level) +
                ", Economy " + F(World[Domain.Economy].Level) + ".");
        }

        /// <summary>
        /// Institutions maintain the domain matching their type while the inventor is away (SYSTEMS §7): strength ×
        /// rate, scaled by how much of each you steer and how much of the domain it holds against its rivals
        /// (full at half the domain, decided 2026-09-27).
        /// </summary>
        public double MaintainBonus(Domain d) =>
            World.Institutions.Where(i => i.Def.Maintains == d && i.Strength >= T.Get("institutions.dissolvedBelow") && (HasInfluence(i) || i.Backed && !i.Def.IsOwn))
                         .Sum(i => (HasInfluence(i) ? ControlFactor(i) : T.Get("joining.smallMemberUpkeepFactor"))
                                   * Math.Min(1, T.Get("stakes.swayPerInfluence") * DomainShare(i)) * i.Strength * T.Get("jump.maintainPerStrength")
                                   * CampUpkeepFactor(i));

        /// <summary>
        /// One decade for a domain: the level drifts toward the historical baseline (plus what institutions
        /// maintain), then ten years of the §6 debt rule run against the expectation.
        /// </summary>
        private void DomainDecadeStep(Domain d, int startYear)
        {
            var s = World[d];
            double levelBefore = s.Level, debtBefore = s.Debt;
            // Rome follows history's own ups and downs through the decade; only your mark (the gap from history) moves toward
            // its target (decided 2026-09-28: a player who left no mark tracks history, rather than lagging behind its swings).
            double baseStart = Benchmark(d, startYear), baseEnd = Benchmark(d, startYear + _stepYears);
            double gap = s.Level - baseStart, targetGap = DecadeTarget(d, startYear) - baseEnd;
            gap += (targetGap - gap) * ScaleShare(DecadeDrift(startYear));
            s.Level = Math.Max(Benchmark(d, Now.Year) * T.Get("domains.minLevelFraction"), Math.Min(T.Get("domains.maxLevel"), baseEnd + gap));
            bool maintained = MaintainBonus(d) > 0;
            for (int y = 1; y <= _stepYears; y++)
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

        private bool AfterWindow(int decadeStartYear) => decadeStartYear - DepartureYear >= T.GetInt("institutions.holdings.windowYears");

        /// <summary>
        /// Where a domain heads this decade: the long-run target, baseline + k × (departure level − baseline at departure),
        /// plus what surviving institutions maintain on top, plus the policy you left. The same target holds from the day
        /// you leave (decided 2026-09-28, P0-30: the lasting mark; before, the first 30 years pulled toward the bare
        /// baseline); only the speed differs, slower in the 30-year window.
        /// </summary>
        public double DecadeTarget(Domain d, int decadeStartYear)
        {
            double baseline = Benchmark(d, decadeStartYear + _stepYears);
            double policy = (d == Domain.Economy ? PolicyTargetBonus(decadeStartYear + _stepYears) + WorkshopCarry(decadeStartYear) + OwnPower("house") * T.Get("founding.house.economyCarry") : 0)
                          + (d == Domain.Medicine ? OwnPower("school") * T.Get("founding.school.medicineCarry") : 0)
                          + CoinRelief(d, decadeStartYear + _stepYears);
            return baseline + T.Get("jump.longRun.deviationShare") * World.DepartureDeviation[(int)d] + MaintainBonus(d) + policy;
        }

        private double DecadeDrift(int decadeStartYear) =>
            AfterWindow(decadeStartYear) ? T.Get("jump.longRun.driftPerDecade") : T.Get("jump.convergencePerDecade");

        /// <summary>After the Antonine plague, the same crisis can recur; the chance per decade follows the region's tier.</summary>
        private void MaybeRecurrence(Arrival arrival)
        {
            // Off by default (decided 2026-09-28): random recurrences on no historical date broke "history is the baseline".
            // The historical ones (AD 189, the Plague of Cyprian in the 250s) are to come back on their dates, with their drops in the history curve.
            if (!T.GetBool("jump.randomRecurrences")) return;
            // No new outbreak within 30 years of the last one (decided 2026-09-27).
            if (World.LastOutbreakYear > 0 && Now.Year - World.LastOutbreakYear < T.GetInt("plague.immunityYears")) return;
            double yearly = T.Get("jump.crisisChancePerYear." + PlagueTier().ToString().ToLowerInvariant());
            double chance = 1 - Math.Pow(1 - yearly, _stepYears);
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
            bool tended = Influential().Any(i => i.Def.Maintains == Domain.Medicine && i.Strength >= T.Get("jump.fountainTendedByStrength"))
                          || World[Domain.Medicine].Level >= Benchmark(Domain.Medicine, Now.Year);
            if (tended) return;
            double before = World.FountainCondition;
            World.FountainCondition = Math.Max(0, World.FountainCondition - T.Get("jump.fountainDecayPerDecade") * StepFraction);
            Record("fountain.decay", "fountain", null, new[] { "world" }, new[] { new Effect("fountain.condition", before, World.FountainCondition) },
                "No one maintains the district fountain.");
        }

        private string CurrentName(Institution i) => i.HasDrifted && i.DriftPath != null ? i.DriftPath.Name : i.Def.Name;

        // ---- the four beats -------------------------------------------------

        internal void BuildBeats(Arrival arrival)
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

            // A later arrival reads as a return, not a first sight (decided 2026-09-28): its own Recognition and Personal echo.
            bool later = JumpsMade >= 2;
            values["gap"] = (Now.Year - DepartureYear).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string Key(string section, string k) => later && text.Text.ContainsKey(section + "2." + k) ? section + "2." + k : section + "." + k;

            // 1. Recognition — the hour-one choice.
            string recognitionKey = RecognitionKey(out string? secondKey);
            string recognition = text.Template(Key("recognition", recognitionKey), values);
            if (secondKey != null) recognition += " " + text.Template(Key("recognition", secondKey), values);
            seeded.AtArrival = recognitionKey;
            seeded.Beat = "Recognition";
            arrival.Beats.Add(new ArrivalBeat("Recognition", recognition));

            // 2. Wrongness — the world is not the one history describes.
            arrival.WrongnessKey = WrongnessKey(arrival);
            // The coin in your hand: Rome's debasement, spared or hastened (decided 2026-09-28).
            arrival.CoinKey = CoinKey();
            values["coinHolder"] = PolicyInstitution?.Def.Name ?? "the Curia";
            // Arriving in the year of a recurrence, you step into a sick city (tester 7 arrived in AD 189 and only 'learn more' said so).
            string epidemic = EpidemicYear > 0 ? text.Template("recurrence.now." + EpidemicYear) + " " : "";
            arrival.Beats.Add(new ArrivalBeat("Wrongness", epidemic + text.Template(Key("wrongness", arrival.WrongnessKey), values) + " " +
                                                           text.Template(Key("coin", arrival.CoinKey), values)));

            // 3. Personal echo — the promise.
            var circle = World.Institution("circle");
            bool keeperAlive = HasInfluence(circle) && OutcomeOf(circle) != InstitutionOutcome.Dissolved;
            values["keeper"] = keeperAlive ? CurrentName(circle) : "an old house in the Subura";
            string personalKey;
            switch (World.Promise.Status)
            {
                case PromiseStatus.Kept: personalKey = "kept"; break;
                case PromiseStatus.Broken: personalKey = keeperAlive ? "broken" : "brokenNoKeeper"; break;
                case PromiseStatus.Refused: personalKey = World.Promise.Unanswered ? "unanswered" : "refused"; break;
                default:
                    // She never asked because she had no reason to: you did nothing for the sick (decided 2026-09-28).
                    personalKey = _warningsBeforeDeparture ? "neverAsked" : World.Plague.OutbreakYear > 0 ? "notOffered" : "notOfferedNoOutbreak";
                    break;
            }
            promise.AtArrival = personalKey;
            promise.Beat = "Personal echo";
            string personal = text.Template(Key("personal", personalKey), values);
            // Your place in Rome (P0-32): the highest office you held, and the institution you founded.
            var office = World.Institutions.Where(i => !i.Def.IsOwn && i.DepartureOffice.Length > 0).OrderByDescending(i => Array.IndexOf(i.Def.Offices.ToArray(), i.DepartureOffice)).FirstOrDefault();
            if (office != null)
                personal += " " + text.Template(Key("personal", "office"), new Dictionary<string, string>(values) { { "office", office.DepartureOffice }, { "officeHall", CurrentName(office) } });
            var founded = World.Institutions.FirstOrDefault(i => i.Def.IsOwn && (i.Stake > 0 || i.Collapsed));
            if (founded != null)
            {
                bool standing = founded.Exists && !founded.Collapsed && OutcomeOf(founded) != InstitutionOutcome.Dissolved;
                string name = standing ? CurrentName(founded) : founded.Def.Name;
                personal += " " + text.Template(Key("personal", standing ? "founder" : "founderGone"), new Dictionary<string, string>(values) { { "founded", name }, { "Founded", Cap(name) } });
            }
            // What your answers to Rome's choices left behind (P0-33).
            foreach (var mark in EventMarks(later)) personal += " " + mark;
            // The people you knew (P1 echoes).
            foreach (var line in PeopleEchoes(arrival)) personal += " " + line;
            arrival.Beats.Add(new ArrivalBeat("Personal echo", personal));

            // 4. Discovery — what the institutions became.
            var parts = new List<string>();
            var keys = new List<string>();
            foreach (var i in Influential().ToList())
            {
                var outcome = OutcomeOf(i);
                string state = i.ForcedOutcome == InstitutionOutcome.Captured && outcome == InstitutionOutcome.Captured ? "corrupted"
                    : outcome == InstitutionOutcome.Captured && i.Key == "circle" ? "drifted" : outcome.ToString().ToLowerInvariant();
                string key = i.Key + "." + state;
                var v = new Dictionary<string, string>(values)
                {
                    { "leader", i.Leader },
                    { "name", i.Def.Name },
                    { "how", i.Def.IsOwn ? "you founded" : "you held " + StakePercent(i) + "% of" },
                    { "pathName", i.DriftPath?.Name ?? i.Def.Name },
                    { "pathDescription", i.DriftPath?.Description ?? "" },
                };
                // Institutions without their own templates use the generic ones.
                string template = text.Text.ContainsKey("discovery." + key) ? "discovery." + key : "discovery.generic." + state;
                // L6 (tester 6): thriving because it became the camp you ordered, it is that camp now, not its old self
                // (a thriving Grain Cartel was described as "still arguing for open markets").
                if (outcome == InstitutionOutcome.Thriving && i.HasDrifted && i.DriftPath != null) template = "discovery.generic.thrivingAs";
                // L10 (tester 2): the Circle's own thriving line mentions your charter; only if you gave it one.
                else if (key == "circle.thriving" && !i.Chartered) template = "discovery.circle.thrivingUnchartered";
                parts.Add(Cap(text.Template(template, v)));
                // Your office and your parting words (P0-32).
                if (i.DepartureOffice.Length > 0 && !i.Def.IsOwn) parts.Add(text.Template("discovery.office", new Dictionary<string, string>(v) { { "office", i.DepartureOffice } }));
                if (i.OrderCamp >= 0 && outcome != InstitutionOutcome.Dissolved)
                {
                    var ov = new Dictionary<string, string>(v) { { "camp", CampName(i, i.OrderCamp) }, { "short", i.Def.ShortName } };
                    bool held = i.DriftPath == i.Def.DriftPaths[i.OrderCamp];
                    string band = i.OrderForce >= T.Get("offices.textStrong") ? "strong" : i.OrderForce >= T.Get("offices.textSome") ? "some" : "weak";
                    parts.Add(text.Template("discovery.orders." + band + (held ? ".held" : ".lost"), ov));
                }
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
            parts.AddRange(WorkEchoes(arrival));
            parts.AddRange(SavingsLines(arrival, values));
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
            // The fountain you didn't choose at first, but repaired later yourself.
            if (World.CompletedProjects.Contains("fountain")) secondKey = fountainRuns ? "unchosenFountain.laterRuns" : "unchosenFountain.laterDry";
            else secondKey = fountainRuns ? "unchosenFountain.fixed" : "unchosenFountain.foul";
            // What the apprentices carried, the smith's regard and the Economy decide the workshop's fate (P0-34).
            return "workshop." + WorkshopFate();
        }

        private string CoinKey()
        {
            double factor = CoinStanceFactor();
            if (factor > 0) return "sound";
            if (factor < 0) return "debased";
            return HistoricalSilver(Now.Year) < T.Get("policy.coin.debasedBelowSilver") ? "historyDebased" : "historyMild";
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
