using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// A recurring person (P1, master handoff §11): a goal, a vulnerability, people they care about and someone they
    /// distrust, a household, status, an opinion, an outside interest and a voice. Authored in data/content/people.json.
    /// </summary>
    public sealed class PersonDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Known { get; }
        public string Role { get; }
        public string Goal { get; }
        public string Vulnerability { get; }
        public string CaresAbout { get; }
        public string Distrusts { get; }
        public string Household { get; }
        public string Status { get; }
        public string Opinion { get; }
        public string Interest { get; }
        public string Voice { get; }
        /// <summary>What the inventor finds of this person on the first arrival: the first whose requirements hold.</summary>
        public IReadOnlyList<KeyValuePair<IReadOnlyList<string>, string>> Echoes { get; }

        public PersonDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Known = o.Str("known");
            Role = o.Str("role");
            Goal = o.Str("goal");
            Vulnerability = o.Str("vulnerability");
            CaresAbout = o.Str("caresAbout");
            Distrusts = o.Str("distrusts");
            Household = o.Str("household");
            Status = o.Str("status");
            Opinion = o.Str("opinion");
            Interest = o.Str("interest");
            Voice = o.Str("voice");
            Echoes = o.Has("echoes")
                ? o.Arr("echoes").Cast<JsonObject>().Select(x => new KeyValuePair<IReadOnlyList<string>, string>(x.Arr("requires").Cast<string>().ToList(), x.Str("text"))).ToList()
                : new List<KeyValuePair<IReadOnlyList<string>, string>>();
        }
    }

    /// <summary>Something that can happen in a person's life without the player: once, when its requirements hold, by chance each month.</summary>
    public sealed class LifeEventDef
    {
        public string Id { get; }
        public string Person { get; }
        public int FromMonth { get; }
        public double Chance { get; }
        public IReadOnlyList<string> Requires { get; }
        public int AwayMonths { get; }
        public SceneCategory Category { get; }
        public string Text { get; }
        public string ReturnText { get; }
        /// <summary>New status lines, by person, in authored order.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> StatusChanges { get; }
        public string Capability { get; }
        public CapabilityLevel CapabilityTo { get; }
        public bool Distorted { get; }

        public LifeEventDef(JsonObject o)
        {
            Id = o.Str("id");
            Person = o.Str("person");
            FromMonth = (int)o.Num("fromMonth");
            Chance = o.Num("chance");
            Requires = o.Arr("requires").Cast<string>().ToList();
            AwayMonths = (int)o.NumOr("awayMonths", 0);
            Category = CommissionSceneDef.ParseCategory(o.Str("category"));
            Text = o.Str("text");
            ReturnText = o.StrOr("returnText", "") ?? "";
            var s = o.Has("status") ? o.Obj("status") : null;
            StatusChanges = s == null ? new List<KeyValuePair<string, string>>() : s.Keys.Select(k => new KeyValuePair<string, string>(k, s.Str(k))).ToList();
            var c = o.Has("capability") ? o.Obj("capability") : null;
            Capability = c?.Str("id") ?? "";
            CapabilityTo = c == null ? CapabilityLevel.None
                : Enum.TryParse<CapabilityLevel>(c.Str("to"), out var l) ? l : throw new FormatException("Unknown capability level: " + c.Str("to"));
            Distorted = c != null && c.BoolOr("distorted", false);
        }
    }

    /// <summary>Where a person's life stands in this game; it changes whether or not the player is watching.</summary>
    public sealed class PersonState
    {
        public string Id { get; }
        public string Status { get; set; }
        /// <summary>Away (ill, travelling) until the start of this turn; 0 if here.</summary>
        public int AwayUntilTurn { get; set; }
        public string ReturnText { get; set; } = "";
        public List<string> Happened { get; } = new List<string>();

        public PersonState(string id, string status) { Id = id; Status = status; }
    }
}
