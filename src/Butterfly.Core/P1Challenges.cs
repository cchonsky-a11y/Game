using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>A resource a stage needs that Rome doesn't hand a stranger: cheap through its source, dearer without.</summary>
    public sealed class ChallengeResourceDef
    {
        public string Name { get; }
        public IReadOnlyList<string> Source { get; }
        public double WithoutSource { get; }
        public string Note { get; }

        public ChallengeResourceDef(JsonObject o)
        {
            Name = o.Str("name");
            Source = o.Arr("source").Cast<string>().ToList();
            WithoutSource = o.Num("withoutSource");
            Note = o.Str("note");
        }
    }

    /// <summary>One stage of a Grand Challenge: a chunky project that moves a capability.</summary>
    public sealed class ChallengeStageDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Capability { get; }
        public CapabilityLevel To { get; }
        public int Months { get; }
        public int Attention { get; }
        public double Gold { get; }
        public SceneCategory Category { get; }
        public ChallengeResourceDef? Resource { get; }
        public IReadOnlyList<string> Needs { get; }
        public string NeedsPerson { get; }
        public string NeedsText { get; }
        public string Text { get; }

        public ChallengeStageDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Capability = o.Str("capability");
            To = Enum.TryParse<CapabilityLevel>(o.Str("to"), out var l) ? l : throw new FormatException("Unknown capability level: " + o.Str("to"));
            Months = (int)o.Num("months");
            Attention = (int)o.Num("attention");
            Gold = o.Num("gold");
            Category = CommissionSceneDef.ParseCategory(o.Str("category"));
            Resource = o.Has("resource") ? new ChallengeResourceDef(o.Obj("resource")) : null;
            Needs = o.Has("needs") ? o.Arr("needs").Cast<string>().ToList() : new List<string>();
            NeedsPerson = o.StrOr("needsPerson", "") ?? "";
            NeedsText = o.StrOr("needsText", "") ?? "";
            Text = o.Str("text");
        }
    }

    /// <summary>
    /// A P1 Grand Challenge (decided 2026-10-02): a question the work raises, a goal capability, and a few chunky stages.
    /// Authored in data/content/challenges.json.
    /// </summary>
    public sealed class ChallengeDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Question { get; }
        public IReadOnlyList<string> Opens { get; }
        public SceneCategory OpenCategory { get; }
        public string OpenText { get; }
        public string GoalCapability { get; }
        public CapabilityLevel GoalLevel { get; }
        public IReadOnlyList<ChallengeStageDef> Stages { get; }
        public string CompleteText { get; }

        public ChallengeDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Question = o.Str("question");
            Opens = o.Arr("opens").Cast<string>().ToList();
            OpenCategory = CommissionSceneDef.ParseCategory(o.Str("openCategory"));
            OpenText = o.Str("openText");
            var g = o.Obj("goal");
            GoalCapability = g.Str("capability");
            GoalLevel = Enum.TryParse<CapabilityLevel>(g.Str("level"), out var l) ? l : throw new FormatException("Unknown capability level: " + g.Str("level"));
            Stages = o.Arr("stages").Cast<JsonObject>().Select(x => new ChallengeStageDef(x)).ToList();
            CompleteText = o.Str("completeText");
        }
    }

    public enum ChallengeStatus { NotYet, Open, Working, Done, Abandoned }

    /// <summary>A Grand Challenge's progress in this game.</summary>
    public sealed class ChallengeState
    {
        public string Id { get; }
        public ChallengeStatus Status { get; set; } = ChallengeStatus.NotYet;
        /// <summary>The next stage to start (or the one under way).</summary>
        public int StageIndex { get; set; }
        public int MonthsLeft { get; set; }
        public int ReservedFromTurn { get; set; }
        public ChallengeState(string id) => Id = id;
        public string ProjectId => "challenge:" + Id;
    }
}
