using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Pacing (decided 2026-10-02, Corey, P1): 1-month turns, 240 in the era. A month ends only when the player ends it
    /// (End Month), never because Attention ran out; fast-forward ends months until one needs the player.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Event types that stop auto-advance so the player can react.</summary>
        private static readonly HashSet<string> NotableEvents = new HashSet<string>
        {
            "plague.warning", "plague.outbreak", "plague.passed", "debt.tier", "project.complete", "seeded.payoff",
            "seeded.choice", "commitment.complete", "institution.unpaid", "promise.offer", "promise.kept", "plague.opening",
            "bust.warning", "bust.outbreak", "bust.toll", "machine.step", "machine.assessed", "invention.complete", "office.offer", "event.offer", "workshop.orders",
            "commission.encounter", "commission.stage", "commission.complete", "invitation.offer", "institution.join", "person.life"
        };

        private int _turnEventStart = 1;

        public int EraYears => T.GetInt("time.eraYears");
        /// <summary>The era ends by the calendar (decided 2026-09-28: 20 years), whatever the turn length.</summary>
        public SimTime EraEnd => SimTime.FromYear(T.GetInt("time.startYear") + EraYears, T.GetInt("time.startMonth"));

        public bool EraOver => Now.TotalMonths >= EraEnd.TotalMonths;

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
            if (PendingEvent != null) reasons.Add(PendingEvent.Title.ToLowerInvariant());
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.TermsOffered)) reasons.Add(CommissionDefOf(c).Client + "'s offer");
            foreach (var p in World.Invitations.Where(p => p.Pending != InvitationOffer.None)) reasons.Add(InvitationPathDefFor(p.Institution)!.Inviter + "'s invitation");
            if (Log.Events.Skip(_turnEventStart - 1).Any(e => NotableEvents.Contains(e.Type))) reasons.Add("news this month");
            if (AffordableInvestment()) reasons.Add("money to invest");
            if (EraOver) reasons.Add("the era's " + EraYears + " years are over");
            return reasons;
        }

        /// <summary>
        /// A project or institution step that gold and Attention allow now: founding your own, buying up to the next
        /// stake threshold (10%, 25%, 50%), or a charter, audit or minimum endowment for one you control.
        /// </summary>
        public bool AffordableInvestment()
        {
            if (AvailableProjects().Any(p => ProjectAuthorityBlocker(p) == null && ProjectGold(p) <= World.Gold && p.AttentionPerTurn <= World.Attention)) return true;
            if (!MachineAssessed && !World.ActiveMachineSteps.Any(a => a.Def == MachineAssessment) && MachineAssessment!.AttentionPerTurn <= World.Attention) return true;
            foreach (var system in MachineSystems)
            {
                var step = NextMachineStep(system);
                if (step != null && MachineStepGold(step) <= World.Gold && step.AttentionPerTurn <= World.Attention) return true;
            }
            foreach (var i in World.Institutions)
            {
                if (i.Def.IsOwn && !i.Exists && !i.Collapsed) { if (Can(FoundCost(i.Def.Maintains), "founding.attention")) return true; continue; }
                if (!i.Exists) continue;
                int next = NextThresholdPercent(i);
                if (next > 0 && Can(BuyCost(i, next - StakePercent(i)), "stakes.buyAttention")) return true;
                if (!Controls(i)) continue;
                if (!i.Chartered && Can(T.Get("institutions.charterGold"), "institutions.charterAttention")) return true;
                if (!i.AuditCharter && Can(T.Get("institutions.auditGold"), "institutions.auditAttention")) return true;
                if (!i.Endowed && Can(T.Get("institutions.endowGold") - i.Holdings, "institutions.endowAttention")) return true;
            }
            return false;
        }

        /// <summary>The next stake threshold (10, 25 or 50 percent) above your stake, or 0 once you control it.</summary>
        public int NextThresholdPercent(Institution i)
        {
            foreach (var t in new[] { InfluenceAt, VoiceAt, ControlAt })
            {
                int p = (int)Math.Round(t * 100);
                if (StakePercent(i) < p) return p;
            }
            return 0;
        }

        private bool Can(double gold, string attentionKey) => World.Gold >= gold && World.Attention >= T.GetInt(attentionKey);

        /// <summary>
        /// No action is possible this turn: no Attention is free and no prompt is open, Rome's choices included (tester 6: a
        /// treasury loan opened and lapsed while fully committed turns passed on their own). (Changing a priority or paying
        /// down debt is a standing setting the player can adjust in any turn, so it doesn't count.)
        /// </summary>
        public bool NoActionPossible() =>
            !Arrived && World.Attention == 0 && !SeededChoiceOpen && World.Promise.Status != PromiseStatus.Offered && !OutbreakAwaitingResponse && PendingEvent == null && !EraOver;

        /// <summary>
        /// "End Month" (decided 2026-10-02, Corey, P1): exactly one month, always. No month passes on its own, even when
        /// no Attention is free; the player ends each one, or fast-forwards.
        /// </summary>
        public void EndMonth() => EndTurn();

        /// <summary>
        /// Fast-forward (opt-in): ends months until one needs the player: an open prompt, news, an affordable investment or
        /// the era's end (or <paramref name="maxTurns"/> pass). Returns months advanced.
        /// </summary>
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
