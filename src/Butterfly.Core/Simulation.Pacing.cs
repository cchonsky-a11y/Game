using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P0-only pacing (decided 2026-09-26, recorded in PROTOTYPE_SCOPE.md): 3-month turns, 40 per era,
    /// and turns with no pending decision advance on their own.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Event types that stop auto-advance so the player can react.</summary>
        private static readonly HashSet<string> NotableEvents = new HashSet<string>
        {
            "plague.warning", "plague.outbreak", "plague.passed", "debt.tier", "project.complete", "seeded.payoff",
            "seeded.choice", "commitment.complete", "institution.unpaid", "promise.offer", "promise.kept", "plague.opening",
            "bust.warning", "bust.outbreak", "bust.toll", "machine.step", "machine.assessed", "invention.complete"
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
            if (Log.Events.Skip(_turnEventStart - 1).Any(e => NotableEvents.Contains(e.Type))) reasons.Add("news this turn");
            if (AffordableInvestment()) reasons.Add("gold to invest");
            if (EraOver) reasons.Add("the era's " + EraYears + " years are over");
            return reasons;
        }

        /// <summary>
        /// A project or institution step that gold and Attention allow now: founding your own, buying up to the next
        /// stake threshold (10%, 25%, 50%), or a charter, audit or minimum endowment for one you control.
        /// </summary>
        public bool AffordableInvestment()
        {
            if (AvailableProjects().Any(p => ProjectGold(p) <= World.Gold && p.AttentionPerTurn <= World.Attention)) return true;
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
        /// No action is possible this turn: no Attention is free and no prompt is open. (Changing a priority
        /// or paying down debt is a standing setting the player can adjust in any turn, so it doesn't count.)
        /// </summary>
        public bool NoActionPossible() =>
            !Arrived && World.Attention == 0 && !SeededChoiceOpen && World.Promise.Status != PromiseStatus.Offered && !OutbreakAwaitingResponse && !EraOver;

        /// <summary>
        /// Something the player could still do this turn without Attention that is worth pausing for (decided
        /// 2026-09-28): paying down debt they can afford. Priorities are a standing setting and don't count.
        /// </summary>
        public bool FreeActionWorthPausingFor() =>
            World.Domains.Any(d => d.Debt > 0) && World.Gold >= PaydownCost(1);

        /// <summary>
        /// Auto-end (decided 2026-09-28): once a choice uses up the turn's Attention, the turn ends by itself unless an
        /// open prompt or a free action (paying down debt) is still available.
        /// </summary>
        public bool ShouldAutoEnd() => NoActionPossible() && !FreeActionWorthPausingFor();

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
