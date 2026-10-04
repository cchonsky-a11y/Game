using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>Gold, upkeep, projects and debt paydown (SYSTEMS §5, §6, §9).</summary>
    public sealed partial class Simulation
    {
        internal const string GoldKey = "gold";

        private void InitEconomy(int startEventId)
        {
            // You arrive with a purse of gold scavenged from the machine, not with Roman coin (decided 2026-09-28).
            World.Aurei = T.Get("gold.start");
            World.Gold = 0;
            MarkChanged(GoldKey, startEventId);
        }

        /// <summary>
        /// Yearly income (decided 2026-09-27): nothing passive before you have leverage. Property you funded and own
        /// grows with the Economy; each institution you hold a stake in pays you your share of its surplus.
        /// Work (the personal action) is paid when you do it.
        /// </summary>
        public double YearlyIncome() => OwnedIncome() + Backed().Sum(i => i.Stake * Math.Max(0, InstitutionNet(i)));

        /// <summary>Income from property you funded (the workshop, the warehouses): rate × Economy level; the workshop's inventions and apprentices raise its part.</summary>
        public double OwnedIncome() => (World.IncomeBonus + WorkshopRate() * (WorkshopOutput() - 1)) * World[Domain.Economy].Level + World.InventionIncome + HouseDividend();

        /// <summary>Menodora's trading house pays its founder a dividend (decided 2026-09-28): a year's rate × Economy ÷ its start, × its power.</summary>
        public double HouseDividend() => OwnPower("house") * T.Get("founding.house.dividendPerYear") * World[Domain.Economy].Level / T.Get("domains.startLevel.economy");

        /// <summary>The workshop share's income rate (per Economy point), if you own it; workshop inventions raise it by a percentage.</summary>
        public double WorkshopRate() =>
            World.CompletedProjects.Contains("workshop")
                ? Data.Content.Project("workshop")?.Extras.Where(x => x.Type == "ownedIncome").Sum(x => x.Value) ?? 0 : 0;

        /// <summary>An institution's income: its own rate × Economy × strength (margins are thin until it is established).</summary>
        public double InstitutionIncome(Institution i) =>
            T.Get("institutions.incomePerEconomyLevel." + i.Key) * World[Domain.Economy].Level * i.Strength / 100.0;

        /// <summary>The institution that carries out your priority for a domain: the one you have the largest voice in.</summary>
        public Institution? Maintainer(Domain d) => VoiceIn(d);

        /// <summary>Upkeep of a domain at its current priority, if your institutions carried it all.</summary>
        public double DomainUpkeep(Domain d) => T.Get("priorities.upkeepPerYear." + World[d].Priority.Key());

        /// <summary>
        /// The part of a domain's priority upkeep this institution pays: upkeep × your sway over the domain, split
        /// among your institutions there by how much of the domain each carries for you.
        /// </summary>
        public double PriorityUpkeep(Institution i)
        {
            var d = i.Def.Maintains;
            double weight = ControlFactor(i) * DomainShare(i), influence = Influence(d);
            if (!HasHold(d) || weight <= 0 || influence <= 0) return 0;
            return DomainUpkeep(d) * Sway(d) * weight / influence;
        }

        /// <summary>An institution's running cost (none if endowed) plus its part of the upkeep of your priority for its domain.</summary>
        public double InstitutionCosts(Institution i) =>
            (i.Endowed ? 0 : T.Get("institutions.upkeepPerYear." + i.Key)) + PriorityUpkeep(i);

        /// <summary>Income minus costs. Your stake's share of a surplus comes to you; of a shortfall, you must cover.</summary>
        public double InstitutionNet(Institution i) => InstitutionIncome(i) - InstitutionCosts(i);

        /// <summary>You never pay domain upkeep directly: your institutions pay it, and otherwise Rome runs the domain itself.</summary>
        public double YearlyUpkeep(Domain d) => 0;

        public double YearlyUpkeepTotal() => DomainInfo.All.Sum(YearlyUpkeep) + InstitutionUpkeepTotal();

        /// <summary>Per-turn settlement of yearly income and upkeep, scaled by turn length.</summary>
        private void SettleGold()
        {
            double income = YearlyIncome() * YearsPerTurn;
            double domainUpkeep = DomainInfo.All.Sum(YearlyUpkeep) * YearsPerTurn;
            double institutionUpkeep = InstitutionUpkeepTotal() * YearsPerTurn;
            double before = World.Gold;
            double available = World.Gold + income;

            // Institutions are paid first; domain upkeep shares whatever is left proportionally.
            double instPaid = Math.Min(available, institutionUpkeep);
            available -= instPaid;
            double paidFraction = domainUpkeep <= 0 ? 1 : Math.Min(1, available / domainUpkeep);
            double domainPaid = domainUpkeep * paidFraction;
            World.Gold = available - domainPaid;
            double instFraction = institutionUpkeep <= 0 ? 1 : instPaid / institutionUpkeep;
            foreach (var d in DomainInfo.All)
            {
                // A domain your institutions maintain is paid for unless one of them ran short and you couldn't cover your share.
                bool short_ = HasHold(d) && Backed().Any(i => i.Def.Maintains == d && InstitutionNet(i) < 0);
                World.UpkeepPaidThisYear[(int)d] += HasHold(d) ? (short_ ? instFraction : 1) : YearlyUpkeep(d) > 0 ? paidFraction : 1;
            }
            World.UpkeepTurnsThisYear++;
            if (instPaid < institutionUpkeep) InstitutionUpkeepShortfall();

            string text = "Income +" + F(income) + ", upkeep −" + F(instPaid + domainPaid) + ".";
            if (paidFraction < 1) text += " You could pay only " + F(paidFraction * 100) + "% of domain upkeep; the rest is neglected.";
            var settle = Record("gold.settle", GoldKey, CausesOf(LevelKey(Domain.Economy), PriorityKey(Domain.Medicine),
                    PriorityKey(Domain.Governance), PriorityKey(Domain.Economy)), new[] { "world" },
                new[] { new Effect(GoldKey, before, World.Gold) }, text);
            // The ledger (P1) shows the settlement's parts, not just its net: income, what institutions were owed, upkeep.
            double net = World.Gold - before, upkeepSide = -instPaid - domainPaid;
            double incomeShown = net - upkeepSide;
            if (Math.Abs(incomeShown) > 1e-12) World.Ledger.Record(new LedgerEntry("e" + settle.Id + ":income", LedgerEntryKind.Payment, incomeShown, "", "Income this month (property, work bonuses, institutions' shares)."));
            if (instPaid > 1e-12) World.Ledger.Record(new LedgerEntry("e" + settle.Id + ":institutions", LedgerEntryKind.InstitutionDues, -instPaid, "", "Dues and your share of institutions' costs this month."));
            if (domainPaid > 1e-12) World.Ledger.Record(new LedgerEntry("e" + settle.Id + ":upkeep", LedgerEntryKind.Expense, -domainPaid, "", "Upkeep of the domains you look after this month."));
        }

        internal void SpendGold(double amount) => World.Gold -= amount;

        // ---- projects -------------------------------------------------------

        public IEnumerable<ProjectDef> AvailableProjects() =>
            Data.Content.Projects.Where(p => !World.CompletedProjects.Contains(p.Id) && World.ActiveProjects.All(a => a.Def.Id != p.Id));

        /// <summary>
        /// What a project costs you: with a voice in an institution that maintains its domain, the institution pays a
        /// quarter of it (decided 2026-09-28: a voice unlocks a little more).
        /// </summary>
        public int ProjectGold(ProjectDef def) =>
            (int)Math.Round(def.Gold * World.PriceLevel * (1 - Math.Max(HasHold(def.Domain) ? T.Get("stakes.voiceProjectShare") : 0, OfficeProjectShare(def.Domain)))
                            * (def.Domain == Domain.Governance && RivalFactionObstructs() != null ? 1 + T.Get("tradeoffs.rivalFactionProjectMarkup") : 1)
                            // Friends in several houses (decided 2026-09-28): each membership takes a little off; the Subura's trust makes quarantine cheaper.
                            * (1 - NetworkDiscount())
                            * (def.Id == "quarantine" && World.Flags.Contains("suburaTrust") ? 1 - T.Get("events.suburaTrustQuarantineDiscount") : 1));

        /// <summary>The network bonus: each established institution you belong to takes network.projectDiscountPerMembership off projects, up to a cap.</summary>
        public double NetworkDiscount() => Math.Min(T.Get("network.projectDiscountMax"), Memberships() * T.Get("network.projectDiscountPerMembership"));

        /// <summary>The faction that obstructs your Governance projects because you hold 10%+ of its rival (P0-31), or null.</summary>
        public Institution? RivalFactionObstructs()
        {
            foreach (var i in World.Institutions.Where(x => x.Def.ExclusiveWith != null && x.Exists))
                if (i.Stake >= T.Get("joining.exclusiveAtStake") - 1e-9)
                {
                    var rival = World.Institution(i.Def.ExclusiveWith!);
                    if (rival.Exists && !rival.Collapsed) return rival;
                }
            return null;
        }

        private static readonly Domain[] PlagueAuthorityDomains = { Domain.Medicine, Domain.Governance };

        private bool AnyInDomains(IEnumerable<Domain> domains, System.Func<Institution, bool> test) =>
            World.Institutions.Where(i => domains.Contains(i.Def.Maintains)).Any(test);

        /// <summary>
        /// Who must back a public project (decided 2026-09-28): influence in an institution of its domain. Plague measures
        /// need influence in a Medicine or Governance institution, or only membership once the first warning has come:
        /// the signs prove your foreknowledge right. Null if you may start it.
        /// </summary>
        public string? ProjectAuthorityBlocker(ProjectDef def)
        {
            double stake = T.Get("authority.publicStake");
            switch (def.Authority)
            {
                case null: return null;
                case "public":
                    return AnyInDomains(new[] { def.Domain }, i => i.Backed && i.Stake >= stake - 1e-9) ? null
                        : def.Name + " is public business: you need " + F(stake * 100) + "% of an institution in " + def.Domain + " to push it through.";
                case "plague":
                    if (AnyInDomains(PlagueAuthorityDomains, i => i.Backed && i.Stake >= stake - 1e-9)) return null;
                    bool warned = World.Plague.Stage >= 1 && T.Get("authority.plagueMemberAfterWarning") > 0;
                    if (warned && AnyInDomains(PlagueAuthorityDomains, i => i.Backed)) return null;
                    return def.Name + " needs the harbor officials to listen: " + F(stake * 100) + "% of a Medicine or Governance institution" +
                           (warned ? ", or membership in one." : "; once the first signs of pestilence prove you right, membership in one will do.");
                default: throw new InvalidOperationException("Unknown project authority: " + def.Authority);
            }
        }

        public CommandResult StartProject(string id)
        {
            var def = Data.Content.Project(id);
            if (def == null) return CommandResult.Fail("No project called '" + id + "'.");
            if (World.CompletedProjects.Contains(id)) return CommandResult.Fail(def.Name + " is already done.");
            if (World.ActiveProjects.Any(a => a.Def.Id == id)) return CommandResult.Fail(def.Name + " is already under way.");
            var authority = ProjectAuthorityBlocker(def);
            if (authority != null) return CommandResult.Fail(authority);
            if (World.Gold < ProjectGold(def)) return CommandResult.Fail(def.Name + " costs " + Money(ProjectGold(def)) + "; you have " + Money(World.Gold) + ".");
            var attention = CheckAttention(def.AttentionPerTurn, def.DurationMonths);
            if (attention != null) return attention;
            return BeginProject(def, new[] { "player" }, null);
        }

        internal CommandResult BeginProject(ProjectDef def, IEnumerable<string> actors, IEnumerable<int>? causes)
        {
            double before = World.Gold;
            int gold = ProjectGold(def);
            var partner = gold < def.Gold ? VoiceIn(def.Domain) : null;
            SpendGold(gold);
            SpendAttention(def.AttentionPerTurn);
            var e = Record("project.start", def.Id, causes, partner == null ? actors : actors.Concat(new[] { partner.Leader }),
                new[] { new Effect(GoldKey, before, World.Gold) },
                "Work begins: " + def.Name + " (" + Money(gold) + (partner == null ? "" : ", " + partner.Def.ShortName + " pays the other " + Money(def.Gold * World.PriceLevel - gold)) + ", " + def.DurationMonths + " month" + (def.DurationMonths == 1 ? "" : "s") + ").");
            var active = new ActiveProject(def, e.Id);
            World.ActiveProjects.Add(active);
            OnProjectStarted(active);
            return CommandResult.Success("Started: " + def.Name + ".");
        }

        /// <summary>Called at end of turn: each active project advances one turn; finished ones take effect.</summary>
        private void ProgressProjects()
        {
            foreach (var p in World.ActiveProjects.ToList())
            {
                p.TurnsRemaining--;
                if (p.TurnsRemaining > 0) continue;
                World.ActiveProjects.Remove(p);
                CompleteProject(p);
            }
        }

        private void CompleteProject(ActiveProject p)
        {
            var def = p.Def;
            World.CompletedProjects.Add(def.Id);
            // Some investments are local: they change nothing Rome-wide by themselves (decided 2026-09-28).
            var e = def.LevelGain != 0
                ? ChangeLevel(def.Domain, def.LevelGain, "project.complete", new[] { p.StartEventId }, new[] { "player" },
                    def.CompletionText + " " + def.Domain + " " + Signed(def.LevelGain) + ".")
                : Record("project.complete", def.Id, new[] { p.StartEventId }, new[] { "player" }, null, def.CompletionText);
            int causeId = e?.Id ?? p.StartEventId;
            foreach (var x in def.Extras) ApplyProjectExtra(def, x, causeId);
        }

        private void ApplyProjectExtra(ProjectDef def, ProjectExtra x, int causeId)
        {
            switch (x.Type)
            {
                case "ownedIncome":
                    World.IncomeBonus += x.Value;
                    Record("income.bonus", GoldKey, new[] { causeId }, new[] { "player" },
                        new[] { new Effect("income.bonus", World.IncomeBonus - x.Value, World.IncomeBonus) },
                        "You own a share of it: about " + Money(x.Value * World[Domain.Economy].Level) + " a year, rising and falling with the Economy.");
                    break;
                case "plagueResilience":
                    World.PlagueResilienceBonus += x.Value;
                    Record("plague.resilience", "plague", new[] { causeId }, new[] { "player" },
                        new[] { new Effect("plague.resilience", World.PlagueResilienceBonus - x.Value, World.PlagueResilienceBonus) },
                        def.Name + " will blunt any epidemic.");
                    break;
                case "cleanWater":
                    World.CleanWater = true;
                    World.FountainCondition = 100;
                    Record("water.clean", "fountain", new[] { causeId }, new[] { "player" },
                        new[] { new Effect("fountain.clean", 0, 1) }, "The district drinks clean water.");
                    break;
                case "levelCost":
                {
                    // A measure's cost elsewhere (P0-31), e.g. quarantine halting trade at Ostia.
                    if (x.Institution != null && DomainInfo.TryParseDomain(x.Institution, out var d))
                        ChangeLevel(d, x.Value, "project.cost", new[] { causeId }, new[] { "world" }, def.Name + " has a cost: " + d + " " + Signed(x.Value) + ".");
                    break;
                }
                case "grievance":
                {
                    var i = x.Institution == null ? null : FindInstitution(x.Institution);
                    if (i != null && i.Exists)
                        Grieve(i, x.Value, "institution.loyalty", new[] { causeId }, new[] { i.Leader }, i.Leader + " of " + i.Def.ShortName + " resents " + def.Name + ".");
                    break;
                }
                default:
                    ApplyInstitutionExtra(def, x, causeId);
                    break;
            }
        }

        // ---- debt paydown ---------------------------------------------------

        public double PaydownCost(double points) =>
            Formulas.PaydownCost(points, T.Get("debt.preventionGoldPerPoint"), T.Get("debt.paydownMultiplier"));

        public CommandResult PayDown(Domain d, double points)
        {
            var s = World[d];
            points = Math.Min(points, s.Debt);
            if (points <= 0) return CommandResult.Fail(d + " has no debt to pay down.");
            double cost = PaydownCost(points);
            if (cost > World.Gold)
            {
                points = Math.Floor(World.Gold / PaydownCost(1));
                if (points <= 0) return CommandResult.Fail("Not enough money to pay down any " + d + " debt.");
                cost = PaydownCost(points);
            }
            double goldBefore = World.Gold, debtBefore = s.Debt;
            SpendGold(cost);
            s.Debt -= points;
            Record("debt.paydown", d.Key(), CausesOf(DebtKey(d)), new[] { "player" },
                new[] { new Effect(DebtKey(d), debtBefore, s.Debt), new Effect(GoldKey, goldBefore, World.Gold) },
                "You pay " + Money(cost) + " to clear " + F(points) + " " + d + " debt (1.5× what prevention would have cost).");
            UpdateTier(d);
            return CommandResult.Success("Paid " + Money(cost) + "; " + d + " debt now " + F(s.Debt) + ".");
        }
    }
}
