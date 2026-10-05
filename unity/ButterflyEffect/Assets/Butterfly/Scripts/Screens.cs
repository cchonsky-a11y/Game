#nullable enable
using System.Collections.Generic;
using System.Linq;
using Butterfly.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Butterfly.Unity
{
    /// <summary>
    /// The slice's screens (P2), each built from the session's screen models. Every button runs a command the simulation
    /// offered; nothing here decides what happens.
    /// </summary>
    public static class Screens
    {
        // ---- the HUD -------------------------------------------------------------------------------------------------

        /// <summary>The HUD over a page: date, Attention, money, the machine, the sections, and the month.</summary>
        public static VisualElement WithHud(ButterflyApp app, VisualElement content)
        {
            var page = Ui.Column();
            page.Add(Hud(app));
            Ui.Fill(content);
            var body = Ui.Box();
            Ui.Fill(body);
            Ui.Pad(body, 14);
            body.Add(content);
            page.Add(body);
            if (!app.Session.Sim.Arrived) page.Add(OutcomeStrip(app));
            return page;
        }

        private static VisualElement Hud(ButterflyApp app)
        {
            var s = app.Session;
            var h = s.Hud();
            var bar = Ui.Row();
            bar.style.backgroundColor = Ui.Ink;
            Ui.Pad(bar, 10);
            bar.style.justifyContent = Justify.SpaceBetween;

            var facts = Ui.Row();
            facts.Add(Fact(h.Date, 20, true));
            if (!h.Away)
            {
                facts.Add(Fact("Attention " + Pips(h.AttentionFree, h.AttentionTotal) + "  " + h.AttentionFree + " free / " + h.AttentionTotal, 16));
                if (h.Reserved.Count > 0) facts.Add(Fact("reserved: " + string.Join(", ", h.Reserved), 13));
                facts.Add(Fact(h.Money + " · " + h.Aurei, 16));
                facts.Add(Fact("Machine: " + h.Machine, 16, h.MachineReady));
            }
            bar.Add(facts);

            var nav = Ui.Row();
            if (!h.Away)
            {
                foreach (var (label, screen) in new[] { ("Rome", Screen.Rome), ("People", Screen.People), ("Work", Screen.Work), ("Machine", Screen.Machine), ("Journal", Screen.Journal), ("Everything", Screen.Everything) })
                    nav.Add(Ui.Button(label, () => app.Go(screen), app.Screen == screen));
                var card = s.CurrentDecision();
                if (card != null && card.Key == app.DismissedDecision)
                    nav.Add(Ui.Button("Waiting: " + card.Title, () => { app.DismissedDecision = ""; app.Refresh(); }, true));
                if (h.MachineReady) nav.Add(Ui.Button("Prepare to leave…", () => { s.OpenDeparture(); app.Refresh(); }));
                nav.Add(Ui.Button("End Month", () => app.Act("end"), true));
                nav.Add(Ui.Button("Fast-forward", () => app.Act("wait")));
            }
            else
            {
                nav.Add(Ui.Button("The city", () => app.Go(Screen.Rome), app.Screen != Screen.Journal));
                nav.Add(Ui.Button("Your journal", () => app.Go(Screen.Journal), app.Screen == Screen.Journal));
            }
            bar.Add(nav);
            return bar;
        }

        private static string Pips(int free, int total) => new string('●', Mathf.Max(0, free)) + new string('○', Mathf.Max(0, total - free));

        private static Label Fact(string text, int size, bool bold = false)
        {
            var l = Ui.Text(text, size, Ui.Paper, bold);
            l.style.marginRight = 18;
            l.style.marginBottom = 0;
            return l;
        }

        /// <summary>What the last action did, said once: the simulation's answer and what happened because of it.</summary>
        private static VisualElement OutcomeStrip(ButterflyApp app)
        {
            var strip = Ui.Column();
            strip.style.backgroundColor = Ui.PaperDark;
            Ui.Pad(strip, 10);
            strip.style.maxHeight = Length.Percent(30);
            var o = app.LastOutcome;
            if (o == null) { strip.Add(Ui.Text("Choose a place on the map, or a section above. Time moves only when you end the month.", 14, Ui.InkSoft)); return strip; }
            var scroll = Ui.Scroll();
            if (o.Message.Length > 0) scroll.Add(Ui.Text(o.Message, 15, o.Ok ? Ui.Ink : Ui.Terracotta));
            foreach (var f in o.Feed) scroll.Add(FeedRow(f));
            strip.Add(scroll);
            return strip;
        }

        public static VisualElement FeedRow(FeedLine f)
        {
            var row = Ui.Row();
            row.style.flexWrap = Wrap.NoWrap;
            row.style.alignItems = Align.FlexStart;
            if (f.Portrait.Length > 0)
            {
                var art = ArtLibrary.Slot(f.Portrait, f.Speaker, 40, 40);
                art.style.marginRight = 8;
                row.Add(art);
            }
            var text = Ui.Text((f.Speaker.Length > 0 ? f.Speaker + ": " : "") + f.Text, 15);
            Ui.Fill(text);
            row.Add(text);
            return row;
        }

        private static void Groups(ButterflyApp app, VisualElement into, IEnumerable<ActionGroup> groups)
        {
            foreach (var g in groups.Where(g => g.Items.Count > 0))
            {
                into.Add(Ui.Text(g.Title, 14, Ui.InkSoft, true));
                var row = Ui.Row();
                foreach (var a in g.Items) row.Add(Ui.Button(Cap(a.Label), () => app.Act(a.Command)));
                into.Add(row);
            }
        }

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ---- the opening ---------------------------------------------------------------------------------------------

        public static VisualElement Opening(ButterflyApp app)
        {
            var page = Ui.Column();
            page.style.backgroundColor = Ui.Night;
            page.style.alignItems = Align.Center;
            Ui.Pad(page, 30);
            var scroll = Ui.Scroll();
            scroll.style.maxWidth = 900;
            var m = app.Session.Opening();
            var art = ArtLibrary.Slot("scene.opening", "Rome", 860, 220);
            art.style.marginBottom = 18;
            scroll.Add(art);
            scroll.Add(Ui.Text(m.Title, 30, Ui.Paper, true));
            foreach (var p in m.Paragraphs) scroll.Add(Ui.Text(p, 18, Ui.Paper));
            var row = Ui.Row();
            row.Add(Ui.Button("Continue", () => { app.OpeningRead = true; app.Refresh(); }, true));
            var seed = new TextField("Seed") { value = app.Seed.ToString() };
            seed.style.width = 200;
            row.Add(seed);
            row.Add(Ui.Button("New game with this seed", () => { if (ulong.TryParse(seed.value, out var n)) app.NewGame(n); }));
            scroll.Add(row);
            page.Add(scroll);
            return page;
        }

        // ---- the narrative view --------------------------------------------------------------------------------------

        public static VisualElement Decision(ButterflyApp app, DecisionCard card)
        {
            var shade = Ui.Box("decision");
            shade.style.position = Position.Absolute;
            shade.style.left = 0; shade.style.right = 0; shade.style.top = 0; shade.style.bottom = 0;
            shade.style.backgroundColor = Ui.Shade;
            shade.style.justifyContent = Justify.Center;
            shade.style.alignItems = Align.Center;
            var c = Ui.Card();
            c.style.maxWidth = 860;
            c.style.width = Length.Percent(90);
            c.style.maxHeight = Length.Percent(90);
            var head = Ui.Row();
            head.style.flexWrap = Wrap.NoWrap;
            if (card.Portrait.Length > 0)
            {
                var art = ArtLibrary.Slot(card.Portrait, card.Speaker, 110, 130);
                art.style.marginRight = 14;
                head.Add(art);
            }
            var titles = Ui.Column();
            Ui.Fill(titles);
            titles.Add(Ui.Heading(card.Title));
            if (card.Speaker.Length > 0) titles.Add(Ui.Text(card.Speaker, 15, Ui.InkSoft, true));
            head.Add(titles);
            c.Add(head);
            var scroll = Ui.Scroll();
            scroll.Add(Ui.Text(card.Text, 17));
            c.Add(scroll);
            var choices = Ui.Column();
            foreach (var a in card.Choices) choices.Add(Ui.Button(Cap(a.Label), () => app.Act(a.Command), true));
            choices.Add(Ui.Button("Not now (it waits in the bar above)", () => { app.DismissedDecision = card.Key; app.Refresh(); }));
            c.Add(choices);
            shade.Add(c);
            return shade;
        }

        // ---- Rome ----------------------------------------------------------------------------------------------------

        public static VisualElement Rome(ButterflyApp app)
        {
            var s = app.Session;
            var page = app.Narrow ? Ui.Column() : Ui.Row();
            page.style.flexWrap = Wrap.NoWrap;
            page.style.alignItems = Align.Stretch;

            var map = Map(app);
            map.style.flexGrow = 3;
            if (app.Narrow) map.style.minHeight = 320;
            page.Add(map);

            var side = Ui.Card();
            side.style.flexGrow = 2;
            side.style.flexBasis = 0;
            side.style.marginLeft = app.Narrow ? 0 : 14;
            var scroll = Ui.Scroll();
            var place = RomeMap.Find(app.SelectedPlace) ?? RomeMap.Places[0];
            var art = ArtLibrary.Slot(place.ArtKey, place.Name, 340, 130);
            art.style.width = Length.Percent(100);
            scroll.Add(art);
            scroll.Add(Ui.Heading(place.Name));
            scroll.Add(Ui.Text(place.Caption, 14, Ui.InkSoft));
            if (place.Id == RomeMap.Lodging)
            {
                string ready = s.ReadyLine();
                if (ready.Length > 0) scroll.Add(Ui.Text(ready, 16, Ui.Terracotta, true));
            }
            scroll.Add(Ui.Button(place.Id == RomeMap.Lodging ? "Look at the machine" : "Look around (free)", () => { app.LookText = s.LookAround(place.Id); app.Refresh(); }));
            if (app.LookText.Length > 0) scroll.Add(Ui.Text(app.LookText, 15));
            Groups(app, scroll, s.ActionsAt(place.Id));
            if (place.Id == RomeMap.Lodging)
            {
                scroll.Add(Ui.Text("In Rome", 14, Ui.InkSoft, true));
                foreach (var line in s.News()) scroll.Add(Ui.Text(line, 14));
            }
            side.Add(scroll);
            page.Add(side);
            return page;
        }

        private static VisualElement Map(ButterflyApp app)
        {
            var map = Ui.Box("map");
            map.style.backgroundColor = new Color(0.80f, 0.73f, 0.58f);
            Ui.Border(map, Ui.Bronze, 2, 10);
            var tex = ArtLibrary.Find("map.rome");
            if (tex != null) map.style.backgroundImage = new StyleBackground(tex);
            else
            {
                // Placeholder: the Tiber as a band and the city as a lighter field.
                var river = Ui.Box();
                river.style.position = Position.Absolute;
                river.style.left = Length.Percent(12); river.style.width = Length.Percent(6);
                river.style.top = 0; river.style.bottom = 0;
                river.style.backgroundColor = new Color(0.45f, 0.58f, 0.62f);
                map.Add(river);
                var label = Ui.Text("ROMA · placeholder map", 13, Ui.InkSoft);
                label.style.position = Position.Absolute;
                label.style.right = 10; label.style.bottom = 4;
                map.Add(label);
            }
            foreach (var p in RomeMap.Places)
            {
                int things = app.Session.ActionsAt(p.Id).Sum(g => g.Items.Count);
                var b = Ui.Button(p.Name + (things > 0 ? "  (" + things + ")" : ""), () => { app.SelectedPlace = p.Id; app.LookText = ""; app.Refresh(); }, p.Id == app.SelectedPlace);
                b.style.position = Position.Absolute;
                b.style.left = Length.Percent(p.X * 100);
                b.style.top = Length.Percent(p.Y * 100);
                b.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                map.Add(b);
            }
            return map;
        }

        // ---- people, journal, machine, work, everything --------------------------------------------------------------

        public static VisualElement People(ButterflyApp app)
        {
            var scroll = Ui.Scroll();
            scroll.Add(Ui.Heading("People you know"));
            var people = app.Session.People();
            if (people.Count == 0) scroll.Add(Ui.Text("You don't know anyone here by name yet.", 16, Ui.InkSoft));
            var grid = Ui.Row();
            grid.style.alignItems = Align.FlexStart;
            foreach (var p in people)
            {
                var c = Ui.Card();
                c.style.width = app.Narrow ? Length.Percent(100) : Length.Percent(31);
                c.style.marginRight = 10;
                var head = Ui.Row();
                head.style.flexWrap = Wrap.NoWrap;
                var art = ArtLibrary.Slot(p.Portrait, p.Name, 80, 96);
                art.style.marginRight = 10;
                head.Add(art);
                var names = Ui.Column();
                Ui.Fill(names);
                names.Add(Ui.Text(p.Name, 17, Ui.Ink, true));
                names.Add(Ui.Text(p.Role, 14, Ui.InkSoft));
                if (p.Away) names.Add(Ui.Text("Laid up or away for now.", 14, Ui.Terracotta));
                head.Add(names);
                c.Add(head);
                c.Add(Ui.Text(p.Status + ".", 15));
                c.Add(Ui.Text("Household: " + p.Household, 13, Ui.InkSoft));
                c.Add(Ui.Text("Cares about " + p.CaresAbout + "; wants " + p.Wants + ".", 13, Ui.InkSoft));
                grid.Add(c);
            }
            scroll.Add(grid);
            return scroll;
        }

        public static VisualElement Journal(ButterflyApp app, bool returned)
        {
            var j = app.Session.Journal();
            var scroll = Ui.Scroll();
            scroll.Add(Ui.Heading("Your journal"));
            if (j.Entries.Count == 0) scroll.Add(Ui.Text("Nothing in it yet that you'd want to keep.", 16, Ui.InkSoft));
            foreach (var e in j.Entries) scroll.Add(Ui.Text(e, 17));
            if (!returned)
            {
                if (j.Recent.Count > 0) scroll.Add(Ui.Heading("Lately", 18));
                foreach (var f in Enumerable.Reverse(j.Recent)) scroll.Add(FeedRow(f));
                if (j.Ledger.Count > 0) scroll.Add(Ui.Heading("Your money", 18));
                foreach (var l in j.Ledger) scroll.Add(Ui.Text(l, 14));
            }
            scroll.Add(Ui.Text("Seed " + app.Seed + " · replay: " + app.ReplayPath, 12, Ui.InkSoft));
            return scroll;
        }

        public static VisualElement Machine(ButterflyApp app)
        {
            var s = app.Session;
            var m = s.Machine();
            var scroll = Ui.Scroll();
            var head = app.Narrow ? Ui.Column() : Ui.Row();
            head.style.flexWrap = Wrap.NoWrap;
            head.style.alignItems = Align.FlexStart;
            var art = ArtLibrary.Slot("machine.core", "Core", 240, 240);
            art.style.marginRight = 16;
            head.Add(art);
            var col = Ui.Column();
            Ui.Fill(col);
            col.Add(Ui.Heading("The machine"));
            string ready = s.ReadyLine();
            if (ready.Length > 0) col.Add(Ui.Text(ready, 17, Ui.Terracotta, true));
            if (!m.Assessed) col.Add(Ui.Text("You don't yet know what is wrong with it.", 16, Ui.InkSoft));
            foreach (var sys in m.Systems)
            {
                col.Add(Ui.Text(Cap(sys.Id) + ": " + sys.Done + " of " + sys.Total + (sys.UnderWay.Length > 0 ? " · under way: " + sys.UnderWay : sys.Next.Length > 0 && m.Assessed ? " · next: " + sys.Next : ""), 15));
                col.Add(Ui.Bar(sys.Total == 0 ? 0 : (float)sys.Done / sys.Total, Ui.Bronze));
            }
            col.Add(Ui.Text("Gold back in the machine: " + m.GoldRestored.ToString("0") + " of " + m.GoldNeeded.ToString("0") + " aurei", 15));
            col.Add(Ui.Bar(m.GoldNeeded <= 0 ? 1 : (float)(m.GoldRestored / m.GoldNeeded), new Color(0.80f, 0.64f, 0.20f)));
            if (m.UpgradesDone.Count > 0) col.Add(Ui.Text("Upgrades: " + string.Join(", ", m.UpgradesDone), 14, Ui.InkSoft));
            head.Add(col);
            scroll.Add(head);
            var panel = Ui.Card(Ui.Night);
            panel.Add(Ui.Text("PANEL", 13, new Color(0.6f, 0.9f, 0.6f), true));
            foreach (var l in m.Panel) panel.Add(Ui.Text(l, 14, new Color(0.75f, 0.95f, 0.75f)));
            scroll.Add(panel);
            var row = Ui.Row();
            foreach (var a in m.Actions) row.Add(Ui.Button(Cap(a.Label), () => app.Act(a.Command)));
            if (m.Ready) row.Add(Ui.Button("Prepare to leave…", () => { s.OpenDeparture(); app.Refresh(); }, true));
            scroll.Add(row);
            return scroll;
        }

        public static VisualElement Work(ButterflyApp app)
        {
            var w = app.Session.Work();
            var scroll = Ui.Scroll();
            scroll.Add(Ui.Heading("Work"));
            void Items(string title, List<WorkItem> items)
            {
                if (items.Count == 0) return;
                scroll.Add(Ui.Heading(title, 18));
                foreach (var i in items)
                {
                    var c = Ui.Card();
                    var row = Ui.Row();
                    row.style.flexWrap = Wrap.NoWrap;
                    if (i.Portrait.Length > 0)
                    {
                        var art = ArtLibrary.Slot(i.Portrait, i.Who, 56, 64);
                        art.style.marginRight = 10;
                        row.Add(art);
                    }
                    var col = Ui.Column();
                    Ui.Fill(col);
                    col.Add(Ui.Text(i.Title, 16, Ui.Ink, true));
                    if (i.Who.Length > 0) col.Add(Ui.Text(i.Who, 14, Ui.InkSoft));
                    col.Add(Ui.Text(i.State, 15));
                    row.Add(col);
                    c.Add(row);
                    scroll.Add(c);
                }
            }
            Items("People's work", w.Commissions);
            Items("Grand Challenges", w.Challenges);
            Items("Under way", w.UnderWay);
            if (w.Commissions.Count + w.Challenges.Count + w.UnderWay.Count == 0)
                scroll.Add(Ui.Text("No one has brought you work yet. People will, as you meet them.", 16, Ui.InkSoft));
            Groups(app, scroll, w.Actions);
            return scroll;
        }

        /// <summary>Every action available now, by section: the depth the other screens organize (institutions, policy, …).</summary>
        public static VisualElement Everything(ButterflyApp app)
        {
            var scroll = Ui.Scroll();
            scroll.Add(Ui.Heading("Everything you can do now"));
            Groups(app, scroll, app.Session.Actions().Where(g => g.Key != "look" && g.Key != "month" && g.Key != "leave" && g.Key != "decide"));
            return scroll;
        }

        // ---- departure, arrival, the return --------------------------------------------------------------------------

        public static VisualElement Departure(ButterflyApp app)
        {
            var s = app.Session;
            var d = s.Departure();
            var page = Ui.Column();
            page.style.backgroundColor = Ui.Night;
            page.style.alignItems = Align.Center;
            Ui.Pad(page, 30);
            var scroll = Ui.Scroll();
            scroll.style.maxWidth = 980;
            scroll.Add(Ui.Text("Before you leave", 28, Ui.Paper, true));
            scroll.Add(Ui.Text(d.Range, 17, Ui.Paper));
            scroll.Add(Ui.Text("What you leave behind (" + d.InHand + "):", 16, Ui.PaperDark, true));
            foreach (var line in d.Briefing) scroll.Add(Ui.Text("• " + line.Trim(), 16, Ui.Paper));
            if (app.LastOutcome != null && app.LastOutcome.Message.Length > 0 && d.Prepare.Count > 0) scroll.Add(Ui.Text(app.LastOutcome.Message, 15, Ui.PaperDark));
            var prep = Ui.Row();
            foreach (var a in d.Prepare) prep.Add(Ui.Button(Cap(a.Label), () => app.Act(a.Command)));
            scroll.Add(prep);
            var go = Ui.Row();
            go.style.marginTop = 16;
            go.Add(Ui.Button("Stay in Rome", () => { s.CloseDeparture(); app.Refresh(); }));
            go.Add(Ui.Button("Leave now", () => { app.ArrivalBeat = 0; app.Act("jump"); }, true, d.CanLeave));
            scroll.Add(go);
            page.Add(scroll);
            return page;
        }

        public static VisualElement Arrival(ButterflyApp app)
        {
            var a = app.Session.Arrival()!;
            var page = Ui.Column();
            page.style.backgroundColor = Ui.Night;
            page.style.alignItems = Align.Center;
            Ui.Pad(page, 30);
            var scroll = Ui.Scroll();
            scroll.style.maxWidth = 900;
            var art = ArtLibrary.Slot("scene.jump", "", 860, 160);
            art.style.marginBottom = 14;
            scroll.Add(art);
            scroll.Add(Ui.Text("The machine shudders. Decades pass in the dark. It carries you " + a.JumpYears + " years.", 18, Ui.Paper));
            int shown = Mathf.Clamp(app.ArrivalBeat, 0, a.Beats.Count - 1);
            for (int k = 0; k <= shown && k < a.Beats.Count; k++)
            {
                scroll.Add(Ui.Text(a.Beats[k].Name, 15, Ui.PaperDark, true));
                scroll.Add(Ui.Text(a.Beats[k].Text, 17, Ui.Paper));
            }
            if (shown < a.Beats.Count - 1) scroll.Add(Ui.Button("Go on", () => { app.ArrivalBeat++; app.Refresh(); }, true));
            else scroll.Add(Ui.Button("Walk into the city", () => { app.Session.FinishArrival(); app.Screen = Screen.Rome; app.LastOutcome = null; app.Refresh(); }, true));
            page.Add(scroll);
            return page;
        }

        public static VisualElement Return(ButterflyApp app)
        {
            var s = app.Session;
            var r = s.Return()!;
            var content = app.Narrow ? Ui.Column() : Ui.Row();
            content.style.flexWrap = Wrap.NoWrap;
            content.style.alignItems = Align.Stretch;

            var left = Ui.Column();
            left.style.flexGrow = 3;
            left.style.flexBasis = 0;
            var scrollL = Ui.Scroll();
            scrollL.Add(Ui.Heading("Rome, AD " + r.ArrivalYear));
            scrollL.Add(Ui.Text(r.YearsAway + " years after you left. You have seen " + r.Seen + " of " + r.Sites.Count + " places.", 15, Ui.InkSoft));
            var grid = Ui.Row();
            grid.style.alignItems = Align.FlexStart;
            foreach (var site in r.Sites)
            {
                var c = Ui.Card(site.Number == app.SelectedSite ? Ui.PaperDark : Ui.Paper);
                c.style.width = app.Narrow ? Length.Percent(47) : Length.Percent(31);
                c.style.marginRight = 8;
                var art = ArtLibrary.Slot(site.Portrait.Length > 0 ? site.Portrait : site.Art, site.Place, 150, 90);
                art.style.width = Length.Percent(100);
                c.Add(art);
                c.Add(Ui.Text(site.Place, 15, Ui.Ink, true));
                // What you have done there, and nothing else: never true, false, important or your doing.
                c.Add(Ui.Text(site.State == SiteState.Unvisited ? "○ not yet visited" : site.State == SiteState.Seen ? "◐ seen" : "● looked closer", 13, Ui.InkSoft));
                int number = site.Number;
                c.RegisterCallback<ClickEvent>(_ => { app.SelectedSite = number; app.ReturnDetail = ""; app.Refresh(); });
                grid.Add(c);
            }
            scrollL.Add(grid);
            scrollL.Add(Ui.Text("Walk around", 14, Ui.InkSoft, true));
            var walk = Ui.Row();
            foreach (var p in RomeMap.Places.Where(p => RomeMap.IsWalkPlace(p.Id)))
                walk.Add(Ui.Button(p.Name, () => { app.SelectedSite = 0; app.ReturnDetail = s.LookAround(p.Id); app.Refresh(); }));
            scrollL.Add(walk);
            left.Add(scrollL);
            content.Add(left);

            var right = Ui.Card();
            right.style.flexGrow = 2;
            right.style.flexBasis = 0;
            right.style.marginLeft = app.Narrow ? 0 : 14;
            var scrollR = Ui.Scroll();
            var sel = r.Sites.FirstOrDefault(x => x.Number == app.SelectedSite);
            if (sel == null)
            {
                scrollR.Add(Ui.Text(app.ReturnDetail.Length > 0 ? app.ReturnDetail : "Choose a place to go.", 16, app.ReturnDetail.Length > 0 ? Ui.Ink : Ui.InkSoft));
            }
            else
            {
                scrollR.Add(Ui.Heading(sel.Place));
                if (sel.State == SiteState.Unvisited) scrollR.Add(Ui.Button("Go there", () => app.Act(sel.VisitCommand), true));
                else
                {
                    scrollR.Add(Ui.Text(sel.Recognition, 17));
                    scrollR.Add(Ui.Text(sel.Contradiction, 17));
                    if (sel.State == SiteState.Seen) scrollR.Add(Ui.Button("Look closer: " + sel.Lead, () => app.Act(sel.LookCloserCommand), true));
                    else
                    {
                        scrollR.Add(Ui.Text(sel.Lead, 15, Ui.InkSoft, true));
                        scrollR.Add(Ui.Text(sel.Finding, 17));
                    }
                }
            }
            right.Add(scrollR);
            var end = Ui.Row();
            if (r.HasJournal) end.Add(Ui.Button("Your journal", () => app.Go(Screen.Journal)));
            if (r.CanFinish) end.Add(Ui.Button("Finish looking", () => app.Act("done"), true));
            right.Add(end);
            if (r.Finished)
            {
                right.Add(Ui.Text("You have seen enough of this Rome. This is where the slice ends: you can keep walking, or close the game.", 15, Ui.Terracotta, true));
                right.Add(Ui.Button("Learn more (the Index and your institutions)", () => { app.SelectedSite = 0; app.ReturnDetail = s.LearnMore(); app.Refresh(); }));
            }
            content.Add(right);
            return WithHud(app, content);
        }
    }
}
