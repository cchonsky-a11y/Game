using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>One scene of a commission: what happens, and which P1 scene category it counts as for pacing.</summary>
    public sealed class CommissionSceneDef
    {
        public SceneCategory Category { get; }
        public string Text { get; }
        public int Attention { get; }

        public CommissionSceneDef(JsonObject o)
        {
            Category = ParseCategory(o.Str("category"));
            Text = o.Str("text");
            Attention = (int)o.NumOr("attention", 0);
        }

        internal static SceneCategory ParseCategory(string s) =>
            Enum.TryParse<SceneCategory>(s, out var c) ? c : throw new FormatException("Unknown scene category: " + s);
    }

    /// <summary>A stage of the work once terms are agreed: a project stage, a length in months and the Attention it holds.</summary>
    public sealed class CommissionWorkDef
    {
        public ProjectStage Stage { get; }
        public SceneCategory Category { get; }
        public int DurationMonths { get; }
        public int Attention { get; }
        public string Text { get; }

        public CommissionWorkDef(JsonObject o)
        {
            Stage = Enum.TryParse<ProjectStage>(o.Str("stage"), out var s) ? s : throw new FormatException("Unknown project stage: " + o.Str("stage"));
            Category = CommissionSceneDef.ParseCategory(o.Str("category"));
            DurationMonths = (int)o.Num("durationMonths");
            Attention = (int)o.Num("attention");
            Text = o.Str("text");
            Capability = o.StrOr("capability", "") ?? "";
            CapabilityTo = o.Has("capabilityTo")
                ? (Enum.TryParse<CapabilityLevel>(o.Str("capabilityTo"), out var lv) ? lv : throw new FormatException("Unknown capability level: " + o.Str("capabilityTo")))
                : (CapabilityLevel?)null;
        }

        /// <summary>A stage may move its own capability, or the commission's to a level of its own (else from the stage kind).</summary>
        public string Capability { get; }
        public CapabilityLevel? CapabilityTo { get; }
    }

    /// <summary>
    /// A P1 commission (decided 2026-10-02): encounter → help/diagnosis → agreed terms → staged work → paid completion →
    /// referral, authored in data/content/commissions.json. The terms say who pays and who buys materials before work starts.
    /// </summary>
    public sealed class CommissionDef
    {
        public string Id { get; }
        public string Title { get; }
        public string Client { get; }
        public string ClientRole { get; }
        public string Introducer { get; }
        public int OpensAfterMonths { get; }
        /// <summary>What must hold before the work can come (people.json syntax): repeat work follows the first.</summary>
        public IReadOnlyList<string> Requires { get; }
        public CommissionSceneDef Encounter { get; }
        public CommissionSceneDef Diagnosis { get; }
        public CommissionSceneDef TermsScene { get; }
        public ProjectFundingModel FundingModel { get; }
        public string Payer { get; }
        public string MaterialsPayer { get; }
        public double Upfront { get; }
        public double Completion { get; }
        /// <summary>A profit share's later payments: aurei each (AD 155 prices), how many, how far apart, and one text per payment.</summary>
        public double ShareAmount { get; }
        public int SharePayments { get; }
        public int ShareEveryMonths { get; }
        public IReadOnlyList<string> ShareTexts { get; }
        public double CounterCompletion { get; }
        public double AcceptWeight { get; }
        public double StandFirmWeight { get; }
        public double WalkWeight { get; }
        public string AcceptText { get; }
        public string StandFirmText { get; }
        public string WalkText { get; }
        public string DeclineText { get; }
        public IReadOnlyList<CommissionWorkDef> Work { get; }
        public string ReferralInstitution { get; }
        public string ReferralMember { get; }
        public string ReferralText { get; }
        /// <summary>What finishing the work leaves besides money: people's regard, their standing, flags later content reads.</summary>
        public IReadOnlyList<KeyValuePair<string, int>> OnCompleteRegard { get; }
        public IReadOnlyList<KeyValuePair<string, string>> OnCompleteStatus { get; }
        public IReadOnlyList<string> OnCompleteSets { get; }
        /// <summary>The capability (data/content/capabilities.json) this work moves Rome along, or empty.</summary>
        public string Capability { get; }

        public CommissionDef(JsonObject o)
        {
            Id = o.Str("id");
            Title = o.Str("title");
            Client = o.Str("client");
            ClientRole = o.Str("clientRole");
            Introducer = o.StrOr("introducer", "") ?? "";
            OpensAfterMonths = (int)o.Num("opensAfterMonths");
            Requires = o.Has("requires") ? o.Arr("requires").Cast<string>().ToList() : new List<string>();
            Encounter = new CommissionSceneDef(o.Obj("encounter"));
            Diagnosis = new CommissionSceneDef(o.Obj("diagnosis"));
            var t = o.Obj("terms");
            TermsScene = new CommissionSceneDef(t);
            FundingModel = Enum.TryParse<ProjectFundingModel>(t.Str("fundingModel"), out var f) ? f : throw new FormatException("Unknown funding model: " + t.Str("fundingModel"));
            Payer = t.Str("payer");
            MaterialsPayer = t.Str("materialsPayer");
            Upfront = t.NumOr("upfront", 0);
            Completion = t.NumOr("completion", 0);
            var sh = t.Has("share") ? t.Obj("share") : null;
            ShareAmount = sh?.Num("amount") ?? 0;
            ShareEveryMonths = sh == null ? 0 : (int)sh.Num("everyMonths");
            ShareTexts = sh == null ? new List<string>() : sh.Arr("texts").Cast<string>().ToList();
            SharePayments = ShareTexts.Count;
            if (FundingModel == ProjectFundingModel.ProfitShare && SharePayments == 0)
                throw new FormatException("Commission " + Id + " is a profit share but names no share payments.");
            var c = o.Obj("counter");
            CounterCompletion = c.Num("completion");
            AcceptWeight = c.Num("acceptWeight");
            StandFirmWeight = c.Num("standFirmWeight");
            WalkWeight = c.Num("walkWeight");
            AcceptText = c.Str("acceptText");
            StandFirmText = c.Str("standFirmText");
            WalkText = c.Str("walkText");
            DeclineText = o.Str("declineText");
            Work = o.Arr("work").Cast<JsonObject>().Select(x => new CommissionWorkDef(x)).ToList();
            // Not every job leads into an institution (Corey, 2026-10-04): the referral is optional, and may be only a lead.
            var r = o.Has("referral") ? o.Obj("referral") : null;
            ReferralInstitution = r?.StrOr("institution", "") ?? "";
            ReferralMember = r?.StrOr("member", "") ?? "";
            ReferralText = r?.StrOr("text", "") ?? "";
            var oc = o.Has("onComplete") ? o.Obj("onComplete") : null;
            var rg = oc != null && oc.Has("regard") ? oc.Obj("regard") : null;
            OnCompleteRegard = rg == null ? new List<KeyValuePair<string, int>>() : rg.Keys.Select(k => new KeyValuePair<string, int>(k, (int)rg.Num(k))).ToList();
            var st = oc != null && oc.Has("status") ? oc.Obj("status") : null;
            OnCompleteStatus = st == null ? new List<KeyValuePair<string, string>>() : st.Keys.Select(k => new KeyValuePair<string, string>(k, st.Str(k))).ToList();
            OnCompleteSets = oc != null && oc.Has("sets") ? oc.Arr("sets").Cast<string>().ToList() : new List<string>();
            Capability = o.StrOr("capability", "") ?? "";
        }
    }

    /// <summary>Where a commission stands for the player.</summary>
    public enum CommissionStatus
    {
        NotYet,
        Offered,      // the encounter has happened; you can look at the problem (unpaid)
        TermsOffered, // you looked; the client has named terms
        Working,      // agreed; the work is under way
        Done,
        Declined,
        Walked,       // the client walked away after you pushed
        Abandoned     // you left Rome with it unfinished
    }

    /// <summary>A commission's progress in this game.</summary>
    public sealed class CommissionState
    {
        public string Id { get; }
        public CommissionStatus Status { get; set; } = CommissionStatus.NotYet;
        public bool Countered { get; set; }
        public double CompletionPay { get; set; }
        public int WorkIndex { get; set; }
        public int MonthsLeftInStage { get; set; }
        /// <summary>From this turn on, the current work stage holds its Attention at the start of each month.</summary>
        public int ReservedFromTurn { get; set; }
        /// <summary>A finished profit share: payments still to come, and the turn the next is due.</summary>
        public int SharesLeft { get; set; }
        public int NextShareTurn { get; set; }
        public int CompletedEventId { get; set; }

        public CommissionState(string id) => Id = id;
        public string ProjectId => "commission:" + Id;
    }
}
