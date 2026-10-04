using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P1 institution access (decided 2026-10-02, Corey): an institution on an invitation path doesn't sell seats. You get in
    /// through a person: you know a member, he invites you as a guest, you're asked back, he sponsors you, the members admit
    /// you. Each forward step passes the five-part gate (a specific inviter, an existing relationship, relevant work in the
    /// domain, demonstrated usefulness, an inviter willing to take the risk); fame never counts. Membership brings obligations.
    /// During migration, a member holds the standing the P0 systems read at 10% (P1_PROPOSALS P1-03).
    /// </summary>
    public sealed partial class Simulation
    {
        public InvitationPathDef? InvitationPathDefFor(string institution) => Data.Content.InvitationPaths.FirstOrDefault(p => p.Institution == institution);

        /// <summary>True if this institution takes members only by invitation (its seats aren't for sale).</summary>
        public bool OnInvitationPath(Institution i) => InvitationPathDefFor(i.Key) != null;

        public InvitationPathState? InvitationState(string institution) => World.Invitations.FirstOrDefault(p => p.Institution == institution);

        private void InitInvitations()
        {
            foreach (var d in Data.Content.InvitationPaths) World.Invitations.Add(new InvitationPathState(d.Institution));
        }

        /// <summary>The five-part gate, from what has actually happened in this game.</summary>
        public InstitutionInvitationContext InvitationGate(InvitationPathDef d)
        {
            var access = World.AccessTo(d.Institution);
            var state = InvitationState(d.Institution)!;
            // Five separate pieces of evidence (Corey, 2026-10-04): who invites, that you really know them, work in their
            // domain, that the work proved useful, and that the inviter will risk their name now.
            bool relationship = access.KnownMemberId == d.Inviter && d.RelationshipEvidence.All(Holds);
            bool work = d.WorkEvidence.All(Holds);
            bool useful = d.UsefulnessEvidence.All(Holds);
            // Willing to take the risk: not still smarting from a refusal, and not laid up or away (P1 people).
            bool risk = Turn - state.DeclinedTurn >= T.GetInt("invitations.inviterPatienceMonths") && !IsPersonAway(d.Inviter);
            return new InstitutionInvitationContext(d.Institution, d.Inviter, relationship, work, useful, risk);
        }

        /// <summary>At the start of each month: the referral month is noted, and a sponsored candidate's vote is held.</summary>
        private void AdvanceInvitations()
        {
            foreach (var d in Data.Content.InvitationPaths)
            {
                var state = InvitationState(d.Institution)!;
                var access = World.AccessTo(d.Institution);
                if (state.Pending != InvitationOffer.None) continue;
                if (access.Stage == InstitutionAccessStage.KnowsMember && state.StepTurn == 0) state.StepTurn = Turn;   // the referral month
                if (access.Stage == InstitutionAccessStage.SponsoredCandidate && Turn - state.StepTurn >= d.AdmitAfterMonths) Admit(d, state, access);
            }
        }

        /// <summary>The next invitation each inviter is ready to make (its time come, the gate holding), waiting for the scene router.</summary>
        private IEnumerable<(InvitationPathDef Def, InvitationOffer Offer)> InvitationOffersDue()
        {
            foreach (var d in Data.Content.InvitationPaths)
            {
                var state = InvitationState(d.Institution)!;
                var access = World.AccessTo(d.Institution);
                if (state.Pending != InvitationOffer.None) continue;
                int months = Turn - state.StepTurn;
                InvitationOffer next = access.Stage == InstitutionAccessStage.KnowsMember && months >= d.GuestAfterMonths ? InvitationOffer.Guest
                    : access.Stage == InstitutionAccessStage.Guest && months >= d.AgainAfterMonths ? InvitationOffer.Again
                    : access.Stage == InstitutionAccessStage.InvitedBack && months >= d.SponsorAfterMonths ? InvitationOffer.Sponsor
                    : InvitationOffer.None;
                if (next == InvitationOffer.None || !InvitationGate(d).IsWarranted) continue;
                yield return (d, next);
            }
        }

        private SceneCategory OfferCategory(InvitationPathDef d, InvitationOffer offer) =>
            (offer == InvitationOffer.Guest ? d.Guest : offer == InvitationOffer.Again ? d.Again : d.Sponsor).Category;

        private void OfferInvitation(InvitationPathDef d, InvitationOffer next)
        {
            var state = InvitationState(d.Institution)!;
            state.Pending = next;
            var step = next == InvitationOffer.Guest ? d.Guest : next == InvitationOffer.Again ? d.Again : d.Sponsor;
            World.ScenePacing.Record(step.Category);
            Record("invitation.offer", d.Institution, null, new[] { d.Inviter, "player" }, null, step.Offer + " " + InvitationLine(d, next));
        }

        private string InvitationLine(InvitationPathDef d, InvitationOffer offer) =>
            offer == InvitationOffer.Sponsor ? "(invitation accept " + d.Institution + " / decline " + d.Institution + ")"
                : "(invitation accept " + d.Institution + ": " + d.MealAttention + " Attention and " + Money(Priced(d.MealCost)) + " for the wine / decline " + d.Institution + ")";

        public CommandResult AcceptInvitation(string institution)
        {
            var d = InvitationPathDefFor((institution ?? "").Trim().ToLowerInvariant());
            var state = d == null ? null : InvitationState(d.Institution);
            if (d == null || state == null || state.Pending == InvitationOffer.None) return CommandResult.Fail("No one has invited you there.");
            var access = World.AccessTo(d.Institution);
            var gate = InvitationGate(d);
            var offer = state.Pending;
            if (offer != InvitationOffer.Sponsor)
            {
                double cost = Priced(d.MealCost);
                if (World.Gold < cost) return CommandResult.Fail("You need " + Money(cost) + " for your share of the wine.");
                var attention = CheckAttention(d.MealAttention);
                if (attention != null) return attention;
                if (!access.TryAcceptGuestInvitation(gate)) return CommandResult.Fail("The invitation no longer stands.");
                SpendAttention(d.MealAttention);
                double before = World.Gold;
                World.Gold -= cost;
                var step = offer == InvitationOffer.Guest ? d.Guest : d.Again;
                World.ScenePacing.Record(step.Category);
                Record("invitation.guest", d.Institution, null, new[] { "player", d.Inviter }, new[] { new Effect(GoldKey, before, World.Gold), AccessEffect(d, offer) }, step.Scene);
            }
            else
            {
                if (!access.TryBecomeSponsoredCandidate(gate)) return CommandResult.Fail("The offer no longer stands.");
                World.ScenePacing.Record(d.Sponsor.Category);
                Record("invitation.sponsor", d.Institution, null, new[] { "player", d.Inviter }, new[] { AccessEffect(d, offer) }, d.Sponsor.Scene);
            }
            state.Pending = InvitationOffer.None;
            state.StepTurn = Turn;
            return CommandResult.Success(offer == InvitationOffer.Sponsor ? d.Sponsor.Scene : (offer == InvitationOffer.Guest ? d.Guest : d.Again).Scene);
        }

        private Effect AccessEffect(InvitationPathDef d, InvitationOffer offer)
        {
            int before = offer == InvitationOffer.Guest ? (int)InstitutionAccessStage.KnowsMember : offer == InvitationOffer.Again ? (int)InstitutionAccessStage.Guest : (int)InstitutionAccessStage.InvitedBack;
            return new Effect("access." + d.Institution, before, (int)World.AccessTo(d.Institution).Stage);
        }

        public CommandResult DeclineInvitation(string institution)
        {
            var d = InvitationPathDefFor((institution ?? "").Trim().ToLowerInvariant());
            var state = d == null ? null : InvitationState(d.Institution);
            if (d == null || state == null || state.Pending == InvitationOffer.None) return CommandResult.Fail("No one has invited you there.");
            state.Pending = InvitationOffer.None;
            state.DeclinedTurn = Turn;
            state.StepTurn = Turn;
            Record("invitation.decline", d.Institution, null, new[] { "player", d.Inviter }, null, d.DeclineText);
            return CommandResult.Success(d.DeclineText);
        }

        /// <summary>
        /// The members' chance of voting a sponsored candidate in (P1-13): a base, more for each point of the sponsor's regard,
        /// less if the institution holds a grievance against you; within [0, 1].
        /// </summary>
        public double AdmissionSupport(InvitationPathDef d)
        {
            var inst = World.Institution(d.Institution);
            double support = T.Get("invitations.vote.base") + T.Get("invitations.vote.perSponsorRegard") * (PersonOf(d.Inviter)?.Regard ?? 0)
                             - (inst.Regard < 0 ? T.Get("invitations.vote.grievance") : 0);
            return Math.Max(0, Math.Min(1, support));
        }

        /// <summary>
        /// The vote. Admission is not a timer (Corey, 2026-10-04): members can object. A failed vote is put off once; a second
        /// failure is a refusal, and the sponsor waits before trying again. Won: you pay the entry and take on the obligations.
        /// </summary>
        private void Admit(InvitationPathDef d, InvitationPathState state, InstitutionAccessState access)
        {
            var inst = World.Institution(d.Institution);
            // The vote is held once; if you won it but couldn't pay the entry, they wait for your money, not for another vote.
            if (!state.VotedIn && !Rng.Chance(AdmissionSupport(d)))
            {
                if (!state.Postponed)
                {
                    state.Postponed = true;
                    state.StepTurn = Turn;
                    World.ScenePacing.Record(d.Admit.Category);
                    Record("invitation.postponed", d.Institution, null, new[] { d.Inviter }, null, d.PostponedText);
                    return;
                }
                state.Postponed = false;
                state.StepTurn = Turn;
                state.DeclinedTurn = Turn;
                access.Refuse();
                World.ScenePacing.Record(d.Admit.Category);
                Record("invitation.refused", d.Institution, null, new[] { d.Inviter }, new[] { new Effect("access." + d.Institution, (int)InstitutionAccessStage.SponsoredCandidate, (int)access.Stage) },
                    d.RefusedText);
                return;
            }
            double fee = EntryFee(inst);
            if (World.Gold < fee)
            {
                if (state.VotedIn) return;   // say it once
                state.VotedIn = true;
                Record("invitation.wait", d.Institution, null, new[] { d.Inviter }, null,
                    Cap(inst.Def.ShortName) + " has voted you in, but the entry is " + Money(fee) + " and you can't pay it yet. They'll wait.");
                return;
            }
            state.VotedIn = false;
            state.Postponed = false;
            if (!access.AdmitMember(d.Inviter)) return;
            double gold = World.Gold, stake = inst.Stake, loyalty = inst.Loyalty;
            World.Gold -= fee;
            // Standing for the P0 systems still running (P1-03): a member counts as holding 10%.
            inst.Stake = Math.Max(inst.Stake, InfluenceAt);
            inst.Loyalty = Math.Max(0, Math.Min(100, T.Get("stakes.memberLoyalty") + inst.Regard));
            inst.Rank = Member;
            inst.Regard = 0;
            inst.JoinedAt = Now.YearFraction;
            World.ScenePacing.Record(d.Admit.Category);
            Record("institution.join", d.Institution, null, new[] { "player", d.Inviter, inst.Leader },
                new[] { new Effect(GoldKey, gold, World.Gold), new Effect(StakeKey(inst), stake, inst.Stake), new Effect(LoyaltyKey(inst), loyalty, inst.Loyalty),
                        new Effect("access." + d.Institution, (int)InstitutionAccessStage.SponsoredCandidate, (int)InstitutionAccessStage.Member) },
                d.Admit.Scene + " (Entry " + Money(fee) + "; dues " + Money(AnnualDues(inst)) + " a year.)");
            state.StepTurn = Turn;
        }
    }
}
