using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>All mutable state of the single P0 region (Rome) and the inventor.</summary>
    public sealed class World
    {
        public List<DomainState> Domains { get; } = new List<DomainState>();

        public double Gold { get; set; }
        /// <summary>Extra yearly income from completed projects.</summary>
        public double IncomeBonus { get; set; }
        /// <summary>Plague resilience from completed projects (quarantine rules, census).</summary>
        public double PlagueResilienceBonus { get; set; }
        /// <summary>True once the district fountain has been repaired.</summary>
        public bool CleanWater { get; set; }
        /// <summary>Condition of the repaired fountain (0–100); tracked for the arrival.</summary>
        public double FountainCondition { get; set; }

        /// <summary>Population of Rome in thousands (flavor and plague toll).</summary>
        public double Population { get; set; }

        public PlagueState Plague { get; } = new PlagueState();
        /// <summary>Year of the most recent plague outbreak (the Antonine plague or a recurrence); 0 if none.</summary>
        public int LastOutbreakYear { get; set; }
        /// <summary>Stance per economic policy issue: +1 Austrian, 0 as history, −1 interventionist.</summary>
        public int[] Policy { get; } = new int[4];
        /// <summary>Ventures that only pay while an intervention-driven boom lasts.</summary>
        public double Malinvestment { get; set; }
        public BustState Bust { get; } = new BustState();
        /// <summary>Per domain: level − historical value at departure (sets the long-run target during the absence).</summary>
        public double[] DepartureDeviation { get; } = new double[3];

        /// <summary>Attention left this turn (SYSTEMS §3: 4 per turn, never scales).</summary>
        public int Attention { get; set; }
        /// <summary>Times you consulted for a wealthy household (a way into the Caecilian faction).</summary>
        public int ConsultJobs { get; set; }
        /// <summary>Turn on which the one personal action was last used.</summary>
        public int PersonalActionTurn { get; set; }

        /// <summary>The hour-one seeded choice: "fountain", "workshop", "neither", or null while open.</summary>
        public string? SeededChoice { get; set; }
        public int SeededChoiceEventId { get; set; }

        public PromiseState Promise { get; } = new PromiseState();

        public List<Commitment> Commitments { get; } = new List<Commitment>();

        public List<Institution> Institutions { get; } = new List<Institution>();

        public Institution Institution(string id) => Institutions.First(i => i.Def.Id == id);

        public List<ActiveProject> ActiveProjects { get; } = new List<ActiveProject>();
        public List<string> CompletedProjects { get; } = new List<string>();

        /// <summary>Per-domain fraction of this year's upkeep actually paid (summed per turn).</summary>
        public double[] UpkeepPaidThisYear { get; } = new double[3];
        public int UpkeepTurnsThisYear { get; set; }

        public DomainState this[Domain d] => Domains.First(x => x.Domain == d);
    }

    /// <summary>Plague stages: 0 quiet, 1–3 visible warnings, 4 outbreak, 5 passed.</summary>
    public sealed class PlagueState
    {
        public const int Quiet = 0;
        public const int Outbreak = 4;
        public const int Passed = 5;

        public int Stage { get; set; }
        public int StageEnteredYear { get; set; }
        public int LastStageEventId { get; set; }
        /// <summary>Year the first warning appears, drawn from the seeded generator at start.</summary>
        public int FirstWarningYear { get; set; }
        public int OutbreakYear { get; set; }
        public double Severity { get; set; }
        public double Deaths { get; set; }
        /// <summary>Contained, severe or catastrophic, by share of the population dead.</summary>
        public string SeverityLabel { get; set; } = "";
        /// <summary>Chosen response to the outbreak, or null while undecided.</summary>
        public string? Response { get; set; }
        public int ResponseEventId { get; set; }
        public string? Opening { get; set; }
        /// <summary>True if the outbreak struck while the inventor was away.</summary>
        public bool StruckInAbsence { get; set; }

        public bool IsWarning => Stage >= 1 && Stage <= 3;
    }

    public enum PromiseStatus
    {
        NotOffered,
        Offered,
        Active,
        Refused,
        Kept,
        Broken
    }

    /// <summary>The one P0 promise: an institution leader asks the inventor to stay through the plague.</summary>
    public sealed class PromiseState
    {
        public PromiseStatus Status { get; set; }
        /// <summary>True if the promise lapsed because it was never answered (text only).</summary>
        public bool Unanswered { get; set; }
        public int OfferEventId { get; set; }
        public int LastEventId { get; set; }
    }

    /// <summary>A multi-turn commitment of Attention (GDD §7, tested in P0).</summary>
    public sealed class Commitment
    {
        public string Id { get; }
        public string InstitutionId { get; }
        public int TurnsRemaining { get; set; }
        public int StartEventId { get; }

        public Commitment(string id, string institutionId, int turns, int startEventId)
        {
            Id = id;
            InstitutionId = institutionId;
            TurnsRemaining = turns;
            StartEventId = startEventId;
        }
    }

    /// <summary>Institution quality, which sets decay per decade during absence (SYSTEMS §7).</summary>
    public enum InstitutionQuality
    {
        Bare,
        CharteredAndEndowed,
        Strong
    }

    /// <summary>Arrival outcomes (SYSTEMS §7).</summary>
    public enum InstitutionOutcome
    {
        /// <summary>You never held an influential stake in it.</summary>
        NotBacked,
        Thriving,
        Drifted,
        Captured,
        Dissolved,
        Rogue
    }

    /// <summary>Corruption severity during the absence (worst so far).</summary>
    public enum CorruptionLevel
    {
        None,
        Minor,
        Major,
        Total
    }

    /// <summary>Runtime state of one institution: an established one you can buy into, or one you found yourself.</summary>
    public sealed class Institution
    {
        public InstitutionDef Def { get; }
        /// <summary>True while the institution exists in Rome (established ones from the start; yours once founded, until it collapses).</summary>
        public bool Exists { get; set; }
        /// <summary>Your share of it, 0–1 (decided 2026-09-27): 10% counts toward influence, 25% gives a voice, 50% control.</summary>
        public double Stake { get; set; }
        /// <summary>True if you hold any stake in an existing institution.</summary>
        public bool Backed => Exists && Stake > 0;
        /// <summary>True if one of your own institutions collapsed before it was established.</summary>
        public bool Collapsed { get; set; }
        /// <summary>An established institution's share of its domain at the start; rivals push back when it grows past this.</summary>
        public double BaselineShare { get; set; }
        public double Strength { get; set; }
        public double Loyalty { get; set; }
        public bool Chartered { get; set; }
        public bool Endowed { get; set; }
        public string Leader { get; set; }
        /// <summary>Accumulated drift away from the founding identity during absence.</summary>
        public double Drift { get; set; }
        /// <summary>The pre-authored drift path this institution is heading down (chosen at departure).</summary>
        public DriftPathDef? DriftPath { get; set; }
        public bool HasDrifted { get; set; }
        public InstitutionQuality Quality { get; set; }
        public InstitutionOutcome Outcome { get; set; }
        public int OverseenTurn { get; set; }

        /// <summary>Gold the institution holds (from endowments).</summary>
        public double Holdings { get; set; }
        public double HoldingsAtDeparture { get; set; }
        /// <summary>An audit charter halves the corruption hazard and softens its severity.</summary>
        public bool AuditCharter { get; set; }
        public CorruptionLevel Corruption { get; set; }
        /// <summary>Outcome imposed by total corruption (Captured or Rogue), if any.</summary>
        public InstitutionOutcome? ForcedOutcome { get; set; }
        /// <summary>Debt points this institution paid down while the inventor was away.</summary>
        public double DebtPaidAway { get; set; }
        public double GoldLostToCorruption { get; set; }

        public Institution(InstitutionDef def)
        {
            Def = def;
            Leader = def.Leader;
        }

        public string Key => Def.Id;
    }

    /// <summary>The boom-bust track: 0 quiet, 1–3 visible warnings, then the bust.</summary>
    public sealed class BustState
    {
        public int Stage { get; set; }
        public int StageEnteredYear { get; set; }
        public int LastEventId { get; set; }
        public int Busts { get; set; }
    }

    /// <summary>A project in progress: its definition and the turns of work still needed.</summary>
    public sealed class ActiveProject
    {
        public ProjectDef Def { get; }
        public int TurnsRemaining { get; set; }
        public int StartEventId { get; }

        public ActiveProject(ProjectDef def, int startEventId)
        {
            Def = def;
            TurnsRemaining = def.Turns;
            StartEventId = startEventId;
        }
    }

    /// <summary>Outcome of a player command: success flag and a message for the player.</summary>
    public sealed class CommandResult
    {
        public bool Ok { get; }
        public string Message { get; }

        private CommandResult(bool ok, string message)
        {
            Ok = ok;
            Message = message;
        }

        public static CommandResult Success(string message) => new CommandResult(true, message);
        public static CommandResult Fail(string message) => new CommandResult(false, message);
    }
}
