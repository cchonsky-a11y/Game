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

        /// <summary>Challenges whose question the work has raised, waiting for the scene router.</summary>
        private IEnumerable<ChallengeState> ChallengesDue() =>
            World.Challenges.Where(c => c.Status == ChallengeStatus.NotYet && OpeningRoute(c) != null);

        /// <summary>
        /// The route that raised the question: the first one that became eligible, once remembered; until then the first
        /// route (in authored order) whose requirements all hold now, or null.
        /// </summary>
        public ChallengeRoute? OpeningRoute(ChallengeState c)
        {
            var routes = ChallengeDefOf(c).Routes;
            if (c.FirstEligibleRoute.Length > 0) return routes.First(r => r.Id == c.FirstEligibleRoute);
            return routes.FirstOrDefault(r => r.Requires.All(Holds));
        }

        /// <summary>
        /// Each month, before routing, a waiting challenge remembers the first route that has become eligible (strict route
        /// causality, Corey 2026-10-04). Routes that first hold in the same month are settled by authored order; no chance
        /// is involved, and a later route never replaces the one remembered.
        /// </summary>
        private void NoteEligibleRoutes()
        {
            foreach (var c in World.Challenges.Where(c => c.Status == ChallengeStatus.NotYet && c.FirstEligibleRoute.Length == 0))
            {
                var route = OpeningRoute(c);
                if (route == null) continue;
                c.FirstEligibleRoute = route.Id;
                c.FirstEligibleTurn = Turn;
            }
        }

        private void OpenChallenge(ChallengeState c, ChallengeRoute route)
        {
            if (c.Status != ChallengeStatus.NotYet) return;
            var d = ChallengeDefOf(c);
            c.Status = ChallengeStatus.Open;
            c.OpenedBy = route.Id;
            World.ScenePacing.Record(route.Category);
            Record("challenge.open", c.ProjectId, null, new[] { "player" }, null,
                route.Text + " Grand Challenge: " + d.Name + ". " + d.Question + " (challenge " + d.Id + ")");
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
            if (!s.Needs.All(Holds)) return s.NeedsText;
            if (s.NeedsPerson.Length > 0 && StagePerson(s) == null)
            {
                var first = s.NeedsPerson.Split('|')[0];
                return Knows(first) ? PersonDefOf(first)!.Name + " is laid up or away; this waits." : s.NeedsText;
            }
            return CapabilityBlocker(s.Capability, s.To);
        }

        /// <summary>Who does the stage's work: the first person named who is known and here, or null.</summary>
        public string? StagePerson(ChallengeStageDef s) =>
            s.NeedsPerson.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(p => Knows(p) && !IsPersonAway(p));

        /// <summary>The stage's months: longer when the first person named isn't there and another stands in.</summary>
        public int StageMonths(ChallengeStageDef s) =>
            s.Months + (s.SlowerWithout > 0 && StagePerson(s) is string who && who != s.NeedsPerson.Split('|')[0] ? s.SlowerWithout : 0);

        /// <summary>The next stage in plain words: what it costs, what holds it up, what the resource costs without its source.</summary>
        public string StageLine(ChallengeStageDef s)
        {
            string line = s.Name + ": " + StageMonths(s) + " month(s), " + s.Attention + " Attention a month, " + Money(StageGold(s));
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
            int months = StageMonths(s);
            var attention = CheckAttention(s.Attention, months);
            if (attention != null) return attention;
            SpendAttention(s.Attention);
            if (!World.Projects.Any(p => p.Id == c.ProjectId))
                World.Projects.Add(new ProjectState(c.ProjectId, d.Name, d.Question, "player",
                    new ProjectTerms(ProjectFundingModel.SelfFundedResearch, "player", "player", 0, 0), d.Stages.Sum(x => x.Months), ProjectStage.Agreed));
            World.Projects.First(p => p.Id == c.ProjectId).SetStage(ProjectStageFor(s.To));
            double before = World.Gold;
            World.Gold -= gold;
            c.Status = ChallengeStatus.Working;
            c.DoneBy = StagePerson(s) ?? "";
            c.MonthsLeft = months;
            c.ReservedFromTurn = Turn + 1;
            string resource = s.Resource == null ? "" : HasSource(s) ? " " + Cap(s.Resource.Name) + " comes through your friends." : " " + s.Resource.Note;
            if (months > s.Months && s.SlowerText.Length > 0) resource += " " + s.SlowerText;
            Record("challenge.materials", c.ProjectId, null, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                "You begin: " + s.Name.ToLowerInvariant() + ", self-funded, " + Money(gold) + " for materials and wages." + resource);
            return CommandResult.Success("You begin " + s.Name.ToLowerInvariant() + " (" + months + (months == 1 ? " month; " : " months; ") + Money(gold) + ")." + resource);
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
                bool standIn = s.StandInText.Length > 0 && c.DoneBy.Length > 0 && c.DoneBy != s.NeedsPerson.Split('|')[0];
                var actors = c.DoneBy.Length > 0 ? new[] { "player", c.DoneBy } : new[] { "player" };
                var done = Record("challenge.stage", c.ProjectId, null, actors, null, standIn ? s.StandInText : s.Text);
                AdvanceCapability(s.Capability, s.To, new[] { done.Id }, "Rome's " + CapabilityDefOf(s.Capability)!.Name + " now stand at " + s.To.ToString().ToLowerInvariant() + ".");
                foreach (var kv in s.Also)
                    AdvanceCapability(kv.Key, kv.Value, new[] { done.Id }, "Rome's " + CapabilityDefOf(kv.Key)!.Name + " now stand at " + kv.Value.ToString().ToLowerInvariant() + ".");
                c.StageIndex++;
                c.Status = ChallengeStatus.Open;
                if (CapabilityLevelOf(d.GoalCapability) >= d.GoalLevel || c.StageIndex >= d.Stages.Count)
                {
                    c.Status = ChallengeStatus.Done;
                    World.Projects.First(p => p.Id == c.ProjectId).Complete();
                    Record("challenge.complete", c.ProjectId, new[] { done.Id }, new[] { "player" }, null, d.CompleteText);
                    // The human consequence follows: not "output +20%", a person asking what it means (Corey, 2026-10-04).
                    if (d.Consequence.Length > 0) World.TriggeredEvents.Add(d.Consequence);
                }
            }
        }

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
