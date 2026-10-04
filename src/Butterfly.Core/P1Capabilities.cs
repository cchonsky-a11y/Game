using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// How far Rome has taken a capability (handoff 2026-10-02, technology ladder §1 and master handoff §17). The player may
    /// understand the theory long before Rome can do any of it: knowing is not making.
    /// </summary>
    public enum CapabilityLevel
    {
        None,
        Theory,
        Demonstrated,
        Prototype,
        Reproducible,
        Manufacturable,
        Economical,
        Adopted,
        Institutionalized
    }

    /// <summary>
    /// One node of the hidden capability network (data/content/capabilities.json): the next step beyond what Rome already
    /// does well, with the Roman baseline, the bottleneck, and the kind of leap (never "invent" what Rome had).
    /// </summary>
    public sealed class CapabilityDef
    {
        public static readonly string[] Leaps = { "formalize", "improve", "combine", "scale", "standardize", "apply" };

        public string Id { get; }
        public string Name { get; }
        public string Domain { get; }
        public string Baseline { get; }
        public string Bottleneck { get; }
        public string Leap { get; }
        public IReadOnlyList<string> Prerequisites { get; }
        /// <summary>
        /// Each prerequisite with the level it must reach, if the edge names one ({"id", "level"} in content); null means the
        /// default rule: as far as the target, up to reproducible (P1-07).
        /// </summary>
        public IReadOnlyList<CapabilityEdge> Edges { get; }
        /// <summary>Institutions whose members carry a reproducible capability forward during an absence (P1-09).</summary>
        public IReadOnlyList<string> Carriers { get; }
        /// <summary>What the inventor finds on arrival, by level name (and "distorted" for bad copies).</summary>
        public IReadOnlyDictionary<string, string> Echo { get; }

        public CapabilityDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Domain = o.Str("domain");
            Baseline = o.Str("baseline");
            Bottleneck = o.Str("bottleneck");
            Leap = o.Str("leap");
            Edges = o.Arr("prerequisites").Select(x => x is JsonObject e
                ? new CapabilityEdge(e.Str("id"), Enum.TryParse<CapabilityLevel>(e.Str("level"), out var l) ? l : throw new FormatException("Unknown capability level on " + Id + ": " + e.Str("level")))
                : new CapabilityEdge((string)x!, null)).ToList();
            Prerequisites = Edges.Select(e => e.Id).ToList();
            Carriers = o.Has("carriers") ? o.Arr("carriers").Cast<string>().ToList() : new List<string>();
            var e = o.Has("echo") ? o.Obj("echo") : null;
            Echo = e == null ? new Dictionary<string, string>() : e.Keys.ToDictionary(k => k, k => e.Str(k));
        }
    }

    /// <summary>A dependency edge: the prerequisite and, optionally, the level it must reach.</summary>
    public sealed class CapabilityEdge
    {
        public string Id { get; }
        public CapabilityLevel? Level { get; }
        public CapabilityEdge(string id, CapabilityLevel? level) { Id = id; Level = level; }

        /// <summary>
        /// The level this prerequisite must reach before the capability can reach <paramref name="target"/>: an edge's own
        /// level once the target is a working thing (prototype or beyond), never more than the target below that; without
        /// one, the default rule (as far as the target, up to reproducible).
        /// </summary>
        public CapabilityLevel RequiredFor(CapabilityLevel target) =>
            Level is CapabilityLevel l
                ? (target >= CapabilityLevel.Prototype ? l : (CapabilityLevel)Math.Min((int)l, (int)target))
                : (CapabilityLevel)Math.Min((int)target, (int)CapabilityLevel.Reproducible);
    }

    /// <summary>How far the inventor's work has spread beyond the shops that do it right (separate from true maturity).</summary>
    public enum CapabilitySpread
    {
        None,
        Local,      // the shops the inventor worked with
        Copied,     // others imitate it, rightly or wrongly
        Widespread  // copies are everywhere
    }

    /// <summary>
    /// Where Rome stands with one capability in this game. <see cref="Level"/> is the true maturity of the method done right;
    /// spread, distortion and misattribution record what imitation did, and never raise the level (Corey, 2026-10-04).
    /// </summary>
    public sealed class CapabilityState
    {
        public string Id { get; }
        public CapabilityLevel Level { get; set; }
        public CapabilitySpread Spread { get; set; }
        /// <summary>Bad copies are about: they look like the real thing and don't work like it (an unintended echo).</summary>
        public bool Distorted { get; set; }
        /// <summary>The copies carry someone else's name, or the inventor's on work that isn't his.</summary>
        public bool Misattributed { get; set; }
        public CapabilityState(string id) => Id = id;
    }
}
