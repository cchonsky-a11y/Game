using System;
using System.Collections.Generic;

namespace Butterfly.Core
{
    /// <summary>
    /// Player-facing information architecture for P1. Systems may be deep under the hood;
    /// the player should not have to browse every system at the same level at once.
    /// </summary>
    public enum MenuSection
    {
        Now,
        Projects,
        People,
        Institutions,
        Knowledge,
        Civilization,
        Machine,
        Journal
    }

    /// <summary>Meaningful scene families used by the P1 pacing router.</summary>
    public enum SceneCategory
    {
        Engineering,
        Personal,
        RomanLife,
        WorkEconomy,
        MachineMystery,
        CityHistory,
        Exploration,
        InstitutionsPolitics
    }

    /// <summary>
    /// Tracks recent meaningful scenes. After two consecutive scenes of one category,
    /// the router should strongly deprioritize a third unless the player explicitly
    /// chose to remain focused on that category. This does not forbid world interruptions.
    /// </summary>
    public sealed class ScenePacingState
    {
        /// <summary>Consecutive scenes of one category before a further one is deprioritized (tuning: scenes.consecutiveSoftCap).</summary>
        public int ConsecutiveSceneSoftCap { get; }

        public ScenePacingState(int consecutiveSceneSoftCap)
        {
            if (consecutiveSceneSoftCap < 1) throw new ArgumentOutOfRangeException(nameof(consecutiveSceneSoftCap));
            ConsecutiveSceneSoftCap = consecutiveSceneSoftCap;
        }

        public static ScenePacingState FromTuning(Tuning t) => new ScenePacingState(t.GetInt("scenes.consecutiveSoftCap"));

        public SceneCategory? LastCategory { get; private set; }
        public int ConsecutiveCount { get; private set; }

        public void Record(SceneCategory category)
        {
            if (LastCategory == category)
            {
                ConsecutiveCount++;
                return;
            }

            LastCategory = category;
            ConsecutiveCount = 1;
        }

        public bool ShouldDeprioritize(SceneCategory category, bool playerExplicitlyFocused = false)
        {
            return !playerExplicitlyFocused &&
                   LastCategory == category &&
                   ConsecutiveCount >= ConsecutiveSceneSoftCap;
        }

        public void Reset()
        {
            LastCategory = null;
            ConsecutiveCount = 0;
        }
    }

    /// <summary>How a substantial project is financed or exchanged.</summary>
    public enum ProjectFundingModel
    {
        ClientPaid,
        SharedDevelopment,
        SelfFundedResearch,
        Favor,
        ProfitShare
    }

    /// <summary>
    /// Generic project progression. Not every project must visit every stage, but substantial
    /// technology work should normally progress beyond a one-off prototype before it counts as
    /// a durable civilizational capability.
    /// </summary>
    public enum ProjectStage
    {
        Proposed,
        Agreed,
        Observation,
        Prototype,
        Failure,
        Refinement,
        CraftAdaptation,
        Repeatability,
        Adoption,
        Spread,
        Complete,
        Abandoned
    }

    /// <summary>
    /// Explicit commercial terms for P1 projects. A substantial project must state who is paying,
    /// who carries material cost, or that the inventor is deliberately self-funding the work.
    /// </summary>
    public sealed class ProjectTerms
    {
        public ProjectFundingModel FundingModel { get; }
        public string Payer { get; }
        public string MaterialsPayer { get; }
        public double UpfrontGold { get; }
        public double CompletionGold { get; }
        public double PlayerMaterialCost { get; }
        public double ProfitShare { get; }
        public string NonCashConsideration { get; }

        public ProjectTerms(
            ProjectFundingModel fundingModel,
            string payer,
            string materialsPayer,
            double upfrontGold = 0,
            double completionGold = 0,
            double playerMaterialCost = 0,
            double profitShare = 0,
            string nonCashConsideration = "")
        {
            if (upfrontGold < 0) throw new ArgumentOutOfRangeException(nameof(upfrontGold));
            if (completionGold < 0) throw new ArgumentOutOfRangeException(nameof(completionGold));
            if (playerMaterialCost < 0) throw new ArgumentOutOfRangeException(nameof(playerMaterialCost));
            if (profitShare < 0 || profitShare > 1) throw new ArgumentOutOfRangeException(nameof(profitShare));

            FundingModel = fundingModel;
            Payer = payer ?? string.Empty;
            MaterialsPayer = materialsPayer ?? string.Empty;
            UpfrontGold = upfrontGold;
            CompletionGold = completionGold;
            PlayerMaterialCost = playerMaterialCost;
            ProfitShare = profitShare;
            NonCashConsideration = nonCashConsideration ?? string.Empty;
        }
    }

    /// <summary>
    /// P1 project state shared by commissions, experiments, Grand Challenges and institutional work.
    /// Existing P0 project types can migrate onto this model incrementally.
    /// </summary>
    public sealed class ProjectState
    {
        public string Id { get; }
        public string Title { get; }
        public string Purpose { get; }
        public string OwnerOrClient { get; }
        public ProjectTerms Terms { get; }
        public int DurationMonths { get; }
        public int MonthsRemaining { get; private set; }
        public ProjectStage Stage { get; private set; }
        public bool CanContinueWithoutPlayer { get; set; }
        public List<string> Collaborators { get; } = new List<string>();
        public List<string> Dependencies { get; } = new List<string>();

        public bool IsFinished => Stage == ProjectStage.Complete || Stage == ProjectStage.Abandoned;

        public ProjectState(
            string id,
            string title,
            string purpose,
            string ownerOrClient,
            ProjectTerms terms,
            int durationMonths,
            ProjectStage stage = ProjectStage.Proposed)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Project id is required.", nameof(id));
            if (durationMonths < 0) throw new ArgumentOutOfRangeException(nameof(durationMonths));

            Id = id;
            Title = title ?? string.Empty;
            Purpose = purpose ?? string.Empty;
            OwnerOrClient = ownerOrClient ?? string.Empty;
            Terms = terms ?? throw new ArgumentNullException(nameof(terms));
            DurationMonths = durationMonths;
            MonthsRemaining = durationMonths;
            Stage = stage;
        }

        public void SetStage(ProjectStage stage)
        {
            if (IsFinished) throw new InvalidOperationException("A finished project cannot change stage.");
            Stage = stage;
        }

        public void AdvanceMonth()
        {
            if (IsFinished || MonthsRemaining == 0) return;
            MonthsRemaining--;
        }

        public void Complete()
        {
            MonthsRemaining = 0;
            Stage = ProjectStage.Complete;
        }

        public void Abandon()
        {
            Stage = ProjectStage.Abandoned;
        }
    }

    /// <summary>Relationship-first access path for consequential institutions.</summary>
    public enum InstitutionAccessStage
    {
        Unaware,
        Aware,
        KnowsMember,
        Guest,
        InvitedBack,
        SponsoredCandidate,
        Member,
        Officer
    }

    /// <summary>
    /// Evidence required before a consequential institution may invite the player forward.
    /// General fame is intentionally not part of the gate.
    /// </summary>
    public sealed class InstitutionInvitationContext
    {
        public string InstitutionId { get; }
        public string InviterId { get; }
        public bool HasExistingRelationship { get; }
        public bool HasRelevantWork { get; }
        public bool DemonstratedUsefulness { get; }
        public bool InviterAcceptsSocialRisk { get; }

        public bool IsWarranted =>
            !string.IsNullOrWhiteSpace(InviterId) &&
            HasExistingRelationship &&
            HasRelevantWork &&
            DemonstratedUsefulness &&
            InviterAcceptsSocialRisk;

        public InstitutionInvitationContext(
            string institutionId,
            string inviterId,
            bool hasExistingRelationship,
            bool hasRelevantWork,
            bool demonstratedUsefulness,
            bool inviterAcceptsSocialRisk)
        {
            InstitutionId = institutionId ?? string.Empty;
            InviterId = inviterId ?? string.Empty;
            HasExistingRelationship = hasExistingRelationship;
            HasRelevantWork = hasRelevantWork;
            DemonstratedUsefulness = demonstratedUsefulness;
            InviterAcceptsSocialRisk = inviterAcceptsSocialRisk;
        }
    }
}
