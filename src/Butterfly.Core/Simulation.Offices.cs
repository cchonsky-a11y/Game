using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Camps, offices and last orders (decided 2026-09-28, P0-32). Each institution's two drift paths are two camps; attending
    /// a meeting is a vote for one, and the camp that leads when you leave sets the path the institution follows while you
    /// are away. You can rise through offices with period titles, which weigh more in votes and cost Attention in duties.
    /// Last orders back a camp and, from the head's seat or in your own institution, name a successor; their force scales
    /// with your office, the leader's loyalty and your voting record.
    /// </summary>
    public sealed partial class Simulation
    {
        public const int Member = 0, Officer = 1, Deputy = 2, Head = 3;

        public string OfficeTitle(Institution i, int rank) =>
            rank < 0 ? "not a member" : i.Def.Offices[Math.Min(rank, i.Def.Offices.Count - 1)];

        public string CampName(Institution i, int camp) => i.Def.DriftPaths[camp].Name;

        /// <summary>A camp by its id or part of its name ("freetraders", "free traders", "cartel").</summary>
        public int? FindCamp(Institution i, string text)
        {
            string t = (text ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0) return null;
            for (int k = 0; k < i.Def.DriftPaths.Count; k++)
            {
                var p = i.Def.DriftPaths[k];
                if (p.Id == t || p.Name.ToLowerInvariant().Contains(t) || p.Id.StartsWith(t, StringComparison.Ordinal)) return k;
            }
            return null;
        }

        /// <summary>The camp that leads now (0 or 1), or null while neither does.</summary>
        public int? LeadingCamp(Institution i) =>
            Math.Abs(i.Lean) < T.Get("offices.undecidedBelow") ? (int?)null : i.Lean > 0 ? 0 : 1;

        /// <summary>The camp you have voted for most, or null if you haven't voted or are split evenly.</summary>
        public int? YourCamp(Institution i) =>
            i.Votes[0] == i.Votes[1] ? (int?)null : i.Votes[0] > i.Votes[1] ? 0 : 1;

        private double VoteWeight(Institution i) => T.GetArray("offices.voteWeight")[Math.Max(0, Math.Min(Head, i.Rank))];

        /// <summary>A vote at a meeting moves the institution toward that camp, more from a higher office.</summary>
        internal void Vote(Institution i, int camp, int causeId)
        {
            double before = i.Lean;
            i.Votes[camp]++;
            i.Lean = Math.Max(-1, Math.Min(1, i.Lean + (camp == 0 ? 1 : -1) * T.Get("offices.votePerWeight") * VoteWeight(i)));
            Record("institution.vote", i.Key, new[] { causeId }, new[] { "player" }, new[] { new Effect(i.Key + ".lean", before, i.Lean) },
                "You speak for " + CampName(i, camp) + " at " + i.Def.ShortName + "'s meeting" + (i.Rank >= Officer ? ", as " + OfficeTitle(i, i.Rank) : "") + ".");
        }

        /// <summary>
        /// Each year the camps pull on their own: toward the first camp while its old condition holds (sound money, a charter,
        /// a kept promise), toward the second otherwise. Your votes have to outweigh it.
        /// </summary>
        private void CampsYearTick()
        {
            foreach (var i in World.Institutions.Where(x => x.Exists && x.Def.DriftPaths.Count >= 2))
            {
                double pull = T.Get("offices.campPullPerYear") * (DriftConditionHolds(i.Def.DriftPaths[0].Condition, i) ? 1 : -1);
                i.Lean = Math.Max(-1, Math.Min(1, i.Lean + pull));
            }
        }

        /// <summary>Why you can't take the next office yet, or null if you qualify.</summary>
        public string? OfficeBlocker(Institution i, int rank)
        {
            if (i.Def.IsOwn) return "you already lead it";
            if (!i.Backed) return "you aren't a member";
            if (rank > i.Def.OfficeCeiling) return "a foreigner can rise no higher than " + OfficeTitle(i, i.Def.OfficeCeiling) + " here";
            string key = rank == Officer ? "officer" : rank == Deputy ? "deputy" : "head";
            if (rank < Head && StakePercent(i) < T.Get("offices." + key + ".stake")) return "it takes a " + F(T.Get("offices." + key + ".stake")) + "% stake";
            if (YearsAsMember(i) < T.Get("offices." + key + ".years") - 1e-9) return "it takes " + F(T.Get("offices." + key + ".years")) + " years as a member";
            if (i.Loyalty < T.Get("offices." + key + ".loyalty")) return i.Leader + "'s regard must reach " + F(T.Get("offices." + key + ".loyalty"));
            if (rank >= Deputy && (YourCamp(i) == null || LeadingCamp(i) != YourCamp(i))) return "your camp must lead it";
            if (rank == Head)
            {
                bool election = i.Key == "guild" && Now.Year % T.GetInt("offices.guildElectionYears") == 0 && StakePercent(i) >= T.Get("offices.head.electionStake");
                if (!election && StakePercent(i) < T.Get("offices.head.stake"))
                    return i.Key == "guild" ? "the guild elects its quinquennalis every " + T.GetInt("offices.guildElectionYears") + " years (next AD " + NextElectionYear() + ")"
                                            : "it takes a " + F(T.Get("offices.head.stake")) + "% stake to lead it";
            }
            return null;
        }

        private int NextElectionYear()
        {
            int every = T.GetInt("offices.guildElectionYears");
            return (Now.Year / every + 1) * every;
        }

        /// <summary>Each year: offers of office to members who qualify (unanswered offers lapse).</summary>
        private void OfficesYearTick()
        {
            foreach (var i in World.Institutions.Where(x => x.Backed && !x.Def.IsOwn).ToList())
            {
                if (i.OfferedRank > 0)
                {
                    Record("office.lapsed", i.Key, null, new[] { i.Leader }, null, "The offer to serve " + i.Def.ShortName + " as " + OfficeTitle(i, i.OfferedRank) + " lapses.");
                    i.OfferedRank = 0;
                }
                int next = Math.Max(Member, i.Rank) + 1;
                if (next > Head || OfficeBlocker(i, next) != null) continue;
                i.OfferedRank = next;
                Record("office.offer", i.Key, CausesOf(StakeKey(i), LoyaltyKey(i)), new[] { i.Leader },
                    new[] { new Effect(i.Key + ".offer", 0, next) },
                    (next == Head && i.Key == "guild" ? "The guild's members elect you quinquennalis" : i.Leader + " asks you to serve " + i.Def.ShortName + " as " + OfficeTitle(i, next)) +
                    ": " + DutyAttention(next, false) + " Attention a turn in duties. (office accept " + i.Key + " / office decline " + i.Key + ")");
            }
        }

        public int DutyAttention(int rank, bool own) =>
            own ? T.GetInt("offices.duty.founder") : rank >= Head ? T.GetInt("offices.duty.head") : rank >= Deputy ? T.GetInt("offices.duty.deputy") : rank >= Officer ? T.GetInt("offices.duty.officer") : 0;

        /// <summary>Attention your offices take every turn.</summary>
        public int OfficeDuties() =>
            World.Institutions.Where(i => i.Exists && (i.Def.IsOwn ? i.Stake > 0 : i.Backed)).Sum(i => DutyAttention(i.Rank, i.Def.IsOwn));

        public CommandResult AnswerOffice(string id, bool accept)
        {
            var i = FindInstitution(id);
            if (i == null || i.OfferedRank <= 0) return CommandResult.Fail("No office is offered to you" + (i == null ? "." : " at " + i.Def.ShortName + "."));
            int rank = i.OfferedRank;
            i.OfferedRank = 0;
            if (!accept)
            {
                Record("office.decline", i.Key, null, new[] { "player" }, null, "You decline to serve " + i.Def.ShortName + " as " + OfficeTitle(i, rank) + ".");
                return CommandResult.Success("You decline. " + i.Leader + " will ask someone else.");
            }
            int before = i.Rank;
            i.Rank = rank;
            if (rank == Head)
            {
                // The old head steps aside; you lead until you leave.
                Record("institution.leader", i.Key, null, new[] { "player", i.Leader }, null, i.Leader + " steps aside: you lead " + i.Def.Name + " now.");
                i.Leader = "you";
            }
            Record("office.accept", i.Key, null, new[] { "player" }, new[] { new Effect(i.Key + ".rank", before, rank) },
                "You serve " + i.Def.Name + " as " + OfficeTitle(i, rank) + ".");
            return CommandResult.Success("You are " + OfficeTitle(i, rank) + " of " + i.Def.ShortName + ": your vote weighs more, your parting words will carry, and the duties take " +
                                         DutyAttention(rank, false) + " Attention a turn (resign " + i.Key + " to step down).");
        }

        public CommandResult Resign(string id)
        {
            var i = FindInstitution(id);
            if (i == null || i.Def.IsOwn || i.Rank < Officer) return CommandResult.Fail("You hold no office there.");
            int before = i.Rank;
            if (before == Head) i.Leader = i.Def.Successors.Count > 0 ? i.Def.Successors[LeadingCamp(i) ?? 0].Name : "a successor of " + i.Def.Leader;
            i.Rank = Member;
            double loyalty = i.Loyalty;
            i.Loyalty = Math.Max(0, i.Loyalty - T.Get("offices.resignLoyalty"));
            Record("office.resign", i.Key, null, new[] { "player" }, new[] { new Effect(i.Key + ".rank", before, Member), new Effect(LoyaltyKey(i), loyalty, i.Loyalty) },
                "You step down as " + OfficeTitle(i, before) + " of " + i.Def.ShortName + ".");
            return CommandResult.Success("You step down; " + i.Def.ShortName + " is disappointed (loyalty " + F(loyalty) + " → " + F(i.Loyalty) + ").");
        }

        /// <summary>Your standing instructions for when you leave: the camp to back and, from the head's seat, a successor.</summary>
        public CommandResult Orders(string id, string camp, int? successor = null)
        {
            var i = FindInstitution(id);
            if (i == null || !i.Exists) return CommandResult.Fail("No institution called '" + id + "'.");
            if (!(i.Def.IsOwn ? i.Stake > 0 : i.Backed)) return CommandResult.Fail("Only members can leave orders; " + i.Def.ShortName + " won't listen to a stranger.");
            int? c = FindCamp(i, camp);
            if (c == null) return CommandResult.Fail("Back which camp? " + string.Join(" or ", i.Def.DriftPaths.Select(p => p.Id + " (" + p.Name + ")")) + ".");
            bool canName = i.Def.IsOwn || i.Rank >= Head;
            if (successor != null)
            {
                if (!canName) return CommandResult.Fail("Only the head names the next head; as " + OfficeTitle(i, i.Rank) + " you can back a camp.");
                if (successor < 1 || successor > i.Def.Successors.Count) return CommandResult.Fail("Name successor 1 or 2: " + SuccessorList(i) + ".");
                i.OrderSuccessor = successor.Value - 1;
            }
            i.OrderCamp = c.Value;
            Record("institution.orders", i.Key, null, new[] { "player" }, null,
                "Your last orders for " + i.Def.ShortName + ": back " + CampName(i, c.Value) + (i.OrderSuccessor >= 0 ? ", and " + i.Def.Successors[i.OrderSuccessor].Name + " to lead it" : "") + ".");
            return CommandResult.Success("When you leave, you will back " + CampName(i, c.Value) + " at " + i.Def.ShortName +
                                         (i.OrderSuccessor >= 0 ? " and name " + i.Def.Successors[i.OrderSuccessor].Name + " to succeed you" : "") +
                                         ". Your words would carry " + OrdersBand(LastOrderForce(i)) + " weight (" + OrdersWhy(i) + ")." +
                                         (canName && i.OrderSuccessor < 0 ? " You can name a successor: " + SuccessorList(i) + " (orders " + i.Key + " " + i.Def.DriftPaths[c.Value].Id + " 1|2)." : ""));
        }

        public string SuccessorList(Institution i) =>
            string.Join("; ", i.Def.Successors.Select((s, k) => (k + 1) + " " + s.Name + ", " + s.Note));

        /// <summary>
        /// How much your last orders weigh: your office (a member only asks; the head decides) × the leader's loyalty to you ×
        /// your voting record for that camp (a sudden change of heart on your way out carries less).
        /// </summary>
        public double LastOrderForce(Institution i)
        {
            if (i.OrderCamp < 0) return 0;
            double office = i.Def.IsOwn ? 1 : T.GetArray("offices.orderWeight")[Math.Max(0, Math.Min(Head, i.Rank))];
            int total = i.Votes[0] + i.Votes[1];
            double record = total == 0 ? T.Get("offices.noRecordShare") : (double)i.Votes[i.OrderCamp] / total;
            double loyalty = (i.Leader == "you" ? 100 : i.Loyalty) / 100.0;
            return Math.Max(0, Math.Min(1, office * loyalty * (T.Get("offices.recordFloor") + (1 - T.Get("offices.recordFloor")) * record)));
        }

        private string OrdersWhy(Institution i) =>
            (i.Def.IsOwn ? "you founded it" : "as " + OfficeTitle(i, i.Rank)) + ", loyalty " + F(i.Leader == "you" ? 100 : i.Loyalty) +
            ", " + (i.Votes[0] + i.Votes[1] == 0 ? "no votes cast" : i.Votes[Math.Max(0, i.OrderCamp)] + " of your " + (i.Votes[0] + i.Votes[1]) + " votes for that camp");

        public static string OrdersBand(double force) => force >= 0.6 ? "great" : force >= 0.3 ? "real" : force >= 0.1 ? "little" : "almost no";

        /// <summary>At departure: last orders move the institution toward your camp and seat your successor.</summary>
        private void ApplyLastOrders(int departId)
        {
            foreach (var i in World.Institutions.Where(x => x.Exists && (x.Def.IsOwn ? x.Stake > 0 : x.Backed)).ToList())
            {
                // The head who leaves without naming anyone is followed by the leading camp's candidate.
                if ((i.Def.IsOwn || i.Rank >= Head) && i.Def.Successors.Count > 0)
                {
                    int k = i.OrderSuccessor >= 0 ? i.OrderSuccessor : Math.Min(i.Def.Successors.Count - 1, LeadingCamp(i) ?? 0);
                    var s = i.Def.Successors[k];
                    string before = i.Leader;
                    i.Leader = s.Name;
                    i.Integrity = s.Integrity;
                    if (i.OrderSuccessor >= 0) i.Loyalty = Math.Min(100, Math.Max(i.Loyalty, T.Get("offices.namedSuccessorLoyalty")));
                    Record("institution.leader", i.Key, new[] { departId }, new[] { "player", s.Name }, null,
                        s.Name + " succeeds " + (before == "you" || i.Def.IsOwn ? "you" : before) + " at the head of " + i.Def.Name + (i.OrderSuccessor >= 0 ? ", as you asked." : "."));
                }
                i.OrderForce = LastOrderForce(i);
                if (i.OrderCamp < 0) continue;
                double before2 = i.Lean, target = i.OrderCamp == 0 ? 1 : -1;
                i.Lean += i.OrderForce * (target - i.Lean);
                Record("institution.orders", i.Key, new[] { departId }, new[] { "player", i.Leader }, new[] { new Effect(i.Key + ".lean", before2, i.Lean) },
                    "Your parting words to " + i.Def.ShortName + " for " + CampName(i, i.OrderCamp) + " carry " + OrdersBand(i.OrderForce) + " weight.");
            }
        }
    }
}
