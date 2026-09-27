using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The news of Rome (decided 2026-09-28): what is going on in the city, heard in the Forum. Three parts: history's dated
    /// news (data/content/news.json, text only, changing nothing), what the simulation itself had happen to Rome lately
    /// (world events from the log, never the player's own actions), and the talk of the market (prices, the aureus, the coin).
    /// Reading it is free and draws no random numbers.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>World events worth hearing about as news: things Rome did or suffered, not things you did.</summary>
        private static readonly HashSet<string> WorldNewsEvents = new HashSet<string>
        {
            "plague.warning", "plague.outbreak", "plague.toll", "plague.passed", "plague.opening", "bust.warning", "bust.outbreak",
            "bust.toll", "debt.tier", "institution.collapse", "institution.captured", "institution.leader", "crisis.recurrence",
        };

        /// <summary>The last turn a news item can belong to: one shows on the turn that covers its date (like the plague's stages).</summary>
        private SimTime NewsHorizon => Now.AddMonths(MonthsPerTurn - 1);

        /// <summary>History's news heard so far this era, oldest first. None after a jump: history is no longer a guide.</summary>
        public IEnumerable<NewsDef> HistoryNewsSoFar() =>
            Arrived ? Enumerable.Empty<NewsDef>() : Data.Content.News.Where(n => n.Time.TotalMonths <= NewsHorizon.TotalMonths);

        /// <summary>History's news that arrives this turn.</summary>
        public IEnumerable<NewsDef> HistoryNewsThisTurn() =>
            HistoryNewsSoFar().Where(n => n.Time.TotalMonths >= Now.TotalMonths);

        /// <summary>The news: history's latest, what befell Rome in the last year, and the talk of the market.</summary>
        public List<string> News(int historyItems = 5)
        {
            var lines = new List<string>();
            var history = HistoryNewsSoFar().ToList();
            if (history.Count > 0)
            {
                lines.Add("In the Forum:");
                foreach (var n in history.Skip(Math.Max(0, history.Count - historyItems)))
                    lines.Add("  " + SimTime.FromYear(n.Year, n.Month - 1).Display + ": " + n.Text);
            }
            var since = Now.AddMonths(-12).TotalMonths;
            var world = Log.Events.Where(e => WorldNewsEvents.Contains(e.Type) && e.Time.TotalMonths >= since && !e.Actors.Contains("player")).ToList();
            if (world.Count > 0)
            {
                lines.Add("In Rome this past year:");
                foreach (var e in world) lines.Add("  " + e.Time.Display + ": " + e.Text);
            }
            lines.Add("At the market: an aureus fetches " + R(Denarii(AureusPrice), "0.#") + " denarii" +
                      (World.PriceLevel > 1.0001 ? "; prices are up " + R((World.PriceLevel - 1) * 100) + "% since AD 155, and rising " + R(InflationRate() * 100) + "% a year" : "") +
                      "; the new denarii are about " + R(CoinSilverNow() * 100, "0") + "% silver.");
            return lines;
        }
    }
}
