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

        /// <summary>
        /// Every strategy repairs the time machine (all 9 steps are needed to jump): it assesses the machine first, then
        /// starts the next affordable step of each system before its own turn, and once the repairs are done puts the
        /// scavenged gold back as it can.
        /// </summary>
        public static void RepairMachine(Simulation sim)
        {
            if (sim.Turn == 1) return; // the hour-one choice comes first
            // Change the rest of the scavenged gold into denarii to live on (decided 2026-09-28: two currencies).
            if (sim.World.Aurei >= 1 && sim.MachineGoldRestored < 1 && sim.MachineStepsDone < sim.MachineStepsTotal) sim.SellAurei(sim.World.Aurei);
            if (!sim.MachineAssessed) { sim.Assess(); return; }
            if (sim.MachineStepsDone >= sim.MachineStepsTotal && sim.MachineGoldRestored < sim.MachineGoldNeeded)
            {
                // Buy back the machine's gold at today's rate, then put it back.
                double missing = sim.MachineGoldNeeded - sim.MachineGoldRestored - sim.World.Aurei;
                if (missing >= 1) sim.BuyAurei(Math.Min(missing, sim.AffordableAurei()));
                if (sim.World.Aurei >= 1) sim.RestoreGold(sim.MachineGoldNeeded - sim.MachineGoldRestored);
            }
            foreach (var system in Simulation.MachineSystems)
            {
                var step = sim.NextMachineStep(system);
                if (step != null && sim.MachineStepGold(step) <= sim.World.Gold && step.AttentionPerTurn <= sim.World.Attention) sim.Repair(system);
            }
        }

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

        /// <summary>Buys as much of an established institution as gold allows, up to <paramref name="targetPercent"/>.</summary>
        protected static void BuyToward(Simulation sim, string id, int targetPercent, double reserve)
        {
            var i = sim.World.Institution(id);
            if (!i.Exists || i.Def.IsOwn) return;
            int points = 0;
            while (sim.StakePercent(i) + points < targetPercent && sim.BuyCost(i, points + 1) <= sim.World.Gold - reserve) points++;
            if (points > 0) sim.Buy(id, points);
        }

        /// <summary>
        /// Works toward controlling an institution (founding it if it is the player's own, buying up to 50% otherwise),
        /// then charters and endows it.
        /// </summary>
        protected static void TryInstitution(Simulation sim, string id, double reserve)
        {
            var i = sim.World.Institution(id);
            if (i.Def.IsOwn)
            {
                if (!i.Exists && !i.Collapsed && sim.World.Gold - sim.FoundCost(i.Def.Maintains) >= reserve) sim.Found(id);
            }
            else if (!sim.Controls(i)) BuyToward(sim, id, (int)System.Math.Round(sim.ControlAt * 100), reserve);
            if (!sim.Controls(i)) return;
            if (!i.Chartered && sim.World.Gold - sim.T.Get("institutions.charterGold") >= reserve) sim.Charter(id);
            if (!i.Endowed && sim.World.Gold - sim.T.Get("institutions.endowGold") >= reserve) sim.Endow(id);
        }

        protected static bool Controls(Simulation sim, string id) => sim.Controls(sim.World.Institution(id));

        protected static void OverseeIfNeeded(Simulation sim, string id, double below)
        {
            var i = sim.World.Institution(id);
            if (sim.Controls(i) && i.Loyalty < below) sim.Oversee(id);
        }

        /// <summary>
        /// Jump preparation: split the remaining gold evenly as endowments. An audit charter is bought first
        /// for any institution whose share would be large, or whose leader is venal.
        /// </summary>
        protected static void AuditAndEndow(Simulation sim, params string[] ids)
        {
            var founded = ids.Select(id => sim.World.Institution(id)).Where(sim.Controls).ToList();
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

        /// <summary>Spends leftover Attention attending meetings of institutions it belongs to but doesn't control.</summary>
        protected static void AttendMeetings(Simulation sim)
        {
            foreach (var i in sim.Backed().Where(i => !i.Def.IsOwn && !sim.Controls(i)).ToList())
                if (sim.World.Attention >= sim.T.GetInt("stakes.attendAttention")) sim.Attend(i.Key);
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
    /// Spreads investment across all three domains and takes control of established institutions. Three variants differ only in how
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

        /// <summary>0: leave Rome's policy as history; +1 Austrian; −1 interventionist.</summary>
        protected virtual int PolicyStance => 0;

        public override void PlayTurn(Simulation sim)
        {
            if (sim.Turn == 1) sim.ChooseSeeded("fountain");
            if (PolicyStance != 0)
            {
                // Policy strategies take control of the faction first (policy), then a voice in the guild (Economy), then the Circle.
                TryInstitution(sim, "faction", 0);
                // The guild wants a business of your own; without one, the bank takes a depositor instead.
                string economy = sim.World.Institution("guild").Stake > 0 || sim.JoinBlocker(sim.World.Institution("guild")) == null ? "guild" : "bank";
                if (Controls(sim, "faction")) BuyToward(sim, economy, (int)System.Math.Round(sim.VoiceAt * 100), 0);
                foreach (var issue in Simulation.Issues)
                    if (sim.Stance(issue) != PolicyStance) sim.SetPolicy(issue, PolicyStance);
                if (sim.World[Domain.Economy].Priority != Priority.Protect) sim.SetPriority(Domain.Economy, Priority.Protect);
            }
            // Foreknowledge: the inventor knows a plague arrives around AD 165. Balanced response: Protect Medicine
            // as soon as an institution gives them a hold over it.
            if (sim.World[Domain.Medicine].Priority != Priority.Protect) sim.SetPriority(Domain.Medicine, Priority.Protect);
            AnswerPending(sim, true, "hospice", "quarantine");
            OverseeIfNeeded(sim, "circle", 65);
            OverseeIfNeeded(sim, "faction", 65);
            // Take control of the circle (then charter and endow it) before buying into the faction.
            var circle = sim.World.Institution("circle");
            if (PolicyStance == 0 || Controls(sim, "faction")) TryInstitution(sim, "circle", 0);
            if (circle.Chartered && circle.Endowed) TryInstitution(sim, "faction", 0);
            // Invest in the weakest domain first.
            foreach (var d in DomainInfo.All.OrderBy(sim.SubScore))
            {
                var project = sim.AvailableProjects().Where(p => p.Domain == d && sim.ProjectAuthorityBlocker(p) == null && sim.ProjectGold(p) <= sim.World.Gold).OrderBy(p => sim.ProjectGold(p)).FirstOrDefault();
                if (project != null && sim.StartProject(project.Id).Ok) break;
            }
            if (_paydownShare > 0)
                PayDownDebts(sim, DomainInfo.All.Where(d => sim.World[d].Tier >= DebtTier.Strained), double.MaxValue, _paydownShare);
            WorkBest(sim);
            AttendMeetings(sim);
        }

        public override void BeforeJump(Simulation sim)
        {
            if (_paydownShare >= 1) PayDownDebts(sim, DomainInfo.All);
            else if (_paydownShare > 0) PayDownDebts(sim, DomainInfo.All, sim.World.Gold * _paydownShare, 1);
            AuditAndEndow(sim, "circle", "faction", "guild", "bank");
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

    /// <summary>Puts every spare coin and hour into Medicine and a school of its own, which it founds and builds up.</summary>
    public sealed class SpecializedStrategy : Strategy
    {
        public override string Name => "Specialized";

        public override void PlayTurn(Simulation sim)
        {
            if (sim.Turn == 1)
            {
                sim.ChooseSeeded("fountain");
            }
            // Foreknowledge of the plague matches its chosen focus: Protect Medicine once the school gives it a voice.
            if (sim.World[Domain.Medicine].Priority != Priority.Protect) sim.SetPriority(Domain.Medicine, Priority.Protect);
            AnswerPending(sim, true, "hospice", "quarantine");
            OverseeIfNeeded(sim, "school", 80);
            TryInstitution(sim, "school", 0);
            // Build the school up past the point where it could fail, then keep growing its share.
            var school = sim.World.Institution("school");
            if (sim.Controls(school) && school.Strength < 60 && sim.World.Gold >= 30) sim.Invest("school", System.Math.Floor(sim.World.Gold / 2));
            var project = sim.AvailableProjects().Where(p => p.Domain == Domain.Medicine && sim.ProjectAuthorityBlocker(p) == null && sim.ProjectGold(p) <= sim.World.Gold).OrderBy(p => sim.ProjectGold(p)).FirstOrDefault();
            if (project != null) sim.StartProject(project.Id);
            if (sim.Controls(school) && sim.CommitmentsEnabled && sim.World.Commitments.Count == 0) sim.Mentor("school");
            PayDownDebts(sim, new[] { Domain.Medicine });
            WorkBest(sim);
        }

        public override void BeforeJump(Simulation sim)
        {
            PayDownDebts(sim, DomainInfo.All);
            AuditAndEndow(sim, "school");
        }
    }

    /// <summary>Balanced play, but takes the faction (policy) and a voice in the guild first, with Austrian stances on every issue (free-market).</summary>
    public sealed class FreeMarketStrategy : BalancedStrategy
    {
        public FreeMarketStrategy() : base("FreeMarket", 1) { }
        protected override int PolicyStance => 1;
    }

    /// <summary>Balanced play, but takes the faction (policy) and a voice in the guild first, with interventionist stances on every issue.</summary>
    public sealed class InterventionistStrategy : BalancedStrategy
    {
        public InterventionistStrategy() : base("Interventionist", 1) { }
        protected override int PolicyStance => -1;
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
