using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Institutions (SYSTEMS §7): charters, endowments, audits and oversight for those you control, loyalty,
    /// decay per decade during absence and the two pre-authored drift paths each. Stakes, founding and
    /// rivals live in Simulation.Stakes.cs.
    /// </summary>
    public sealed partial class Simulation
    {
        private void InitInstitutions()
        {
            foreach (var def in Data.Content.Institutions)
            {
                var i = new Institution(def);
                if (!def.IsOwn)
                {
                    i.Exists = true;
                    i.Strength = T.Get("institutions.establishedStrength." + def.Id);
                }
                World.Institutions.Add(i);
            }
            foreach (var i in World.Institutions.Where(i => i.Exists)) i.BaselineShare = DomainShare(i);
        }

        internal static string StrengthKey(Institution i) => i.Key + ".strength";
        internal static string LoyaltyKey(Institution i) => i.Key + ".loyalty";
        internal static string HoldingsKey(Institution i) => i.Key + ".holdings";

        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "senate", "faction" }, { "physicians", "circle" }, { "island", "sanctuary" }, { "temple", "sanctuary" },
            { "junius", "junian" }, { "octavius", "bank" }, { "menodora", "house" }, { "aventine", "club" }, { "ostia", "guild" },
        };

        public Institution? FindInstitution(string text)
        {
            text = (text ?? "").Trim().ToLowerInvariant();
            if (text.Length < 3) return null;
            if (Aliases.TryGetValue(text, out var alias)) text = alias;
            return World.Institutions.FirstOrDefault(i => i.Key == text)
                   ?? World.Institutions.FirstOrDefault(i => i.Def.ShortName.ToLowerInvariant().Replace("the ", "").StartsWith(text, StringComparison.Ordinal));
        }

        public CommandResult Charter(string id)
        {
            var inst = FindInstitution(id)!;
            var fail = RequireControl(inst, id);
            if (fail != null) return fail;
            if (inst.Chartered) return CommandResult.Fail(Cap(inst.Def.ShortName) + " already has a charter.");
            double cost = T.Get("institutions.charterGold");
            if (World.Gold < cost) return CommandResult.Fail("A charter costs " + Money(cost) + ".");
            var attention = CheckAttention(T.GetInt("institutions.charterAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("institutions.charterAttention"));
            double gold = World.Gold;
            SpendGold(cost);
            inst.Chartered = true;
            Record("institution.charter", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player", inst.Leader },
                new[] { new Effect(inst.Key + ".chartered", 0, 1), new Effect(GoldKey, gold, World.Gold) },
                "You write a charter for " + inst.Def.Name + ": its founding principles, in your hand. Charters slow drift.");
            return CommandResult.Success(Cap(inst.Def.ShortName) + " is chartered (" + F(cost) + " gold, " + T.GetInt("institutions.charterAttention") + " Attention). A charter slows drift; with an endowment of " + F(T.Get("institutions.endowGold")) + "+ gold it also slows decay while you are away.");
        }

        /// <summary>Endows the institution with the minimum endowment.</summary>
        public CommandResult Endow(string id) => Endow(id, T.Get("institutions.endowGold"));

        /// <summary>
        /// Gives the institution gold to hold. Once holdings reach the minimum endowment it counts as endowed
        /// (no more yearly upkeep; needed for the chartered-and-endowed quality). During the 30 years after
        /// departure, holdings grow with the economy and pay down the institution's domain debt.
        /// </summary>
        public CommandResult Endow(string id, double amount)
        {
            var inst = FindInstitution(id)!;
            var fail = RequireControl(inst, id);
            if (fail != null) return fail;
            if (amount <= 0) return CommandResult.Fail("Endow how much?");
            if (World.Gold < amount) return CommandResult.Fail("You have only " + Money(World.Gold) + ".");
            var attention = CheckAttention(T.GetInt("institutions.endowAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("institutions.endowAttention"));
            double gold = World.Gold, holdings = inst.Holdings;
            SpendGold(amount);
            inst.Holdings += amount;
            bool nowEndowed = !inst.Endowed && inst.Holdings >= T.Get("institutions.endowGold");
            if (nowEndowed) inst.Endowed = true;
            var effects = new List<Effect> { new Effect(HoldingsKey(inst), holdings, inst.Holdings), new Effect(GoldKey, gold, World.Gold) };
            if (nowEndowed) effects.Add(new Effect(inst.Key + ".endowed", 0, 1));
            Record("institution.endow", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player" }, effects,
                "You give " + inst.Def.Name + " " + Money(amount) + " to hold (now " + Money(inst.Holdings) + ")." +
                (nowEndowed ? " It is endowed: it no longer needs your yearly upkeep." : ""));
            return CommandResult.Success(Cap(inst.Def.ShortName) + " holds " + Money(inst.Holdings) +
                (inst.Endowed ? "." : " (" + F(T.Get("institutions.endowGold")) + " makes it endowed)."));
        }

        /// <summary>An audit charter: halves the corruption hazard and shifts its severity toward Minor.</summary>
        public CommandResult Audit(string id)
        {
            var inst = FindInstitution(id)!;
            var fail = RequireControl(inst, id);
            if (fail != null) return fail;
            if (inst.AuditCharter) return CommandResult.Fail(Cap(inst.Def.ShortName) + " already has an audit charter.");
            double cost = T.Get("institutions.auditGold");
            if (World.Gold < cost) return CommandResult.Fail("An audit charter costs " + Money(cost) + ".");
            var attention = CheckAttention(T.GetInt("institutions.auditAttention"));
            if (attention != null) return attention;
            SpendAttention(T.GetInt("institutions.auditAttention"));
            double gold = World.Gold;
            SpendGold(cost);
            inst.AuditCharter = true;
            Record("institution.audit", inst.Key, CausesOf(HoldingsKey(inst)), new[] { "player", inst.Leader },
                new[] { new Effect(inst.Key + ".audit", 0, 1), new Effect(GoldKey, gold, World.Gold) },
                "You found an audit charter for " + inst.Def.Name + ": outside auditors will open its books every year.");
            return CommandResult.Success(Cap(inst.Def.ShortName) + " has an audit charter (" + Money(cost) + ", " + T.GetInt("institutions.auditAttention") + " Attention).");
        }

        /// <summary>Overseeing in person: one Attention, raises loyalty (GDD §7). Once per turn per institution.</summary>
        public CommandResult Oversee(string id)
        {
            var inst = FindInstitution(id)!;
            var fail = RequireControl(inst, id);
            if (fail != null) return fail;
            if (inst.OverseenTurn == Turn) return CommandResult.Fail("You already oversaw " + inst.Def.ShortName + " this month.");
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

        /// <summary>What you owe institutions each year: your stake's share of their shortfalls, plus your dues.</summary>
        private double InstitutionUpkeepTotal() => Backed().Sum(i => i.Stake * Math.Max(0, -InstitutionNet(i)) + AnnualDues(i));

        private void InstitutionUpkeepShortfall()
        {
            foreach (var i in Backed().Where(i => InstitutionNet(i) < 0 || AnnualDues(i) > 0))
            {
                i.MissedDuesThisYear = true;
                ChangeLoyalty(i, -T.Get("institutions.unpaidLoyaltyLoss"), "institution.unpaid", CausesOf(GoldKey), new[] { i.Leader },
                    "You couldn't pay what you owe " + i.Def.ShortName + " this season (your dues or your share of its costs); no seniority this year.");
            }
        }

        private void ApplyInstitutionExtra(ProjectDef def, ProjectExtra x, int causeId)
        {
            var inst = x.Institution == null ? null : FindInstitution(x.Institution);
            if (inst == null || !inst.Backed) return;
            if (x.Type == "institutionLoyalty")
                ChangeLoyalty(inst, x.Value, "institution.loyalty", new[] { causeId }, new[] { "player" },
                    def.Name + " pleases " + inst.Leader + ".");
            else if (x.Type == "institutionStrength")
                ChangeStrength(inst, x.Value, "institution.strength", new[] { causeId }, new[] { "player" },
                    def.Name + " strengthens " + inst.Def.Name + ".");
        }

        /// <summary>
        /// A grievance (or goodwill) from an institution (P0-31): a member's loyalty changes now; otherwise it is remembered
        /// and counts toward the loyalty you start with if you join.
        /// </summary>
        internal void Grieve(Institution i, double delta, string type, IEnumerable<int>? causes, IEnumerable<string> actors, string text)
        {
            if (i.Backed) { ChangeLoyalty(i, delta, type, causes, actors, text); return; }
            double before = i.Regard;
            i.Regard += delta;
            Record(type, i.Key, causes, actors, new[] { new Effect(i.Key + ".regard", before, i.Regard) }, text + " (It will remember if you join.)");
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

        /// <summary>
        /// In-era yearly step for institutions you control: loyalty fades without attention; loyal ones grow, disloyal
        /// ones wither. Then young institutions of your own may fail, and rivals push back.
        /// </summary>
        private void InstitutionsYearTick()
        {
            foreach (var i in Controlled().ToList())
            {
                ChangeLoyalty(i, -T.Get("institutions.loyaltyFadePerYear"), "institution.loyalty", CausesOf(LoyaltyKey(i)), new[] { i.Leader },
                    // L15: not "without your presence" (the inventor may be in Rome), and L5: not "you follows" when you lead it.
                    YouLead(i) ? "Without your direct oversight, the members of " + i.Def.ShortName + " go more their own way."
                               : "Without your direct oversight, " + i.Leader + " relies more on personal judgment.");
                double growth = i.Loyalty >= T.Get("institutions.growthLoyaltyThreshold")
                    ? T.Get("institutions.growthPerYear") : -T.Get("institutions.witherPerYear");
                ChangeStrength(i, growth, "institution.strength", CausesOf(LoyaltyKey(i)), new[] { i.Leader },
                    Cap(i.Def.ShortName) + (growth > 0 ? " recruits members." : " loses members."));
            }
            FragileFoundationsYearTick();
            RivalryYearTick();
            SeniorityYearTick();
        }

        // ---- plague hooks ---------------------------------------------------

        /// <summary>The Medicine institution you have a voice in, if it is strong enough to run a hospice.</summary>
        internal Institution? HospiceInstitution()
        {
            var m = VoiceIn(Domain.Medicine);
            return m != null && m.Strength >= T.Get("institutions.hospiceMinStrength") ? m : null;
        }

        private bool HospiceAvailable() => HospiceInstitution() != null;

        /// <summary>When the inventor is away, institutions respond on their own if loyal enough.</summary>
        private string AutomaticResponse()
        {
            double min = T.Get("institutions.autonomousResponseLoyalty");
            var h = HospiceInstitution();
            if (h != null && h.Loyalty >= min) return "hospice";
            var g = VoiceIn(Domain.Governance);
            if (g != null && g.Loyalty >= min) return "quarantine";
            return "none";
        }

        /// <summary>Medicine institutions you steer blunt an epidemic in proportion to their strength.</summary>
        private double InstitutionPlagueResilience() =>
            InDomain(Domain.Medicine).Sum(i => ControlFactor(i) * i.Strength / 100.0) * T.Get("institutions.circlePlagueResilience");

        private void OnPlagueResolved(int tollEventId)
        {
            var m = VoiceIn(Domain.Medicine);
            if (m == null) return;
            if (World.Plague.Response == "hospice")
            {
                ChangeStrength(m, T.Get("institutions.hospiceStrengthGain"), "institution.strength", new[] { tollEventId }, new[] { m.Leader },
                    "The hospice made " + m.Def.ShortName + "'s name in the district.");
            }
            else
            {
                ChangeLoyalty(m, -T.Get("institutions.noHospiceLoyaltyLoss"), "institution.loyalty", new[] { tollEventId }, new[] { m.Leader },
                    m.Leader + " watched the district die without a plan.");
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
            // The camp that leads when you leave sets the path (P0-32); while neither leads, the old conditions decide.
            var lead = LeadingCamp(i);
            if (lead != null && i.Def.DriftPaths.Count > lead.Value) return i.Def.DriftPaths[lead.Value];
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
                case "charteredAndDomainHeld":
                    return i.Chartered && World[i.Def.Maintains].Level >= Benchmark(i.Def.Maintains, Now.Year);
                case "soundMoneyAndFreePrices": return SoundMoneyAndFreePrices();
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
            if (!HasInfluence(i) || i.Strength <= 0) return;
            double rate = DecayRate(i.Quality);
            double s = i.Strength, l = i.Loyalty, drift = i.Drift;
            i.Strength = Formulas.Decay(i.Strength, rate, StepFraction);
            i.Loyalty = Formulas.Decay(i.Loyalty, rate, StepFraction);
            double baseDrift = T.Get("institutions.driftPerDecade.base");
            if (i.Chartered) baseDrift *= T.Get("institutions.driftPerDecade.charterMultiplier");
            if (i.Quality == InstitutionQuality.Strong) baseDrift *= T.Get("institutions.driftPerDecade.strongMultiplier");
            // Money is power (P0-31): an endowed institution drifts faster toward its own interests.
            if (i.Holdings >= T.Get("institutions.endowGold")) baseDrift *= T.Get("tradeoffs.endowedDriftMultiplier");
            // Your parting words hold it (P0-32).
            baseDrift *= 1 - T.Get("offices.ordersDriftCut") * i.OrderForce;
            i.Drift += StepFraction * (baseDrift
                       + Rng.NextDouble() * T.Get("institutions.driftPerDecade.random")
                       + (i.Loyalty < T.Get("institutions.lowLoyalty") ? T.Get("institutions.driftPerDecade.lowLoyalty") : 0));
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
            if (i.Collapsed) return InstitutionOutcome.Dissolved;
            if (!HasInfluence(i)) return InstitutionOutcome.NotBacked;
            if (i.Strength < T.Get("institutions.dissolvedBelow")) return InstitutionOutcome.Dissolved;
            if (i.ForcedOutcome.HasValue) return i.ForcedOutcome.Value;
            if (i.Loyalty < T.Get("institutions.rogueBelowLoyalty")) return InstitutionOutcome.Rogue;
            if (i.HasDrifted && i.DriftPath != null && i.DriftPath.Identity == "politically powerful") return InstitutionOutcome.Captured;
            // It became what you asked of it, with weight behind your words (P0-32, revised 2026-09-28): that is thriving, not drift.
            if (i.HasDrifted && i.DriftPath != null && i.OrderCamp >= 0 && i.DriftPath == i.Def.DriftPaths[i.OrderCamp]
                && i.OrderForce >= T.Get("offices.textSome")) return InstitutionOutcome.Thriving;
            if (i.HasDrifted) return InstitutionOutcome.Drifted;
            return InstitutionOutcome.Thriving;
        }

        public static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
