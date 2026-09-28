using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The pestilence's historical recurrences (SYSTEMS §6; decided 2026-09-28, Corey: build them now): the plague in Rome
    /// under Commodus (AD 189) and the Plague of Cyprian (about AD 251). Like the Antonine plague they come on their dates
    /// whatever the player does, and the player changes only how hard they hit. Their drops are in the history curve, so a
    /// Rome that tracks history takes them as history had them; the player's mark is the difference: better hazard and
    /// resilience (clean water, Medicine, a school, institutions ready to respond) spare Rome part of the drop, debt and
    /// neglect make it worse. No random recurrences (jump.randomRecurrences).
    /// </summary>
    public sealed partial class Simulation
    {
        private readonly HashSet<int> _recurrencesStruck = new HashSet<int>();

        /// <summary>The year of a historical recurrence that is raging now (you arrived in its year), or 0.</summary>
        public int EpidemicYear => _recurrencesStruck.Contains(Now.Year) ? Now.Year : 0;

        /// <summary>How a recurrence in <paramref name="year"/> compares with history: hazard × (1 − resilience) against history's, raised by debt tiers.</summary>
        public double RecurrenceRatio(int year, string response)
        {
            double historical = Benchmark(Domain.Medicine, year) / 100.0 * T.Get("plague.resiliencePerMedicine")
                              + Benchmark(Domain.Governance, year) / 100.0 * T.Get("plague.resiliencePerGovernance")
                              + T.Get("plague.response.none.resilience");
            historical = Math.Min(T.Get("plague.maxResilience"), historical);
            return (PlagueHazard() / HistoricalPlagueHazard) * ((1 - PlagueResilience(response)) / (1 - historical)) * PlagueSeverityMultiplier();
        }

        /// <summary>Each simulated year (present or away): a historical recurrence strikes in its year.</summary>
        private void HistoricalRecurrences(Arrival? arrival)
        {
            var years = T.GetArray("plague.recurrences.years");
            var shares = T.GetArray("plague.recurrences.deathShare");
            for (int k = 0; k < years.Length; k++)
            {
                int year = (int)years[k];
                if (Now.Year != year || _recurrencesStruck.Contains(year)) continue;
                _recurrencesStruck.Add(year);
                string response = AutomaticResponse();
                double ratio = RecurrenceRatio(year, response);
                double share = Math.Min(T.Get("plague.recurrences.maxDeathShare"), shares[k] * ratio);
                double popBefore = World.Population, deaths = World.Population * share;
                World.Population -= deaths;
                World.LastOutbreakYear = year;
                string label = SeverityLabel(share);
                var text = Data.Content.Template("recurrence." + year, new Dictionary<string, string>
                {
                    { "label", label }, { "deaths", F(Math.Round(deaths)) }, { "share", F(Math.Round(share * 100)) }, { "response", response },
                });
                var start = Record("crisis.recurrence", "plague", CausesOf(DebtKey(Domain.Medicine), LevelKey(Domain.Medicine), "plague.resilience", "fountain.clean"),
                    new[] { "world" }, new[] { new Effect("population", popBefore, World.Population) }, text);
                // History's drop is already in the curve Rome follows; what differs from history is the player's mark.
                foreach (var d in DomainInfo.All)
                {
                    double drop = Math.Max(0, Benchmark(d, year + 0.75) - Benchmark(d, year + 1));
                    double extra = drop * (ratio - 1) * T.Get("plague.damage." + d.Key());
                    if (Math.Abs(extra) > 1e-9)
                        ChangeLevel(d, -extra, "plague.damage", new[] { start.Id }, new[] { "world" },
                            "The pestilence of AD " + year + " strikes " + d + (extra > 0 ? " harder than it did in history." : " more lightly than it did in history."));
                }
                // A crisis releases debt and resets what people expect (SYSTEMS §6).
                foreach (var d in DomainInfo.All)
                {
                    var s = World[d];
                    s.Peak = s.Level;
                    if (s.Debt <= 0) continue;
                    double before = s.Debt;
                    s.Debt *= 1 - T.Get("plague.debtRelease");
                    Record("debt.release", d.Key(), new[] { start.Id }, new[] { "world" }, new[] { new Effect(DebtKey(d), before, s.Debt) }, "The crisis releases " + d + " debt.");
                    UpdateTier(d);
                }
                arrival?.Crises.Add("AD " + year + ": " + text);
            }
        }
    }
}
