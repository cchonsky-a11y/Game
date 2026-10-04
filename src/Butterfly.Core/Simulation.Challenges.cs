using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P1 Grand Challenges (decided 2026-10-02; master handoff §14): small problems prove principles, big projects change
    /// capability. A challenge opens when the work has raised its question; the player starts each chunky stage (gold paid,
    /// Attention held each month), and a finished stage moves its capability. A stage can be blocked by people (no one to
    /// vouch for you), by what Rome can do (knowing is not making), or by a resource: without its source it costs more
    /// (PROPOSED P1-10). Once the bottleneck is understood there are no micro-scenes, only the next stage.
    /// </summary>
    public sealed partial class Simulation
    {
        public ChallengeDef ChallengeDefOf(ChallengeState c) => Data.Content.Challenges.First(d => d.Id == c.Id);
        public ChallengeState? FindChallenge(string id) => World.Challenges.FirstOrDefault(c => c.Id == (id ?? "").Trim().ToLowerInvariant());

        private void InitChallenges()
        {
            foreach (var d in Data.Content.Challenges) World.Challenges.Add(new ChallengeState(d.Id));
        }

        /// <summary>At the start of each month: a challenge whose question the work has raised opens.</summary>
        private void OpenChallengesDue()
        {
            foreach (var c in World.Challenges.Where(c => c.Status == ChallengeStatus.NotYet))
            {
                var d = ChallengeDefOf(c);
                if (!d.Opens.All(Holds)) continue;
                c.Status = ChallengeStatus.Open;
                World.ScenePacing.Record(d.OpenCategory);
                Record("challenge.open", c.ProjectId, null, new[] { "player" }, null,
                    d.OpenText + " Grand Challenge: " + d.Name + ". " + d.Question + " (challenge " + d.Id + ")");
            }
        }

        public ChallengeStageDef? NextStage(ChallengeState c) =>
            c.StageIndex < ChallengeDefOf(c).Stages.Count ? ChallengeDefOf(c).Stages[c.StageIndex] : null;

        /// <summary>True if the stage's resource comes through its source; otherwise it costs more.</summary>
        public bool HasSource(ChallengeStageDef s) => s.Resource == null || s.Resource.Source.All(Holds);

        /// <summary>The gold a stage costs now (price level, and the resource premium without its source).</summary>
        public double StageGold(ChallengeStageDef s) => Priced(s.Gold) * (HasSource(s) ? 1 : s.Resource!.WithoutSource);

        /// <summary>Why the next stage can't start (people, capability), or null. Gold and Attention are checked when starting.</summary>
        public string? StageBlocker(ChallengeStageDef s)
        {
            if (!s.Needs.All(Holds) || s.NeedsPerson.Length > 0 && !Knows(s.NeedsPerson)) return s.NeedsText;
            if (s.NeedsPerson.Length > 0 && IsPersonAway(s.NeedsPerson)) return s.NeedsPerson + " is laid up or away; this waits for him.";
            return CapabilityBlocker(s.Capability, s.To);
        }

        /// <summary>The next stage in plain words: what it costs, what holds it up, what the resource costs without its source.</summary>
        public string StageLine(ChallengeStageDef s)
        {
            string line = s.Name + ": " + s.Months + " month(s), " + s.Attention + " Attention a month, " + Money(StageGold(s));
            if (s.Resource != null) line += HasSource(s) ? " (" + s.Resource.Name + " at the usual price)" : " (" + s.Resource.Name + " at a stranger's price: " + s.Resource.Note + ")";
            var blocker = StageBlocker(s);
            return blocker == null ? line : line + ". Blocked: " + blocker;
        }

        /// <summary>Starts the next stage: the gold is paid, the Attention held each month from the next.</summary>
        public CommandResult StartChallengeStage(string id)
        {
            var c = FindChallenge(id);
            if (c == null || c.Status == ChallengeStatus.NotYet) return CommandResult.Fail("No such challenge has come up yet.");
            if (c.Status == ChallengeStatus.Working) return CommandResult.Fail("A stage is already under way (" + c.MonthsLeft + " month(s) left).");
            if (c.Status != ChallengeStatus.Open) return CommandResult.Fail("That challenge is over.");
            var d = ChallengeDefOf(c);
            var s = NextStage(c)!;
            var blocker = StageBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);
            double gold = StageGold(s);
            if (World.Gold < gold) return CommandResult.Fail("This stage needs " + Money(gold) + "; you have " + Money(World.Gold) + ".");
            var attention = CheckAttention(s.Attention, s.Months);
            if (attention != null) return attention;
            SpendAttention(s.Attention);
            if (!World.Projects.Any(p => p.Id == c.ProjectId))
                World.Projects.Add(new ProjectState(c.ProjectId, d.Name, d.Question, "player",
                    new ProjectTerms(ProjectFundingModel.SelfFundedResearch, "player", "player", 0, 0), d.Stages.Sum(x => x.Months), ProjectStage.Agreed));
            World.Projects.First(p => p.Id == c.ProjectId).SetStage(ProjectStageFor(s.To));
            double before = World.Gold;
            World.Gold -= gold;
            c.Status = ChallengeStatus.Working;
            c.MonthsLeft = s.Months;
            c.ReservedFromTurn = Turn + 1;
            string resource = s.Resource == null ? "" : HasSource(s) ? " " + Cap(s.Resource.Name) + " comes through your friends." : " " + s.Resource.Note;
            Record("challenge.materials", c.ProjectId, null, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                "You begin: " + s.Name.ToLowerInvariant() + ", self-funded, " + Money(gold) + " for materials and wages." + resource);
            return CommandResult.Success("You begin " + s.Name.ToLowerInvariant() + " (" + s.Months + " months; " + Money(gold) + ")." + resource);
        }

        private static ProjectStage ProjectStageFor(CapabilityLevel to) =>
            to <= CapabilityLevel.Prototype ? ProjectStage.Prototype : to == CapabilityLevel.Reproducible ? ProjectStage.Repeatability : ProjectStage.Adoption;

        /// <summary>At the end of each month: a stage under way advances; when it ends, its capability moves.</summary>
        private void ProgressChallenges()
        {
            foreach (var c in World.Challenges.Where(c => c.Status == ChallengeStatus.Working).ToList())
            {
                World.Projects.First(p => p.Id == c.ProjectId).AdvanceMonth();
                if (--c.MonthsLeft > 0) continue;
                var d = ChallengeDefOf(c);
                var s = d.Stages[c.StageIndex];
                World.ScenePacing.Record(s.Category);
                var done = Record("challenge.stage", c.ProjectId, null, new[] { "player" }, null, s.Text);
                AdvanceCapability(s.Capability, s.To, new[] { done.Id }, "Rome's " + CapabilityDefOf(s.Capability)!.Name + " now stand at " + s.To.ToString().ToLowerInvariant() + ".");
                c.StageIndex++;
                c.Status = ChallengeStatus.Open;
                if (CapabilityLevelOf(d.GoalCapability) >= d.GoalLevel || c.StageIndex >= d.Stages.Count)
                {
                    c.Status = ChallengeStatus.Done;
                    World.Projects.First(p => p.Id == c.ProjectId).Complete();
                    Record("challenge.complete", c.ProjectId, new[] { done.Id }, new[] { "player" }, null, d.CompleteText);
                }
            }
        }

        private int ReservedChallengeAttention() =>
            World.Challenges.Where(c => c.Status == ChallengeStatus.Working && Turn >= c.ReservedFromTurn).Sum(c => NextStage(c)!.Attention);

        private int ChallengeAttentionInMonth(int k) =>
            World.Challenges.Where(c => c.Status == ChallengeStatus.Working && c.MonthsLeft > k).Sum(c => NextStage(c)!.Attention);

        /// <summary>Leaving Rome: a stage under way is abandoned; the challenge stays where it got to.</summary>
        private void AbandonChallengesOnDeparture(int departId)
        {
            foreach (var c in World.Challenges.Where(c => c.Status == ChallengeStatus.Working))
            {
                c.Status = ChallengeStatus.Abandoned;
                World.Projects.First(p => p.Id == c.ProjectId).Abandon();
                Record("challenge.abandoned", c.ProjectId, new[] { departId }, new[] { "player" }, null,
                    NextStage(c)!.Name + " is left unfinished.");
            }
        }
    }
}
