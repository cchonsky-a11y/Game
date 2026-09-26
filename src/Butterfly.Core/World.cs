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

        /// <summary>Population of Rome in thousands (flavor and plague toll).</summary>
        public double Population { get; set; }

        public PlagueState Plague { get; } = new PlagueState();

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
