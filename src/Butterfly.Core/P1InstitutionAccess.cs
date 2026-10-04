using System;

namespace Butterfly.Core
{
    /// <summary>
    /// Relationship-first P1 access state for one institution. This lives beside the legacy P0 stake model until
    /// migration is complete; new content should use this path rather than direct stake purchases.
    /// </summary>
    public sealed class InstitutionAccessState
    {
        public string InstitutionId { get; }
        public InstitutionAccessStage Stage { get; private set; }
        public string KnownMemberId { get; private set; } = string.Empty;
        public string SponsorId { get; private set; } = string.Empty;
        public int GuestVisits { get; private set; }

        public InstitutionAccessState(string institutionId)
        {
            if (string.IsNullOrWhiteSpace(institutionId)) throw new ArgumentException("Institution id is required.", nameof(institutionId));
            InstitutionId = institutionId;
            Stage = InstitutionAccessStage.Unaware;
        }

        public void BecomeAware()
        {
            if (Stage < InstitutionAccessStage.Aware) Stage = InstitutionAccessStage.Aware;
        }

        public void RecordMemberRelationship(string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId)) throw new ArgumentException("Member id is required.", nameof(memberId));
            BecomeAware();
            KnownMemberId = memberId;
            if (Stage < InstitutionAccessStage.KnowsMember) Stage = InstitutionAccessStage.KnowsMember;
        }

        public bool TryAcceptGuestInvitation(InstitutionInvitationContext invitation)
        {
            if (!Matches(invitation) || !invitation.IsWarranted) return false;
            if (Stage < InstitutionAccessStage.KnowsMember || Stage >= InstitutionAccessStage.Member) return false;

            GuestVisits++;
            Stage = GuestVisits == 1 ? InstitutionAccessStage.Guest : InstitutionAccessStage.InvitedBack;
            return true;
        }

        public bool TryBecomeSponsoredCandidate(InstitutionInvitationContext invitation)
        {
            if (!Matches(invitation) || !invitation.IsWarranted) return false;
            if (Stage < InstitutionAccessStage.InvitedBack || Stage >= InstitutionAccessStage.Member) return false;

            SponsorId = invitation.InviterId;
            Stage = InstitutionAccessStage.SponsoredCandidate;
            return true;
        }

        public bool AdmitMember(string sponsorId)
        {
            if (Stage != InstitutionAccessStage.SponsoredCandidate) return false;
            if (string.IsNullOrWhiteSpace(sponsorId) || sponsorId != SponsorId) return false;
            Stage = InstitutionAccessStage.Member;
            return true;
        }

        /// <summary>The members refuse a sponsored candidate: back to having been asked back; the sponsor's word is spent for now.</summary>
        public bool Refuse()
        {
            if (Stage != InstitutionAccessStage.SponsoredCandidate) return false;
            SponsorId = null;
            Stage = InstitutionAccessStage.InvitedBack;
            return true;
        }

        private bool Matches(InstitutionInvitationContext invitation)
        {
            return invitation != null && invitation.InstitutionId == InstitutionId;
        }
    }
}
