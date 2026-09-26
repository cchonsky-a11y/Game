using System;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P0 Attention budget (PROTOTYPE_SCOPE, P0 pacing). Demand counts every project once, every institution
    /// step (found, charter, audit, endow), a full mentoring commitment per institution, the oversight needed
    /// to hold loyalty over the era, one plague response, and one personal action per turn.
    /// </summary>
    public static class AttentionBudget
    {
        public static double Supply(GameData data) =>
            data.Tuning.Get("time.eraTurns") * data.Tuning.Get("attention.perTurn");

        public static double Demand(GameData data)
        {
            var t = data.Tuning;
            double turns = t.Get("time.eraTurns");
            double years = turns * t.Get("time.monthsPerTurn") / 12.0;
            double demand = data.Content.Projects.Sum(p => p.AttentionPerTurn * p.Turns);
            demand += data.Content.Institutions.Count * (t.Get("institutions.foundAttention") + t.Get("institutions.charterAttention")
                + t.Get("institutions.auditAttention") + t.Get("institutions.endowAttention")
                + t.Get("commitments.mentor.turns") * t.Get("commitments.mentor.attentionPerTurn")
                + Math.Ceiling(years * t.Get("institutions.loyaltyFadePerYear") / t.Get("institutions.overseeLoyalty")));
            demand += t.Get("plague.response.hospice.attention");
            demand += turns;
            return demand;
        }
    }
}
