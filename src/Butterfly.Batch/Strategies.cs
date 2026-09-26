using System.Collections.Generic;
using System.Linq;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>A scripted player. Each turn it issues commands through the same API as the console.</summary>
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

        /// <summary>Pays down the most indebted of the given domains with whatever gold is available.</summary>
        protected static void PayDownDebts(Simulation sim, IEnumerable<Domain> domains)
        {
            foreach (var d in domains.OrderByDescending(d => sim.World[d].Debt))
                if (sim.World[d].Debt > 0) sim.PayDown(d, sim.World[d].Debt);
        }
    }

    /// <summary>Spreads investment across all three domains and both institutions.</summary>
    public sealed class BalancedStrategy : Strategy
    {
        public override string Name => "Balanced";

        public override void PlayTurn(Simulation sim)
        {
            if (sim.SeededChoiceOpen) sim.ChooseSeeded("fountain");
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
            PayDownDebts(sim, DomainInfo.All.Where(d => sim.World[d].Tier >= DebtTier.Strained));
            sim.Work();
        }

        public override void BeforeJump(Simulation sim) => PayDownDebts(sim, DomainInfo.All);
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
                sim.SetPriority(Domain.Medicine, Priority.Protect);
            }
            AnswerPending(sim, true, "hospice", "quarantine");
            OverseeIfNeeded(sim, "circle", 80);
            TryInstitution(sim, "circle", 0);
            var project = sim.AvailableProjects().Where(p => p.Domain == Domain.Medicine && p.Gold <= sim.World.Gold).OrderBy(p => p.Gold).FirstOrDefault();
            if (project != null) sim.StartProject(project.Id);
            if (sim.World.Institution("circle").Founded && sim.CommitmentsEnabled && sim.World.Commitments.Count == 0) sim.Mentor("circle");
            PayDownDebts(sim, new[] { Domain.Medicine });
            sim.Work();
        }

        public override void BeforeJump(Simulation sim) => PayDownDebts(sim, DomainInfo.All);
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
                foreach (var d in DomainInfo.All) sim.SetPriority(d, Priority.AcceptRisk);
            }
            AnswerPending(sim, false, "none");
            sim.Work();
        }
    }
}
