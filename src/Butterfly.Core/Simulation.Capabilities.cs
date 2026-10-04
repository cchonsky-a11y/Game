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
            var missing = def.Edges.Where(e => CapabilityLevelOf(e.Id) < e.RequiredFor(to)).ToList();
            return missing.Count == 0 ? null
                : "Knowing is not making: " + def.Name + " needs " + string.Join(" and ", missing.Select(e =>
                    CapabilityDefOf(e.Id)!.Name + (e.Level != null && to >= CapabilityLevel.Prototype ? " (" + e.Level.ToString()!.ToLowerInvariant() + ")" : ""))) + " first.";
        }

        /// <summary>
        /// Imitation without the method (a bad copy): the work spreads, possibly distorted and under the wrong name, while its
        /// true maturity stays where it was. Logged with who spread it.
        /// </summary>
        internal void SpreadCapability(string id, CapabilitySpread spread, bool distorted, bool misattributed, IEnumerable<int>? causes, string text, string actor)
        {
            var state = World.Capabilities.FirstOrDefault(c => c.Id == id) ?? throw new FormatException("No such capability: " + id);
            var before = state.Spread;
            if (spread > state.Spread) state.Spread = spread;
            state.Distorted |= distorted;
            state.Misattributed |= misattributed;
            Record("capability.spread", "capability." + id, causes, new[] { actor },
                new[] { new Effect("capability." + id + ".spread", (int)before, (int)state.Spread) }, text);
        }

        /// <summary>Moves Rome's capability up to a level (never down here), logged with its cause. False if blocked or already there.</summary>
        internal bool AdvanceCapability(string id, CapabilityLevel to, IEnumerable<int>? causes, string text, string actor = "player")
        {
            var state = World.Capabilities.FirstOrDefault(c => c.Id == id);
            if (state == null || state.Level >= to || CapabilityBlocker(id, to) != null) return false;
            var before = state.Level;
            state.Level = to;
            if (state.Spread == CapabilitySpread.None) state.Spread = CapabilitySpread.Local;
            Record("capability.advance", "capability." + id, causes, new[] { actor },
                new[] { new Effect("capability." + id, (int)before, (int)to) }, text);
            return true;
        }

        /// <summary>
        /// Why authored work that will move these capabilities, in this order, can't be done yet, or null. Each step counts the
        /// steps before it as done, so a stage may build on the one it follows. A step already at or above its level is fine.
        /// </summary>
        public string? AuthoredAdvanceBlocker(IEnumerable<(string Id, CapabilityLevel To)> steps)
        {
            var levels = new Dictionary<string, CapabilityLevel>(StringComparer.Ordinal);
            CapabilityLevel LevelOf(string id) => levels.TryGetValue(id, out var l) ? l : CapabilityLevelOf(id);
            foreach (var (id, to) in steps)
            {
                var def = CapabilityDefOf(id);
                if (def == null) return "No such capability: " + id + ".";
                if (LevelOf(id) >= to) continue;
                var missing = def.Edges.Where(e => LevelOf(e.Id) < e.RequiredFor(to)).ToList();
                if (missing.Count > 0)
                    return "Knowing is not making: " + def.Name + " needs " + string.Join(" and ", missing.Select(e =>
                        CapabilityDefOf(e.Id)!.Name + (e.Level != null && to >= CapabilityLevel.Prototype ? " (" + e.Level.ToString()!.ToLowerInvariant() + ")" : ""))) + " first.";
                levels[id] = to;
            }
            return null;
        }

        /// <summary>
        /// Authored work (a commission stage, a challenge stage, a practical project) moving the capability it declares. Already
        /// at or above the level: nothing changes and nothing is claimed. Blocked by a missing prerequisite: an invariant broken,
        /// since such work can't be offered or started while its advances are out of reach (capabilities never fall), so it
        /// fails loudly instead of finishing as if Rome had moved.
        /// </summary>
        private void AdvanceAuthored(string id, CapabilityLevel to, IEnumerable<int> causes, string source)
        {
            if (CapabilityLevelOf(id) >= to) return;
            if (CapabilityBlocker(id, to) is string blocker)
                throw new InvalidOperationException(source + " declares " + id + " → " + to + " but the capability network refuses it: " + blocker);
            AdvanceCapability(id, to, causes, "Rome's " + CapabilityDefOf(id)!.Name + " now stand at " + to.ToString().ToLowerInvariant() + ".");
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
