namespace Butterfly.Core
{
    /// <summary>
    /// A point in game time, stored as whole months since AD 0 (SYSTEMS.md §2).
    /// Simulated change runs per year; turns are a fixed number of months.
    /// </summary>
    public readonly struct SimTime
    {
        public readonly int TotalMonths;

        public SimTime(int totalMonths)
        {
            TotalMonths = totalMonths;
        }

        public static SimTime FromYear(int year, int month = 0) => new SimTime(year * 12 + month);

        public int Year => TotalMonths / 12;
        public int Month => TotalMonths % 12;
        public double YearFraction => TotalMonths / 12.0;

        public SimTime AddMonths(int months) => new SimTime(TotalMonths + months);

        private static readonly string[] MonthNames =
        {
            "Ianuarius", "Februarius", "Martius", "Aprilis", "Maius", "Iunius",
            "Iulius", "Augustus", "September", "October", "November", "December"
        };

        /// <summary>Stable text used in logs and hashes, e.g. "AD 155-04".</summary>
        public string Stamp => "AD " + Year + "-" + (Month + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture);

        public string Display => MonthNames[Month] + ", AD " + Year;

        public override string ToString() => Stamp;
    }
}
