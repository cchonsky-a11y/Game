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

        /// <summary>Attention left this turn (SYSTEMS §3: 4 per turn, never scales).</summary>
        public int Attention { get; set; }
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
        NotFounded,
        Thriving,
        Drifted,
        Captured,
        Dissolved,
        Rogue
    }

    /// <summary>Runtime state of one of the two P0 institutions.</summary>
    public sealed class Institution
    {
        public InstitutionDef Def { get; }
        public bool Founded { get; set; }
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

        public Institution(InstitutionDef def)
        {
            Def = def;
            Leader = def.Leader;
        }

        public string Key => Def.Id;
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
