using System.Collections.Generic;
using System.Linq;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>
    /// A scripted player. Each turn it issues commands through the same API as the console. Every strategy has
    /// the inventor's foreknowledge that a plague arrives around AD 165 and responds from turn one in its own style
    /// (decided 2026-09-27); none of them wait for the warnings, since early leavers depart before they appear.
    /// </summary>
    public abstract class Strategy
    {
        public abstract string Name { get; }
        public abstract void PlayTurn(Simulation sim);

        /// <summary>Last actions before leaving (e.g. paying down debt).</summary>
        public virtual void BeforeJump(Simulation sim) { }

        protected static void AnswerPending(Simulation sim, bool acceptPromise, params string[] responsePreference)
        {
            if (sim.World.Promise.Status == PromiseStatus.Offered) sim.AnswerPromise(acceptPromise);
            if (sim.OutbreakAwaitingResponse)
            {
                foreach (var r in responsePreference)
                    if (sim.RespondToPlague(r).Ok) return;
                sim.RespondToPlague("none");
            }
        }

        protected static void TryInstitution(Simulation sim, string id, double reserve)
        {
            var i = sim.World.Institution(id);
            if (!i.Founded) { if (sim.World.Gold - sim.T.Get("institutions.foundGold") >= reserve) sim.Found(id); return; }
            if (!i.Chartered && sim.World.Gold - sim.T.Get("institutions.charterGold") >= reserve) sim.Charter(id);
            if (!i.Endowed && sim.World.Gold - sim.T.Get("institutions.endowGold") >= reserve) sim.Endow(id);
        }

        protected static void OverseeIfNeeded(Simulation sim, string id, double below)
        {
            var i = sim.World.Institution(id);
            if (i.Founded && i.Loyalty < below) sim.Oversee(id);
        }

        /// <summary>
        /// Jump preparation: split the remaining gold evenly as endowments. An audit charter is bought first
        /// for any institution whose share would be large, or whose leader is venal.
        /// </summary>
        protected static void AuditAndEndow(Simulation sim, params string[] ids)
        {
            var founded = ids.Select(id => sim.World.Institution(id)).Where(i => i.Founded).ToList();
            if (founded.Count == 0) return;
            foreach (var i in founded)
            {
                double planned = i.Holdings + sim.World.Gold / founded.Count;
                if (planned >= sim.T.Get("institutions.holdings.largeHoldings") || i.Def.LeaderIntegrity == "venal") sim.Audit(i.Key);
            }
            for (int k = 0; k < founded.Count; k++)
            {
                double share = System.Math.Floor(sim.World.Gold / (founded.Count - k));
                if (share > 0) sim.Endow(founded[k].Key, share);
            }
        }

        /// <summary>The personal action: the best-paid work the remaining Attention allows.</summary>
        protected static void WorkBest(Simulation sim)
        {
            foreach (var kind in Simulation.WorkKinds.Reverse())
                if (sim.WorkAttention(kind) <= sim.World.Attention && sim.Work(kind).Ok) return;
        }

        /// <summary>Pays down the most indebted of the given domains with whatever gold is available.</summary>
        protected static void PayDownDebts(Simulation sim, IEnumerable<Domain> domains) => PayDownDebts(sim, domains, double.MaxValue, 1);

        /// <summary>Pays down up to <paramref name="share"/> of each domain's debt, spending at most <paramref name="budget"/> gold.</summary>
        protected static void PayDownDebts(Simulation sim, IEnumerable<Domain> domains, double budget, double share)
        {
            foreach (var d in domains.OrderByDescending(d => sim.World[d].Debt))
            {
                double points = System.Math.Min(sim.World[d].Debt * share, System.Math.Floor(budget / sim.PaydownCost(1)));
                if (points <= 0) continue;
                double before = sim.World.Gold;
                sim.PayDown(d, points);
                budget -= before - sim.World.Gold;
            }
        }
    }

    /// <summary>
    /// Spreads investment across all three domains and both institutions. Three variants differ only in how
    /// they treat debt: Pay-down (the Balanced strategy) clears it, Endow leaves it to the institutions and
    /// endows them, Split pays half and endows the rest.
    /// </summary>
    public class BalancedStrategy : Strategy
    {
        private readonly string _name;
        private readonly double _paydownShare;

        public BalancedStrategy() : this("Balanced", 1) { }

        protected BalancedStrategy(string name, double paydownShare)
        {
            _name = name;
            _paydownShare = paydownShare;
        }

        public override string Name => _name;

        public override void PlayTurn(Simulation sim)
        {
            if (sim.Turn == 1) sim.ChooseSeeded("fountain");
            // Foreknowledge: the inventor knows a plague arrives around AD 165. Balanced response: Protect Medicine
            // as soon as an institution gives them a hold over it.
            if (sim.World[Domain.Medicine].Priority != Priority.Protect) sim.SetPriority(Domain.Medicine, Priority.Protect);
            AnswerPending(sim, true, "hospice", "quarantine");
            OverseeIfNeeded(sim, "circle", 65);
            OverseeIfNeeded(sim, "faction", 65);
            // Finish the circle (found, charter, endow) before starting the faction.
            var circle = sim.World.Institution("circle");
            TryInstitution(sim, "circle", 0);
            if (circle.Chartered && circle.Endowed) TryInstitution(sim, "faction", 0);
            // Invest in the weakest domain first.
            foreach (var d in DomainInfo.All.OrderBy(sim.SubScore))
            {
                var project = sim.AvailableProjects().Where(p => p.Domain == d && p.Gold <= sim.World.Gold).OrderBy(p => p.Gold).FirstOrDefault();
                if (project != null && sim.StartProject(project.Id).Ok) break;
            }
            if (_paydownShare > 0)
                PayDownDebts(sim, DomainInfo.All.Where(d => sim.World[d].Tier >= DebtTier.Strained), double.MaxValue, _paydownShare);
            WorkBest(sim);
        }

        public override void BeforeJump(Simulation sim)
        {
            if (_paydownShare >= 1) PayDownDebts(sim, DomainInfo.All);
            else if (_paydownShare > 0) PayDownDebts(sim, DomainInfo.All, sim.World.Gold * _paydownShare, 1);
            AuditAndEndow(sim, "circle", "faction");
        }
    }

    /// <summary>Balanced play, but leaves all debt to the institutions and endows them with everything.</summary>
    public sealed class EndowStrategy : BalancedStrategy
    {
        public EndowStrategy() : base("Endow", 0) { }
    }

    /// <summary>Balanced play; pays down debt with half its gold and endows the institutions with the rest.</summary>
    public sealed class SplitStrategy : BalancedStrategy
    {
        public SplitStrategy() : base("Split", 0.5) { }
    }

    /// <summary>Puts every spare coin and hour into Medicine and the physicians' circle.</summary>
    public sealed class SpecializedStrategy : Strategy
    {
        public override string Name => "Specialized";

        public override void PlayTurn(Simulation sim)
        {
            if (sim.Turn == 1)
            {
                sim.ChooseSeeded("fountain");
            }
            // Foreknowledge of the plague matches its chosen focus: Protect Medicine once the Circle gives it a hold.
            if (sim.World[Domain.Medicine].Priority != Priority.Protect) sim.SetPriority(Domain.Medicine, Priority.Protect);
            AnswerPending(sim, true, "hospice", "quarantine");
            OverseeIfNeeded(sim, "circle", 80);
            TryInstitution(sim, "circle", 0);
            var project = sim.AvailableProjects().Where(p => p.Domain == Domain.Medicine && p.Gold <= sim.World.Gold).OrderBy(p => p.Gold).FirstOrDefault();
            if (project != null) sim.StartProject(project.Id);
            if (sim.World.Institution("circle").Founded && sim.CommitmentsEnabled && sim.World.Commitments.Count == 0) sim.Mentor("circle");
            PayDownDebts(sim, new[] { Domain.Medicine });
            WorkBest(sim);
        }

        public override void BeforeJump(Simulation sim)
        {
            PayDownDebts(sim, DomainInfo.All);
            AuditAndEndow(sim, "circle");
        }
    }

    /// <summary>Spends nothing on upkeep or the future; hoards gold.</summary>
    public sealed class NeglectfulStrategy : Strategy
    {
        public override string Name => "Neglectful";

        public override void PlayTurn(Simulation sim)
        {
            if (sim.Turn == 1)
            {
                sim.ChooseSeeded("workshop");
                // Foreknowledge, minimal response: keep Medicine at Maintain; accept risk everywhere else.
                sim.SetPriority(Domain.Governance, Priority.AcceptRisk);
                sim.SetPriority(Domain.Economy, Priority.AcceptRisk);
            }
            AnswerPending(sim, false, "none");
            WorkBest(sim);
        }
    }
}
