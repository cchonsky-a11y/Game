using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The hidden capability network (decided 2026-10-02): what Rome can actually do, step by step from theory to an
    /// institution that outlives the inventor, separate from what the inventor knows. Knowing is not making: a capability
    /// can't pass a level its prerequisites haven't reached (up to reproducible; PROPOSED P1-07). Every advance is logged.
    /// </summary>
    public sealed partial class Simulation
    {
        public CapabilityDef? CapabilityDefOf(string id) => Data.Content.Capabilities.FirstOrDefault(c => c.Id == id);

        public CapabilityLevel CapabilityLevelOf(string id) => World.Capabilities.FirstOrDefault(c => c.Id == id)?.Level ?? CapabilityLevel.None;

        /// <summary>Why Rome can't yet take this capability to that level, or null if it can.</summary>
        public string? CapabilityBlocker(string id, CapabilityLevel to)
        {
            var def = CapabilityDefOf(id);
            if (def == null) return "No such capability: " + id + ".";
            var needed = (CapabilityLevel)Math.Min((int)to, (int)CapabilityLevel.Reproducible);
            var missing = def.Prerequisites.Where(p => CapabilityLevelOf(p) < needed).ToList();
            return missing.Count == 0 ? null
                : "Knowing is not making: " + def.Name + " needs " + string.Join(" and ", missing.Select(p => CapabilityDefOf(p)!.Name)) + " first.";
        }

        /// <summary>Moves Rome's capability up to a level (never down here), logged with its cause. False if blocked or already there.</summary>
        internal bool AdvanceCapability(string id, CapabilityLevel to, IEnumerable<int>? causes, string text, string actor = "player")
        {
            var state = World.Capabilities.FirstOrDefault(c => c.Id == id);
            if (state == null || state.Level >= to || CapabilityBlocker(id, to) != null) return false;
            var before = state.Level;
            state.Level = to;
            Record("capability.advance", "capability." + id, causes, new[] { actor },
                new[] { new Effect("capability." + id, (int)before, (int)to) }, text);
            return true;
        }

        /// <summary>The level a commission work stage brings its capability to (Prototype → prototype, teaching the shop → reproducible …).</summary>
        public static CapabilityLevel CapabilityLevelFor(ProjectStage stage) => stage switch
        {
            ProjectStage.Observation => CapabilityLevel.Theory,
            ProjectStage.Prototype => CapabilityLevel.Prototype,
            ProjectStage.Failure => CapabilityLevel.Prototype,
            ProjectStage.Refinement => CapabilityLevel.Prototype,
            ProjectStage.CraftAdaptation => CapabilityLevel.Reproducible,
            ProjectStage.Repeatability => CapabilityLevel.Reproducible,
            ProjectStage.Adoption => CapabilityLevel.Adopted,
            ProjectStage.Spread => CapabilityLevel.Adopted,
            _ => CapabilityLevel.None,
        };
    }
}
