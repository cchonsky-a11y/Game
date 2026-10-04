using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Decision events (decided 2026-09-28, P0-33) and leaders' requests (P0-32): dated choices from Rome's history, and
    /// requests from the leaders of institutions you belong to, each with a cost. One comes on the turn that covers its date
    /// if its requirement holds then (otherwise it passes), waits for your choice, and lapses to its last option (staying
    /// out, or refusing) after a few turns.
    /// </summary>
    public sealed partial class Simulation
    {
        private readonly HashSet<string> _eventsSeen = new HashSet<string>();
        private string? _pendingEvent;
        /// <summary>The choices you made yourself (not lapsed), in the order made: what Rome may remember at arrival.</summary>
        private readonly List<(EventDef Event, EventOptionDef Option)> _eventChoices = new List<(EventDef, EventOptionDef)>();
        /// <summary>The answers you gave yourself, as "event:option", in the order given.</summary>
        public IEnumerable<string> EventAnswers => _eventChoices.Select(c => c.Event.Id + ":" + c.Option.Id);
        private readonly HashSet<string> _marksShown = new HashSet<string>();
        private int _pendingSinceTurn;

        public EventDef? PendingEvent => _pendingEvent == null ? null : Data.Content.Events.First(e => e.Id == _pendingEvent);

        /// <summary>What an option costs you in gold now (prices scale it), or 0 if it pays or costs nothing.</summary>
        public double EventCost(EventOptionDef o) =>
            Math.Max(0, -o.Effects.Where(e => e.Type == "gold").Sum(e => e.Value)) * World.PriceLevel;

        private bool EventRequirementHolds(EventDef e)
        {
            if (e.Requires == "any") return true;
            if (e.Requires == "workshop") return OwnsWorkshop;
            if (e.Requires.StartsWith("workshopBelow:", StringComparison.Ordinal))
                return OwnsWorkshop && Math.Max(WorkshopSize, World.WorkshopBuildingTo) < int.Parse(e.Requires.Substring(14), System.Globalization.CultureInfo.InvariantCulture);
            if (e.Requires == "member") return World.Institutions.Any(i => i.Backed && !i.Def.IsOwn);
            if (e.Requires.StartsWith("member:", StringComparison.Ordinal)) return World.Institution(e.Requires.Substring(7)).Backed;
            throw new InvalidOperationException("Unknown event requirement: " + e.Requires);
        }

        /// <summary>At the start of each turn: a waiting choice lapses, or the next one due comes up.</summary>
        private void AdvanceEvents()
        {
            if (_pendingEvent != null)
            {
                if (Turn - _pendingSinceTurn < T.GetInt("events.lapseTurns")) return;
                var e = PendingEvent!;
                Resolve(e, e.Options[e.Options.Count - 1], lapsed: true);
            }
            if (World.TriggeredEvents.Count > 0)
            {
                var t = Data.Content.Events.First(x => x.Id == World.TriggeredEvents[0]);
                World.TriggeredEvents.RemoveAt(0);
                _eventsSeen.Add(t.Id);
                _pendingEvent = t.Id;
                _pendingSinceTurn = Turn;
                World.ScenePacing.Record(t.Category);   // a consequence of your own actions: it comes, unrouted
                Record("event.offer", t.Id, null, new[] { "world" }, null,
                    t.Title + ". " + t.Text + " (" + string.Join(" / ", t.Options.Select(o => "decide " + o.Id)) + ")");
                return;
            }
            var horizon = Now.AddMonths(MonthsPerTurn - 1).TotalMonths;
            foreach (var e in Data.Content.Events.Where(x => !x.Triggered && !_eventsSeen.Contains(x.Id) && x.Time.TotalMonths <= horizon))
            {
                _eventsSeen.Add(e.Id);
                if (!EventRequirementHolds(e)) continue;          // it passes: you weren't in a position to be asked
                _pendingEvent = e.Id;
                _pendingSinceTurn = Turn;
                World.ScenePacing.Record(SceneCategory.CityHistory);   // Rome's dated choices interrupt; they are never routed
                Record("event.offer", e.Id, null, new[] { "world" }, null,
                    e.Title + ". " + e.Text + " (" + string.Join(" / ", e.Options.Select(o => "decide " + o.Id)) + ")");
                return;
            }
        }

        public CommandResult Decide(string optionId)
        {
            var e = PendingEvent;
            if (e == null) return CommandResult.Fail("Nothing is waiting for your decision.");
            var o = e.Options.FirstOrDefault(x => x.Id.Equals((optionId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) ??
                    e.Options.FirstOrDefault(x => x.Id.StartsWith((optionId ?? "").Trim().ToLowerInvariant(), StringComparison.Ordinal) && (optionId ?? "").Trim().Length > 0);
            if (o == null) return CommandResult.Fail("Decide " + string.Join(", ", e.Options.Select(x => x.Id + " (" + x.Label + ")")) + ".");
            double cost = EventCost(o);
            if (cost > World.Gold + 1e-9) return CommandResult.Fail("That would cost " + Money(cost) + "; you have " + Money(World.Gold) + ".");
            Resolve(e, o, lapsed: false);
            return CommandResult.Success(o.Text);
        }

        /// <summary>A waiting choice passes you by when you leave Rome.</summary>
        private void LapsePendingEvent()
        {
            var e = PendingEvent;
            if (e != null) Resolve(e, e.Options[e.Options.Count - 1], lapsed: true);
        }

        private void Resolve(EventDef e, EventOptionDef o, bool lapsed)
        {
            _pendingEvent = null;
            if (!lapsed) _eventChoices.Add((e, o));
            var ev = Record("event.decide", e.Id, null, new[] { lapsed ? "world" : "player" }, null,
                e.Title + ": " + (lapsed ? "you let it pass (" + o.Label + "). " : "you chose to " + o.Label + ". ") + o.Text);
            ApplyEffects(e.Title, o.Effects, new[] { ev.Id });
            if (lapsed) return;
            foreach (var flag in o.Sets) World.Flags.Add(flag);
            StreetYearCheck(ev.Id);
        }

        /// <summary>How many of your answers were of a kind (generous, profit, ...).</summary>
        public int AnswersOfStyle(string style) => _eventChoices.Count(c => c.Option.Style == style);

        /// <summary>
        /// The street keeps count (P0-33, answers stack): profiteer twice and the Subura turns against you (Governance debt, work
        /// pays less); give generously three times and people seek you out (work pays more).
        /// </summary>
        private void StreetYearCheck(int causeId)
        {
            if (!World.Flags.Contains("streetAgainst") && AnswersOfStyle("profit") >= T.GetInt("events.street.profitsToTurn"))
            {
                World.Flags.Add("streetAgainst");
                AddDebt(Domain.Governance, T.Get("events.street.governanceDebt"), "event.street", causeId,
                    "The Subura has seen you profit from its bad years twice. Children throw mud at your door; buyers find other sellers.");
            }
            if (!World.Flags.Contains("streetFor") && AnswersOfStyle("generous") >= T.GetInt("events.street.generousToWin"))
            {
                World.Flags.Add("streetFor");
                Record("event.street", "street", new[] { causeId }, new[] { "world" }, null,
                    "The Subura knows your name now, and says it kindly: people come looking for the foreigner who helps.");
            }
        }

        /// <summary>What the street's memory does to your pay: +/- events.street.workPay.</summary>
        public double StreetWorkFactor() =>
            1 + (World.Flags.Contains("streetFor") ? T.Get("events.street.workPay") : 0) - (World.Flags.Contains("streetAgainst") ? T.Get("events.street.workPay") : 0);

        private bool EffectConditionHolds(string? cond)
        {
            if (cond == null) return true;
            if (cond.StartsWith("own:", StringComparison.Ordinal)) return OwnStands(World.Institution(cond.Substring(4)));
            if (cond.StartsWith("not:", StringComparison.Ordinal)) return !World.Flags.Contains(cond.Substring(4));
            return World.Flags.Contains(cond);
        }

        /// <summary>Applies authored effects (decision events, workshop orders): gold, aurei, levels, debt, loyalty, lean, income, the workshop, the smith.</summary>
        internal void ApplyEffects(string title, IEnumerable<EventEffect> effects, int[] causes)
        {
            foreach (var fx in effects)
            {
                if (!EffectConditionHolds(fx.If)) continue;
                switch (fx.Type)
                {
                    case "gold":
                    {
                        double before = World.Gold, amount = fx.Value * World.PriceLevel;
                        World.Gold = Math.Max(0, World.Gold + amount);
                        Record("event.gold", GoldKey, causes, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                            title + ": " + (amount >= 0 ? "you gain " : "it costs you ") + Money(Math.Abs(amount)) + ".");
                        break;
                    }
                    case "aurei":
                    {
                        double before = World.Aurei;
                        World.Aurei += fx.Value;
                        Record("event.aurei", "aurei", causes, new[] { "player" }, new[] { new Effect("aurei", before, World.Aurei) }, title + ": " + AureiText(fx.Value) + " in gold.");
                        break;
                    }
                    case "level":
                        if (DomainInfo.TryParseDomain(fx.Domain ?? "", out var d))
                            ChangeLevel(d, fx.Value, "event.effect", causes, new[] { "player" }, title + ": " + d + " " + Signed(fx.Value) + ".");
                        break;
                    case "debt":
                        if (DomainInfo.TryParseDomain(fx.Domain ?? "", out var dd))
                            AddDebt(dd, fx.Value, "event.effect", causes[0], title + ": " + dd + " debt +" + F(fx.Value) + ".");
                        break;
                    case "loyalty":
                        foreach (var i in EventInstitutions(fx.Institution))
                            Grieve(i, fx.Value, "institution.loyalty", causes, new[] { i.Leader },
                                title + ": " + (YouLead(i) ? "the members of " + i.Def.ShortName + (fx.Value >= 0 ? " think better of you." : " think less of you.")
                                                                     : i.Leader + " of " + i.Def.ShortName + (fx.Value >= 0 ? " thinks better of you." : " thinks less of you.")));
                        break;
                    case "lean":
                        foreach (var i in EventInstitutions(fx.Institution))
                        {
                            double before = i.Lean;
                            i.Lean = Math.Max(-1, Math.Min(1, i.Lean + fx.Value));
                            Record("institution.lean", i.Key, causes, new[] { "player" }, new[] { new Effect(i.Key + ".lean", before, i.Lean) },
                                title + ": " + CampName(i, fx.Value > 0 ? 0 : 1) + " gain ground in " + i.Def.ShortName + ".");
                        }
                        break;
                    case "income":
                    {
                        double before = World.InventionIncome;
                        World.InventionIncome += fx.Value;
                        Record("income.bonus", GoldKey, causes, new[] { "player" }, new[] { new Effect("income.inventions", before, World.InventionIncome) },
                            title + ": it pays you " + Money(fx.Value) + " a year.");
                        break;
                    }
                    case "workshop":
                    {
                        double before = World.WorkshopBonus;
                        World.WorkshopBonus += fx.Value;
                        Record("workshop.output", "workshop", causes, new[] { "player" }, new[] { new Effect("income.workshop", before, World.WorkshopBonus) },
                            title + ": the workshop's income " + (fx.Value >= 0 ? "rises " : "falls ") + F(Math.Abs(fx.Value) * 100) + "%.");
                        break;
                    }
                    case "stake":
                        // Favor turns into standing (P0-33): members of the institution gain stake, as inventions give it.
                        foreach (var i in EventInstitutions(fx.Institution).Where(x => x.Backed && !x.Def.IsOwn))
                        {
                            double before = i.Stake;
                            i.Stake = Math.Max(before, Math.Min(ExclusiveCapPercent(i) / 100.0, Math.Min(1, (StakePercent(i) + (int)fx.Value) / 100.0)));
                            if (i.Stake > before)
                                Record("institution.stake", i.Key, causes, new[] { "player", i.Leader }, new[] { new Effect(StakeKey(i), before, i.Stake) },
                                    title + ": " + (YouLead(i) ? "your say in " + i.Def.ShortName + " grows" : i.Leader + " gives you a larger say in " + i.Def.ShortName) + ": " + StakePercent(i) + "%." + Crossed(before, i.Stake));
                        }
                        break;
                    case "standing":
                        // P1 (2026-10-04): favor with an institution, not a share of it. Members' leaders think better of them;
                        // a house you don't belong to takes an interest in you (its regard), which its people remember.
                        foreach (var i in EventInstitutions(fx.Institution))
                        {
                            if (i.Backed) { Grieve(i, fx.Value, "institution.loyalty", causes, new[] { i.Leader }, title + ": " + (YouLead(i) ? "the members of " + i.Def.ShortName + " think better of you." : i.Leader + " of " + i.Def.ShortName + " thinks better of you.")); continue; }
                            double before = i.Regard;
                            i.Regard += fx.Value;
                            Record("institution.regard", i.Key, causes, new[] { i.Leader }, new[] { new Effect(i.Key + ".regard", before, i.Regard) },
                                title + ": " + Cap(i.Def.ShortName) + " takes notice of you.");
                        }
                        break;
                    case "resilience":
                    {
                        double before = World.PlagueResilienceBonus;
                        World.PlagueResilienceBonus += fx.Value;
                        Record("plague.preparation", "plague", causes, new[] { "player" }, new[] { new Effect("plague.resilience", before, World.PlagueResilienceBonus) },
                            title + ": Rome is a little better prepared for a pestilence.");
                        break;
                    }
                    case "workshopSize":
                        if (OwnsWorkshop && WorkshopSize < (int)fx.Value) SetWorkshopSize((int)fx.Value, causes, title + ".");
                        break;
                    case "regard":
                    {
                        var p = PersonOf(fx.Person ?? "") ?? throw new InvalidOperationException("Unknown person in an event: " + fx.Person);
                        int before = p.Regard;
                        p.Regard += (int)fx.Value;
                        Record("person.regard", "person." + p.Id, causes, new[] { "player", p.Id }, new[] { new Effect("person." + p.Id + ".regard", before, p.Regard) },
                            title + ": " + PersonDefOf(p.Id)!.Name + (fx.Value >= 0 ? " thinks better of you." : " thinks less of you."));
                        break;
                    }
                    case "status":
                    {
                        var p = PersonOf(fx.Person ?? "") ?? throw new InvalidOperationException("Unknown person in an event: " + fx.Person);
                        p.Status = fx.Text ?? p.Status;
                        break;
                    }
                    case "smith":
                        ChangeSmithRegard(fx.Value, causes, title + ": " + Data.Content.Smith + (fx.Value >= 0 ? " thinks better of you." : " thinks less of you."));
                        break;
                    default: throw new InvalidOperationException("Unknown event effect: " + fx.Type);
                }
            }
        }

        /// <summary>
        /// What your answers left in Rome (P0-33, revised 2026-09-28): up to events.marksPerArrival lines for the Personal echo,
        /// present conditions only. A later arrival shows the choices not yet shown first, in their aged form.
        /// </summary>
        private List<string> EventMarks(bool later)
        {
            var lines = new List<string>();
            var candidates = _eventChoices.Where(c => c.Option.Mark != null)
                .OrderBy(c => later && _marksShown.Contains(c.Event.Id) ? 1 : 0).ToList();
            foreach (var (e, o) in candidates)
            {
                if (lines.Count >= T.GetInt("events.marksPerArrival")) break;
                string? line;
                if (o.MarkInstitution != null)
                {
                    var i = World.Institution(o.MarkInstitution);
                    bool standing = i.Exists && !i.Collapsed && OutcomeOf(i) != InstitutionOutcome.Dissolved;
                    line = standing ? (later ? o.Mark2 ?? o.Mark : o.Mark) : (later ? o.MarkGone2 ?? o.MarkGone : o.MarkGone);
                    if (line != null && standing)
                    {
                        string name = CurrentName(i);
                        line = line.Replace("{inst}", name).Replace("{Inst}", Cap(name));
                    }
                }
                else line = later ? o.Mark2 ?? o.Mark : o.Mark;
                // Never the same line twice across arrivals (Corey, 2026-10-04).
                if (line == null || World.EchoesShown.Contains(line)) continue;
                World.EchoesShown.Add(line);
                _marksShown.Add(e.Id);
                lines.Add(line);
            }
            return lines;
        }

        private IEnumerable<Institution> EventInstitutions(string? id) =>
            id == "members" ? World.Institutions.Where(i => i.Backed && !i.Def.IsOwn).ToList()
            : id == null ? Enumerable.Empty<Institution>() : new[] { World.Institution(id) }.Where(i => i.Exists);
    }
}
