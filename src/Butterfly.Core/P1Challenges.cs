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
        /// <summary>Months added when the first person named in "needsPerson" ("a|b") isn't there and the second stands in.</summary>
        public int SlowerWithout { get; }
        public string SlowerText { get; }
        /// <summary>Further capabilities the stage moves, in order, after its own.</summary>
        public IReadOnlyList<KeyValuePair<string, CapabilityLevel>> Also { get; }
        public string Text { get; }
        /// <summary>The stage's text when the second person named in "needsPerson" does it instead of the first (optional).</summary>
        public string StandInText { get; }

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
            SlowerWithout = (int)o.NumOr("slowerWithout", 0);
            SlowerText = o.StrOr("slowerText", "") ?? "";
            Also = o.Has("also") ? o.Arr("also").Cast<JsonObject>().Select(x => new KeyValuePair<string, CapabilityLevel>(x.Str("capability"),
                       Enum.TryParse<CapabilityLevel>(x.Str("to"), out var lv) ? lv : throw new FormatException("Unknown capability level: " + x.Str("to")))).ToList()
                 : new List<KeyValuePair<string, CapabilityLevel>>();
            Text = o.Str("text");
            StandInText = o.StrOr("standInText", "") ?? "";
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
        /// <summary>
        /// The ways the work can raise the question (any one will do; each route's requirements must all hold). Several routes
        /// keep a challenge from hanging on one client: they converge on the same challenge, which opens once.
        /// </summary>
        public IReadOnlyList<ChallengeRoute> Routes { get; }
        public string GoalCapability { get; }
        public CapabilityLevel GoalLevel { get; }
        public IReadOnlyList<ChallengeStageDef> Stages { get; }
        public string CompleteText { get; }
        /// <summary>A triggered event (events.json) offered when the challenge is done: its human consequence.</summary>
        public string Consequence { get; }

        public ChallengeDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Question = o.Str("question");
            // "routes": [{ id, requires, category, text }]; or the single-route form "opens", "openCategory", "openText".
            Routes = o.Has("routes")
                ? o.Arr("routes").Cast<JsonObject>().Select(r => new ChallengeRoute(r.Str("id"), r.Arr("requires").Cast<string>().ToList(),
                      CommissionSceneDef.ParseCategory(r.Str("category")), r.Str("text"))).ToList()
                : new List<ChallengeRoute> { new ChallengeRoute("main", o.Arr("opens").Cast<string>().ToList(),
                      CommissionSceneDef.ParseCategory(o.Str("openCategory")), o.Str("openText")) };
            if (Routes.Count == 0) throw new FormatException("Challenge " + Id + " has no way to open.");
            var g = o.Obj("goal");
            GoalCapability = g.Str("capability");
            GoalLevel = Enum.TryParse<CapabilityLevel>(g.Str("level"), out var l) ? l : throw new FormatException("Unknown capability level: " + g.Str("level"));
            Stages = o.Arr("stages").Cast<JsonObject>().Select(x => new ChallengeStageDef(x)).ToList();
            CompleteText = o.Str("completeText");
            Consequence = o.StrOr("consequence", "") ?? "";
        }
    }

    /// <summary>One way a Grand Challenge's question comes up: what must all hold, and the scene that raises it.</summary>
    public sealed class ChallengeRoute
    {
        public string Id { get; }
        public IReadOnlyList<string> Requires { get; }
        public SceneCategory Category { get; }
        public string Text { get; }
        public ChallengeRoute(string id, IReadOnlyList<string> requires, SceneCategory category, string text)
        {
            Id = id; Requires = requires; Category = category; Text = text;
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
        /// <summary>The route by which the question came up (empty until it opens).</summary>
        public string OpenedBy { get; set; } = "";
        /// <summary>The first route that became eligible, and the turn it did (Corey, 2026-10-04: strict route causality).
        /// Fixed once set, so a route that holds later never takes the credit while the challenge waits in the router.</summary>
        public string FirstEligibleRoute { get; set; } = "";
        public int FirstEligibleTurn { get; set; } = -1;
        /// <summary>Who is doing the stage under way (from "needsPerson"), fixed when it starts.</summary>
        public string DoneBy { get; set; } = "";
        public ChallengeState(string id) => Id = id;
        public string ProjectId => "challenge:" + Id;
    }
}
