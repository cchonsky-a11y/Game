using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The first return (Part II prototype, 2026-10-04; docs/FIRST_RETURN_PROTOTYPE_2026-10-04.md). After the first jump the
    /// arrival beats are an impression; the experience is the city itself: a few places, chosen from what actually happened, each
    /// offering something you recognize, something that contradicts it, and a lead to look closer. Nothing says "because you did
    /// X"; every site keeps the first-life events that justify it (hidden; the causes of its visit in the log). The second jump
    /// waits until enough of the return has been seen. Selection draws no randomness: it is decided by state alone.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>True while the first return is open and not yet finished: the machine won't take you on until it is.</summary>
        public bool ReturnPending => World.Return != null && !World.Return.Completed;

        public int ReturnVisitsRequired => T.GetInt("return.visitsRequired");

        /// <summary>Whether the return has been seen enough to be finished (it never needs every site).</summary>
        public bool ReturnCanComplete => World.Return != null && !World.Return.Completed &&
                                         World.Return.Visited.Count >= Math.Min(ReturnVisitsRequired, World.Return.Sites.Count);

        // ---- elapsed time: the core cast ---------------------------------------------------------------------------------

        /// <summary>
        /// What has become of a person by a year (PROPOSED P1-25): alive (themselves, or old past return.elderAge), or dead at the
        /// age they die at, carried by heirs for return.heirsYears and a memory after. Deterministic: no mortality draw. Null for
        /// people outside the core cast (no age authored).
        /// </summary>
        public (HumanBand Band, int Age, int SinceDeath)? HumanBandAt(PersonDef p, int year)
        {
            if (p.Age <= 0 || p.LivesTo <= 0) return null;
            int age = p.Age + (year - T.GetInt("time.startYear"));
            if (age < p.LivesTo) return (age >= T.GetInt("return.elderAge") ? HumanBand.Elder : HumanBand.Self, age, 0);
            int since = age - p.LivesTo;
            return (since <= T.GetInt("return.heirsYears") ? HumanBand.Heirs : HumanBand.Memory, age, since);
        }

        /// <summary>A band's text, falling back to its neighbour if the person can't reach it with the jumps P1 allows.</summary>
        private static string BandText(ReturnSiteDef d, HumanBand band)
        {
            var order = band switch
            {
                HumanBand.Self => new[] { HumanBand.Self, HumanBand.Elder, HumanBand.Heirs, HumanBand.Memory },
                HumanBand.Elder => new[] { HumanBand.Elder, HumanBand.Self, HumanBand.Heirs, HumanBand.Memory },
                HumanBand.Heirs => new[] { HumanBand.Heirs, HumanBand.Memory, HumanBand.Elder, HumanBand.Self },
                _ => new[] { HumanBand.Memory, HumanBand.Heirs, HumanBand.Elder, HumanBand.Self },
            };
            foreach (var b in order) if (d.Bands.TryGetValue(b, out var text)) return text;
            return "";
        }

        // ---- the journal --------------------------------------------------------------------------------------------------

        /// <summary>Each month in the first life: a journal line is written the first month its requirements hold.</summary>
        private void WriteJournal()
        {
            if (JumpsMade > 0 || IsAway) return;
            foreach (var a in Data.Content.JournalAnchors)
            {
                if (World.Journal.Any(j => j.Id == a.Id) || !a.Requires.All(Holds)) continue;
                var causes = a.Requires.Select(EventBehind).Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
                var e = Record("journal.note", "journal." + a.Id, causes, new[] { "player" }, null, "In your journal: “" + a.Then + "”");
                World.Journal.Add(new JournalEntry(a.Id, Now.Year, a.Then, e.Id));
            }
        }

        /// <summary>The journal as written, oldest first.</summary>
        public IEnumerable<string> JournalLines() => World.Journal.Select(j => "AD " + j.Year + ": “" + j.Text + "”");

        // ---- starting the return ------------------------------------------------------------------------------------------

        /// <summary>The first return being prepared during the arrival (its sites decide which beat lines they replace).</summary>
        private ReturnChapter? _preparingReturn;

        /// <summary>At the first arrival, before the beats: choose the sites from what happened and fix their words.</summary>
        private void PrepareReturnChapter(Arrival arrival, int departId)
        {
            var r = new ReturnChapter { DepartureYear = arrival.DepartureYear, ArrivalYear = Now.Year, JumpYears = arrival.JumpYears };
            foreach (var site in ChooseReturnSites(r, departId)) r.Sites.Add(site);
            _preparingReturn = r;
        }

        /// <summary>True if a site of the return being prepared tells this arrival echo line instead (kind:id).</summary>
        private bool CoveredByReturn(string echoKey) => _preparingReturn != null && _preparingReturn.Sites.Any(s => s.Covers.Contains(echoKey));

        /// <summary>After the beats: open the chapter.</summary>
        private void StartReturnChapter(int departId)
        {
            var r = _preparingReturn!;
            _preparingReturn = null;
            World.Return = r;
            Record("return.begin", "return", new[] { departId }, new[] { "world" }, null,
                "AD " + Now.Year + ". There are " + r.Sites.Count + " places to look: " + string.Join("; ", r.Sites.Select(s => s.Place)) + ".");
        }

        /// <summary>
        /// The sites, deterministically: every candidate whose requirements hold, then the category order from returns.json,
        /// taking the next unused candidate of each category (at most return.maxPerCategory each) up to return.sitesMax. People
        /// come closest first (regard, then authored order).
        /// </summary>
        private List<ReturnSite> ChooseReturnSites(ReturnChapter r, int departId)
        {
            var candidates = ReturnCandidates(r);
            // A site whose requirements are all unconditional (where you came down, where you began) is grounded in the jump itself.
            foreach (var s in candidates.Where(s => s.Grounds.Count == 0)) s.Grounds.Add(departId);

            int max = T.GetInt("return.sitesMax"), perCategory = T.GetInt("return.maxPerCategory");
            var chosen = new List<ReturnSite>();
            bool Take(ReturnCategory c)
            {
                if (chosen.Count >= max || chosen.Count(s => s.Category == c) >= perCategory) return false;
                var next = candidates.FirstOrDefault(s => s.Category == c && !chosen.Contains(s));
                if (next == null) return false;
                chosen.Add(next);
                return true;
            }
            foreach (var c in Data.Content.ReturnOrder) Take(c);
            // Room left (a run without some kinds of evidence): fill in category order again, the same caps.
            for (bool more = true; more && chosen.Count < max;)
            {
                more = false;
                foreach (var c in Data.Content.ReturnOrder) more |= Take(c);
            }
            return chosen;
        }

        /// <summary>Every site that could be found for this return, people closest first, before the category order picks.</summary>
        internal List<ReturnSite> ReturnCandidates(ReturnChapter r)
        {
            var candidates = new List<ReturnSite>();
            var people = Data.Content.ReturnSites.Where(d => d.Category == ReturnCategory.Human)
                .Select((d, i) => (d, i)).OrderByDescending(x => PersonOf(x.d.Person)?.Regard ?? 0).ThenBy(x => x.i).Select(x => x.d);
            foreach (var d in people.Concat(Data.Content.ReturnSites.Where(d => d.Category != ReturnCategory.Human)))
                if (d.Requires.All(Holds) && BuildSite(d, r) is ReturnSite s) candidates.Add(s);
            foreach (var a in Data.Content.JournalAnchors)
                if (World.Journal.FirstOrDefault(j => j.Id == a.Id) is JournalEntry entry) candidates.Add(BuildJournalSite(a, entry));
            return candidates;
        }

        /// <summary>
        /// For tests and inspection: the sites a return would offer if you arrived in this year, from the state as it stands. Read
        /// only (no log entry, no randomness).
        /// </summary>
        internal List<ReturnSite> ReturnSitesAt(int arrivalYear, int departId = 0)
        {
            var r = new ReturnChapter { DepartureYear = Now.Year, ArrivalYear = arrivalYear, JumpYears = arrivalYear - Now.Year };
            return ChooseReturnSites(r, departId);
        }

        private ReturnSite? BuildSite(ReturnSiteDef d, ReturnChapter r)
        {
            var v = d.Variants.FirstOrDefault(x => x.Requires.All(Holds));
            if (v == null) return null;
            var values = new Dictionary<string, string>
            {
                { "years", r.JumpYears.ToString(CultureInfo.InvariantCulture) },
                { "year", r.ArrivalYear.ToString(CultureInfo.InvariantCulture) },
                { "departed", r.DepartureYear.ToString(CultureInfo.InvariantCulture) },
            };
            var site = new ReturnSite
            {
                Id = d.Id, Category = d.Category, Variant = v.Id, Place = v.Place.Length > 0 ? v.Place : d.Place, Person = d.Person,
                Recognition = v.Recognition, Contradiction = v.Contradiction, Lead = v.Lead, Investigation = v.Investigation,
                Evidence = v.Evidence, Misattributed = v.Misattributed, Thread = v.Thread,
            };
            site.Covers.AddRange(d.Covers);
            if (d.Person.Length > 0 && PersonDefOf(d.Person) is PersonDef p && HumanBandAt(p, r.ArrivalYear) is var band && band != null)
            {
                site.Band = band.Value.Band;
                int since = band.Value.SinceDeath;
                values["sinceDeath"] = since == 0 ? "this year" : since == 1 ? "a year ago" : since.ToString(CultureInfo.InvariantCulture) + " years ago";
                values["age"] = band.Value.Age.ToString(CultureInfo.InvariantCulture);
                site.Recognition = BandText(d, band.Value.Band);
            }
            site.Recognition = Fill(site.Recognition, values);
            site.Contradiction = Fill(site.Contradiction, values);
            site.Investigation = Fill(site.Investigation, values);
            foreach (var id in d.Requires.Concat(v.Requires).Select(EventBehind).Where(id => id != null).Select(id => id!.Value).Distinct())
                site.Grounds.Add(id);
            return site;
        }

        private ReturnSite BuildJournalSite(JournalAnchorDef a, JournalEntry entry)
        {
            var now = a.Now.First(n => n.Requires.All(Holds));
            var site = new ReturnSite
            {
                Id = "journal-" + a.Id, Category = ReturnCategory.Journal, Variant = now.Id, Place = now.Where,
                Recognition = "In your journal, AD " + entry.Year + ", you wrote: “" + entry.Text + "”",
                Contradiction = "What survives here: “" + now.Text + "”", Lead = now.Lead, Investigation = now.Investigation,
                Evidence = "contested", Misattributed = now.Misattributed,
            };
            site.Grounds.Add(entry.EventId);
            foreach (var id in now.Requires.Select(EventBehind).Where(id => id != null).Select(id => id!.Value).Distinct())
                if (!site.Grounds.Contains(id)) site.Grounds.Add(id);
            return site;
        }

        private static string Fill(string text, IReadOnlyDictionary<string, string> values)
        {
            foreach (var kv in values) text = text.Replace("{" + kv.Key + "}", kv.Value);
            return text;
        }

        /// <summary>
        /// The last event behind a requirement (a requirement that holds): what made it true in the log. Null when the log has
        /// no single event for it (a negation, a month, a regard level).
        /// </summary>
        internal int? EventBehind(string requirement)
        {
            if (requirement.StartsWith("!", StringComparison.Ordinal)) return null;
            if (requirement.IndexOf('|') >= 0)
                return requirement.Split('|').Where(Holds).Select(EventBehind).FirstOrDefault(id => id != null);
            var parts = requirement.Split(':');
            GameEvent? last = parts[0] switch
            {
                "commission" => Log.Events.LastOrDefault(ev => ev.Target == "commission:" + parts[1]),
                "access" => Log.Events.LastOrDefault(ev => ev.Effects.Any(f => f.Key == "access." + parts[1])),
                "capability" => Log.Events.LastOrDefault(ev => ev.Target == "capability." + parts[1] && ev.Type == "capability.advance"),
                "challenge" or "stage" => Log.Events.LastOrDefault(ev => ev.Target == "challenge:" + parts[1]),
                "project" => Log.Events.LastOrDefault(ev => ev.Target == parts[1] && ev.Type.StartsWith("project.", StringComparison.Ordinal)),
                "scene" => Log.Events.LastOrDefault(ev => ev.Target == parts[1] || ev.Target == "scene:" + parts[1]),
                "answered" => Log.Events.LastOrDefault(ev => ev.Type == "event.decide" && ev.Target == parts[1]),
                "journal" => Log.Events.LastOrDefault(ev => ev.Target == "journal." + parts[1]),
                "knows" => Log.Events.FirstOrDefault(ev => ev.Actors.Contains(parts[1])),
                "flag" => _eventChoices.LastOrDefault(c => c.Option.Sets.Contains(parts[1])) is var ch && ch.Event != null
                    ? Log.Events.LastOrDefault(ev => ev.Type == "event.decide" && ev.Target == ch.Event.Id) : null,
                _ => null,
            };
            if (last != null) return last.Id;
            if (parts[0] == "life" && World.LifeEventLog.TryGetValue(parts[1], out var id)) return id;
            return null;
        }

        // ---- exploring ----------------------------------------------------------------------------------------------------

        /// <summary>The sites, numbered, with what you have done at each.</summary>
        public IEnumerable<string> ReturnLeads()
        {
            var r = World.Return;
            if (r == null) yield break;
            for (int k = 0; k < r.Sites.Count; k++)
            {
                var s = r.Sites[k];
                string mark = r.Investigated.Contains(s.Id) ? " (looked closer)" : r.Visited.Contains(s.Id) ? " (seen)" : "";
                yield return (k + 1) + ". " + s.Place + mark;
            }
        }

        /// <summary>A site by its number in the list or its id, or null.</summary>
        public ReturnSite? FindReturnSite(string key)
        {
            var r = World.Return;
            if (r == null) return null;
            string k = (key ?? "").Trim().ToLowerInvariant();
            if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) return n >= 1 && n <= r.Sites.Count ? r.Sites[n - 1] : null;
            return r.Sites.FirstOrDefault(s => s.Id == k);
        }

        /// <summary>Goes to a site: what you recognize there, and what doesn't fit. The first visit is logged with its grounds.</summary>
        public CommandResult VisitReturnSite(string key)
        {
            var r = World.Return;
            if (r == null) return CommandResult.Fail("There is no return to explore.");
            var s = FindReturnSite(key);
            if (s == null) return CommandResult.Fail("Where? " + string.Join(" · ", ReturnLeads()));
            string text = s.Place + ". " + s.Recognition + " " + s.Contradiction + " (Look closer: " + s.Lead + ".)";
            if (!r.Visited.Contains(s.Id))
            {
                r.Visited.Add(s.Id);
                Record("return.visit", "return." + s.Id, s.Grounds, new[] { "player" }, null, s.Place + ". " + s.Recognition + " " + s.Contradiction);
            }
            return CommandResult.Success(text);
        }

        /// <summary>Follows a visited site's lead.</summary>
        public CommandResult InvestigateReturnSite(string key)
        {
            var r = World.Return;
            if (r == null) return CommandResult.Fail("There is no return to explore.");
            var s = FindReturnSite(key);
            if (s == null) return CommandResult.Fail("Where? " + string.Join(" · ", ReturnLeads()));
            if (!r.Visited.Contains(s.Id)) return CommandResult.Fail("Go there first (visit " + (r.Sites.IndexOf(s) + 1) + ").");
            if (!r.Investigated.Contains(s.Id))
            {
                r.Investigated.Add(s.Id);
                Record("return.investigate", "return." + s.Id, s.Grounds, new[] { "player" }, null, Cap(s.Lead) + ": " + s.Investigation);
            }
            return CommandResult.Success(Cap(s.Lead) + ". " + s.Investigation);
        }

        /// <summary>Ends the return chapter once enough of it has been seen; the machine will take you on after this.</summary>
        public CommandResult CompleteReturn()
        {
            var r = World.Return;
            if (r == null) return CommandResult.Fail("There is no return to finish.");
            if (r.Completed) return CommandResult.Fail("You have already finished looking.");
            if (!ReturnCanComplete)
                return CommandResult.Fail("You have seen " + r.Visited.Count + " of the places; look at " + Math.Min(ReturnVisitsRequired, r.Sites.Count) + " before you go on.");
            r.Completed = true;
            Record("return.complete", "return", null, new[] { "player" }, null,
                "You stop looking. You saw " + r.Visited.Count + " of " + r.Sites.Count + " places and looked closer at " + r.Investigated.Count + ".");
            return CommandResult.Success("You have seen enough of this Rome. The machine is ready when you are.");
        }

        // ---- before leaving -----------------------------------------------------------------------------------------------

        /// <summary>
        /// What you would leave unresolved (the departure briefing): facts, never what they will lead to. Only before the first
        /// jump; staying long enough to settle one changes what the return holds.
        /// </summary>
        public IEnumerable<string> DepartureStakes()
        {
            if (JumpsMade > 0 || Arrived) yield break;
            var standards = FindChallenge("standards");
            if (standards != null && standards.Status != ChallengeStatus.NotYet && standards.Status != ChallengeStatus.Done
                && CapabilityLevelOf("gauges") < CapabilityLevel.Reproducible)
                yield return "The shared foot isn't finished: there is one set of gauges, tried against nothing but your word.";
            if (Holds("life:pollio-copy") && !Holds("access:guild:Member"))
                yield return "Pollio is selling seats cut by eye, and nothing on a pump says yours are different.";
            if (Knows("Marcus") && !Holds("life:marcus-stays") && !Holds("life:marcus-priscus"))
                yield return "Marcus still thinks he should be a journeyman, and hasn't decided whose.";
            var power = FindChallenge("power");
            if (power != null && power.StageIndex > 0 && power.Status != ChallengeStatus.Done)
                yield return "Aulus's shaft has run " + power.StageIndex + " of its " + ChallengeDefOf(power).Stages.Count + " stages. Nobody else knows why it is built the way it is.";
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Working))
                yield return Cap(CommissionDefOf(c).Client) + "'s job is half done.";
        }
    }
}
