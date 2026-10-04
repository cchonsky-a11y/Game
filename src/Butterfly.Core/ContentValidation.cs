using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Referential and structural checks over the loaded content, run when content loads (code-health pass, 2026-10-04):
    /// a typo in an id or a requirement fails at start-up with a clear message instead of silently never holding, or
    /// throwing halfway through a campaign. The requirement grammar mirrors <see cref="Simulation.Holds"/>; a test feeds every
    /// prefix accepted here through Holds so the two stay in step.
    /// </summary>
    public static class ContentValidation
    {
        /// <summary>The requirement prefixes <see cref="Simulation.Holds"/> understands.</summary>
        public static readonly string[] RequirementPrefixes =
        {
            "commission", "access", "capability", "life", "flag", "knows", "scene", "month", "monthsIn", "machine",
            "project", "regard", "regardBelow", "challenge", "stage", "invented", "join", "promise"
        };

        public static IReadOnlyList<string> Problems(Content c)
        {
            var p = new List<string>();
            var people = new HashSet<string>(c.People.Select(x => x.Id));
            var caps = new HashSet<string>(c.Capabilities.Select(x => x.Id));
            var institutions = new HashSet<string>(c.Institutions.Select(x => x.Id));
            var triggered = new HashSet<string>(c.Events.Where(e => e.Triggered).Select(e => e.Id));
            var flags = new HashSet<string>(c.Commissions.SelectMany(x => x.OnCompleteSets)
                .Concat(c.Events.SelectMany(e => e.Options.SelectMany(o => o.Sets)))
                .Concat(new[] { "r17-opened", "streetFor", "streetAgainst" }));          // set in code (Mystery, Events)

            Unique(p, "project", c.Projects.Select(x => x.Id));
            Unique(p, "institution", c.Institutions.Select(x => x.Id));
            Unique(p, "machine step", c.MachineSteps.Concat(c.MachineUpgrades).Select(x => x.Id));
            Unique(p, "invention", c.Inventions.Select(x => x.Id));
            Unique(p, "event", c.Events.Select(x => x.Id));
            Unique(p, "commission", c.Commissions.Select(x => x.Id));
            Unique(p, "invitation path", c.InvitationPaths.Select(x => x.Institution));
            Unique(p, "capability", c.Capabilities.Select(x => x.Id));
            Unique(p, "scene", c.Scenes.Select(x => x.Id));
            Unique(p, "challenge", c.Challenges.Select(x => x.Id));
            Unique(p, "person", c.People.Select(x => x.Id));
            Unique(p, "life event", c.Lives.Select(x => x.Id));

            void Person(string where, string id) { if (!people.Contains(id)) p.Add(where + ": unknown person '" + id + "'"); }
            void Capability(string where, string id) { if (id.Length > 0 && !caps.Contains(id)) p.Add(where + ": unknown capability '" + id + "'"); }
            void Requirements(string where, IEnumerable<string> reqs) { foreach (var r in reqs) Requirement(p, c, flags, where, r); }
            void Positive(string where, double value, string what) { if (value <= 0) p.Add(where + ": " + what + " must be positive (" + value.ToString(CultureInfo.InvariantCulture) + ")"); }
            void NotNegative(string where, double value, string what) { if (value < 0) p.Add(where + ": " + what + " is negative (" + value.ToString(CultureInfo.InvariantCulture) + ")"); }

            foreach (var x in c.People)
            {
                Requirement(p, c, flags, "person " + x.Id + " (known)", x.Known);
                foreach (var e in x.Echoes) Requirements("person " + x.Id + " echo", e.Requires);
            }
            foreach (var e in c.Lives)
            {
                string w = "life event " + e.Id;
                Person(w, e.Person);
                foreach (var kv in e.StatusChanges) Person(w, kv.Key);
                Capability(w, e.Capability);
                Requirements(w, e.Requires);
                if (e.Chance < 0 || e.Chance > 1) p.Add(w + ": chance must be within 0–1");
            }
            foreach (var d in c.Commissions)
            {
                string w = "commission " + d.Id;
                if (d.Introducer.Length > 0) Person(w + " (introducer)", d.Introducer);
                if (d.ReferralMember.Length > 0) Person(w + " (referral)", d.ReferralMember);
                if (d.ReferralInstitution.Length > 0 && !institutions.Contains(d.ReferralInstitution)) p.Add(w + ": unknown referral institution '" + d.ReferralInstitution + "'");
                foreach (var kv in d.OnCompleteRegard) Person(w, kv.Key);
                foreach (var kv in d.OnCompleteStatus) Person(w, kv.Key);
                Capability(w, d.Capability);
                foreach (var s in d.Work) { Capability(w, s.Capability); Positive(w, s.DurationMonths, "a work stage's months"); NotNegative(w, s.Attention, "a work stage's Attention"); }
                if (d.Work.Count == 0) p.Add(w + ": no work stages");
                NotNegative(w, d.Upfront, "upfront pay");
                NotNegative(w, d.Completion, "completion pay");
                Requirements(w, d.Requires);
            }
            foreach (var d in c.InvitationPaths)
            {
                string w = "invitation " + d.Institution;
                if (!institutions.Contains(d.Institution)) p.Add(w + ": unknown institution");
                Person(w, d.Inviter);
                Requirements(w, d.RelationshipEvidence.Concat(d.WorkEvidence).Concat(d.UsefulnessEvidence));
            }
            foreach (var d in c.Capabilities)
            {
                string w = "capability " + d.Id;
                if (!CapabilityDef.Leaps.Contains(d.Leap)) p.Add(w + ": leap '" + d.Leap + "' is not one of " + string.Join(", ", CapabilityDef.Leaps));
                foreach (var pre in d.Prerequisites) Capability(w + " (prerequisite)", pre);
                foreach (var e in d.Edges) Capability(w + " (edge)", e.Id);
            }
            foreach (var s in c.Scenes)
            {
                string w = "scene " + s.Id;
                Requirements(w, s.Requires);
                foreach (var kv in s.Regard) Person(w, kv.Key);
                foreach (var kv in s.StatusChanges) Person(w, kv.Key);
                if (s.KnowsMember.Length > 0) Person(w, s.KnowsMember);
                if (s.KnowsInstitution.Length > 0 && !institutions.Contains(s.KnowsInstitution)) p.Add(w + ": unknown institution '" + s.KnowsInstitution + "'");
                if (s.Triggers.Length > 0 && !triggered.Contains(s.Triggers)) p.Add(w + ": triggers unknown event '" + s.Triggers + "'");
                if (s.DelaysChallenge.Length > 0 && c.Challenges.All(x => x.Id != s.DelaysChallenge)) p.Add(w + ": delays unknown challenge '" + s.DelaysChallenge + "'");
            }
            foreach (var d in c.Challenges)
            {
                string w = "challenge " + d.Id;
                Unique(p, w + " route", d.Routes.Select(r => r.Id));
                Unique(p, w + " stage", d.Stages.Select(s => s.Id));
                foreach (var r in d.Routes) Requirements(w + " route " + r.Id, r.Requires);
                Capability(w + " (goal)", d.GoalCapability);
                if (d.Consequence.Length > 0 && !triggered.Contains(d.Consequence)) p.Add(w + ": consequence is unknown event '" + d.Consequence + "'");
                foreach (var s in d.Stages)
                {
                    string ws = w + " stage " + s.Id;
                    Capability(ws, s.Capability);
                    foreach (var kv in s.Also) Capability(ws, kv.Key);
                    foreach (var who in s.NeedsPerson.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) Person(ws, who);
                    Requirements(ws, s.Needs.Concat(s.Resource?.Source ?? new List<string>()));
                    Positive(ws, s.Months, "months");
                    NotNegative(ws, s.Gold, "gold");
                }
            }
            foreach (var i in c.Inventions) Capability("invention " + i.Id, i.Capability);
            foreach (var e in c.Events)
                foreach (var o in e.Options)
                    foreach (var fx in o.Effects.Where(fx => fx.Type == "regard" || fx.Type == "status"))
                        Person("event " + e.Id + " option " + o.Id, fx.Person ?? "");
            return p;
        }

        private static void Unique(List<string> p, string what, IEnumerable<string> ids)
        {
            foreach (var g in ids.GroupBy(x => x).Where(g => g.Count() > 1)) p.Add("duplicate " + what + " id '" + g.Key + "'");
        }

        /// <summary>One requirement (people.json syntax): known prefix, ids that exist, levels and stages that parse.</summary>
        private static void Requirement(List<string> p, Content c, HashSet<string> flags, string where, string requirement)
        {
            if (string.IsNullOrWhiteSpace(requirement)) { p.Add(where + ": empty requirement"); return; }
            if (requirement.IndexOf('|') >= 0) { foreach (var part in requirement.Split('|')) Requirement(p, c, flags, where, part); return; }
            if (requirement.StartsWith("!", StringComparison.Ordinal)) { Requirement(p, c, flags, where, requirement.Substring(1)); return; }
            var parts = requirement.Split(':');
            string Bad(string why) => where + ": requirement '" + requirement + "' " + why;
            bool Arity(int n) { if (parts.Length == n) return true; p.Add(Bad("needs " + (n - 1) + " argument(s)")); return false; }
            bool Int(string s) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            switch (parts[0])
            {
                case "commission":
                    if (!Arity(3)) return;
                    if (c.Commissions.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown commission"));
                    if (parts[2] != "Offered" && !Enum.TryParse<CommissionStatus>(parts[2], out _)) p.Add(Bad("has an unknown status"));
                    return;
                case "access":
                    if (!Arity(3)) return;
                    if (c.Institutions.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown institution"));
                    if (!Enum.TryParse<InstitutionAccessStage>(parts[2], out _)) p.Add(Bad("has an unknown access stage"));
                    return;
                case "capability":
                    if (!Arity(3)) return;
                    if (c.Capabilities.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown capability"));
                    if (!Enum.TryParse<CapabilityLevel>(parts[2], out _)) p.Add(Bad("has an unknown level"));
                    return;
                case "life":
                    if (Arity(2) && c.Lives.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown life event"));
                    return;
                case "flag":
                    if (Arity(2) && !flags.Contains(parts[1])) p.Add(Bad("names a flag nothing sets"));
                    return;
                case "knows":
                    if (Arity(2) && c.People.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown person"));
                    return;
                case "scene":
                    if (Arity(2) && c.Scenes.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown scene"));
                    return;
                case "month":
                    if (Arity(2) && (!Int(parts[1]) || int.Parse(parts[1], CultureInfo.InvariantCulture) < 1 || int.Parse(parts[1], CultureInfo.InvariantCulture) > 12)) p.Add(Bad("needs a month 1–12"));
                    return;
                case "monthsIn":
                    if (Arity(2) && !Int(parts[1])) p.Add(Bad("needs a number of months"));
                    return;
                case "machine":
                    if (parts.Length == 2 && parts[1] == "assessed") return;
                    if (parts.Length == 3 && parts[1] == "steps" && Int(parts[2])) return;
                    p.Add(Bad("must be machine:assessed or machine:steps:N"));
                    return;
                case "project":
                    if (!Arity(3)) return;
                    if (c.Projects.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown project"));
                    if (parts[2] != "done") p.Add(Bad("must end in ':done'"));
                    return;
                case "regard":
                case "regardBelow":
                    if (!Arity(3)) return;
                    if (c.People.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown person"));
                    if (!Int(parts[2])) p.Add(Bad("needs a number"));
                    return;
                case "challenge":
                    if (!Arity(3)) return;
                    if (c.Challenges.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown challenge"));
                    if (!Enum.TryParse<ChallengeStatus>(parts[2], out _)) p.Add(Bad("has an unknown status"));
                    return;
                case "stage":
                    if (!Arity(3)) return;
                    if (c.Challenges.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown challenge"));
                    if (!Int(parts[2])) p.Add(Bad("needs a stage number"));
                    return;
                case "invented":
                    if (Arity(2) && c.Inventions.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown invention"));
                    return;
                case "join":
                    if (Arity(2) && c.Institutions.All(x => x.Id != parts[1])) p.Add(Bad("names an unknown institution"));
                    return;
                case "promise":
                    Arity(1);
                    return;
                default:
                    p.Add(Bad("has an unknown prefix"));
                    return;
            }
        }
    }
}
