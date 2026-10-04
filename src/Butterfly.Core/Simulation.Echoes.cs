using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P1 jump echoes (decided 2026-10-02; master handoff §19): a jump mixes technical, personal, institutional and
    /// unintended echoes. During the absence a reproducible capability carried by an institution the inventor belongs to
    /// rises a level every so many years (PROPOSED P1-09); on arrival the inventor finds what became of the work, the people
    /// and the houses they were guests in. Present conditions only: no causal chains after the jump (PROTOTYPE_SCOPE).
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>The full ladder a carried capability climbs, one rung per period (Corey, 2026-10-04: no skipped states).</summary>
        private static readonly CapabilityLevel[] CraftPath =
        {
            CapabilityLevel.Reproducible, CapabilityLevel.Manufacturable, CapabilityLevel.Economical, CapabilityLevel.Adopted, CapabilityLevel.Institutionalized
        };

        /// <summary>During the absence: capabilities carried by a surviving institution you belong to spread.</summary>
        private void CarryCapabilities(int years, int departId)
        {
            int steps = (int)(years / T.Get("capabilities.carriedYearsPerLevel"));
            if (steps <= 0) return;
            foreach (var c in World.Capabilities.Where(c => c.Level >= CapabilityLevel.Reproducible && c.Level < CapabilityLevel.Institutionalized))
            {
                var def = CapabilityDefOf(c.Id)!;
                var carrier = def.Carriers.Select(World.Institution).FirstOrDefault(i =>
                    World.AccessTo(i.Key).Stage >= InstitutionAccessStage.Member && Influential().Contains(i) && OutcomeOf(i) != InstitutionOutcome.Dissolved);
                if (carrier == null) continue;
                // Reproducible → manufacturable → economical → adopted → institutionalized, a rung per period carried.
                int from = Array.IndexOf(CraftPath, c.Level);
                var to = CraftPath[Math.Min(CraftPath.Length - 1, from + steps)];
                AdvanceCapability(c.Id, to, new[] { departId }, Cap(carrier.Def.Name) + " carried " + def.Name + " on while you were away.", carrier.Leader);
            }
        }

        /// <summary>A line not shown on an earlier arrival, marked shown; null if it was.</summary>
        private string? Fresh(string text)
        {
            if (World.EchoesShown.Contains(text)) return null;
            World.EchoesShown.Add(text);
            return text;
        }

        /// <summary>
        /// People you knew, as you find them (at most two an arrival, on every arrival). A line is never repeated; on a later
        /// arrival the lines written for it (aged consequences, descendants, deaths, contradictory memory) come first.
        /// </summary>
        private IEnumerable<string> PeopleEchoes(Arrival arrival)
        {
            int arrivalNumber = JumpsMade;          // already counts this jump
            int shown = 0;
            // Prefer people not yet featured on an earlier arrival (unresolved lives first), then the rest, in authored order.
            foreach (var p in KnownPeople().OrderBy(p => World.EchoesShown.Contains("person:" + p.Id) ? 1 : 0))
            {
                if (shown >= T.GetInt("echoes.peoplePerArrival")) yield break;
                var echo = p.Echoes.Where(e => e.FromJump <= arrivalNumber && !World.EchoesShown.Contains(e.Text) && e.Requires.All(Holds))
                                   .OrderByDescending(e => e.FromJump).FirstOrDefault();
                if (echo == null) continue;
                shown++;
                arrival.P1Echoes.Add("person:" + p.Id);
                if (!World.EchoesShown.Contains("person:" + p.Id)) World.EchoesShown.Add("person:" + p.Id);
                yield return Fresh(echo.Text)!;
            }
        }

        /// <summary>The work and the houses: what your capabilities became, the bad copies, the guild you only visited.</summary>
        private IEnumerable<string> WorkEchoes(Arrival arrival)
        {
            foreach (var c in World.Capabilities.Where(c => c.Level >= CapabilityLevel.Reproducible))
            {
                var def = CapabilityDefOf(c.Id)!;
                var level = def.Echo.Keys.Where(k => Enum.TryParse<CapabilityLevel>(k, out var l) && l <= c.Level)
                    .OrderByDescending(k => (int)Enum.Parse(typeof(CapabilityLevel), k)).FirstOrDefault();
                if (level != null && Fresh(def.Echo[level]) is string line) { arrival.P1Echoes.Add("technical:" + c.Id); yield return line; }
            }
            // The bad copies, separately: what spread without the method (never the method's own maturity).
            foreach (var c in World.Capabilities.Where(c => c.Distorted))
            {
                var def = CapabilityDefOf(c.Id)!;
                string? bad = def.Echo.TryGetValue("distorted", out var b1) && !World.EchoesShown.Contains(b1) ? b1
                            : def.Echo.TryGetValue("distorted.later", out var b2) && !World.EchoesShown.Contains(b2) ? b2 : null;
                if (bad != null) { arrival.P1Echoes.Add("unintended:" + c.Id); yield return Fresh(bad)!; }
            }
            foreach (var d in Data.Content.InvitationPaths)
            {
                var stage = World.AccessTo(d.Institution).Stage;
                if (stage >= InstitutionAccessStage.Guest && stage < InstitutionAccessStage.Member && d.EchoGuest.Length > 0 && !World.EchoesShown.Contains(d.EchoGuest))
                {
                    Fresh(d.EchoGuest);
                    arrival.P1Echoes.Add("access:" + d.Institution);
                    yield return d.EchoGuest;
                }
            }
        }
    }
}
