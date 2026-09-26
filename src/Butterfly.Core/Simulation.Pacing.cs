using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P0-only pacing (decided 2026-09-26, recorded in PROTOTYPE_SCOPE.md): 6-month turns, 20 per era,
    /// and turns with no pending decision advance on their own.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Event types that stop auto-advance so the player can react.</summary>
        private static readonly HashSet<string> NotableEvents = new HashSet<string>
        {
            "plague.warning", "plague.outbreak", "plague.passed", "debt.tier", "project.complete", "seeded.payoff",
            "seeded.choice", "commitment.complete", "institution.unpaid", "promise.offer", "promise.kept", "plague.opening"
        };

        private int _turnEventStart = 1;

        public int EraTurns => T.GetInt("time.eraTurns");

        public bool EraOver => Turn > EraTurns;

        /// <summary>
        /// Why the player is needed this turn: an open prompt, something notable just happened, a new
        /// investment is affordable, or the era's turns are used up. Empty means the turn can pass on its own.
        /// </summary>
        public List<string> PendingDecisions()
        {
            var reasons = new List<string>();
            if (Arrived) return reasons;
            if (SeededChoiceOpen) reasons.Add("the first choice: fountain or workshop");
            if (World.Promise.Status == PromiseStatus.Offered) reasons.Add("Demetria's request");
            if (OutbreakAwaitingResponse) reasons.Add("the outbreak");
            if (Log.Events.Skip(_turnEventStart - 1).Any(e => NotableEvents.Contains(e.Type))) reasons.Add("news this turn");
            if (AffordableInvestment()) reasons.Add("gold to invest");
            if (EraOver) reasons.Add("the era's " + EraTurns + " turns are over");
            return reasons;
        }

        /// <summary>A project or institution step (found, charter, audit, minimum endowment) that gold and Attention allow now.</summary>
        public bool AffordableInvestment()
        {
            if (AvailableProjects().Any(p => p.Gold <= World.Gold && p.AttentionPerTurn <= World.Attention)) return true;
            foreach (var i in World.Institutions)
            {
                if (!i.Founded) { if (Can(T.Get("institutions.foundGold"), "institutions.foundAttention")) return true; continue; }
                if (!i.Chartered && Can(T.Get("institutions.charterGold"), "institutions.charterAttention")) return true;
                if (!i.AuditCharter && Can(T.Get("institutions.auditGold"), "institutions.auditAttention")) return true;
                if (!i.Endowed && Can(T.Get("institutions.endowGold") - i.Holdings, "institutions.endowAttention")) return true;
            }
            return false;
        }

        private bool Can(double gold, string attentionKey) => World.Gold >= gold && World.Attention >= T.GetInt(attentionKey);

        /// <summary>
        /// No action is possible this turn: no Attention is free and no prompt is open. (Changing a priority
        /// or paying down debt is a standing setting the player can adjust in any turn, so it doesn't count.)
        /// </summary>
        public bool NoActionPossible() =>
            !Arrived && World.Attention == 0 && !SeededChoiceOpen && World.Promise.Status != PromiseStatus.Offered && !OutbreakAwaitingResponse && !EraOver;

        /// <summary>"End turn": exactly one turn, then only turns where no action is possible pass on their own. Returns turns advanced.</summary>
        public int EndTurnAndSkipIdle(int maxTurns = 100)
        {
            int n = 0;
            do
            {
                EndTurn();
                n++;
            } while (n < maxTurns && NoActionPossible());
            return n;
        }

        /// <summary>"Wait": ends turns until one needs the player (or <paramref name="maxTurns"/> pass). Returns turns advanced.</summary>
        public int AdvanceUntilDecision(int maxTurns = 100)
        {
            int n = 0;
            do
            {
                EndTurn();
                n++;
            } while (n < maxTurns && PendingDecisions().Count == 0);
            return n;
        }

        private void MarkTurnEventStart(int firstEventId) => _turnEventStart = firstEventId;
    }
}
