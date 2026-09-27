using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The inventor's personal limits and bonds: Attention (SYSTEMS §3), multi-turn commitments,
    /// one personal action per turn, the hour-one seeded choice, and the promise (SYSTEMS §10).
    /// </summary>
    public sealed partial class Simulation
    {
        // ---- attention ------------------------------------------------------

        public int AttentionPerTurn => T.GetInt("attention.perTurn");

        /// <summary>Attention already pledged to multi-turn projects and commitments for this turn.</summary>
        public int ReservedAttention() =>
            World.ActiveProjects.Where(p => p.TurnsRemaining < p.Def.Turns).Sum(p => p.Def.AttentionPerTurn)
            + World.Commitments.Sum(c => T.GetInt("commitments.mentor.attentionPerTurn"))
            + ReservedMachineAttention()
            + ReservedInventionAttention()
            + OfficeDuties();

        private void InitAttention()
        {
            World.Attention = AttentionPerTurn;
        }

        private void RefreshAttention()
        {
            World.Attention = Math.Max(0, AttentionPerTurn - ReservedAttention());
        }

        internal CommandResult? CheckAttention(int amount)
        {
            if (amount > World.Attention)
                return CommandResult.Fail("That needs " + amount + " Attention; you have " + World.Attention + " left this turn.");
            return null;
        }

        internal void SpendAttention(int amount) => World.Attention -= amount;

        private void OnProjectStarted(ActiveProject p) { }

        // ---- personal action ------------------------------------------------

        /// <summary>The one personal action per turn: practice your trade for gold.</summary>
        public static readonly string[] WorkKinds = { "odd", "craft", "consult" };

        public int WorkAttention(string kind) => T.GetInt("personal.work." + kind + ".attention");
        public double WorkGold(string kind) => T.Get("personal.work." + kind + ".gold");

        /// <summary>Memberships in established institutions (their members bring you customers and patrons).</summary>
        public int Memberships() => Backed().Count(i => !i.Def.IsOwn);

        /// <summary>Pay for work before tax: each membership raises it 10% (decided 2026-09-28: benefits to joining).</summary>
        public double WorkPay(string kind) =>
            WorkGold(kind) * WageLevel() * (1 + T.Get("joining.workBonusPerMembership") * Memberships() + (kind == "consult" ? World.ConsultBonus : 0));

        /// <summary>Pay catches up with prices only partly (decided 2026-09-28).</summary>
        public double WageLevel() => 1 + (World.PriceLevel - 1) * T.Get("prices.wageCatchUp");

        /// <summary>
        /// The one personal action per turn: work for pay. Better-paid work takes more Attention (decided 2026-09-27):
        /// odd jobs, skilled craft commissions, or consulting for a wealthy household.
        /// </summary>
        public CommandResult Work(string kind = "odd")
        {
            if (!WorkKinds.Contains(kind)) return CommandResult.Fail("Work at what? odd, craft or consult.");
            if (World.PersonalActionTurn == Turn) return CommandResult.Fail("You already took your personal action this turn.");
            int cost = WorkAttention(kind);
            var attention = CheckAttention(cost);
            if (attention != null) return attention;
            SpendAttention(cost);
            World.PersonalActionTurn = Turn;
            if (kind == "consult") World.ConsultJobs++;
            double before = World.Gold;
            double pay = WorkPay(kind);
            double tax = pay * WorkTaxRate();
            World.Gold += pay - tax;
            string text = kind == "odd" ? "You spend the season mending tools and running errands for pay."
                        : kind == "craft" ? "You take a builder's commission: a crane gear, a better pump."
                        : "You advise a wealthy household on its baths and its books.";
            var workEvent = Record("personal.work", GoldKey, null, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) }, text);
            if (kind == "consult")
            {
                // Consulting ties you to the rich (P0-31): the physicians of the poor think less of you.
                var circle = World.Institution("circle");
                if (circle.Exists)
                    Grieve(circle, T.Get("tradeoffs.consultCircleRegard"), "institution.loyalty", new[] { workEvent.Id }, new[] { circle.Leader },
                        circle.Leader + " notes whose baths you tend.");
            }
            int members = Memberships();
            return CommandResult.Success("You earn " + Money(pay) + (members > 0 ? " (" + Money(WorkGold(kind) * WageLevel() * T.Get("joining.workBonusPerMembership") * members) + " of it through your " + members +
                                         " membership" + (members == 1 ? "" : "s") + ")" : "") + "; tax takes " + Money(tax) + ", you keep " + Money(pay - tax) + ".");
        }

        // ---- multi-turn commitments -----------------------------------------

        public bool CommitmentsEnabled => T.GetBool("commitments.enabled");

        /// <summary>Mentor an institution's members for a season: Attention every turn, pays off at the end.</summary>
        public CommandResult Mentor(string institutionId)
        {
            if (!CommitmentsEnabled) return CommandResult.Fail("Multi-turn commitments are switched off in tuning.json.");
            var inst = FindInstitution(institutionId)!;
            var fail = RequireControl(inst, institutionId);
            if (fail != null) return fail;
            if (World.Commitments.Any(c => c.InstitutionId == inst.Key)) return CommandResult.Fail("You are already mentoring " + inst.Def.ShortName + ".");
            int perTurn = T.GetInt("commitments.mentor.attentionPerTurn");
            var attention = CheckAttention(perTurn);
            if (attention != null) return attention;
            SpendAttention(perTurn);
            int turns = T.GetInt("commitments.mentor.turns");
            var e = Record("commitment.start", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player", inst.Leader }, null,
                "You commit to mentoring " + inst.Def.ShortName + "'s members for " + turns + " turns (" + perTurn + " Attention each turn).");
            World.Commitments.Add(new Commitment("mentor", inst.Key, turns, e.Id));
            return CommandResult.Success("Commitment made: " + turns + " turns.");
        }

        private void ProgressCommitments()
        {
            foreach (var c in World.Commitments.ToList())
            {
                c.TurnsRemaining--;
                if (c.TurnsRemaining > 0) continue;
                World.Commitments.Remove(c);
                var inst = World.Institution(c.InstitutionId);
                ChangeStrength(inst, T.Get("commitments.mentor.strength"), "commitment.complete", new[] { c.StartEventId }, new[] { "player", inst.Leader },
                    "Your season of mentoring pays off: " + inst.Def.ShortName + " has members who can teach others.");
                ChangeLoyalty(inst, T.Get("commitments.mentor.loyalty"), "commitment.complete", new[] { c.StartEventId }, new[] { "player", inst.Leader },
                    inst.Leader + " is grateful.");
            }
        }

        // ---- the hour-one seeded choice -------------------------------------

        public bool SeededChoiceOpen => World.SeededChoice == null;

        /// <summary>Fund the smith's workshop or repair the district fountain (GDD §13). Always an Echo.</summary>
        public CommandResult ChooseSeeded(string option)
        {
            if (!SeededChoiceOpen) return CommandResult.Fail("That choice has already been made.");
            if (option != "fountain" && option != "workshop") return CommandResult.Fail("Choose 'fountain' or 'workshop'.");
            var def = Data.Content.Project(option)!;
            // The smith and the fountain-menders take your gold as it is (decided 2026-09-28): no trip to the changers.
            double aureiNeeded = Math.Ceiling(Math.Max(0, ProjectGold(def) - World.Gold) / AureusPrice - 1e-9);
            if (aureiNeeded > World.Aurei + 1e-9) return CommandResult.Fail("You can't afford it.");
            var attention = CheckAttention(def.AttentionPerTurn);
            if (attention != null) return attention;
            if (aureiNeeded > 0)
            {
                double aureiBefore = World.Aurei, goldBefore = World.Gold;
                World.Aurei -= aureiNeeded;
                World.Gold += aureiNeeded * AureusPrice;
                Record("currency.exchange", "aurei", null, new[] { "player" },
                    new[] { new Effect("aurei", aureiBefore, World.Aurei), new Effect(GoldKey, goldBefore, World.Gold) },
                    "You pay in gold from your purse: " + AureiText(aureiNeeded) + ".");
            }
            World.SeededChoice = option;
            string other = option == "fountain" ? "the smith's workshop" : "the district fountain";
            var e = Record("seeded.choice", option, null, new[] { "player" }, new[] { new Effect("seeded.choice", 0, option == "fountain" ? 1 : 2) },
                "You can afford only one: you choose " + (option == "fountain" ? "the district fountain" : "the smith's workshop") + " over " + other + ".");
            World.SeededChoiceEventId = e.Id;
            BeginProject(def, new[] { "player" }, new[] { e.Id });
            return CommandResult.Success("You chose the " + option + ".");
        }

        private void LapseSeededChoiceIfDue()
        {
            if (!SeededChoiceOpen || Turn < T.GetInt("seededChoice.deadlineTurn")) return;
            World.SeededChoice = "neither";
            var e = Record("seeded.choice", "neither", null, new[] { "world" }, new[] { new Effect("seeded.choice", 0, 3) },
                "The smith and the fountain's neighbors stop waiting for you. You chose neither.");
            World.SeededChoiceEventId = e.Id;
        }

        /// <summary>The seeded choice pays off a couple of years later (GDD §13, minute 40–50).</summary>
        private void SeededPayoffYearTick()
        {
            if (Now.Year != T.GetInt("seededChoice.payoffYear")) return;
            string remember = World.SeededChoice == "workshop" ? " You remember choosing the workshop over the fountain."
                            : World.SeededChoice == "neither" ? " You remember letting both chances pass." : "";
            if (!World.CleanWater)
                ChangeLevel(Domain.Medicine, -T.Get("seededChoice.feverLoss"), "seeded.payoff", new[] { World.SeededChoiceEventId, CauseOf("fountain.clean") },
                    new[] { "world" }, "Fever in the district: children who drink from the broken fountain fall sick." + remember);
            if (!World.CompletedProjects.Contains("workshop"))
            {
                remember = World.SeededChoice == "fountain" ? " You remember choosing the fountain over his workshop." : "";
                // One smith doesn't move Rome's economy; a weak economy is what drives him out (decided 2026-09-28).
                var economy = World[Domain.Economy];
                double history = Benchmark(Domain.Economy, Now.Year);
                if (economy.Level < history)
                    Record("seeded.payoff", "workshop", new[] { World.SeededChoiceEventId, CauseOf(LevelKey(Domain.Economy)) }, new[] { "world" }, null,
                        "Trade is worse than it should be (Economy " + F(economy.Level) + ", history " + F(history) + "): the smith by the Porta Trigemina " +
                        "gives up and takes his tools to Ostia." + remember);
                else
                    Record("seeded.payoff", "workshop", new[] { World.SeededChoiceEventId }, new[] { "world" }, null,
                        "The smith by the Porta Trigemina struggles on with one forge and no apprentices." + remember);
            }
        }

        // ---- the promise ----------------------------------------------------

        public Institution PromiseInstitution => World.Institution("circle");

        /// <summary>
        /// Why Demetria would ask anything of you (decided 2026-09-28): something she has seen you do for the sick.
        /// Null if you have stayed out of medicine entirely; then she never asks.
        /// </summary>
        public string? PromiseMotive()
        {
            if (PromiseInstitution.Backed) return "your years in her Circle";
            var seen = new List<string>();
            if (World.CompletedProjects.Contains("fountain")) seen.Add("the fountain you had cleaned");
            if (World.CompletedProjects.Contains("physician")) seen.Add("the physician you paid for");
            if (World.CompletedProjects.Contains("quarantine")) seen.Add("the quarantine rules you drafted for the docks");
            if (World.CompletedProjects.Contains("midwives")) seen.Add("the midwives you trained");
            if (World.Invented.Contains("soap")) seen.Add("your soap and boiled linen");
            if (World.Institution("sanctuary").Backed) seen.Add("your gifts to the Island sanctuary");
            return seen.Count == 0 ? null : string.Join(", ", seen);
        }

        private void OfferPromise(int causeId)
        {
            if (World.Promise.Status != PromiseStatus.NotOffered || IsAway) return;
            string? motive = PromiseMotive();
            if (motive == null) return;
            World.Promise.Status = PromiseStatus.Offered;
            string who = PromiseInstitution.Backed ? PromiseInstitution.Def.Leader + ", head of the Circle you belong to,"
                                                   : PromiseInstitution.Def.Leader + ", a Greek physician who treats the Subura's poor,";
            var e = Record("promise.offer", "promise", new[] { causeId }, new[] { PromiseInstitution.Def.Leader }, null,
                who + " has watched you: " + motive + ", and the way you talk about fever as if you had seen it before. " +
                "\"You have heard what the letters from Seleucia say. If that sickness comes here, the rich will leave for their villas, and the famous physicians with them. " +
                "Promise me you will stay and help until it has passed.\"");
            World.Promise.OfferEventId = e.Id;
            World.Promise.LastEventId = e.Id;
        }

        public CommandResult AnswerPromise(bool accept)
        {
            var p = World.Promise;
            if (p.Status != PromiseStatus.Offered) return CommandResult.Fail("No promise is waiting for an answer.");
            p.Status = accept ? PromiseStatus.Active : PromiseStatus.Refused;
            var leader = PromiseInstitution.Def.Leader;
            var e = Record(accept ? "promise.accept" : "promise.refuse", "promise", new[] { p.OfferEventId }, new[] { "player", leader },
                new[] { new Effect("promise.status", (int)PromiseStatus.Offered, (int)p.Status) },
                accept ? "You promise " + leader + " you will stay until the sickness has passed." : "You tell " + leader + " you can't promise that.");
            p.LastEventId = e.Id;
            if (PromiseInstitution.Backed)
                ChangeLoyalty(PromiseInstitution, accept ? T.Get("promise.acceptLoyalty") : -T.Get("promise.refuseLoyalty"),
                    "institution.loyalty", new[] { e.Id }, new[] { leader }, accept ? leader + " trusts you." : leader + " is disappointed.");
            return CommandResult.Success(accept ? "You gave your word." : "You refused.");
        }

        private void OnPlaguePassedForPromise(int passedEventId)
        {
            var p = World.Promise;
            if (p.Status == PromiseStatus.Offered) { p.Status = PromiseStatus.Refused; p.Unanswered = true; } // never answered
            if (p.Status != PromiseStatus.Active || IsAway) return;
            p.Status = PromiseStatus.Kept;
            var leader = PromiseInstitution.Def.Leader;
            var e = Record("promise.kept", "promise", new[] { p.LastEventId, passedEventId }, new[] { "player", leader },
                new[] { new Effect("promise.status", (int)PromiseStatus.Active, (int)PromiseStatus.Kept) },
                "You stayed through the pestilence, as you promised " + leader + ".");
            p.LastEventId = e.Id;
            if (PromiseInstitution.Backed)
                ChangeLoyalty(PromiseInstitution, T.Get("promise.keptLoyalty"), "institution.loyalty", new[] { e.Id }, new[] { leader },
                    leader + " will not forget it.");
        }

        public bool PromiseKept() => World.Promise.Status == PromiseStatus.Kept;

        /// <summary>True if leaving now would break an active promise (SCOPE: the promise conflicts with jump timing).</summary>
        public bool LeavingBreaksPromise => World.Promise.Status == PromiseStatus.Active && World.Plague.Stage != PlagueState.Passed;
    }
}
