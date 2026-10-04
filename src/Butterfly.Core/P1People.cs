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
        /// <summary>What the inventor may find of this person on an arrival (each shown once; later arrivals prefer later lines).</summary>
        public IReadOnlyList<PersonEchoDef> Echoes { get; }

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
            Echoes = o.Has("echoes") ? o.Arr("echoes").Cast<JsonObject>().Select(x => new PersonEchoDef(x)).ToList() : new List<PersonEchoDef>();
        }
    }

    /// <summary>One thing the inventor may find of a person after a jump: from which arrival on, and what must hold.</summary>
    public sealed class PersonEchoDef
    {
        public IReadOnlyList<string> Requires { get; }
        public string Text { get; }
        /// <summary>The first arrival (1, 2, …) this line can appear on: later lines carry aged consequences.</summary>
        public int FromJump { get; }

        public PersonEchoDef(JsonObject o)
        {
            Requires = o.Arr("requires").Cast<string>().ToList();
            Text = o.Str("text");
            FromJump = (int)o.NumOr("fromJump", 1);
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
        /// <summary>How far the person's imitation spreads the work (never its true maturity), or None.</summary>
        public CapabilitySpread Spread { get; }
        public bool Misattributed { get; }
        /// <summary>A world interruption (illness, fire): it happens when it happens, not when the scene router allows.</summary>
        public bool Interrupt { get; }
        /// <summary>Months after it first becomes possible during which it can happen; 0 for no limit (Corey, 2026-10-04: no inevitability).</summary>
        public int WindowMonths { get; }
        /// <summary>The monthly chance is multiplied by this for each month it has been possible (a declining hazard).</summary>
        public double Decay { get; }
        /// <summary>Mutually exclusive branches: once one event of a group happens (or waits to), the others never do.</summary>
        public string Group { get; }
        /// <summary>The person leaves Rome for good.</summary>
        public bool Leaves { get; }

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
            // "to": a true advance by the person (they improve the method); "spread": imitation, which never raises maturity.
            CapabilityTo = c == null || !c.Has("to") ? CapabilityLevel.None
                : Enum.TryParse<CapabilityLevel>(c.Str("to"), out var l) ? l : throw new FormatException("Unknown capability level: " + c.Str("to"));
            Spread = c == null || !c.Has("spread") ? CapabilitySpread.None
                : Enum.TryParse<CapabilitySpread>(c.Str("spread"), out var sp) ? sp : throw new FormatException("Unknown spread: " + c.Str("spread"));
            Distorted = c != null && c.BoolOr("distorted", false);
            Misattributed = c != null && c.BoolOr("misattributed", false);
            Interrupt = o.BoolOr("interrupt", false);
            WindowMonths = (int)o.NumOr("windowMonths", 0);
            Decay = o.NumOr("decay", 1);
            Group = o.StrOr("group", "") ?? "";
            Leaves = o.BoolOr("leaves", false);
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
        /// <summary>How this person regards the inventor (P1): small whole steps from shared life and work.</summary>
        public int Regard { get; set; }
        public List<string> Happened { get; } = new List<string>();
        /// <summary>Gone from Rome for good.</summary>
        public bool Gone { get; set; }

        public PersonState(string id, string status) { Id = id; Status = status; }
    }
}

namespace Butterfly.Core
{
    /// <summary>
    /// An authored optional scene (Roman life, the machine mystery, relationship moments): data/content/scenes.json. It happens
    /// once, when its requirements hold and the scene router picks it.
    /// </summary>
    public sealed class AuthoredSceneDef
    {
        public string Id { get; }
        public SceneCategory Category { get; }
        public IReadOnlyList<string> Requires { get; }
        public double Weight { get; }
        public string Text { get; }
        public IReadOnlyList<KeyValuePair<string, int>> Regard { get; }
        public IReadOnlyList<KeyValuePair<string, string>> StatusChanges { get; }
        /// <summary>Through this scene the inventor comes to know a member of an institution (the first step of access).</summary>
        public string KnowsInstitution { get; }
        public string KnowsMember { get; }
        /// <summary>Life gets in the way: the named Grand Challenge's stage under way takes this many more months.</summary>
        public string DelaysChallenge { get; }
        public int DelayMonths { get; }
        /// <summary>A choice the scene puts to the player: a triggered event (events.json) offered the next month.</summary>
        public string Triggers { get; }
        /// <summary>Reusable texture (markets, fountains, weather): it may come back after this many months, each time with the
        /// next variant's text, never the same words twice; 0 for a one-off scene.</summary>
        public int CooldownMonths { get; }
        public IReadOnlyList<string> Variants { get; }
        /// <summary>Money the scene costs or brings, in aurei at AD 155 prices (scaled by the price level).</summary>
        public double Gold { get; }
        /// <summary>A critical moment that interrupts rather than waits for the router (the panel lighting R-17 ACTIVE).</summary>
        public bool Interrupt { get; }

        public AuthoredSceneDef(JsonObject o)
        {
            Id = o.Str("id");
            Category = CommissionSceneDef.ParseCategory(o.Str("category"));
            Requires = o.Arr("requires").Cast<string>().ToList();
            Weight = o.NumOr("weight", 0);
            Text = o.Str("text");
            var r = o.Has("regard") ? o.Obj("regard") : null;
            Regard = r == null ? new List<KeyValuePair<string, int>>() : r.Keys.Select(k => new KeyValuePair<string, int>(k, (int)r.Num(k))).ToList();
            var s = o.Has("status") ? o.Obj("status") : null;
            StatusChanges = s == null ? new List<KeyValuePair<string, string>>() : s.Keys.Select(k => new KeyValuePair<string, string>(k, s.Str(k))).ToList();
            var km = o.Has("knowsMember") ? o.Obj("knowsMember") : null;
            KnowsInstitution = km?.Str("institution") ?? "";
            KnowsMember = km?.Str("member") ?? "";
            var dl = o.Has("delays") ? o.Obj("delays") : null;
            DelaysChallenge = dl?.Str("challenge") ?? "";
            DelayMonths = dl == null ? 0 : (int)dl.Num("months");
            Triggers = o.StrOr("triggers", "") ?? "";
            CooldownMonths = (int)o.NumOr("cooldownMonths", 0);
            Variants = o.Has("variants") ? o.Arr("variants").Cast<string>().ToList() : new List<string>();
            Gold = o.NumOr("gold", 0);
            Interrupt = o.BoolOr("interrupt", false);
        }
    }
}
