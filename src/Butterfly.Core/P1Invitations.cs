using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>One step of an invitation path: the message that offers it, and the scene when you go.</summary>
    public sealed class InvitationStepDef
    {
        public string Offer { get; }
        public SceneCategory Category { get; }
        public string Scene { get; }

        public InvitationStepDef(JsonObject o)
        {
            Offer = o.StrOr("offer", "") ?? "";
            Category = Enum.TryParse<SceneCategory>(o.Str("category"), out var c) ? c : throw new FormatException("Unknown scene category: " + o.Str("category"));
            Scene = o.Str("scene");
        }
    }

    /// <summary>
    /// How one institution brings a person in (decided 2026-10-02, data/content/invitations.json): a specific inviter,
    /// the work that warrants it, and the months between guest supper, invitation back, sponsorship and admission.
    /// </summary>
    public sealed class InvitationPathDef
    {
        public string Institution { get; }
        public string Inviter { get; }
        /// <summary>The five-part gate's evidence, each from its own facts in the game (Corey, 2026-10-04: kept distinct).</summary>
        public IReadOnlyList<string> RelationshipEvidence { get; }
        public IReadOnlyList<string> WorkEvidence { get; }
        public IReadOnlyList<string> UsefulnessEvidence { get; }
        public int GuestAfterMonths { get; }
        public int AgainAfterMonths { get; }
        public int SponsorAfterMonths { get; }
        public int AdmitAfterMonths { get; }
        public int MealAttention { get; }
        public double MealCost { get; }
        public InvitationStepDef Guest { get; }
        public InvitationStepDef Again { get; }
        public InvitationStepDef Sponsor { get; }
        /// <summary>The sponsor's words the second time, after a refused vote (optional; else the first words again).</summary>
        public InvitationStepDef? SponsorAgain { get; }
        public InvitationStepDef Admit { get; }
        public string DeclineText { get; }
        /// <summary>The members put the vote off; the members refuse.</summary>
        public string PostponedText { get; }
        public string RefusedText { get; }
        /// <summary>What the inventor finds on arrival if they were a guest but never joined (present conditions only).</summary>
        public string EchoGuest { get; }

        public InvitationPathDef(JsonObject o)
        {
            Institution = o.Str("institution");
            Inviter = o.Str("inviter");
            var ev = o.Obj("evidence");
            RelationshipEvidence = ev.Arr("relationship").Cast<string>().ToList();
            WorkEvidence = ev.Arr("work").Cast<string>().ToList();
            UsefulnessEvidence = ev.Arr("usefulness").Cast<string>().ToList();
            GuestAfterMonths = (int)o.Num("guestAfterMonths");
            AgainAfterMonths = (int)o.Num("againAfterMonths");
            SponsorAfterMonths = (int)o.Num("sponsorAfterMonths");
            AdmitAfterMonths = (int)o.Num("admitAfterMonths");
            MealAttention = (int)o.Num("mealAttention");
            MealCost = o.Num("mealCost");
            Guest = new InvitationStepDef(o.Obj("guest"));
            Again = new InvitationStepDef(o.Obj("again"));
            Sponsor = new InvitationStepDef(o.Obj("sponsor"));
            SponsorAgain = o.Has("sponsorAgain") ? new InvitationStepDef(o.Obj("sponsorAgain")) : null;
            Admit = new InvitationStepDef(o.Obj("admit"));
            DeclineText = o.Str("decline");
            EchoGuest = o.StrOr("echoGuest", "") ?? "";
            PostponedText = o.Str("postponed");
            RefusedText = o.Str("refused");
        }
    }

    /// <summary>An invitation waiting for your answer.</summary>
    public enum InvitationOffer { None, Guest, Again, Sponsor }

    /// <summary>Where one invitation path stands in this game.</summary>
    public sealed class InvitationPathState
    {
        public string Institution { get; }
        public InvitationOffer Pending { get; set; }
        /// <summary>The turn the access stage last moved (months are counted from it).</summary>
        public int StepTurn { get; set; }
        /// <summary>The turn you last declined: the inviter won't take the risk again for a while.</summary>
        public int DeclinedTurn { get; set; } = -1000;
        /// <summary>The members put the vote off once already (a second failure is a refusal).</summary>
        public bool Postponed { get; set; }
        /// <summary>The members voted you in; only the entry fee is still owed.</summary>
        public bool VotedIn { get; set; }

        public InvitationPathState(string institution) => Institution = institution;
    }
}
