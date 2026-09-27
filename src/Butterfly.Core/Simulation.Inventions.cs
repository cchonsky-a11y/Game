using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Inventions (decided 2026-09-28; a first version, to be fleshed out later): things the inventor makes from future
    /// knowledge. Knowing is not making: each needs something from Rome (the workshop, a membership, a finished project).
    /// Each pays off in income, standing (leader loyalty) and influence (stake), and can be made once.
    /// </summary>
    public sealed partial class Simulation
    {
        public IEnumerable<InventionDef> AvailableInventions() =>
            Data.Content.Inventions.Where(i => !World.Invented.Contains(i.Id) && World.ActiveInventions.All(a => a.Def.Id != i.Id));

        public static readonly string[] InventionBranches = { "mechanics", "accounts", "hygiene", "workshop" };

        private static readonly Dictionary<string, string> InventionGroupNames = new Dictionary<string, string>
        {
            { "trade", "the guild or the bank" }, { "medicine", "the Circle or the sanctuary" }, { "faction", "a senate faction" },
            { "guild", "the guild" }, { "junian", "the Junian faction" }, { "bank", "the banking house" },
            { "circle", "the Physicians' Circle" }, { "sanctuary", "the sanctuary" },
        };

        /// <summary>What an invention pays, in words: income, workshop, levels, and standing with its patrons (varies by invention).</summary>
        public string InventionPayoffText(InventionDef def)
        {
            var parts = new List<string>();
            foreach (var fx in def.Effects)
                switch (fx.Type)
                {
                    case "income": parts.Add("+" + Money(fx.Value) + " a year"); break;
                    case "workshop": parts.Add("workshop income +" + F(fx.Value * 100) + "%"); break;
                    case "consultBonus": parts.Add("consulting +" + F(fx.Value * 100) + "%"); break;
                    case "level": parts.Add(fx.Domain + " " + Signed(fx.Value)); break;
                    case "plagueResilience": parts.Add("plague resilience +" + F(fx.Value * 100) + "%"); break;
                    case "unrest": parts.Add("but Governance debt +" + F(fx.Value) + " (laborers out of work)"); break;
                    case "grievance": parts.Add("and " + World.Institution(fx.Group!).Def.ShortName + " " + Signed(fx.Value) + " loyalty (it takes their work)"); break;
                }
            foreach (var g in def.Effects.Where(x => x.Group != null).Select(x => x.Group!).Distinct())
            {
                var loyalty = def.Effects.FirstOrDefault(x => x.Group == g && x.Type == "loyalty");
                var stake = def.Effects.FirstOrDefault(x => x.Group == g && x.Type == "stake");
                parts.Add(InventionGroupNames[g] + ": " + string.Join(", ", new[] {
                    stake != null ? "+" + F(stake.Value) + "% stake" : null,
                    loyalty != null ? "+" + F(loyalty.Value) + " loyalty" : null }.Where(x => x != null)) + " (if you're a member)");
            }
            return string.Join("; ", parts);
        }

        /// <summary>The invention tree (decided 2026-09-28): an invention's branch predecessor must be made first.</summary>
        public bool InventionUnlocked(InventionDef def) => def.Prerequisite == null || World.Invented.Contains(def.Prerequisite);

        public InventionDef? InventionById(string id) => Data.Content.Inventions.FirstOrDefault(i => i.Id == id);

        /// <summary>Where an invention stands: made, under way, locked behind its predecessor, waiting on Rome, or ready.</summary>
        public string InventionState(InventionDef def)
        {
            if (World.Invented.Contains(def.Id)) return "made";
            var active = World.ActiveInventions.FirstOrDefault(a => a.Def.Id == def.Id);
            if (active != null) return "under way (" + active.TurnsRemaining + " turn(s) left)";
            if (!InventionUnlocked(def)) return "locked: first make " + InventionById(def.Prerequisite!)!.Name;
            if (!InventionRequirementMet(def)) return "needs " + InventionRequirementText(def);
            return "ready";
        }

        private static readonly Dictionary<string, string[]> InventionGroups = new Dictionary<string, string[]>
        {
            { "trade", new[] { "guild", "bank" } }, { "medicine", new[] { "circle", "sanctuary" } },
            { "faction", new[] { "faction", "junian" } }, { "guild", new[] { "guild" } },
            { "junian", new[] { "junian" } }, { "bank", new[] { "bank" } }, { "circle", new[] { "circle" } }, { "sanctuary", new[] { "sanctuary" } },
        };

        public int InventionGold(InventionDef def) => (int)Math.Round(def.Gold * World.PriceLevel);

        public bool InventionRequirementMet(InventionDef def)
        {
            bool workshop = World.CompletedProjects.Contains("workshop");
            bool backed(params string[] ids) => ids.Any(id => World.Institution(id).Backed);
            switch (def.Requirement)
            {
                case "workshop": return workshop;
                case "tradeMember": return backed("guild", "bank");
                case "medicineWork":
                    return new[] { "fountain", "physician", "quarantine", "midwives" }.Any(World.CompletedProjects.Contains) || backed("circle", "sanctuary");
                case "workshopAndFaction": return workshop && backed("faction", "junian");
                case "workshopAndTrade": return workshop && backed("guild", "bank");
                case "workshopAndGuild10": return workshop && HasInfluence(World.Institution("guild"));
                case "tradeInfluence": return HasInfluence(World.Institution("guild")) || World.Institution("bank").Backed;
                case "factionInfluence": return HasInfluence(World.Institution("faction")) || HasInfluence(World.Institution("junian"));
                case "medicineMember": return backed("circle", "sanctuary");
                case "medicineInfluence": return HasInfluence(World.Institution("circle")) || HasInfluence(World.Institution("sanctuary"));
                default: throw new InvalidOperationException("Unknown invention requirement: " + def.Requirement);
            }
        }

        public string InventionRequirementText(InventionDef def)
        {
            switch (def.Requirement)
            {
                case "workshop": return "the smith's workshop (tools and hands to build it)";
                case "tradeMember": return "membership in the guild or the bank (traders to use it)";
                case "medicineWork": return "a finished Medicine project or membership in the Circle or the sanctuary (physicians to use it)";
                case "workshopAndFaction": return "the workshop and membership in a senate faction (a public-works contract)";
                case "workshopAndTrade": return "the workshop and membership in the guild or the bank (a saddler and carters to use it)";
                case "workshopAndGuild10": return "the workshop and 10% of the guild (a mill site and the guild's backing)";
                case "tradeInfluence": return "10% of the guild or membership in the bank (a house to honor the notes)";
                case "factionInfluence": return "10% of a senate faction (senators to push it through)";
                case "medicineMember": return "membership in the Circle or the sanctuary (surgeons to use it)";
                case "medicineInfluence": return "10% of the Circle or the sanctuary (a house of healing to run it)";
                default: return def.Requirement;
            }
        }

        public CommandResult Invent(string id)
        {
            var def = Data.Content.Inventions.FirstOrDefault(i => i.Id == (id ?? "").Trim().ToLowerInvariant());
            if (def == null) return CommandResult.Fail("No invention called '" + id + "'. (inventions)");
            if (World.Invented.Contains(def.Id)) return CommandResult.Fail(def.Name + " is already made.");
            if (World.ActiveInventions.Any(a => a.Def.Id == def.Id)) return CommandResult.Fail(def.Name + " is already under way.");
            if (!InventionUnlocked(def))
                return CommandResult.Fail(def.Name + " builds on " + InventionById(def.Prerequisite!)!.Name + ": make that first.");
            if (!InventionRequirementMet(def))
                return CommandResult.Fail("Knowing is not making: " + def.Name + " needs " + InventionRequirementText(def) + ".");
            int price = InventionGold(def);
            if (World.Gold < price) return CommandResult.Fail(def.Name + " costs " + Money(price) + "; you have " + Money(World.Gold) + ".");
            var attention = CheckAttention(def.AttentionPerTurn);
            if (attention != null) return attention;
            SpendAttention(def.AttentionPerTurn);
            double before = World.Gold;
            SpendGold(price);
            var e = Record("invention.start", def.Id, null, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                "You start work on " + def.Name + " (" + Money(price) + ", " + def.Turns + " turns).");
            World.ActiveInventions.Add(new ActiveInvention(def, e.Id));
            return CommandResult.Success("Started: " + def.Name + ".");
        }

        private int ReservedInventionAttention() =>
            World.ActiveInventions.Where(a => a.TurnsRemaining < a.Def.Turns).Sum(a => a.Def.AttentionPerTurn);

        private void ProgressInventions()
        {
            foreach (var a in World.ActiveInventions.ToList())
            {
                a.TurnsRemaining--;
                if (a.TurnsRemaining > 0) continue;
                World.ActiveInventions.Remove(a);
                World.Invented.Add(a.Def.Id);
                var done = Record("invention.complete", a.Def.Id, new[] { a.StartEventId }, new[] { "player" },
                    new[] { new Effect("invention." + a.Def.Id, 0, 1) }, a.Def.CompletionText);
                foreach (var fx in a.Def.Effects) ApplyInventionEffect(a.Def, fx, done.Id);
            }
        }

        /// <summary>The institution an invention's effect lands on: the one of its group you hold the most of.</summary>
        private Institution? InventionTarget(InventionEffect fx) =>
            fx.Group == null ? null : InventionGroups[fx.Group].Select(World.Institution).Where(i => i.Backed)
                .OrderByDescending(i => i.Stake).ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();

        private void ApplyInventionEffect(InventionDef def, InventionEffect fx, int causeId)
        {
            switch (fx.Type)
            {
                case "income":
                {
                    double before = World.InventionIncome;
                    World.InventionIncome += fx.Value;
                    Record("income.bonus", GoldKey, new[] { causeId }, new[] { "player" },
                        new[] { new Effect("income.inventions", before, World.InventionIncome) }, def.Name + " pays you " + Money(fx.Value) + " a year.");
                    break;
                }
                case "consultBonus":
                    World.ConsultBonus += fx.Value;
                    Record("income.bonus", GoldKey, new[] { causeId }, new[] { "player" },
                        new[] { new Effect("income.consultBonus", World.ConsultBonus - fx.Value, World.ConsultBonus) },
                        "Households pay more for your advice: consulting +" + F(fx.Value * 100) + "%.");
                    break;
                case "loyalty":
                {
                    var i = InventionTarget(fx);
                    if (i != null) ChangeLoyalty(i, fx.Value, "institution.loyalty", new[] { causeId }, new[] { "player", i.Leader }, def.Name + " impresses " + i.Leader + ".");
                    break;
                }
                case "stake":
                {
                    var i = InventionTarget(fx);
                    if (i == null) break;
                    double before = i.Stake;
                    i.Stake = Math.Min(ExclusiveCapPercent(i) / 100.0, Math.Min(1, (StakePercent(i) + (int)fx.Value) / 100.0));
                    if (i.Stake < before) i.Stake = before;
                    Record("institution.stake", i.Key, new[] { causeId }, new[] { "player", i.Leader }, new[] { new Effect(StakeKey(i), before, i.Stake) },
                        "In return for " + def.Name + ", " + i.Def.Name + " gives you a larger share: " + StakePercent(i) + "%." + Crossed(before, i.Stake));
                    break;
                }
                case "level":
                    if (fx.Domain.HasValue)
                        ChangeLevel(fx.Domain.Value, fx.Value, "invention.effect", new[] { causeId }, new[] { "player" }, def.Name + " spreads: " + fx.Domain.Value + " " + Signed(fx.Value) + ".");
                    break;
                case "workshop":
                {
                    double before = World.WorkshopBonus;
                    World.WorkshopBonus += fx.Value;
                    Record("income.bonus", GoldKey, new[] { causeId }, new[] { "player" },
                        new[] { new Effect("income.workshop", before, World.WorkshopBonus) },
                        def.Name + " improves the workshop: its income is now " + F(World.WorkshopBonus * 100) + "% higher" +
                        (World.CompletedProjects.Contains("workshop") ? "." : " (once you own a share of it)."));
                    break;
                }
                case "plagueResilience":
                    World.PlagueResilienceBonus += fx.Value;
                    Record("plague.resilience", "plague", new[] { causeId }, new[] { "player" },
                        new[] { new Effect("plague.resilience", World.PlagueResilienceBonus - fx.Value, World.PlagueResilienceBonus) }, def.Name + " will blunt any epidemic.");
                    break;
                case "unrest":
                    // Labor-saving inventions put people out of work (P0-31): Governance debt from the unrest.
                    AddDebt(Domain.Governance, fx.Value, "invention.unrest", causeId,
                        def.Name + " puts laborers out of work; the street grumbles (Governance debt +" + F(fx.Value) + ").");
                    break;
                case "grievance":
                {
                    // The trades it undercuts resent it, member or not (P0-31).
                    var i = fx.Group == null ? null : World.Institution(fx.Group);
                    if (i != null && i.Exists)
                        Grieve(i, fx.Value, "institution.loyalty", new[] { causeId }, new[] { i.Leader },
                            i.Leader + " of " + i.Def.ShortName + " resents " + def.Name + ": it takes work from its members.");
                    break;
                }
                default: throw new InvalidOperationException("Unknown invention effect: " + fx.Type);
            }
        }
    }
}
