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

        private readonly HashSet<int> _localHeard = new HashSet<int>();
        private readonly List<(SimTime Time, string Text)> _localNews = new List<(SimTime, string)>();
        private int _localSlot;
        private int _localThisTurnFrom;

        /// <summary>Local talk heard this turn.</summary>
        public IEnumerable<string> LocalNewsThisTurn() => _localNews.Skip(_localThisTurnFrom).Select(n => n.Text);

        /// <summary>
        /// At the start of each turn: a festival that falls in this turn, and one piece of local talk every news.localEveryMonths
        /// (talk about the player's own world first). Each item is heard once. Presentation only: not logged, no random numbers.
        /// </summary>
        private void AdvanceLocalNews()
        {
            _localThisTurnFrom = _localNews.Count;
            var all = Data.Content.LocalNews;
            for (int k = 0; k < all.Count; k++)
                if (all[k].Month > 0 && !_localHeard.Contains(k) && TurnCoversMonth(all[k].Month) && LocalNewsFits(all[k])) Hear(k);
            // Talk about the outbreak comes the month it happens: it lasts a single month (P1, 1-month turns) and would
            // otherwise fall between the every-few-months slots and never reach the street.
            int urgent = Enumerable.Range(0, all.Count).Where(k => all[k].Month == 0 && all[k].When == "plague:outbreak" && !_localHeard.Contains(k) && LocalNewsFits(all[k]))
                .DefaultIfEmpty(-1).First();
            if (urgent >= 0) Hear(urgent);
            int every = T.GetInt("news.localEveryMonths");
            int slot = (Now.TotalMonths - SimTime.FromYear(T.GetInt("time.startYear"), T.GetInt("time.startMonth")).TotalMonths) / every;
            if (slot <= _localSlot) return;
            _localSlot = slot;
            int pick = Enumerable.Range(0, all.Count)
                .Where(k => all[k].Month == 0 && !_localHeard.Contains(k) && LocalNewsFits(all[k]))
                .OrderBy(k => all[k].When == "always" ? 1 : 0).ThenBy(k => k)
                .DefaultIfEmpty(-1).First();
            if (pick >= 0) Hear(pick);
        }

        private void Hear(int k)
        {
            _localHeard.Add(k);
            string text = Data.Content.LocalNews[k].Text;
            foreach (var i in World.Institutions) text = text.Replace("{leader:" + i.Key + "}", i.Leader);
            _localNews.Add((Now, text));
        }

        private bool TurnCoversMonth(int month1to12)
        {
            for (int m = 0; m < MonthsPerTurn; m++)
                if (Now.AddMonths(m).Month == month1to12 - 1) return true;
            return false;
        }

        /// <summary>Whether a piece of local talk fits the player's world now.</summary>
        private bool LocalNewsFits(LocalNewsDef n)
        {
            if (Now.Year < n.From || Now.Year > n.Until) return false;
            string w = n.When;
            if (w == "always") return true;
            if (w == "fountain" || w == "workshop") return World.SeededChoice == w;
            if (w.StartsWith("member:")) { var i = World.Institution(w.Substring(7)); return i.Exists && i.Stake > 0; }
            if (w == "plague:warning") return World.Plague.IsWarning;
            if (w == "plague:outbreak") return World.Plague.Stage == PlagueState.Outbreak;
            if (w == "plague:passed") return World.Plague.Stage == PlagueState.Passed;
            if (w == "bust:warning") return World.Bust.Stage >= 1 && World.Bust.Stage <= 3;
            if (w.StartsWith("prices:")) return (World.PriceLevel - 1) * 100 >= double.Parse(w.Substring(7), System.Globalization.CultureInfo.InvariantCulture);
            throw new InvalidOperationException("Unknown local news condition: " + w);
        }

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
            if (_localNews.Count > 0)
            {
                lines.Add("On your street:");
                foreach (var n in _localNews.Skip(Math.Max(0, _localNews.Count - 3))) lines.Add("  " + n.Time.Display + ": " + n.Text);
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
