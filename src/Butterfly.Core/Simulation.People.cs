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
        public bool IsPersonAway(string personId) => PersonOf(personId) is PersonState p && (p.Gone || p.AwayUntilTurn > Turn);

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
                if (person == null || person.Gone || person.Happened.Contains(e.Id) || MonthsSinceStart < e.FromMonth) continue;
                if (!e.Requires.All(Holds) || World.ReadyLife.Contains(e.Id)) continue;
                // Competing branches: one of a group, never two.
                if (e.Group.Length > 0 && Data.Content.Lives.Any(o => o.Group == e.Group && o.Id != e.Id && (World.LifeEventLog.ContainsKey(o.Id) || World.ReadyLife.Contains(o.Id)))) continue;
                // A bounded window and a declining hazard (Corey, 2026-10-04): a long campaign doesn't make every event certain.
                if (!World.LifeEligibleSince.TryGetValue(e.Id, out int since)) World.LifeEligibleSince[e.Id] = since = Turn;
                int months = Turn - since;
                if (e.WindowMonths > 0 && months >= e.WindowMonths) continue;
                if (!Rng.Chance(e.Chance * Math.Pow(e.Decay, months))) continue;
                // A world interruption happens now; anything else waits its turn with the scene router (P1 pacing).
                if (e.Interrupt) Happen(e, person);
                else World.ReadyLife.Add(e.Id);
            }
        }

        private void Happen(LifeEventDef e, PersonState person)
        {
            World.ReadyLife.Remove(e.Id);
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
            if (e.Leaves)
            {
                person.Gone = true;
                effects.Add(new Effect("person." + e.Person + ".gone", 0, 1));
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
            if (e.Capability.Length > 0 && e.Spread != CapabilitySpread.None)
                SpreadCapability(e.Capability, e.Spread, e.Distorted, e.Misattributed, new[] { happened.Id },
                    Cap(CapabilityDefOf(e.Capability)!.Name) + " spread without you" + (e.Distorted ? ", badly" : "") + (e.Misattributed ? ", under the wrong name" : "") + ".", e.Person);
            if (e.Capability.Length > 0 && e.CapabilityTo > CapabilityLevel.None)
                AdvanceCapability(e.Capability, e.CapabilityTo, new[] { happened.Id }, e.Person + " took " + CapabilityDefOf(e.Capability)!.Name + " further on their own.", e.Person);
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

        /// <summary>
        /// A requirement from people.json or scenes.json: commission:id:Status, access:inst:Stage (or later), capability:id:Level
        /// (or later), life:id, knows:person, scene:id, month:1-12 (calendar), monthsIn:n, machine:assessed, machine:steps:n,
        /// project:id:done, regard:person:n, invented:id, join:institution (its P0 joining condition), promise; "a|b" for either.
        /// </summary>
        internal bool Holds(string requirement)
        {
            // "a|b": either will do.
            if (requirement.IndexOf('|') >= 0) return requirement.Split('|').Any(Holds);
            // "!x": x does not hold.
            if (requirement.StartsWith("!", StringComparison.Ordinal)) return !Holds(requirement.Substring(1));
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
                case "flag":
                    return World.Flags.Contains(parts[1]);
                case "knows":
                    return Knows(parts[1]);
                case "scene":
                    return World.ScenesSeen.Contains(parts[1]) || World.ScenePlays.ContainsKey(parts[1]);
                case "month":
                    return Now.Month + 1 == int.Parse(parts[1]);
                case "monthsIn":
                    return MonthsSinceStart >= int.Parse(parts[1]);
                case "machine":
                    return parts[1] == "assessed" ? MachineAssessed : MachineStepsDone >= int.Parse(parts[2]);
                case "project":
                    return World.CompletedProjects.Contains(parts[1]);
                case "regard":
                    return (PersonOf(parts[1])?.Regard ?? 0) >= int.Parse(parts[2]);
                case "regardBelow":
                    return (PersonOf(parts[1])?.Regard ?? 0) < int.Parse(parts[2]);
                case "challenge":
                    return FindChallenge(parts[1]) is ChallengeState ch && ch.Status.ToString() == parts[2];
                case "stage":
                    return FindChallenge(parts[1]) is ChallengeState cs && cs.StageIndex >= int.Parse(parts[2]);
                case "invented":
                    return World.Invented.Contains(parts[1]);
                case "journal":
                    return World.Journal.Any(j => j.Id == parts[1]);
                case "answered":
                    return _eventChoices.Any(c => c.Event.Id == parts[1] && c.Option.Id == parts[2]);
                case "join":
                    return JoinBlocker(World.Institution(parts[1])) == null;
                case "promise":
                    return World.Promise.Status == PromiseStatus.Active || World.Promise.Status == PromiseStatus.Kept;
                default:
                    throw new FormatException("Unknown requirement: " + requirement);
            }
        }
    }
}
