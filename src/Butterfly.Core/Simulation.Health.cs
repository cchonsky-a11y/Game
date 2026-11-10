using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The inventor's own body (Stakes and Impact pass, slice 1a; Corey, 2026-11-10; SYSTEMS §3): age and the healthy years
    /// left, catching the plague, the Roman fever, withdrawing to the country, and death, which ends the run.
    /// Health draws from its own seeded generator (derived from the run's seed), so the same seed and inputs always give the
    /// same life, and adding health did not reshuffle every other draw in the game.
    /// </summary>
    public sealed partial class Simulation
    {
        private Rng? _healthRng;
        private Rng HealthRng => _healthRng ??= new Rng(Seed ^ 0x4845414C5448UL);

        // ---- age ------------------------------------------------------------

        /// <summary>The inventor's age: one year older for each year lived in an era, none during a jump (SYSTEMS §3).</summary>
        public int InventorAge => T.GetInt("inventor.startAge") + World.MonthsLived / 12;

        /// <summary>
        /// The healthy years Rome's medicine allows now (S-09): the baseline where Medicine is as history had it, a year more
        /// or less for every few points of the Medicine sub-score above or below 100, within a floor and a ceiling. Read-only.
        /// </summary>
        public int HealthyYearsCeiling
        {
            get
            {
                double years = T.GetInt("inventor.healthyYears") + T.Get("inventor.healthyYearsPerMedicinePoint") * (SubScore(Domain.Medicine) - 100);
                return (int)Math.Round(Math.Max(T.GetInt("inventor.healthyYearsMin"), Math.Min(T.GetInt("inventor.healthyYearsMax"), years)));
            }
        }

        /// <summary>Years Rome's medicine adds to (or takes from) the baseline.</summary>
        public int MedicineYears => HealthyYearsCeiling - T.GetInt("inventor.healthyYears");

        /// <summary>Healthy years still ahead: the ceiling, less the age, less the years illness took.</summary>
        public int HealthyYearsLeft => Math.Max(0, HealthyYearsCeiling - InventorAge - World.HealthyYearsLost);

        /// <summary>"Age 36 · about 52 healthy years left (Rome's medicine: +3)" (shown wherever the date is shown).</summary>
        public string LifeLine => "Age " + InventorAge + " · about " + HealthyYearsLeft + " healthy years left" +
                                  (MedicineYears != 0 ? " (Rome's medicine: " + (MedicineYears > 0 ? "+" : "−") + Math.Abs(MedicineYears) + ")" : "");

        /// <summary>The departure briefing's fact: what Rome has cost in years.</summary>
        public string LifeSpentLine()
        {
            int spent = InventorAge - T.GetInt("inventor.startAge") + World.HealthyYearsLost;
            return "You are " + InventorAge + ". You have spent " + spent + " of your " + (HealthyYearsCeiling - T.GetInt("inventor.startAge")) + " healthy years in Rome.";
        }

        // ---- state ----------------------------------------------------------

        public bool Dead => World.Dead;
        public bool Bedridden => World.BedriddenMonths > 0;
        public bool Weak => !Bedridden && World.WeakMonths > 0;
        public bool Withdrawn => World.Withdrawn;
        /// <summary>No work this month: in bed, or away in the country.</summary>
        public bool Incapacitated => Bedridden || Withdrawn;

        /// <summary>This month's Attention before anything is reserved: none in bed or away, one less while weak.</summary>
        public int AttentionThisMonth =>
            Incapacitated ? 0 : Math.Max(0, AttentionPerTurn - (Weak ? T.GetInt("health.weakAttentionLoss") : 0));

        /// <summary>How the inventor is, for the status line; "" when well.</summary>
        public string HealthLine =>
            Withdrawn ? "You are in the country, away from the city. Nothing in Rome can be done from here."
            : Bedridden ? "You are ill, in bed. No work this month."
            : Weak ? "You are still weak from the illness (" + T.GetInt("health.weakAttentionLoss") + " Attention less this month)."
            : "";

        // ---- the month ------------------------------------------------------

        /// <summary>The month just lived counts against the illness or the stay in the country (start of End Month).</summary>
        private void TickHealth()
        {
            if (World.BedriddenMonths > 0) World.BedriddenMonths--;
            else if (World.WeakMonths > 0) World.WeakMonths--;
            if (!World.Withdrawn) return;
            World.WithdrawnMonths++;
            if (World.Plague.Stage == PlagueState.Passed && World.WithdrawnMonths >= T.GetInt("health.withdraw.minMonths")) ReturnFromCountry();
        }

        private void ReturnFromCountry()
        {
            World.Withdrawn = false;
            Record("inventor.return", "inventor", new[] { World.WithdrawEventId }, new[] { "player" }, null,
                "You come back into the city after " + World.WithdrawnMonths + " months in the country.");
        }

        /// <summary>After the date moves: the late-summer fever (S-06).</summary>
        private void AdvanceHealth()
        {
            World.MonthsLived += MonthsPerTurn;
            if (World.Dead || _pendingDeath != null || Now.Month != T.GetInt("health.fever.month") || Bedridden || Withdrawn) return;
            double chance = T.Get("health.fever.chance") + T.Get("health.fever.perYearOver") * Math.Max(0, InventorAge - T.GetInt("health.fever.fromAge"));
            if (!HealthRng.Chance(chance)) return;
            World.BedriddenMonths = T.GetInt("health.fever.bedriddenMonths");
            World.WeakMonths = T.GetInt("health.fever.weakMonths");
            var e = Record("inventor.ill", "inventor", null, new[] { "player" }, new[] { new Effect("inventor.bedridden", 0, World.BedriddenMonths) },
                "The late-summer fever takes you. You keep to your bed.");
            if (InventorAge < T.GetInt("health.fever.deathFromAge")) return;
            double death = T.Get("health.fever.deathChance") * PlagueDeathChance() / T.Get("health.plague.deathChance");
            if (HealthRng.Chance(death)) _pendingDeath = ("the fever", e.Id);
        }

        // ---- the plague -----------------------------------------------------

        /// <summary>Precautions the inventor has actually taken against the sickness (S-02), by name.</summary>
        public List<string> PlaguePrecautions()
        {
            var taken = new List<string>();
            if (World.CleanWater) taken.Add("clean water at the fountain");
            if (World.Invented.Contains("soap")) taken.Add("soap and boiled linen");
            if (World.CompletedProjects.Contains("quarantine") || World.Plague.Response == "quarantine") taken.Add("quarantine");
            if (World.Invented.Contains("ward")) taken.Add("the fever ward");
            return taken;
        }

        /// <summary>Chance the inventor falls ill in an outbreak that kills this share of Rome (S-02, S-05).</summary>
        public double PlagueIllnessChance(double deathShare)
        {
            double chance = Math.Min(T.Get("health.plague.maxIllnessChance"), T.Get("health.plague.illnessPerDeathShare") * deathShare);
            chance *= Math.Max(0, 1 - T.Get("health.plague.perPrecaution") * PlaguePrecautions().Count);
            if (Withdrawn) chance *= T.Get("health.withdraw.illnessFactor");
            return chance;
        }

        /// <summary>Chance of dying once ill (S-04): care lowers it, never below the floor.</summary>
        public double PlagueDeathChance()
        {
            double chance = T.Get("health.plague.deathChance");
            if (Knows("Serenus")) chance -= T.Get("health.care.physician");
            if (World.Institution("circle").Backed) chance -= T.Get("health.care.circle");
            if (World.Invented.Contains("spirits")) chance -= T.Get("health.care.spirits");
            if (World.Institution("sanctuary").Backed) chance -= T.Get("health.care.sanctuary");
            return Math.Max(T.Get("health.plague.minDeathChance"), chance);
        }

        /// <summary>True while the sickness is known to be coming, or is here, and the inventor could catch it.</summary>
        public bool PlagueThreatensInventor => !Arrived && !IsAway && World.Plague.Stage >= 1 && World.Plague.Stage < PlagueState.Passed;

        /// <summary>
        /// The risk of falling ill as a band, never the number (S-08): from the outbreak Rome would have now, with the
        /// response chosen so far. Read-only.
        /// </summary>
        public string PlagueRiskBand()
        {
            double share = Math.Max(0.01, PlagueSeverity(World.Plague.Response)) * T.Get("plague.deathRatePerSeverity");
            double chance = PlagueIllnessChance(share);
            return chance >= T.Get("health.band.highFrom") ? "high" : chance >= T.Get("health.band.moderateFrom") ? "moderate" : "low";
        }

        /// <summary>"You can catch this." and the band, once it is true; "" otherwise (S-08).</summary>
        public string PlagueRiskLine => PlagueThreatensInventor ? "You can catch this. Your risk of falling ill: " + PlagueRiskBand() + "." : "";

        /// <summary>The outbreak reaches the inventor, or doesn't (called when an outbreak is resolved with the inventor in Rome).</summary>
        private void PlagueReachesInventor(int tollEventId, double deathShare)
        {
            if (IsAway || World.Dead) return;
            if (!HealthRng.Chance(PlagueIllnessChance(deathShare))) return;
            int lostBefore = World.HealthyYearsLost;
            World.BedriddenMonths = T.GetInt("health.plague.bedriddenMonths");
            World.WeakMonths = T.GetInt("health.plague.weakMonths");
            World.HealthyYearsLost += T.GetInt("health.plague.healthyYearsLost");
            var ill = Record("inventor.ill", "inventor", new[] { tollEventId }, new[] { "player" },
                new[] { new Effect("inventor.healthyYearsLost", lostBefore, World.HealthyYearsLost) },
                "The sickness finds you. Fever, then the rash. You are in bed for " + World.BedriddenMonths + " months, and it takes " +
                T.GetInt("health.plague.healthyYearsLost") + " years off the end of your life.");
            if (HealthRng.Chance(PlagueDeathChance())) _pendingDeath = ("the pestilence", ill.Id);
        }

        // ---- leaving the city -----------------------------------------------

        public bool CanWithdraw => !Arrived && !IsAway && !World.Dead && !World.Withdrawn && World.Plague.Stage >= T.GetInt("health.withdraw.fromWarning") && World.Plague.Stage < PlagueState.Passed;

        public double WithdrawGold => Priced(T.Get("health.withdraw.gold"));

        /// <summary>Withdraw to the country until the sickness has passed, as the rich did (S-05).</summary>
        public CommandResult Withdraw()
        {
            if (!CanWithdraw)
                return CommandResult.Fail(World.Withdrawn ? "You are already in the country." : "There is nothing to flee from yet.");
            if (World.Gold < WithdrawGold) return CommandResult.Fail("A cart, a room and food in the country cost " + Money(WithdrawGold) + ".");
            double before = World.Gold;
            SpendGold(WithdrawGold);
            World.Withdrawn = true;
            World.WithdrawnMonths = 0;
            World.Attention = 0;
            var e = Record("inventor.withdraw", "inventor", new[] { World.Plague.LastStageEventId }, new[] { "player" },
                new[] { new Effect(GoldKey, before, World.Gold) },
                "You hire a cart and leave the city for the hills, as the rich are doing. Your work in Rome waits.");
            World.WithdrawEventId = e.Id;
            // The stigma follows the inventor's station at the time (Corey, 2026-11-10): only a duty to stay is held against them.
            var duties = WithdrawDuties();
            ApplyWithdrawStigma(e.Id);
            bool broke = World.Promise.Status == PromiseStatus.Active;
            // A promise to stay is broken by leaving, by cart as by machine.
            if (broke) BreakPromise(e.Id, "You leave the city before the sickness has passed. You promised " + PromiseInstitution.Def.Leader + " you would stay.");
            return CommandResult.Success("You leave for the country. You will come back when the sickness has passed." +
                (duties.Count > 0 ? " It is held against you: " + string.Join("; ", duties) + "." : " No one expects it of you to stay.") +
                (broke ? " You promised " + PromiseInstitution.Def.Leader + " you would stay; that promise is broken." : ""));
        }

        /// <summary>
        /// The duties to stay that the inventor's station carries now (Corey, 2026-11-10): leaving the city was ordinary for
        /// anyone who could afford it, and was held only against those with a duty. Empty for a stranger or a private client.
        /// Read-only.
        /// </summary>
        public List<string> WithdrawDuties()
        {
            var duties = new List<string>();
            if (World.Institution("circle").Backed || PromiseMotive() != null) duties.Add("you are known for healing, and the sick are here");
            foreach (var i in World.Institutions.Where(i => i.Exists && i.Rank >= Officer)) duties.Add("you hold office in " + i.Def.ShortName);
            if (OwnsWorkshop && World.Apprentices > 0) duties.Add("your apprentices stay at the bench");
            return duties;
        }

        /// <summary>"Leaving would be held against you: …" or that no one would mind; "" when there is nothing to flee.</summary>
        public string WithdrawStigmaLine()
        {
            if (!CanWithdraw) return "";
            var duties = WithdrawDuties();
            return duties.Count == 0 ? "No one would hold it against you if you left the city." : "Leaving the city would be held against you: " + string.Join("; ", duties) + ".";
        }

        private void ApplyWithdrawStigma(int causeId)
        {
            bool healer = World.Institution("circle").Backed || PromiseMotive() != null;
            if (healer && Knows("Serenus") && PersonOf("Serenus") is PersonState serenus)
            {
                int rb = serenus.Regard;
                serenus.Regard -= T.GetInt("health.withdraw.regardLoss");
                Record("person.regard", "person.Serenus", new[] { causeId }, new[] { "player", "Serenus" }, new[] { new Effect("person.Serenus.regard", rb, serenus.Regard) },
                    PersonDefOf("Serenus")!.Name + " stayed with his patients. You didn't.");
            }
            foreach (var i in World.Institutions.Where(i => i.Exists && (i.Rank >= Officer || healer && i.Key == "circle" && i.Backed)))
                ChangeLoyalty(i, -T.Get("health.withdraw.loyaltyLoss"), "institution.loyalty", new[] { causeId }, new[] { i.Leader },
                    i.Leader + " notes who left the city and who stayed.");
            if (OwnsWorkshop && World.Apprentices > 0)
                ChangeSmithRegard(-T.Get("workshop.smith.regardPerApprentice"), new[] { causeId }, Data.Content.Smith + " keeps the shop open without you.");
        }

        // ---- death ----------------------------------------------------------

        private (string Cause, int EventId)? _pendingDeath;

        /// <summary>What Rome kept of the inventor, once the run has ended in death (S-07); empty otherwise.</summary>
        public IReadOnlyList<string> Epitaph => World.Epitaph;

        /// <summary>
        /// Death ends the run (SYSTEMS §3: no successor is built). The world goes on for as long as the machine would have
        /// carried the inventor, and the epitaph says what Rome kept. Runs at the very end of End Month.
        /// </summary>
        private void DieIfDue()
        {
            if (_pendingDeath == null) return;
            var (cause, eventId) = _pendingDeath.Value;
            _pendingDeath = null;
            int age = InventorAge, year = Now.Year;
            var known = KnownPeople().ToList();
            Record("inventor.death", "inventor", new[] { eventId }, new[] { "player" }, null,
                "You die in Rome in AD " + year + ", aged " + age + ", of " + cause + ". The machine stands where you left it.");
            World.Dead = true;
            _dying = true;
            var after = Jump(true);
            _dying = false;

            var lines = World.Epitaph;
            lines.Add("You died in Rome in AD " + year + ", aged " + age + ", of " + cause + ".");
            lines.Add("AD " + Now.Year + ", " + after.JumpYears + " years later:");
            foreach (var p in known)
            {
                var band = HumanBandAt(p, Now.Year);
                if (band == null) continue;
                lines.Add(band.Value.Band == HumanBand.Self ? p.Name + " is still alive, aged " + band.Value.Age + "."
                    : band.Value.Band == HumanBand.Elder ? p.Name + " is old now, aged " + band.Value.Age + "."
                    : band.Value.Band == HumanBand.Heirs ? p.Name + " is dead. The family carries on."
                    : p.Name + " is dead, and few remember the name.");
            }
            foreach (var c in World.Capabilities.Where(c => c.Level > CapabilityLevel.None))
                lines.Add(Cap(CapabilityDefOf(c.Id)!.Name) + ": " + c.Level.ToString().ToLowerInvariant() +
                          (c.Distorted ? ", and the copies are bad" : "") + (c.Misattributed ? ", under another name" : "") + ".");
            foreach (var i in after.Institutions) lines.Add(Cap(i.Name) + ": " + i.Outcome.ToString().ToLowerInvariant() + ".");
            var journal = JournalLines().ToList();
            if (journal.Count > 0) lines.Add("What you wrote:");
            lines.AddRange(journal);
            Record("inventor.remembered", "inventor", null, new[] { "world" }, null, string.Join(" ", lines.Skip(1)));
        }

        private bool _dying;
    }
}
