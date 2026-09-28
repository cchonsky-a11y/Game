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
        private readonly HashSet<string> _marksShown = new HashSet<string>();
        private int _pendingSinceTurn;

        public EventDef? PendingEvent => _pendingEvent == null ? null : Data.Content.Events.First(e => e.Id == _pendingEvent);

        /// <summary>What an option costs you in gold now (prices scale it), or 0 if it pays or costs nothing.</summary>
        public double EventCost(EventOptionDef o) =>
            Math.Max(0, -o.Effects.Where(e => e.Type == "gold").Sum(e => e.Value)) * World.PriceLevel;

        private bool EventRequirementHolds(EventDef e)
        {
            if (e.Requires == "any") return true;
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
            var horizon = Now.AddMonths(MonthsPerTurn - 1).TotalMonths;
            foreach (var e in Data.Content.Events.Where(x => !_eventsSeen.Contains(x.Id) && x.Time.TotalMonths <= horizon))
            {
                _eventsSeen.Add(e.Id);
                if (!EventRequirementHolds(e)) continue;          // it passes: you weren't in a position to be asked
                _pendingEvent = e.Id;
                _pendingSinceTurn = Turn;
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
            var causes = new[] { ev.Id };
            foreach (var fx in o.Effects)
            {
                switch (fx.Type)
                {
                    case "gold":
                    {
                        double before = World.Gold, amount = fx.Value * World.PriceLevel;
                        World.Gold = Math.Max(0, World.Gold + amount);
                        Record("event.gold", GoldKey, causes, new[] { "player" }, new[] { new Effect(GoldKey, before, World.Gold) },
                            e.Title + ": " + (amount >= 0 ? "you gain " : "it costs you ") + Money(Math.Abs(amount)) + ".");
                        break;
                    }
                    case "aurei":
                    {
                        double before = World.Aurei;
                        World.Aurei += fx.Value;
                        Record("event.aurei", "aurei", causes, new[] { "player" }, new[] { new Effect("aurei", before, World.Aurei) }, e.Title + ": " + AureiText(fx.Value) + " in gold.");
                        break;
                    }
                    case "level":
                        if (DomainInfo.TryParseDomain(fx.Domain ?? "", out var d))
                            ChangeLevel(d, fx.Value, "event.effect", causes, new[] { "player" }, e.Title + ": " + d + " " + Signed(fx.Value) + ".");
                        break;
                    case "debt":
                        if (DomainInfo.TryParseDomain(fx.Domain ?? "", out var dd))
                            AddDebt(dd, fx.Value, "event.effect", ev.Id, e.Title + ": " + dd + " debt +" + F(fx.Value) + ".");
                        break;
                    case "loyalty":
                        foreach (var i in EventInstitutions(fx.Institution))
                            Grieve(i, fx.Value, "institution.loyalty", causes, new[] { i.Leader },
                                e.Title + ": " + i.Leader + " of " + i.Def.ShortName + (fx.Value >= 0 ? " thinks better of you." : " thinks less of you."));
                        break;
                    case "lean":
                        foreach (var i in EventInstitutions(fx.Institution))
                        {
                            double before = i.Lean;
                            i.Lean = Math.Max(-1, Math.Min(1, i.Lean + fx.Value));
                            Record("institution.lean", i.Key, causes, new[] { "player" }, new[] { new Effect(i.Key + ".lean", before, i.Lean) },
                                e.Title + ": " + CampName(i, fx.Value > 0 ? 0 : 1) + " gain ground in " + i.Def.ShortName + ".");
                        }
                        break;
                    case "income":
                    {
                        double before = World.InventionIncome;
                        World.InventionIncome += fx.Value;
                        Record("income.bonus", GoldKey, causes, new[] { "player" }, new[] { new Effect("income.inventions", before, World.InventionIncome) },
                            e.Title + ": it pays you " + Money(fx.Value) + " a year.");
                        break;
                    }
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
                if (line == null) continue;
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
