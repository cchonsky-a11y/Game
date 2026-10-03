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
        private static readonly CapabilityLevel[] CraftPath = { CapabilityLevel.Reproducible, CapabilityLevel.Adopted, CapabilityLevel.Institutionalized };

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
                // A craft network carries it reproducible → adopted → institutionalized (the factory levels come later).
                int from = c.Level >= CapabilityLevel.Adopted ? 1 : 0;
                var to = CraftPath[Math.Min(CraftPath.Length - 1, from + steps)];
                AdvanceCapability(c.Id, to, new[] { departId }, Cap(carrier.Def.Name) + " carried " + def.Name + " on while you were away.", carrier.Leader);
            }
        }

        /// <summary>People you knew, as you find them on the first arrival (at most two).</summary>
        private IEnumerable<string> PeopleEchoes(Arrival arrival, bool later)
        {
            if (later) yield break;
            int shown = 0;
            foreach (var p in KnownPeople())
            {
                if (shown >= 2) yield break;
                var echo = p.Echoes.FirstOrDefault(e => e.Key.All(Holds));
                if (echo.Value == null) continue;
                shown++;
                arrival.P1Echoes.Add("person:" + p.Id);
                yield return echo.Value;
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
                if (level != null) { arrival.P1Echoes.Add("technical:" + c.Id); yield return def.Echo[level]; }
                if (c.Distorted && def.Echo.TryGetValue("distorted", out var bad)) { arrival.P1Echoes.Add("unintended:" + c.Id); yield return bad; }
            }
            foreach (var d in Data.Content.InvitationPaths)
            {
                var stage = World.AccessTo(d.Institution).Stage;
                if (stage >= InstitutionAccessStage.Guest && stage < InstitutionAccessStage.Member && d.EchoGuest.Length > 0)
                {
                    arrival.P1Echoes.Add("access:" + d.Institution);
                    yield return d.EchoGuest;
                }
            }
        }
    }
}
