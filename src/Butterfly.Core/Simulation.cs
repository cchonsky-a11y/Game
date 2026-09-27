using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The P0 simulation. Owns the seeded generator, the clock and the event log.
    /// All player input arrives through methods on this class, so a run is fully
    /// determined by (seed, sequence of calls). Systems live in partial files.
    /// </summary>
    public sealed partial class Simulation
    {
        public GameData Data { get; }
        public Tuning T => Data.Tuning;
        public ulong Seed { get; }
        public Rng Rng { get; }
        public EventLog Log { get; } = new EventLog();
        public World World { get; } = new World();
        public SimTime Now { get; private set; }
        public int Turn { get; private set; } = 1;
        public int MonthsPerTurn { get; }

        /// <summary>Latest event that changed each state key, used as immediate causes.</summary>
        private readonly Dictionary<string, int> _lastChange = new Dictionary<string, int>();

        public Simulation(GameData data, ulong seed)
        {
            Data = data;
            Seed = seed;
            Rng = new Rng(seed);
            MonthsPerTurn = T.GetInt("time.monthsPerTurn");
            Now = SimTime.FromYear(T.GetInt("time.startYear"), T.GetInt("time.startMonth"));
            var start = Log.Record(Now, "scenario.start", "region", null, new[] { "world" }, null,
                "The inventor arrives in Rome. Seed " + seed + ".");
            InitDomains(start.Id);
            InitEconomy(start.Id);
            InitPlague();
            InitInstitutions();
            InitAttention();
        }

        /// <summary>Fraction of a year covered by one turn.</summary>
        public double YearsPerTurn => MonthsPerTurn / 12.0;

        /// <summary>Ends the current turn. Runs the yearly simulation whenever a year boundary is crossed.</summary>
        public void EndTurn()
        {
            if (IsAway || Arrived) throw new System.InvalidOperationException("The era is over; the inventor has jumped.");
            MarkTurnEventStart(Log.NextId);
            ProgressProjects();
            ProgressMachine();
            ProgressInventions();
            ProgressCommitments();
            LapseSeededChoiceIfDue();
            ResolvePendingOutbreak();
            SettleGold();
            int yearBefore = Now.Year;
            Now = Now.AddMonths(MonthsPerTurn);
            Turn++;
            if (Now.Year != yearBefore) YearTick();
            AdvancePlagueToDate();
            Log.Record(Now, "turn.start", "clock", null, new[] { "world" }, null,
                "Turn " + Turn + " begins: " + Now.Display + ".");
            RefreshAttention();
        }

        private void YearTick()
        {
            Log.Record(Now, "year.start", "clock", null, new[] { "world" }, null,
                "The year AD " + Now.Year + " begins.");
            DomainsYearTick();
            PolicyYearTick();
            InstitutionsYearTick();
            SeededPayoffYearTick();
        }

        // ---- cause tracking -------------------------------------------------

        internal int CauseOf(string key) => _lastChange.TryGetValue(key, out var id) ? id : 0;

        internal IEnumerable<int> CausesOf(params string[] keys) => keys.Select(CauseOf).Where(id => id > 0);

        /// <summary>Records an event and marks it as the latest change for each of its effect keys.</summary>
        internal GameEvent Record(string type, string target, IEnumerable<int>? causes, IEnumerable<string>? actors,
            IEnumerable<Effect>? effects, string text)
        {
            var effectList = effects?.ToList() ?? new List<Effect>();
            var e = Log.Record(Now, type, target, causes, actors, effectList, text);
            foreach (var fx in effectList) _lastChange[fx.Key] = e.Id;
            return e;
        }

        internal void MarkChanged(string key, int eventId) => _lastChange[key] = eventId;
    }
}
