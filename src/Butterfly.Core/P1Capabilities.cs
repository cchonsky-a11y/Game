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

        public CapabilityDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Domain = o.Str("domain");
            Baseline = o.Str("baseline");
            Bottleneck = o.Str("bottleneck");
            Leap = o.Str("leap");
            Prerequisites = o.Arr("prerequisites").Cast<string>().ToList();
        }
    }

    /// <summary>Where Rome stands with one capability in this game.</summary>
    public sealed class CapabilityState
    {
        public string Id { get; }
        public CapabilityLevel Level { get; set; }
        /// <summary>Spread by bad copies the inventor didn't make (an unintended echo).</summary>
        public bool Distorted { get; set; }
        public CapabilityState(string id) => Id = id;
    }
}
