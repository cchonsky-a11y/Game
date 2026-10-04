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
    /// much of each you control; rivals push back when one you back takes more of its domain.
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
            double b = T.Get("stakes.costPerPercent." + i.Def.Maintains.Key()) * NewcomerPremium(i);
            double g = T.Get("stakes.costGrowthPerPercent");
            int from = StakePercent(i);
            double cost = 0;
            for (int k = from; k < from + points; k++) cost += b * (1 + g * k);
            return Priced(cost);
        }

        /// <summary>Years you have been a member (0 if you aren't one).</summary>
        public double YearsAsMember(Institution i) => i.Stake > 0 ? Math.Max(0, Now.YearFraction - i.JoinedAt) : 0;

        /// <summary>
        /// Newcomers pay a premium for more stake (decided 2026-09-28): ×3 on joining, falling to ×1 after 5 years of
        /// membership. Heavy gold buys influence fast; time makes it cheap. None for your own institutions.
        /// </summary>
        public double NewcomerPremium(Institution i)
        {
            if (i.Def.IsOwn) return 1;
            double fade = Math.Min(1, YearsAsMember(i) / T.Get("stakes.premiumFadeYears"));
            return 1 + (T.Get("stakes.newcomerPremium") - 1) * (1 - fade);
        }

        /// <summary>The entry fee you pay on joining an established institution (none for your own).</summary>
        public double EntryFee(Institution i) => i.Def.IsOwn ? 0 : Priced(T.Get("joining.entryFee." + i.Key));

        /// <summary>What buying <paramref name="points"/> more percent costs you now, including the entry fee if you are joining.</summary>
        public double BuyCost(Institution i, int points) => StakeCost(i, points) + (i.Stake <= 0 ? EntryFee(i) : 0);

        /// <summary>
        /// Annual dues to an established institution you belong to (decided 2026-09-28): a base per institution
        /// plus a little more for every percent you hold, so more influence costs more. None for your own.
        /// </summary>
        public double AnnualDues(Institution i) =>
            !i.Backed || i.Def.IsOwn ? 0 : Priced((T.Get("joining.duesBasePerYear." + i.Key) + T.Get("joining.duesPerStakePercentPerYear") * StakePercent(i))
                                                  * (HasInfluence(i) ? 1 : T.Get("joining.smallStakeDuesShare")));

        public double AnnualDuesTotal() => Backed().Sum(AnnualDues);

        /// <summary>
        /// Attending an established institution's meetings as a member (decided 2026-09-28: more things to spend
        /// Attention on): 1 Attention, once a turn per institution. The leader thinks better of you, and a member who
        /// attends at least twice a year earns an extra point of seniority that year.
        /// </summary>
        public CommandResult Attend(string id, string? camp = null)
        {
            var inst = FindInstitution(id);
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (inst.Def.IsOwn) return CommandResult.Fail(Cap(inst.Def.ShortName) + " is yours; oversee it instead.");
            if (!inst.Backed) return CommandResult.Fail("You aren't a member of " + inst.Def.ShortName + " (buy " + inst.Key + ").");
            if (inst.AttendedTurn == Turn) return CommandResult.Fail("You already attended " + inst.Def.ShortName + " this month.");
            // A meeting is a vote (P0-32): for the camp you name, or the one you usually back.
            int? vote = camp != null && camp.Trim().Length > 0 ? FindCamp(inst, camp) : YourCamp(inst);
            if (camp != null && camp.Trim().Length > 0 && vote == null)
                return CommandResult.Fail("Back which camp? " + string.Join(" or ", inst.Def.DriftPaths.Select(p => p.Id + " (" + p.Name + ")")) + ".");
            int att = T.GetInt("stakes.attendAttention");
            var attention = CheckAttention(att);
            if (attention != null) return attention;
            SpendAttention(att);
            inst.AttendedTurn = Turn;
            inst.MeetingsThisYear++;
            double loyalty = inst.Loyalty;
            inst.Loyalty = Math.Min(100, inst.Loyalty + T.Get("stakes.attendLoyalty"));
            int needed = T.GetInt("stakes.activeMeetingsPerYear");
            var met = Record("institution.attend", inst.Key, CausesOf(StakeKey(inst)), new[] { "player", inst.Leader },
                new[] { new Effect(LoyaltyKey(inst), loyalty, inst.Loyalty) },
                "You sit through a meeting of " + inst.Def.Name + ", speak once, and are remembered for it.");
            if (vote != null) Vote(inst, vote.Value, met.Id);
            var lead = LeadingCamp(inst);
            return CommandResult.Success("Meetings this year: " + inst.MeetingsThisYear + (inst.MeetingsThisYear >= needed
                ? " (an active member: extra seniority this year)." : " (" + needed + " make you an active member, for extra seniority).") +
                (vote != null ? " You spoke for " + CampName(inst, vote.Value) + "." : " You didn't take a side (attend " + inst.Key + " " + string.Join("|", inst.Def.DriftPaths.Select(p => p.Id)) + ").") +
                " " + (lead == null ? "Neither camp leads yet." : "Leading now: " + CampName(inst, lead.Value) + (lead == (vote ?? YourCamp(inst)) ? ", as you want." : ".")));
        }

        /// <summary>
        /// Seniority (decided 2026-09-28): every full year you stay a member of an established institution and pay what
        /// you owe, your stake grows by 1 percentage point, up to 25%: long membership can earn a voice, never control.
        /// </summary>
        private void SeniorityYearTick()
        {
            foreach (var i in Backed().Where(x => !x.Def.IsOwn).ToList())
            {
                // An office lets your standing keep growing (decided 2026-09-28: 10% opens leadership; from there more influence and control).
                int rank = Math.Max(Member, Math.Min(Head, i.Rank));
                double cap = Math.Max(T.Get("stakes.seniorityCap"), T.GetArray("offices.seniorityCap")[rank]);
                bool eligible = YearsAsMember(i) >= 1 - 1e-9 && !i.MissedDuesThisYear && i.Stake < cap - 1e-9;
                bool active = i.MeetingsThisYear >= T.GetInt("stakes.activeMeetingsPerYear");
                i.MissedDuesThisYear = false;
                i.MeetingsThisYear = 0;
                if (!eligible) continue;
                double before = i.Stake;
                int points = T.GetInt("stakes.seniorityPercentPerYear") + (active ? T.GetInt("stakes.activeSeniorityBonus") : 0) + (int)T.GetArray("offices.extraSeniority")[rank];
                i.Stake = Math.Min(Math.Min(cap, ExclusiveCapPercent(i) / 100.0), (StakePercent(i) + points) / 100.0);
                if (i.Stake <= before + 1e-9) { i.Stake = before; continue; }
                Record("institution.seniority", i.Key, CausesOf(StakeKey(i)), new[] { i.Leader },
                    new[] { new Effect(StakeKey(i), before, i.Stake) },
                    "Another year as " + (rank >= Officer ? OfficeTitle(i, rank) + " of" : active ? "an active member of" : "a member of") + " " + i.Def.Name + ": your seniority raises your stake to " + StakePercent(i) + "%." + Crossed(before, i.Stake));
            }
        }

        /// <summary>Price of going from 0% to 50% in an established institution of this domain (at the normal price, without the newcomer premium).</summary>
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
        /// <summary>An institution you founded that still stands (for its power, decided 2026-09-28).</summary>
        public bool OwnStands(Institution i) => i.Def.IsOwn && i.Exists && !i.Collapsed && (!IsAway && !Arrived || OutcomeOf(i) != InstitutionOutcome.Dissolved);

        /// <summary>How far an institution you founded has grown into its power: strength ÷ founding.powerFullAt, at most 1.</summary>
        public double OwnPower(string id)
        {
            var i = World.Institution(id);
            return OwnStands(i) ? Math.Min(1, i.Strength / T.Get("founding.powerFullAt")) : 0;
        }

        public double FoundCost(Domain d) => Math.Round(T.Get("founding.costShareOfControl") * ControlCost(d));

        /// <summary>
        /// Buys <paramref name="points"/> more percent of an established institution (1 Attention). Your first
        /// purchase makes you a member with 1%; stake gives influence at 10%, a voice at 25%, control at 50%.
        /// </summary>
        /// <remarks>
        /// P1 access is decided here, not in the console (P1 correctness pass, 2026-10-04): only an institution whose ownership
        /// is really for sale (the banking house) sells shares. A senator's house takes clients through an introduction, an
        /// invitation path admits members through its inviter, a sanctuary takes gifts (<see cref="Give"/>), and your own
        /// institutions are founded. The P0 stake purchase survives only as <see cref="BuyLegacyStakeForP0Regression"/>.
        /// </remarks>
        public CommandResult Buy(string id, int points)
        {
            var inst = FindInstitution(id);
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (inst.Def.IsOwn) return CommandResult.Fail(Cap(inst.Def.Name) + " would be your own: found it instead (found " + inst.Key + ").");
            if (AccessRefusal(inst) is string refusal) return CommandResult.Fail(refusal);
            return BuyStake(inst, points);
        }

        /// <summary>Why the P1 player can't buy into this institution, or null if its shares are really for sale.</summary>
        public string? AccessRefusal(Institution inst)
        {
            if (inst.Def.IsOwn) return Cap(inst.Def.Name) + " would be your own: found it instead (found " + inst.Key + ").";
            if (OnInvitationPath(inst)) return Cap(inst.Def.ShortName) + " doesn't sell seats: members bring you in.";
            if (PatronageOnly(inst)) return Cap(inst.Def.ShortName) + " doesn't sell places: a senator's following takes clients through a patron's introduction. (" + Cap(PatronageStanding(inst)) + ".)";
            if (TakesGifts(inst)) return Cap(inst.Def.ShortName) + " doesn't sell shares; it takes gifts. Type 'give " + inst.Key + "' (" + Money(GiftCost(inst)) + ").";
            return null;
        }

        /// <summary>
        /// LEGACY, REGRESSION ONLY: the P0 stake purchase into any established institution, for the P0 batch strategies, the
        /// explorer and the snapshot game, which still exercise the P0 stake model (SYSTEMS §7, legacy compatibility). Internal,
        /// so the console and any future UI can't call it; players go through <see cref="Buy"/>, <see cref="Give"/> and the
        /// invitation and patronage paths.
        /// </summary>
        internal CommandResult BuyLegacyStakeForP0Regression(string id, int points)
        {
            var inst = FindInstitution(id);
            if (inst == null) return CommandResult.Fail("No institution called '" + id + "'.");
            if (inst.Def.IsOwn) return CommandResult.Fail(Cap(inst.Def.Name) + " would be your own: found it instead (found " + inst.Key + ").");
            return BuyStake(inst, points);
        }

        /// <summary>The stake purchase itself, once access has been decided.</summary>
        private CommandResult BuyStake(Institution inst, int points)
        {
            if (!inst.Exists) return CommandResult.Fail(Cap(inst.Def.Name) + " is gone.");
            if (points <= 0) return CommandResult.Fail("Buy how many percent?");
            int from = StakePercent(inst);
            points = Math.Min(points, 100 - from);
            if (points <= 0) return CommandResult.Fail("You already own all of " + inst.Def.ShortName + ".");
            if (inst.Stake <= 0)
            {
                string? unmet = JoinBlocker(inst);
                if (unmet != null) return CommandResult.Fail(unmet);
                int minFirst = inst.Def.JoinRequirement == "deposit" ? T.GetInt("joining.bankMinFirstPercent") : T.GetInt("stakes.firstBuyPercent");
                if (points < minFirst)
                    return CommandResult.Fail(Cap(inst.Def.ShortName) + " takes new partners only with a deposit of at least " + minFirst + "% (" +
                                              Money(BuyCost(inst, minFirst)) + "): buy " + inst.Key + " " + minFirst + ".");
            }
            int capPct = ExclusiveCapPercent(inst);
            if (from + points > capPct)
                return CommandResult.Fail(Cap(inst.Def.ShortName) + " won't let a man of " + World.Institution(inst.Def.ExclusiveWith!).Def.ShortName + " hold " +
                                          (capPct + 1) + "% or more of it (you can hold up to " + capPct + "%).");
            double fee = inst.Stake <= 0 ? EntryFee(inst) : 0;
            double cost = BuyCost(inst, points);
            if (World.Gold < cost) return CommandResult.Fail(points + "% of " + inst.Def.ShortName + " costs " + Money(cost) + "; you have " + Money(World.Gold) + ".");
            int att = T.GetInt("stakes.buyAttention");
            var attention = CheckAttention(att);
            if (attention != null) return attention;
            SpendAttention(att);
            double gold = World.Gold, stake = inst.Stake, loyalty = inst.Loyalty;
            bool first = stake <= 0;
            SpendGold(cost);
            inst.Stake = (from + points) / 100.0;
            if (first)
            {
                inst.Loyalty = Math.Max(0, Math.Min(100, T.Get("stakes.memberLoyalty") + inst.Regard));
                inst.Rank = Member;
                inst.Regard = 0;
                inst.JoinedAt = Now.YearFraction;
            }
            var effects = new List<Effect> { new Effect(StakeKey(inst), stake, inst.Stake), new Effect(GoldKey, gold, World.Gold) };
            if (first) effects.Add(new Effect(LoyaltyKey(inst), loyalty, inst.Loyalty));
            string crossed = Crossed(stake, inst.Stake);
            Record("institution.buy", inst.Key, CausesOf(StrengthKey(inst)), new[] { "player", inst.Leader }, effects,
                (first ? inst.Def.FoundText + (fee > 0 ? " Entry fee: " + Money(fee) + "." : "") + " " : "") + "You now hold " + (from + points) + "% of " + inst.Def.Name + "." + crossed);
            return CommandResult.Success("You hold " + (from + points) + "% of " + inst.Def.ShortName + " (" + Money(cost) +
                                         (fee > 0 ? ", including a " + Money(fee) + " entry fee" : "") + ", " + att + " Attention)." + crossed +
                                         " Dues: " + Money(AnnualDues(inst)) + " a year." +
                                         (Controls(inst) ? "" : " Next 1% costs " + Money(StakeCost(inst, 1)) + "."));
        }

        /// <summary>What joining an established institution asks of you, in words (decided 2026-09-28).</summary>
        public string JoinRequirementText(Institution i)
        {
            string text;
            switch (i.Def.JoinRequirement)
            {
                case "medicineWork": text = "a finished Medicine project (the fountain, a physician, or quarantine rules) or your promise to Demetria"; break;
                case "patronage": text = "consulting for wealthy households at least " + T.GetInt("joining.patronageConsultJobs") + " times, or the senator's patronage project"; break;
                case "property": text = "property in Rome (the workshop or the warehouses)"; break;
                case "business": text = "a business of your own (the workshop or the warehouses)"; break;
                case "deposit": text = "a first purchase of at least " + T.GetInt("joining.bankMinFirstPercent") + "%"; break;
                default: text = "nothing beyond the price"; break;
            }
            if (i.Def.ExclusiveWith != null)
                text += "; and less than " + F(T.Get("joining.exclusiveAtStake") * 100) + "% of " + World.Institution(i.Def.ExclusiveWith).Def.ShortName;
            return text;
        }

        /// <summary>Why you can't join this institution yet, or null if you can (the deposit is checked at purchase).</summary>
        /// <summary>
        /// The two factions won't share a member (P0-31): once you hold the lock-out stake (10%) of one, you can't hold that much
        /// of the other, by purchase, seniority or reward. Returns the highest stake percent the institution may reach.
        /// </summary>
        public int ExclusiveCapPercent(Institution i)
        {
            if (i.Def.ExclusiveWith == null) return 100;
            var rival = World.Institution(i.Def.ExclusiveWith);
            int lockAt = (int)Math.Round(T.Get("joining.exclusiveAtStake") * 100);
            return rival.Stake >= T.Get("joining.exclusiveAtStake") - 1e-9 ? lockAt - 1 : 100;
        }

        public string? JoinBlocker(Institution i)
        {
            if (i.Def.ExclusiveWith != null)
            {
                var rival = World.Institution(i.Def.ExclusiveWith);
                if (rival.Stake >= T.Get("joining.exclusiveAtStake") - 1e-9)
                    return Cap(i.Def.ShortName) + " won't take a member of " + rival.Def.ShortName + " (you hold " + StakePercent(rival) + "% of it).";
            }
            bool done(string id) => World.CompletedProjects.Contains(id);
            bool met;
            switch (i.Def.JoinRequirement)
            {
                case "medicineWork":
                    met = done("fountain") || done("physician") || done("quarantine") ||
                          World.Promise.Status == PromiseStatus.Active || World.Promise.Status == PromiseStatus.Kept;
                    break;
                case "patronage": met = World.ConsultJobs >= T.GetInt("joining.patronageConsultJobs") || done("patronage"); break;
                case "property":
                case "business": met = done("workshop") || done("warehouses"); break;
                default: met = true; break;
            }
            return met ? null : i.Leader + " won't take you yet. " + Cap(i.Def.ShortName) + " asks for " + JoinRequirementText(i) + ".";
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
            if (!inst.Def.IsOwn) return CommandResult.Fail(Cap(inst.Def.Name) + " already exists" + (AccessRefusal(inst) == null ? "; buy into it instead (buy " + inst.Key + ")." : "; you can't found it."));
            if (inst.Exists) return CommandResult.Fail(Cap(inst.Def.Name) + " already exists.");
            if (inst.Collapsed) return CommandResult.Fail(Cap(inst.Def.Name) + " failed; you can't found it again this era.");
            double cost = FoundCost(inst.Def.Maintains);
            if (World.Gold < cost) return CommandResult.Fail("Founding " + inst.Def.Name + " costs " + Money(cost) + ".");
            int att = T.GetInt("founding.attention");
            var attention = CheckAttention(att);
            if (attention != null) return attention;
            SpendAttention(att);
            double gold = World.Gold;
            SpendGold(cost);
            inst.Exists = true;
            inst.Stake = 1;
            inst.Rank = Head;
            inst.Strength = T.Get("founding.startStrength");
            inst.Loyalty = T.Get("founding.startLoyalty");
            Record("institution.found", inst.Key, null, new[] { "player", inst.Leader },
                new[] { new Effect(StakeKey(inst), 0, 1), new Effect(StrengthKey(inst), 0, inst.Strength), new Effect(LoyaltyKey(inst), 0, inst.Loyalty), new Effect(GoldKey, gold, World.Gold) },
                inst.Def.FoundText);
            return CommandResult.Success("You founded " + inst.Def.Name + ", led by " + inst.Leader + " (" + Money(cost) + ", " + att + " Attention). " +
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
            if (World.Gold < amount) return CommandResult.Fail("You have only " + Money(World.Gold) + ".");
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
                "You put " + Money(amount) + " into " + inst.Def.Name + ": rooms, pay, members. Strength " + F(strength) + " → " + F(inst.Strength) + ".");
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
        /// The domain share past which rivals push back: 20% for an institution you founded; for an established one,
        /// the share it held at the start (decided 2026-09-28).
        /// </summary>
        public double RivalryThreshold(Institution i) => i.Def.IsOwn ? T.Get("rivalry.ownStartsAtShare") : i.BaselineShare;

        /// <summary>Chance a year that each rival strikes at this institution: it grows with every point of domain share past the threshold.</summary>
        public double RivalStrikeChance(Institution i)
        {
            double excess = DomainShare(i) - RivalryThreshold(i);
            // Your own draws fire from the threshold itself; an established one only once it grows past where it started.
            if (!i.Exists || excess < -1e-9 || (!i.Def.IsOwn && excess <= 1e-9)) return 0;
            return Math.Min(T.Get("rivalry.maxStrikeChancePerYear"),
                T.Get("rivalry.chanceAtThreshold") + T.Get("rivalry.chancePerSharePoint") * Math.Max(0, excess) * 100);
        }

        /// <summary>
        /// Rivals push back against an institution you back once it takes more of its domain (rumors, lawsuits,
        /// poached members): your own from 20% of the domain, an established one once it grows past its starting
        /// share; more often the more it takes. Buying a stake alone provokes no one.
        /// </summary>
        private void RivalryYearTick()
        {
            foreach (var mine in Backed().Where(i => RivalStrikeChance(i) > 0).ToList())
            {
                double chance = RivalStrikeChance(mine);
                foreach (var rival in InDomain(mine.Def.Maintains).Where(r => r != mine && !r.Backed).ToList())
                {
                    if (!Rng.Chance(chance)) continue;
                    ChangeStrength(mine, -T.Get("rivalry.strikeStrength"), "rivalry.strike", CausesOf(StrengthKey(mine)), new[] { rival.Leader },
                        Cap(rival.Def.Name) + " works against " + mine.Def.Name + ", which now holds " + F(DomainShare(mine) * 100) + "% of " +
                        mine.Def.Maintains + ": " + RivalMove(mine.Def.Maintains) + ".");
                }
            }
        }

        private static string RivalMove(Domain d) =>
            d == Domain.Medicine ? "rumors that its remedies kill, and patients poached with free cures"
            : d == Domain.Governance ? "a lawsuit in the praetor's court and two of its senators bought away"
            : "undercutting its prices and calling in its members' loans";
    }
}
