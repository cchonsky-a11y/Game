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

        public List<ActiveProject> ActiveProjects { get; } = new List<ActiveProject>();
        public List<string> CompletedProjects { get; } = new List<string>();

        /// <summary>Per-domain fraction of this year's upkeep actually paid (summed per turn).</summary>
        public double[] UpkeepPaidThisYear { get; } = new double[3];
        public int UpkeepTurnsThisYear { get; set; }

        public DomainState this[Domain d] => Domains.First(x => x.Domain == d);
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
