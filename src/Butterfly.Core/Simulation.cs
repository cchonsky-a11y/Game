using System.Collections.Generic;

namespace Butterfly.Core
{
    /// <summary>
    /// The P0 simulation. Owns the seeded generator, the clock and the event log.
    /// All player input arrives through methods on this class, so a run is fully
    /// determined by (seed, sequence of calls).
    /// </summary>
    public sealed class Simulation
    {
        public GameData Data { get; }
        public Tuning T => Data.Tuning;
        public ulong Seed { get; }
        public Rng Rng { get; }
        public EventLog Log { get; } = new EventLog();
        public SimTime Now { get; private set; }
        public int Turn { get; private set; } = 1;
        public int MonthsPerTurn { get; }

        public Simulation(GameData data, ulong seed)
        {
            Data = data;
            Seed = seed;
            Rng = new Rng(seed);
            MonthsPerTurn = T.GetInt("time.monthsPerTurn");
            Now = SimTime.FromYear(T.GetInt("time.startYear"), T.GetInt("time.startMonth"));
            Log.Record(Now, "scenario.start", "region", null, new[] { "world" }, null,
                "The inventor arrives in Rome. Seed " + seed + ".");
        }

        /// <summary>Ends the current turn. Runs the yearly simulation whenever a year boundary is crossed.</summary>
        public void EndTurn()
        {
            int yearBefore = Now.Year;
            Now = Now.AddMonths(MonthsPerTurn);
            Turn++;
            if (Now.Year != yearBefore) YearTick();
            Log.Record(Now, "turn.start", "clock", null, new[] { "world" }, null,
                "Turn " + Turn + " begins: " + Now.Display + ".");
        }

        private void YearTick()
        {
            Log.Record(Now, "year.start", "clock", null, new[] { "world" }, null,
                "The year AD " + Now.Year + " begins.");
        }
    }
}
