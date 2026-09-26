using System;
using System.Collections.Generic;
using System.Globalization;

namespace Butterfly.Core
{
    /// <summary>Care domains, priorities, expectations and debt (SYSTEMS.md §5–6).</summary>
    public sealed partial class Simulation
    {
        private void InitDomains(int startEventId)
        {
            foreach (var d in DomainInfo.All)
            {
                var state = new DomainState(d, T.Get("domains.startLevel." + d.Key()));
                World.Domains.Add(state);
                MarkChanged(LevelKey(d), startEventId);
                MarkChanged(PriorityKey(d), startEventId);
            }
        }

        internal static string LevelKey(Domain d) => d.Key() + ".level";
        internal static string DebtKey(Domain d) => d.Key() + ".debt";
        internal static string PriorityKey(Domain d) => d.Key() + ".priority";
        internal static string TierKey(Domain d) => d.Key() + ".tier";

        /// <summary>Historical value of a domain at a year (SYSTEMS §6 benchmark and §12 Index denominator).</summary>
        public double Benchmark(Domain d, double year) =>
            Formulas.Interpolate(T.GetArray("history.years"), T.GetArray("history." + d.Key()), year);

        public double Expectation(Domain d) => Formulas.Expectation(Benchmark(d, Now.Year), World[d].Peak);

        public CommandResult SetPriority(Domain d, Priority p)
        {
            var s = World[d];
            if (s.Priority == p) return CommandResult.Fail(d + " is already set to " + p.Label() + ".");
            var before = s.Priority;
            s.Priority = p;
            Record("priority.set", d.Key(), null, new[] { "player" },
                new[] { new Effect(PriorityKey(d), (int)before, (int)p) },
                "You set " + d + " to " + p.Label() + " (was " + before.Label() + ").");
            return CommandResult.Success(d + " set to " + p.Label() + ".");
        }

        /// <summary>
        /// Yearly domain step: priority upkeep changes the level, then debt compounds and accrues
        /// against the expectation, then the recent peak fades and the tier is re-evaluated.
        /// </summary>
        private void DomainsYearTick()
        {
            foreach (var s in World.Domains)
            {
                var d = s.Domain;

                // 1. Priority upkeep changes the level.
                double change = PriorityLevelChange(d);
                // Neglect alone never takes a domain below a floor; only crises can.
                double floor = Benchmark(d, Now.Year) * T.Get("priorities.neglectFloorFraction");
                if (change < 0) change = Math.Max(change, Math.Min(0, floor - s.Level));
                if (change != 0) ChangeLevel(d, change, "domain.upkeep", CausesOf(PriorityKey(d)), new[] { "world" },
                    d + " " + (change > 0 ? "improves" : "slips") + " under " + s.Priority.Label() + " (" + Signed(change) + ").");

                // 2. Debt compounds and accrues against the expectation.
                double expectation = Expectation(d);
                double accrual = Formulas.DebtAccrual(expectation, s.Level, T.Get("debt.accrualRate"));
                double before = s.Debt;
                s.Debt = Formulas.DebtStep(s.Debt, accrual, T.Get("debt.compoundRate"));
                if (s.Debt != before)
                {
                    string text = accrual > 0
                        ? d + " debt grows to " + F(s.Debt) + ": level " + F(s.Level) + " is below expectation " + F(expectation) + "."
                        : d + " debt compounds to " + F(s.Debt) + " (5% a year until paid down).";
                    Record("debt.accrue", d.Key(), CausesOf(LevelKey(d), DebtKey(d)), new[] { "world" },
                        new[] { new Effect(DebtKey(d), before, s.Debt) }, text);
                }

                // 3. The recent peak fades toward the current level.
                s.Peak = Formulas.FadePeak(s.Peak, s.Level, T.Get("expectation.peakFadePerYear"));

                UpdateTier(d);
            }
            for (int i = 0; i < World.UpkeepPaidThisYear.Length; i++) World.UpkeepPaidThisYear[i] = 0;
            World.UpkeepTurnsThisYear = 0;
        }

        /// <summary>
        /// Yearly level change from the domain's priority. If upkeep went partly unpaid this year,
        /// the unpaid share behaves as Accept Risk.
        /// </summary>
        internal double PriorityLevelChange(Domain d)
        {
            if (!PrioritiesActive) return T.Get("priorities.levelChangePerYear.maintain");
            double chosen = T.Get("priorities.levelChangePerYear." + World[d].Priority.Key());
            double neglect = T.Get("priorities.levelChangePerYear.acceptRisk");
            double paid = World.UpkeepTurnsThisYear == 0 ? 1 : World.UpkeepPaidThisYear[(int)d] / World.UpkeepTurnsThisYear;
            return paid * chosen + (1 - paid) * neglect;
        }

        internal void UpdateTier(Domain d)
        {
            var s = World[d];
            var tier = Formulas.Tier(s.Debt, T.Get("debt.tiers.strained"), T.Get("debt.tiers.fragile"), T.Get("debt.tiers.critical"));
            if (tier == s.Tier) return;
            var before = s.Tier;
            s.Tier = tier;
            Record("debt.tier", d.Key(), CausesOf(DebtKey(d)), new[] { "world" },
                new[] { new Effect(TierKey(d), (int)before, (int)tier) },
                d + " moves from " + before + " to " + tier + ".");
        }

        /// <summary>Changes a domain level (clamped 0–100), updates the peak and logs the change.</summary>
        internal GameEvent? ChangeLevel(Domain d, double delta, string type, IEnumerable<int>? causes,
            IEnumerable<string> actors, string text)
        {
            var s = World[d];
            double before = s.Level;
            double min = Math.Min(s.Level, Benchmark(d, Now.Year) * T.Get("domains.minLevelFraction"));
            s.Level = Math.Max(min, Math.Min(T.Get("domains.maxLevel"), s.Level + delta));
            if (s.Level == before) return null;
            if (s.Level > s.Peak) s.Peak = s.Level;
            return Record(type, d.Key(), causes, actors, new[] { new Effect(LevelKey(d), before, s.Level) }, text);
        }

        internal static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
        internal static string Signed(double v) => (v >= 0 ? "+" : "") + F(v);
    }
}
