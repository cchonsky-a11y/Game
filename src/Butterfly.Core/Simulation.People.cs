using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P1 recurring people (decided 2026-10-02; master handoff §11): their lives go on without the player. Each month a
    /// life event whose requirements hold may happen (seeded chance), once. It can take a person away for a while (no
    /// invitations or introductions from them meanwhile), change how they stand, or spread the player's work in ways the
    /// player didn't choose. Every change is logged with its cause and the people who acted.
    /// </summary>
    public sealed partial class Simulation
    {
        public PersonDef? PersonDefOf(string id) => Data.Content.People.FirstOrDefault(p => p.Id == id);
        public PersonState? PersonOf(string id) => World.People.FirstOrDefault(p => p.Id == id);

        /// <summary>True while the person is ill or away and can't invite, sponsor or introduce anyone.</summary>
        public bool IsPersonAway(string personId) => PersonOf(personId) is PersonState p && p.AwayUntilTurn > Turn;

        /// <summary>True once the player knows this person.</summary>
        public bool Knows(string personId) => PersonDefOf(personId) is PersonDef d && Holds(d.Known);

        /// <summary>The people the player knows, in authored order.</summary>
        public IEnumerable<PersonDef> KnownPeople() => Data.Content.People.Where(p => Holds(p.Known));

        private int MonthsSinceStart => Now.TotalMonths - SimTime.FromYear(T.GetInt("time.startYear"), T.GetInt("time.startMonth")).TotalMonths;

        /// <summary>At the start of each month: people come back, and the next things in their lives may happen.</summary>
        private void AdvancePeople()
        {
            foreach (var p in World.People.Where(p => p.AwayUntilTurn > 0 && p.AwayUntilTurn <= Turn).ToList())
            {
                p.AwayUntilTurn = 0;
                if (p.ReturnText.Length > 0) Record("person.return", "person." + p.Id, null, new[] { p.Id }, null, p.ReturnText);
                p.ReturnText = "";
            }
            foreach (var e in Data.Content.Lives)
            {
                var person = PersonOf(e.Person);
                if (person == null || person.Happened.Contains(e.Id) || MonthsSinceStart < e.FromMonth) continue;
                if (!e.Requires.All(Holds)) continue;
                if (!Rng.Chance(e.Chance)) continue;
                Happen(e, person);
            }
        }

        private void Happen(LifeEventDef e, PersonState person)
        {
            person.Happened.Add(e.Id);
            World.ScenePacing.Record(e.Category);
            var effects = new List<Effect>();
            var actors = new List<string> { e.Person };
            if (e.AwayMonths > 0)
            {
                effects.Add(new Effect("person." + e.Person + ".away", 0, e.AwayMonths));
                person.AwayUntilTurn = Turn + e.AwayMonths;
                person.ReturnText = e.ReturnText;
            }
            foreach (var kv in e.StatusChanges)
            {
                var who = PersonOf(kv.Key);
                if (who == null) throw new FormatException("Life event " + e.Id + " names an unknown person: " + kv.Key);
                who.Status = kv.Value;
                if (!actors.Contains(kv.Key)) actors.Add(kv.Key);
            }
            var happened = Record("person.life", "person." + e.Person, CausesOf(e).ToList(), actors, effects.Count > 0 ? effects : null, e.Text);
            World.LifeEventLog[e.Id] = happened.Id;
            if (e.Capability.Length > 0)
            {
                if (AdvanceCapability(e.Capability, e.CapabilityTo, new[] { happened.Id },
                        "Rome's " + CapabilityDefOf(e.Capability)!.Name + " spread without you" + (e.Distorted ? ", badly." : "."), e.Person) && e.Distorted)
                    World.Capabilities.First(c => c.Id == e.Capability).Distorted = true;
            }
        }

        /// <summary>The events that made a life event possible: the last event behind each requirement.</summary>
        private IEnumerable<int> CausesOf(LifeEventDef e)
        {
            foreach (var r in e.Requires)
            {
                var parts = r.Split(':');
                GameEvent? last = parts[0] switch
                {
                    "commission" => Log.Events.LastOrDefault(ev => ev.Target == "commission:" + parts[1]),
                    "access" => Log.Events.LastOrDefault(ev => ev.Effects.Any(f => f.Key == "access." + parts[1])),
                    "capability" => Log.Events.LastOrDefault(ev => ev.Target == "capability." + parts[1]),
                    _ => null,
                };
                if (last != null) yield return last.Id;
                else if (parts[0] == "life" && World.LifeEventLog.TryGetValue(parts[1], out var id)) yield return id;
            }
        }

        /// <summary>A requirement from people.json: commission:id:Status, access:inst:Stage (or later), capability:id:Level (or later), life:id.</summary>
        internal bool Holds(string requirement)
        {
            var parts = requirement.Split(':');
            switch (parts[0])
            {
                case "commission":
                    return FindCommission(parts[1]) is CommissionState c && c.Status.ToString() == parts[2]
                        || parts[2] == "Offered" && FindCommission(parts[1]) is CommissionState c2 && c2.Status != CommissionStatus.NotYet;
                case "access":
                    return World.AccessTo(parts[1]).Stage >= (InstitutionAccessStage)Enum.Parse(typeof(InstitutionAccessStage), parts[2]);
                case "capability":
                    return CapabilityLevelOf(parts[1]) >= (CapabilityLevel)Enum.Parse(typeof(CapabilityLevel), parts[2]);
                case "life":
                    return World.People.Any(p => p.Happened.Contains(parts[1]));
                default:
                    throw new FormatException("Unknown requirement: " + requirement);
            }
        }
    }
}
