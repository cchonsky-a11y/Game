using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Butterfly.Core
{
    /// <summary>One of the four arrival beats (SYSTEMS §11).</summary>
    public sealed class ArrivalBeat
    {
        public string Name { get; }
        public string Text { get; }

        public ArrivalBeat(string name, string text)
        {
            Name = name;
            Text = text;
        }
    }

    /// <summary>An Echo: a specific element tagged before the jump and surfaced on arrival (SYSTEMS §11).</summary>
    public sealed class EchoRecord
    {
        public string Id { get; }
        public string Name { get; }
        public string AtDeparture { get; }
        public string AtArrival { get; set; } = "";
        /// <summary>The beat in which this Echo is surfaced.</summary>
        public string Beat { get; set; } = "";

        public EchoRecord(string id, string name, string atDeparture)
        {
            Id = id;
            Name = name;
            AtDeparture = atDeparture;
        }
    }

    /// <summary>Institution fate as shown under Learn more.</summary>
    public sealed class InstitutionReport
    {
        public string Name { get; }
        public string NowCalled { get; }
        public InstitutionOutcome Outcome { get; }
        public double Strength { get; }
        public double Loyalty { get; }
        public InstitutionQuality Quality { get; }
        public double HoldingsAtDeparture { get; set; }
        public double HoldingsNow { get; set; }
        public double DebtPaidAway { get; set; }
        public double GoldLostToCorruption { get; set; }
        public CorruptionLevel Corruption { get; set; }

        public InstitutionReport(string name, string nowCalled, InstitutionOutcome outcome, double strength, double loyalty, InstitutionQuality quality)
        {
            Name = name;
            NowCalled = nowCalled;
            Outcome = outcome;
            Strength = strength;
            Loyalty = loyalty;
            Quality = quality;
        }
    }

    /// <summary>The result of a jump: the four beats, the Echoes, and the optional Learn more facts.</summary>
    public sealed class Arrival
    {
        public int DepartureYear { get; set; }
        public int ArrivalYear { get; set; }
        public List<ArrivalBeat> Beats { get; } = new List<ArrivalBeat>();
        public List<EchoRecord> Echoes { get; } = new List<EchoRecord>();
        public Dictionary<Domain, double> SubScoresBefore { get; } = new Dictionary<Domain, double>();
        public Dictionary<Domain, double> SubScoresAfter { get; } = new Dictionary<Domain, double>();
        public double IndexBefore { get; set; }
        public double IndexAfter { get; set; }
        public List<InstitutionReport> Institutions { get; } = new List<InstitutionReport>();
        /// <summary>How far the machine carried you this time.</summary>
        public int JumpYears { get; set; }
        public List<string> Crises { get; } = new List<string>();
        public List<string> Corruption { get; } = new List<string>();
        /// <summary>Sphere Index at the end of each decade of the absence (reporting only).</summary>
        public List<double> IndexByDecade { get; } = new List<double>();
        /// <summary>Which Wrongness template was used (e.g. "medicine.low", "asHistory").</summary>
        public string WrongnessKey { get; set; } = "";
        /// <summary>Which coin line joined the Wrongness beat ("sound", "debased", "historyDebased", "historyMild").</summary>
        public string CoinKey { get; set; } = "";
        /// <summary>Economic policy left in place at departure, e.g. "coinage sound".</summary>
        public List<string> PolicyAtDeparture { get; } = new List<string>();
        /// <summary>Gold across the jump (decided 2026-09-28): carried, deposited, buried, left; and what came back.</summary>
        public double AureiCarried { get; set; }
        public double AureiDeposited { get; set; }
        public double AureiDepositReturned { get; set; }
        public double AureiBuried { get; set; }
        public double AureiHoardFound { get; set; }
        public double AureiLeft { get; set; }
        public double AureiOnArrival => AureiCarried + AureiDepositReturned + AureiHoardFound;
        public double DenariiPerUnit { get; set; } = 25;

        /// <summary>
        /// Learn more: the Index before and after, institution outcomes and the crises that struck.
        /// Facts only; no causal chains after the jump (PROTOTYPE_SCOPE).
        /// </summary>
        public string LearnMore()
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Index (100 = matches real history; geometric mean of Medicine, Governance, Economy)");
            sb.AppendLine("  Departure, AD " + DepartureYear + ": " + IndexBefore.ToString("0", ci) + "   " +
                          string.Join("  ", DomainInfo.All.Select(d => d + " " + SubScoresBefore[d].ToString("0", ci))));
            sb.AppendLine("  Arrival,   AD " + ArrivalYear + ": " + IndexAfter.ToString("0", ci) + "   " +
                          string.Join("  ", DomainInfo.All.Select(d => d + " " + SubScoresAfter[d].ToString("0", ci))));
            if (PolicyAtDeparture.Count > 0) sb.AppendLine("Economic policy you left: " + string.Join(", ", PolicyAtDeparture));
            sb.AppendLine("Institutions");
            if (Institutions.Count == 0) sb.AppendLine("  None you held 10% or more of.");
            foreach (var i in Institutions)
                sb.AppendLine("  " + Simulation.Cap(i.Name) + ": " + i.Outcome + (i.NowCalled != i.Name && i.Outcome != InstitutionOutcome.Dissolved ? " — now " + i.NowCalled : "") +
                              " (left " + Label(i.Quality) + "; strength " + i.Strength.ToString("0", ci) + ", loyalty " + i.Loyalty.ToString("0", ci) + ")" +
                              (i.HoldingsAtDeparture > 0
                                  ? "\n    Money: left " + (i.HoldingsAtDeparture * DenariiPerUnit).ToString("#,0", ci) + " denarii, paid off " + i.DebtPaidAway.ToString("0", ci) + " debt, lost " +
                                    (i.GoldLostToCorruption * DenariiPerUnit).ToString("#,0", ci) + " denarii to corruption" + (i.Corruption != CorruptionLevel.None ? " (" + i.Corruption + ")" : "") +
                                    ", " + (i.HoldingsNow * DenariiPerUnit).ToString("#,0", ci) + " left when the 30 years ended"
                                  : ""));
            sb.AppendLine("Your gold: carried " + AureiCarried.ToString("0", ci) + ", deposited " + AureiDeposited.ToString("0", ci) + " (" + AureiDepositReturned.ToString("0", ci) +
                          " back), buried " + AureiBuried.ToString("0", ci) + " (" + AureiHoardFound.ToString("0", ci) + " recovered), left behind " + AureiLeft.ToString("0", ci) +
                          "; you hold " + AureiOnArrival.ToString("0", ci) + " aurei now.");
            sb.AppendLine("Crises while you were away");
            if (Crises.Count == 0) sb.AppendLine("  None recorded.");
            foreach (var c in Crises) sb.AppendLine("  " + c);
            if (Corruption.Count > 0)
            {
                sb.AppendLine("Corruption");
                foreach (var c in Corruption) sb.AppendLine("  " + c);
            }
            return sb.ToString();
        }

        private static string Label(InstitutionQuality q) =>
            q == InstitutionQuality.Strong ? "strong" : q == InstitutionQuality.CharteredAndEndowed ? "chartered and endowed" : "bare";
    }
}
