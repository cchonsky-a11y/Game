using System;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P0 Attention budget (PROTOTYPE_SCOPE, P0 pacing). Demand counts every project once, every institution
    /// step for one institution per domain (the buys to control or founding, charter, audit, endow, invest), a full
    /// mentoring commitment per institution, the oversight needed to hold loyalty over the era, one plague response,
    /// one stance per policy issue, active membership (meetings) in one institution per domain, and one personal action per turn.
    /// </summary>
    public static class AttentionBudget
    {
        public static double Supply(GameData data) =>
            EraTurns(data) * data.Tuning.Get("attention.perTurn");

        /// <summary>Turns in an era at the default turn length.</summary>
        public static double EraTurns(GameData data) => data.Tuning.Get("time.eraYears") * 12 / data.Tuning.Get("time.monthsPerTurn");

        public static double Demand(GameData data)
        {
            var t = data.Tuning;
            double turns = EraTurns(data);
            double years = t.Get("time.eraYears");
            double demand = data.Content.Projects.Sum(p => p.AttentionPerTurn * p.Turns);
            // One institution per domain taken to control: founding it (or the buys to 10%, 25% and 50%), then
            // charter, audit, endowment and one investment.
            int domains = data.Content.Institutions.Select(i => i.Maintains).Distinct().Count();
            demand += domains * (Math.Max(t.Get("founding.attention"), 3 * t.Get("stakes.buyAttention")) + t.Get("institutions.charterAttention")
                + t.Get("institutions.auditAttention") + t.Get("institutions.endowAttention") + t.Get("stakes.investAttention")
                + t.Get("commitments.mentor.turns") * t.Get("commitments.mentor.attentionPerTurn")
                + Math.Ceiling(years * t.Get("institutions.loyaltyFadePerYear") / t.Get("institutions.overseeLoyalty")));
            demand += t.Get("plague.response.hospice.attention");
            demand += 4 * t.Get("policy.attention"); // one stance per economic issue
            // Attending meetings often enough to be an active member of one established institution per domain.
            demand += domains * years * t.Get("stakes.activeMeetingsPerYear") * t.Get("stakes.attendAttention");
            // The 9 time machine repair steps (all needed to jump).
            demand += data.Content.MachineSteps.Concat(data.Content.MachineUpgrades).Sum(m => m.AttentionPerTurn * m.Turns);
            if (data.Content.MachineAssessment != null) demand += data.Content.MachineAssessment.AttentionPerTurn * data.Content.MachineAssessment.Turns;
            demand += data.Content.Inventions.Sum(i => i.AttentionPerTurn * i.Turns);
            demand += turns;
            return demand;
        }
    }
}
