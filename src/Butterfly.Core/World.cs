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
        /// <summary>Prices relative to AD 155 (1 = unchanged); rises with the debasement of the coin.</summary>
        public double PriceLevel { get; set; } = 1;
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

        // ---- P1 state (decided 2026-10-02; P1 Sprint 1 models, now part of the world) ----

        /// <summary>Recent meaningful scenes, for the scene router's two-in-a-row soft cap (set from tuning at start).</summary>
        public ScenePacingState ScenePacing { get; set; } = new ScenePacingState(2);
        /// <summary>Every movement of the player's money, with who paid and why (mirrors each gold effect in the log).</summary>
        public EconomyLedger Ledger { get; } = new EconomyLedger();
        /// <summary>P1 projects: commissions, experiments and Grand Challenges with explicit terms.</summary>
        public List<ProjectState> Projects { get; } = new List<ProjectState>();
        /// <summary>Relationship-first access to each institution, in institution order (aware → … → member → officer).</summary>
        public List<InstitutionAccessState> Access { get; } = new List<InstitutionAccessState>();
        /// <summary>P1 commissions, one per authored commission, in content order.</summary>
        public List<CommissionState> Commissions { get; } = new List<CommissionState>();
        /// <summary>P1 invitation paths, one per authored path.</summary>
        public List<InvitationPathState> Invitations { get; } = new List<InvitationPathState>();
        /// <summary>Rome's progress on each capability of the hidden network, in content order.</summary>
        public List<CapabilityState> Capabilities { get; } = new List<CapabilityState>();
        /// <summary>P1 Grand Challenges.</summary>
        public List<ChallengeState> Challenges { get; } = new List<ChallengeState>();
        /// <summary>P1 recurring people, whose lives go on without the player.</summary>
        public List<PersonState> People { get; } = new List<PersonState>();
        /// <summary>The log event of each life event that has happened (for causes).</summary>
        public Dictionary<string, int> LifeEventLog { get; } = new Dictionary<string, int>();

        public InstitutionAccessState AccessTo(string institutionId) => Access.First(a => a.InstitutionId == institutionId);

        public Institution Institution(string id) => Institutions.First(i => i.Def.Id == id);

        public List<ActiveProject> ActiveProjects { get; } = new List<ActiveProject>();
        /// <summary>Time machine repair steps finished, and the ones under way (P0 repair track).</summary>
        public List<string> MachineDone { get; } = new List<string>();
        public List<ActiveMachineStep> ActiveMachineSteps { get; } = new List<ActiveMachineStep>();
        /// <summary>Gold put back into the machine so far (decided 2026-09-28: all you scavenged must go back).</summary>
        public double MachineGoldRestored { get; set; }
        /// <summary>Gold aurei in your purse (decided 2026-09-28): they hold their value; changing them costs a fee.</summary>
        public double Aurei { get; set; }
        /// <summary>Gold kept for the jump (decided 2026-09-28): deposited with the banking house, or buried.</summary>
        public double DepositAurei { get; set; }
        public double HoardAurei { get; set; }
        /// <summary>Inventions finished and under way; what they pay you each year; the extra pay they add to consulting.</summary>
        public List<string> Invented { get; } = new List<string>();
        public List<ActiveInvention> ActiveInventions { get; } = new List<ActiveInvention>();
        public double InventionIncome { get; set; }
        public double ConsultBonus { get; set; }
        /// <summary>Workshop inventions: the workshop's income is this much higher (decided 2026-09-28).</summary>
        public double WorkshopBonus { get; set; }
        /// <summary>The workshop (P0-34): free, paid apprentices; the smith's regard for you (0-100); orders taken.</summary>
        public int Apprentices { get; set; }
        public double SmithRegard { get; set; }
        public int OrdersTaken { get; set; }
        /// <summary>The workshop's size (1 smithy .. 4 foundry; 0 before you own it) and a step up under way.</summary>
        public int WorkshopSize { get; set; }
        public int WorkshopBuildingTo { get; set; }
        public int WorkshopBuildTurns { get; set; }
        public List<string> CompletedProjects { get; } = new List<string>();
        /// <summary>What your answers to Rome's choices left for later ones (P0-33: answers stack), e.g. "suburaTrust", "cartel".</summary>
        public HashSet<string> Flags { get; } = new HashSet<string>();
        /// <summary>Advocacy (decided 2026-09-28): stances you pushed without a voice, carried only while you're in Rome.</summary>
        public bool Advocating { get; set; }

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
        /// <summary>When you first joined (fractional year); seniority and the newcomer premium count from here.</summary>
        public double JoinedAt { get; set; }
        /// <summary>True if you failed to pay what you owed it at some point this year (no seniority this year).</summary>
        public bool MissedDuesThisYear { get; set; }
        /// <summary>Turn you last attended its meetings, and how many you attended this year.</summary>
        public int AttendedTurn { get; set; }
        public int MeetingsThisYear { get; set; }
        /// <summary>Grievances or goodwill toward you from before you joined (P0-31); added to your starting loyalty when you join.</summary>
        public double Regard { get; set; }
        /// <summary>Which camp leads (P0-32): +1 its first drift path, −1 its second; 0 undecided.</summary>
        public double Lean { get; set; }
        /// <summary>Your office (P0-32): -1 none, 0 member, 1 officer, 2 deputy, 3 head.</summary>
        public int Rank { get; set; } = -1;
        /// <summary>An office you have been offered and not yet answered (0 = none).</summary>
        public int OfferedRank { get; set; }
        /// <summary>Your votes at meetings for each camp.</summary>
        public int[] Votes { get; } = new int[2];
        /// <summary>Your last orders: the camp you back (-1 none) and the successor you name (-1 none).</summary>
        public int OrderCamp { get; set; } = -1;
        public int OrderSuccessor { get; set; } = -1;
        /// <summary>How much your last orders weigh, set at departure.</summary>
        public double OrderForce { get; set; }
        /// <summary>The office you held when you first left ("" if none).</summary>
        public string DepartureOffice { get; set; } = "";
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
            Integrity = def.LeaderIntegrity;
        }

        /// <summary>The current leader's integrity (honest, average, venal): the founding leader's, or a successor's (P0-32).</summary>
        public string Integrity { get; set; }

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
            TurnsRemaining = def.DurationMonths;
            StartEventId = startEventId;
        }
    }

    /// <summary>A machine repair step in progress.</summary>
    public sealed class ActiveMachineStep
    {
        public MachineStepDef Def { get; }
        public int TurnsRemaining { get; set; }
        /// <summary>True if you paid the gold instead of getting help from Rome.</summary>
        public bool WithoutRome { get; set; }
        public int StartEventId { get; }

        public ActiveMachineStep(MachineStepDef def, int startEventId)
        {
            Def = def;
            TurnsRemaining = def.DurationMonths;
            StartEventId = startEventId;
        }
    }

    /// <summary>An invention in progress.</summary>
    public sealed class ActiveInvention
    {
        public InventionDef Def { get; }
        public int TurnsRemaining { get; set; }
        public int StartEventId { get; }

        public ActiveInvention(InventionDef def, int startEventId)
        {
            Def = def;
            TurnsRemaining = def.DurationMonths;
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
