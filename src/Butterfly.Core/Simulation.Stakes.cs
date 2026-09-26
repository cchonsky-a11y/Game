using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Stakes, influence and rivals (decided 2026-09-27). Each domain has established institutions you can buy
    /// into, 1% at a time and at a rising price, and one you can found yourself. 10% counts toward influence
    /// but gives no oversight; 25% gives a voice (priorities, policy, a plague response); 50% gives oversight
    /// and control. Your influence over a domain is how much of it your institutions hold, weighted by how
    /// much of each you control; rivals push back when one you control grows.
    /// </summary>
    public sealed partial class Simulation
    {
        public static string StakeKey(Institution i) => i.Key + ".stake";

        public double InfluenceAt => T.Get("stakes.influenceAt");
        public double VoiceAt => T.Get("stakes.voiceAt");
        public double ControlAt => T.Get("stakes.controlAt");

        public bool HasInfluence(Institution i) => i.Exists && i.Stake >= InfluenceAt - 1e-9;
        public bool HasVoice(Institution i) => i.Exists && i.Stake >= VoiceAt - 1e-9;
        public bool Controls(Institution i) => i.Exists && i.Stake >= ControlAt - 1e-9;

        /// <summary>Institutions you hold any stake in.</summary>
        public IEnumerable<Institution> Backed() => World.Institutions.Where(i => i.Backed);

        /// <summary>Institutions whose stake counts toward influence (10%+): these are the ones you leave behind.</summary>
        public IEnumerable<Institution> Influential() => World.Institutions.Where(HasInfluence);

        /// <summary>Institutions you control (50%+).</summary>
        public IEnumerable<Institution> Controlled() => World.Institutions.Where(Controls);

        public IEnumerable<Institution> InDomain(Domain d) => World.Institutions.Where(i => i.Exists && i.Def.Maintains == d);

        /// <summary>An institution's share of its domain: its strength over the strength of every institution there.</summary>
        public double DomainShare(Institution i)
        {
            if (!i.Exists) return 0;
            double total = InDomain(i.Def.Maintains).Sum(x => x.Strength);
            return total <= 0 ? 0 : i.Strength / total;
        }

        /// <summary>How far you steer an institution: 0 below 10%, rising to 1 at 50% (control).</summary>
        public double ControlFactor(Institution i) => HasInfluence(i) ? Math.Min(1, i.Stake / ControlAt) : 0;

        /// <summary>Your influence over a domain (0–1): Σ control factor × domain share.</summary>
        public double Influence(Domain d) => InDomain(d).Sum(i => ControlFactor(i) * DomainShare(i));

        /// <summary>How much of a priority or policy takes effect: influence × 2, capped at 1.</summary>
        public double Sway(Domain d) => Math.Min(1, T.Get("stakes.swayPerInfluence") * Influence(d));

        /// <summary>A domain you can set priorities for: you have a voice (25%+) in a working institution that maintains it.</summary>
        public bool HasHold(Domain d) => VoiceIn(d) != null;

        /// <summary>The working institution in a domain where your stake is largest, if it gives you a voice.</summary>
        public Institution? VoiceIn(Domain d) =>
            InDomain(d).Where(i => HasVoice(i) && i.Strength >= T.Get("institutions.dissolvedBelow"))
                       .OrderByDescending(i => i.Stake).ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();

        public int StakePercent(Institution i) => (int)Math.Round(i.Stake * 100);

        /// <summary>Price of the next <paramref name="points"/> percent: each 1% costs domain base × (1 + stake% × 0.1).</summary>
        public double StakeCost(Institution i, int points)
        {
            double b = T.Get("stakes.costPerPercent." + i.Def.Maintains.Key());
            double g = T.Get("stakes.costGrowthPerPercent");
            int from = StakePercent(i);
            double cost = 0;
            for (int k = from; k < from + points; k++) cost += b * (1 + g * k);
            return cost;
        }

        /// <summary>Price of going from 0% to 50% in an established institution of this domain.</summary>
        public double ControlCost(Domain d)
        {
            double b = T.Get("stakes.costPerPercent." + d.Key());
            double g = T.Get("stakes.costGrowthPerPercent");
            int n = (int)Math.Round(ControlAt * 100);
            double cost = 0;
            for (int k = 0; k < n; k++) cost += b * (1 + g * k);
            return cost;
        }

        /// <summary>Founding your own institution costs about 65% of buying control of an established one in the same domain.</summary>
        public double FoundCost(Domain d) => Math.Round(T.Get("founding.costShareOfControl") * ControlCost(d));

        /// <summary>
        /// Buys <paramref name="points"/> more percent of an established institution (1 Attention). Your first
        /// purchase makes you a member with 1%; stake gives influence at 10%, a voice at 25%, control at 50%.
        /// </summary>
        public CommandResult Buy(string id, int points)
        {
            var inst = FindInstitution(id);
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (inst.Def.IsOwn) return CommandResult.Fail(Cap(inst.Def.Name) + " would be your own: found it instead (found " + inst.Key + ").");
            if (!inst.Exists) return CommandResult.Fail(Cap(inst.Def.Name) + " is gone.");
            if (points <= 0) return CommandResult.Fail("Buy how many percent?");
            int from = StakePercent(inst);
            points = Math.Min(points, 100 - from);
            if (points <= 0) return CommandResult.Fail("You already own all of " + inst.Def.ShortName + ".");
            double cost = StakeCost(inst, points);
            if (World.Gold < cost) return CommandResult.Fail(points + "% of " + inst.Def.ShortName + " costs " + F(cost) + " gold; you have " + F(World.Gold) + ".");
            int att = T.GetInt("stakes.buyAttention");
            var attention = CheckAttention(att);
            if (attention != null) return attention;
            SpendAttention(att);
            double gold = World.Gold, stake = inst.Stake, loyalty = inst.Loyalty;
            bool first = stake <= 0;
            SpendGold(cost);
            inst.Stake = (from + points) / 100.0;
            if (first) inst.Loyalty = T.Get("stakes.memberLoyalty");
            var effects = new List<Effect> { new Effect(StakeKey(inst), stake, inst.Stake), new Effect(GoldKey, gold, World.Gold) };
            if (first) effects.Add(new Effect(LoyaltyKey(inst), loyalty, inst.Loyalty));
            string crossed = Crossed(stake, inst.Stake);
            Record("institution.buy", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player", inst.Leader }, effects,
                (first ? inst.Def.FoundText + " " : "") + "You now hold " + (from + points) + "% of " + inst.Def.Name + "." + crossed);
            return CommandResult.Success("You hold " + (from + points) + "% of " + inst.Def.ShortName + " (" + F(cost) + " gold, " + att + " Attention)." + crossed +
                                         (Controls(inst) ? "" : " Next 1% costs " + F(StakeCost(inst, 1)) + "."));
        }

        private string Crossed(double before, double after)
        {
            if (before < ControlAt - 1e-9 && after >= ControlAt - 1e-9) return " You control it now: you can oversee, mentor, charter, endow, audit and invest in it.";
            if (before < VoiceAt - 1e-9 && after >= VoiceAt - 1e-9) return " You have a voice in it now: you can set priorities in its domain" +
                                                                           " (and policy, through a Governance institution).";
            if (before < InfluenceAt - 1e-9 && after >= InfluenceAt - 1e-9) return " Your stake now counts toward influence over its domain.";
            return "";
        }

        /// <summary>Founds your own institution: you control it from the start, but it starts weak and may fail.</summary>
        public CommandResult Found(string id)
        {
            var inst = FindInstitution(id);
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (!inst.Def.IsOwn) return CommandResult.Fail(Cap(inst.Def.Name) + " already exists; buy into it instead (buy " + inst.Key + ").");
            if (inst.Exists) return CommandResult.Fail(Cap(inst.Def.Name) + " already exists.");
            if (inst.Collapsed) return CommandResult.Fail(Cap(inst.Def.Name) + " failed; you can't found it again this era.");
            double cost = FoundCost(inst.Def.Maintains);
            if (World.Gold < cost) return CommandResult.Fail("Founding " + inst.Def.Name + " costs " + F(cost) + " gold.");
            int att = T.GetInt("founding.attention");
            var attention = CheckAttention(att);
            if (attention != null) return attention;
            SpendAttention(att);
            double gold = World.Gold;
            SpendGold(cost);
            inst.Exists = true;
            inst.Stake = 1;
            inst.Strength = T.Get("founding.startStrength");
            inst.Loyalty = T.Get("founding.startLoyalty");
            Record("institution.found", inst.Key, null, new[] { "player", inst.Leader },
                new[] { new Effect(StakeKey(inst), 0, 1), new Effect(StrengthKey(inst), 0, inst.Strength), new Effect(LoyaltyKey(inst), 0, inst.Loyalty), new Effect(GoldKey, gold, World.Gold) },
                inst.Def.FoundText);
            return CommandResult.Success("You founded " + inst.Def.Name + ", led by " + inst.Leader + " (" + F(cost) + " gold, " + att + " Attention). " +
                                         "It holds " + F(DomainShare(inst) * 100) + "% of " + inst.Def.Maintains + "; below strength " +
                                         F(T.Get("founding.fragileBelow")) + " it may fail.");
        }

        /// <summary>Puts gold into an institution you control to build it up (strength), 1 Attention.</summary>
        public CommandResult Invest(string id, double amount)
        {
            var inst = FindInstitution(id);
            var fail = RequireControl(inst, id);
            if (fail != null) return fail;
            if (amount <= 0) return CommandResult.Fail("Invest how much?");
            if (World.Gold < amount) return CommandResult.Fail("You have only " + F(World.Gold) + " gold.");
            if (inst!.Strength >= 100) return CommandResult.Fail(Cap(inst.Def.ShortName) + " is as strong as it can be.");
            int att = T.GetInt("stakes.investAttention");
            var attention = CheckAttention(att);
            if (attention != null) return attention;
            SpendAttention(att);
            double gold = World.Gold, strength = inst.Strength;
            SpendGold(amount);
            inst.Strength = Math.Min(100, inst.Strength + amount * T.Get("stakes.investStrengthPerGold"));
            Record("institution.invest", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player", inst.Leader },
                new[] { new Effect(StrengthKey(inst), strength, inst.Strength), new Effect(GoldKey, gold, World.Gold) },
                "You put " + F(amount) + " gold into " + inst.Def.Name + ": rooms, pay, members. Strength " + F(strength) + " → " + F(inst.Strength) + ".");
            return CommandResult.Success(Cap(inst.Def.ShortName) + ": strength " + F(inst.Strength) + ", " + F(DomainShare(inst) * 100) + "% of " + inst.Def.Maintains + ".");
        }

        /// <summary>Null if you control the institution; otherwise why you can't direct it.</summary>
        internal CommandResult? RequireControl(Institution? inst, string id)
        {
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (!inst.Exists) return CommandResult.Fail(inst.Def.IsOwn && !inst.Collapsed ? "Found it first (found " + inst.Key + ")." : Cap(inst.Def.Name) + " is gone.");
            if (!Controls(inst))
                return CommandResult.Fail("You hold " + StakePercent(inst) + "% of " + inst.Def.ShortName + "; you need " + F(ControlAt * 100) +
                                          "% to direct it (buy " + inst.Key + " <percent>).");
            return null;
        }

        /// <summary>Test setup: gives the player a stake in an institution (founding it if it is your own).</summary>
        internal void GrantStake(string id, double stake)
        {
            var inst = World.Institution(id);
            if (!inst.Exists)
            {
                inst.Exists = true;
                inst.Strength = T.Get("founding.startStrength");
            }
            if (inst.Stake <= 0) inst.Loyalty = inst.Def.IsOwn ? T.Get("founding.startLoyalty") : T.Get("stakes.memberLoyalty");
            inst.Stake = stake;
            MarkChanged(StakeKey(inst), Log.Events.Count > 0 ? Log.Events[Log.Events.Count - 1].Id : 1);
        }

        // ---- the in-era year: fragile foundations and rivals -------------------

        /// <summary>A young institution of your own may fail each year until it is established.</summary>
        private void FragileFoundationsYearTick()
        {
            foreach (var i in World.Institutions.Where(i => i.Exists && i.Def.IsOwn && i.Strength < T.Get("founding.fragileBelow")).ToList())
            {
                if (!Rng.Chance(T.Get("founding.collapseChancePerYear"))) continue;
                double s = i.Strength;
                i.Exists = false;
                i.Collapsed = true;
                i.Strength = 0;
                Record("institution.collapse", i.Key, CausesOf(StrengthKey(i)), new[] { i.Leader },
                    new[] { new Effect(StrengthKey(i), s, 0) },
                    Cap(i.Def.Name) + " fails: too few members, too little money, and " + i.Leader + " takes a better offer. " +
                    "Young institutions fail until they are established (strength " + F(T.Get("founding.fragileBelow")) + ").");
            }
        }

        /// <summary>
        /// Rivals push back: when an institution you control holds a large share of its domain, each rival in the
        /// domain may strike at it (rumors, lawsuits, poached members).
        /// </summary>
        private void RivalryYearTick()
        {
            foreach (var mine in Controlled().ToList())
            {
                if (DomainShare(mine) < T.Get("rivalry.shareThreshold")) continue;
                foreach (var rival in InDomain(mine.Def.Maintains).Where(r => r != mine && !Controls(r)).ToList())
                {
                    if (!Rng.Chance(T.Get("rivalry.strikeChancePerYear"))) continue;
                    ChangeStrength(mine, -T.Get("rivalry.strikeStrength"), "rivalry.strike", CausesOf(StrengthKey(mine)), new[] { rival.Leader },
                        Cap(rival.Def.Name) + " works against " + mine.Def.Name + " (" + F(DomainShare(mine) * 100) + "% of " + mine.Def.Maintains +
                        "): " + RivalMove(mine.Def.Maintains) + ".");
                }
            }
        }

        private static string RivalMove(Domain d) =>
            d == Domain.Medicine ? "rumors that its remedies kill, and patients poached with free cures"
            : d == Domain.Governance ? "a lawsuit in the praetor's court and two of its senators bought away"
            : "undercutting its prices and calling in its members' loans";
    }
}
