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
            World.Gold = T.Get("gold.start");
            MarkChanged(GoldKey, startEventId);
        }

        /// <summary>Yearly income: base + Economy level × rate + project bonuses.</summary>
        public double YearlyIncome() =>
            T.Get("gold.incomeBase") + World[Domain.Economy].Level * T.Get("gold.incomePerEconomyLevel") + World.IncomeBonus;

        public double YearlyUpkeep(Domain d) => T.Get("priorities.upkeepPerYear." + World[d].Priority.Key());

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
            foreach (var d in DomainInfo.All) World.UpkeepPaidThisYear[(int)d] += YearlyUpkeep(d) > 0 ? paidFraction : 1;
            World.UpkeepTurnsThisYear++;
            if (instPaid < institutionUpkeep) InstitutionUpkeepShortfall();

            string text = "Income +" + F(income) + ", upkeep −" + F(instPaid + domainPaid) + ".";
            if (paidFraction < 1) text += " You could pay only " + F(paidFraction * 100) + "% of domain upkeep; the rest is neglected.";
            Record("gold.settle", GoldKey, CausesOf(LevelKey(Domain.Economy), PriorityKey(Domain.Medicine),
                    PriorityKey(Domain.Governance), PriorityKey(Domain.Economy)), new[] { "world" },
                new[] { new Effect(GoldKey, before, World.Gold) }, text);
        }

        internal void SpendGold(double amount) => World.Gold -= amount;

        // ---- projects -------------------------------------------------------

        public IEnumerable<ProjectDef> AvailableProjects() =>
            Data.Content.Projects.Where(p => !World.CompletedProjects.Contains(p.Id) && World.ActiveProjects.All(a => a.Def.Id != p.Id));

        public CommandResult StartProject(string id)
        {
            var def = Data.Content.Project(id);
            if (def == null) return CommandResult.Fail("No project called '" + id + "'.");
            if (World.CompletedProjects.Contains(id)) return CommandResult.Fail(def.Name + " is already done.");
            if (World.ActiveProjects.Any(a => a.Def.Id == id)) return CommandResult.Fail(def.Name + " is already under way.");
            if (World.Gold < def.Gold) return CommandResult.Fail(def.Name + " costs " + def.Gold + " gold; you have " + F(World.Gold) + ".");
            var attention = CheckAttention(def.AttentionPerTurn);
            if (attention != null) return attention;
            return BeginProject(def, new[] { "player" }, null);
        }

        internal CommandResult BeginProject(ProjectDef def, IEnumerable<string> actors, IEnumerable<int>? causes)
        {
            double before = World.Gold;
            SpendGold(def.Gold);
            SpendAttention(def.AttentionPerTurn);
            var e = Record("project.start", def.Id, causes, actors,
                new[] { new Effect(GoldKey, before, World.Gold) },
                "Work begins: " + def.Name + " (" + def.Gold + " gold, " + def.Turns + " turn" + (def.Turns == 1 ? "" : "s") + ").");
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
            var e = ChangeLevel(def.Domain, def.LevelGain, "project.complete", new[] { p.StartEventId }, new[] { "player" },
                def.CompletionText + " " + def.Domain + " " + Signed(def.LevelGain) + ".");
            int causeId = e?.Id ?? p.StartEventId;
            foreach (var x in def.Extras) ApplyProjectExtra(def, x, causeId);
        }

        private void ApplyProjectExtra(ProjectDef def, ProjectExtra x, int causeId)
        {
            switch (x.Type)
            {
                case "income":
                    World.IncomeBonus += x.Value;
                    Record("income.bonus", GoldKey, new[] { causeId }, new[] { "player" },
                        new[] { new Effect("income.bonus", World.IncomeBonus - x.Value, World.IncomeBonus) },
                        def.Name + " adds " + F(x.Value) + " gold a year.");
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
                if (points <= 0) return CommandResult.Fail("Not enough gold to pay down any " + d + " debt.");
                cost = PaydownCost(points);
            }
            double goldBefore = World.Gold, debtBefore = s.Debt;
            SpendGold(cost);
            s.Debt -= points;
            Record("debt.paydown", d.Key(), CausesOf(DebtKey(d)), new[] { "player" },
                new[] { new Effect(DebtKey(d), debtBefore, s.Debt), new Effect(GoldKey, goldBefore, World.Gold) },
                "You pay " + F(cost) + " gold to clear " + F(points) + " " + d + " debt (1.5× what prevention would have cost).");
            UpdateTier(d);
            return CommandResult.Success("Paid " + F(cost) + " gold; " + d + " debt now " + F(s.Debt) + ".");
        }
    }
}
