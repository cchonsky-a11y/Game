using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The physicians' circle and the senate faction (SYSTEMS §7): founding, charters, endowments,
    /// oversight, loyalty, decay per decade during absence and the two pre-authored drift paths each.
    /// </summary>
    public sealed partial class Simulation
    {
        private void InitInstitutions()
        {
            foreach (var def in Data.Content.Institutions) World.Institutions.Add(new Institution(def));
        }

        internal static string StrengthKey(Institution i) => i.Key + ".strength";
        internal static string LoyaltyKey(Institution i) => i.Key + ".loyalty";

        public Institution? FindInstitution(string text)
        {
            text = text.ToLowerInvariant();
            return World.Institutions.FirstOrDefault(i => i.Key == text || i.Def.ShortName.ToLowerInvariant().Contains(text) ||
                                                          (text == "senate" && i.Key == "faction") || (text == "physicians" && i.Key == "circle"));
        }

        public CommandResult Found(string id)
        {
            var inst = FindInstitution(id);
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (inst.Founded) return CommandResult.Fail(Cap(inst.Def.Name) + " already exists.");
            double cost = T.Get("institutions.foundGold");
            if (World.Gold < cost) return CommandResult.Fail("Founding costs " + F(cost) + " gold.");
            var attention = CheckAttention(T.GetInt("institutions.foundAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("institutions.foundAttention"));
            double gold = World.Gold;
            SpendGold(cost);
            inst.Founded = true;
            inst.Strength = T.Get("institutions.startStrength");
            inst.Loyalty = T.Get("institutions.startLoyalty");
            Record("institution.found", inst.Key, null, new[] { "player", inst.Leader },
                new[] { new Effect(StrengthKey(inst), 0, inst.Strength), new Effect(LoyaltyKey(inst), 0, inst.Loyalty), new Effect(GoldKey, gold, World.Gold) },
                inst.Def.FoundText);
            return CommandResult.Success("You founded " + inst.Def.Name + ", led by " + inst.Leader + ".");
        }

        public CommandResult Charter(string id)
        {
            var inst = FindInstitution(id);
            if (inst == null || !inst.Founded) return CommandResult.Fail("Found it first.");
            if (inst.Chartered) return CommandResult.Fail(Cap(inst.Def.ShortName) + " already has a charter.");
            double cost = T.Get("institutions.charterGold");
            if (World.Gold < cost) return CommandResult.Fail("A charter costs " + F(cost) + " gold.");
            var attention = CheckAttention(T.GetInt("institutions.charterAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("institutions.charterAttention"));
            double gold = World.Gold;
            SpendGold(cost);
            inst.Chartered = true;
            Record("institution.charter", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player", inst.Leader },
                new[] { new Effect(inst.Key + ".chartered", 0, 1), new Effect(GoldKey, gold, World.Gold) },
                "You write a charter for " + inst.Def.Name + ": its founding principles, in your hand. Charters slow drift.");
            return CommandResult.Success(Cap(inst.Def.ShortName) + " is chartered.");
        }

        public CommandResult Endow(string id)
        {
            var inst = FindInstitution(id);
            if (inst == null || !inst.Founded) return CommandResult.Fail("Found it first.");
            if (inst.Endowed) return CommandResult.Fail(Cap(inst.Def.ShortName) + " is already endowed.");
            double cost = T.Get("institutions.endowGold");
            if (World.Gold < cost) return CommandResult.Fail("An endowment costs " + F(cost) + " gold.");
            double gold = World.Gold;
            SpendGold(cost);
            inst.Endowed = true;
            Record("institution.endow", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player" },
                new[] { new Effect(inst.Key + ".endowed", 0, 1), new Effect(GoldKey, gold, World.Gold) },
                "You endow " + inst.Def.Name + " with rents from two shops. It no longer needs your yearly upkeep.");
            return CommandResult.Success(Cap(inst.Def.ShortName) + " is endowed.");
        }

        /// <summary>Overseeing in person: one Attention, raises loyalty (GDD §7). Once per turn per institution.</summary>
        public CommandResult Oversee(string id)
        {
            var inst = FindInstitution(id);
            if (inst == null || !inst.Founded) return CommandResult.Fail("Found it first.");
            if (inst.OverseenTurn == Turn) return CommandResult.Fail("You already oversaw " + inst.Def.ShortName + " this turn.");
            var attention = CheckAttention(1);
            if (attention != null) return attention;
            SpendAttention(1);
            inst.OverseenTurn = Turn;
            double loyalty = inst.Loyalty, strength = inst.Strength;
            inst.Loyalty = Math.Min(100, inst.Loyalty + T.Get("institutions.overseeLoyalty"));
            inst.Strength = Math.Min(100, inst.Strength + T.Get("institutions.overseeStrength"));
            Record("institution.oversee", inst.Key, CausesOf(LoyaltyKey(inst)), new[] { "player", inst.Leader },
                new[] { new Effect(LoyaltyKey(inst), loyalty, inst.Loyalty), new Effect(StrengthKey(inst), strength, inst.Strength) },
                "You spend the season working alongside " + inst.Leader + ". " + Cap(inst.Def.ShortName) + " grows more loyal.");
            return CommandResult.Success("Loyalty " + F(inst.Loyalty) + ", strength " + F(inst.Strength) + ".");
        }

        internal IEnumerable<Institution> Founded() => World.Institutions.Where(i => i.Founded);

        private double InstitutionUpkeepTotal() =>
            Founded().Where(i => !i.Endowed).Sum(i => T.Get("institutions.upkeepPerYear"));

        private void InstitutionUpkeepShortfall()
        {
            foreach (var i in Founded().Where(i => !i.Endowed))
                ChangeLoyalty(i, -T.Get("institutions.unpaidLoyaltyLoss"), "institution.unpaid", CausesOf(GoldKey), new[] { i.Leader },
                    Cap(i.Def.ShortName) + " went unpaid this season.");
        }

        private void ApplyInstitutionExtra(ProjectDef def, ProjectExtra x, int causeId)
        {
            var inst = x.Institution == null ? null : FindInstitution(x.Institution);
            if (inst == null || !inst.Founded) return;
            if (x.Type == "institutionLoyalty")
                ChangeLoyalty(inst, x.Value, "institution.loyalty", new[] { causeId }, new[] { "player" },
                    def.Name + " pleases " + inst.Leader + ".");
            else if (x.Type == "institutionStrength")
                ChangeStrength(inst, x.Value, "institution.strength", new[] { causeId }, new[] { "player" },
                    def.Name + " strengthens " + inst.Def.Name + ".");
        }

        internal void ChangeLoyalty(Institution i, double delta, string type, IEnumerable<int>? causes, IEnumerable<string> actors, string text)
        {
            double before = i.Loyalty;
            i.Loyalty = Math.Max(0, Math.Min(100, i.Loyalty + delta));
            if (i.Loyalty != before)
                Record(type, i.Key, causes, actors, new[] { new Effect(LoyaltyKey(i), before, i.Loyalty) },
                    text + " Loyalty " + F(before) + " → " + F(i.Loyalty) + ".");
        }

        internal void ChangeStrength(Institution i, double delta, string type, IEnumerable<int>? causes, IEnumerable<string> actors, string text)
        {
            double before = i.Strength;
            i.Strength = Math.Max(0, Math.Min(100, i.Strength + delta));
            if (i.Strength != before)
                Record(type, i.Key, causes, actors, new[] { new Effect(StrengthKey(i), before, i.Strength) },
                    text + " Strength " + F(before) + " → " + F(i.Strength) + ".");
        }

        /// <summary>In-era yearly step: loyalty fades without attention; loyal institutions grow, disloyal ones wither.</summary>
        private void InstitutionsYearTick()
        {
            foreach (var i in Founded())
            {
                ChangeLoyalty(i, -T.Get("institutions.loyaltyFadePerYear"), "institution.loyalty", CausesOf(LoyaltyKey(i)), new[] { i.Leader },
                    "Without your presence, " + i.Leader + " follows " + (i.Key == "circle" ? "her" : "his") + " own judgment more.");
                double growth = i.Loyalty >= T.Get("institutions.growthLoyaltyThreshold")
                    ? T.Get("institutions.growthPerYear") : -T.Get("institutions.witherPerYear");
                ChangeStrength(i, growth, "institution.strength", CausesOf(LoyaltyKey(i)), new[] { i.Leader },
                    Cap(i.Def.ShortName) + (growth > 0 ? " recruits members." : " loses members."));
            }
        }

        // ---- plague hooks ---------------------------------------------------

        private bool HospiceAvailable()
        {
            var c = World.Institution("circle");
            return c.Founded && c.Strength >= T.Get("institutions.hospiceMinStrength");
        }

        /// <summary>When the inventor is away, institutions respond on their own if loyal enough.</summary>
        private string AutomaticResponse()
        {
            var circle = World.Institution("circle");
            var faction = World.Institution("faction");
            double min = T.Get("institutions.autonomousResponseLoyalty");
            if (HospiceAvailable() && circle.Loyalty >= min) return "hospice";
            if (faction.Founded && faction.Loyalty >= min) return "quarantine";
            return "none";
        }

        private double InstitutionPlagueResilience()
        {
            var c = World.Institution("circle");
            return c.Founded ? c.Strength / 100.0 * T.Get("institutions.circlePlagueResilience") : 0;
        }

        private void OnPlagueResolved(int tollEventId)
        {
            var c = World.Institution("circle");
            if (!c.Founded) return;
            if (World.Plague.Response == "hospice")
            {
                ChangeStrength(c, T.Get("institutions.hospiceStrengthGain"), "institution.strength", new[] { tollEventId }, new[] { c.Leader },
                    "The hospice made the Circle's name in the district.");
            }
            else
            {
                ChangeLoyalty(c, -T.Get("institutions.noHospiceLoyaltyLoss"), "institution.loyalty", new[] { tollEventId }, new[] { c.Leader },
                    c.Leader + " watched the district die without a plan.");
            }
        }

        // ---- absence: quality, decay, drift, outcome -------------------------

        /// <summary>
        /// SYSTEMS §7 quality at departure: bare; chartered and endowed; or strong (chartered, endowed,
        /// thriving region, and a loyal leader). P0-ONLY: the loyal leader stands in for "keeping the
        /// Legend alive", because Legend is out of P0 scope (decided 2026-09-26).
        /// </summary>
        public InstitutionQuality QualityAtDeparture(Institution i)
        {
            if (!(i.Chartered && i.Endowed)) return InstitutionQuality.Bare;
            bool thriving = SphereIndex() >= T.Get("institutions.strongMinIndex");
            bool remembered = i.Loyalty >= T.Get("institutions.strongMinLoyalty");
            return thriving && remembered ? InstitutionQuality.Strong : InstitutionQuality.CharteredAndEndowed;
        }

        public double DecayRate(InstitutionQuality q)
        {
            switch (q)
            {
                case InstitutionQuality.Strong: return T.Get("institutions.decayPerDecade.strong");
                case InstitutionQuality.CharteredAndEndowed: return T.Get("institutions.decayPerDecade.chartered");
                default: return T.Get("institutions.decayPerDecade.bare");
            }
        }

        /// <summary>Picks which of the two pre-authored drift paths an institution will follow.</summary>
        internal DriftPathDef ChooseDriftPath(Institution i)
        {
            foreach (var path in i.Def.DriftPaths)
                if (DriftConditionHolds(path.Condition, i)) return path;
            return i.Def.DriftPaths[i.Def.DriftPaths.Count - 1];
        }

        private bool DriftConditionHolds(string condition, Institution i)
        {
            switch (condition)
            {
                case "promiseKeptOrHospice": return PromiseKept() || World.Plague.Response == "hospice";
                case "charteredAndGovernanceHeld":
                    return i.Chartered && World[Domain.Governance].Level >= Benchmark(Domain.Governance, Now.Year);
                case "otherwise": return true;
                default: throw new InvalidOperationException("Unknown drift condition: " + condition);
            }
        }

        /// <summary>
        /// One decade of absence for an institution: strength and loyalty decay at the quality rate;
        /// drift accumulates (slowed by a charter, sped by low loyalty).
        /// </summary>
        internal void InstitutionDecadeStep(Institution i, int decade)
        {
            if (!i.Founded || i.Strength <= 0) return;
            double rate = DecayRate(i.Quality);
            double s = i.Strength, l = i.Loyalty, drift = i.Drift;
            i.Strength = Formulas.Decay(i.Strength, rate, 1);
            i.Loyalty = Formulas.Decay(i.Loyalty, rate, 1);
            double baseDrift = T.Get("institutions.driftPerDecade.base");
            if (i.Chartered) baseDrift *= T.Get("institutions.driftPerDecade.charterMultiplier");
            if (i.Quality == InstitutionQuality.Strong) baseDrift *= T.Get("institutions.driftPerDecade.strongMultiplier");
            i.Drift += baseDrift
                       + Rng.NextDouble() * T.Get("institutions.driftPerDecade.random")
                       + (i.Loyalty < T.Get("institutions.lowLoyalty") ? T.Get("institutions.driftPerDecade.lowLoyalty") : 0);
            var effects = new List<Effect> { new Effect(StrengthKey(i), s, i.Strength), new Effect(LoyaltyKey(i), l, i.Loyalty), new Effect(i.Key + ".drift", drift, i.Drift) };
            string text = Cap(i.Def.ShortName) + " endures another decade.";
            if (!i.HasDrifted && i.Drift >= T.Get("institutions.driftThreshold") && i.DriftPath != null)
            {
                i.HasDrifted = true;
                text = Cap(i.Def.ShortName) + " has become " + i.DriftPath.Name + ".";
            }
            if (i.Strength < T.Get("institutions.dissolvedBelow") && s >= T.Get("institutions.dissolvedBelow"))
                text = Cap(i.Def.ShortName) + " dwindles to nothing and is dissolved.";
            Record("institution.decade", i.Key, CausesOf(StrengthKey(i)), new[] { i.Leader }, effects, text);
            MaybeSucceedLeader(i, decade);
        }

        private void MaybeSucceedLeader(Institution i, int decade)
        {
            // Leaders are succeeded every few decades; the arrival leader is the drift path's face.
            if (decade % T.GetInt("institutions.leaderGenerationDecades") != 0) return;
            string before = i.Leader;
            i.Leader = i.HasDrifted && i.DriftPath != null ? i.DriftPath.ArrivalLeader : "a successor of " + i.Def.Leader;
            if (before != i.Leader)
                Record("institution.leader", i.Key, CausesOf(StrengthKey(i)), new[] { i.Leader }, null,
                    before + " is succeeded by " + i.Leader + ".");
        }

        /// <summary>SYSTEMS §7 outcome on arrival.</summary>
        public InstitutionOutcome OutcomeOf(Institution i)
        {
            if (!i.Founded) return InstitutionOutcome.NotFounded;
            if (i.Strength < T.Get("institutions.dissolvedBelow")) return InstitutionOutcome.Dissolved;
            if (i.Loyalty < T.Get("institutions.rogueBelowLoyalty")) return InstitutionOutcome.Rogue;
            if (i.HasDrifted && i.DriftPath != null && i.DriftPath.Identity == "politically powerful") return InstitutionOutcome.Captured;
            if (i.HasDrifted) return InstitutionOutcome.Drifted;
            return InstitutionOutcome.Thriving;
        }

        public static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
