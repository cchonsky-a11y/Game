using System;
using System.Collections.Generic;
using System.Linq;
using Butterfly.Core;

/// <summary>
/// The action menu (decided 2026-09-28, Corey: so playtesters don't have to type commands): after each command the
/// console lists, numbered, what you can do right now; type a number, or several ("3 7 1"), to do them in order. Every
/// number stands for an ordinary command, so a game played by numbers replays exactly like one typed out.
/// </summary>
internal sealed partial class ConsoleGame
{
    private readonly List<string> _menu = new List<string>();
    private bool _menuOn;

    /// <summary>"3", "3 7 1" or "3,7": the commands those numbers stand for in the menu last shown, or null if the line isn't numbers.</summary>
    private List<string>? MenuCommands(string line)
    {
        var tokens = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || !tokens.All(t => t.All(char.IsDigit))) return null;
        var commands = new List<string>();
        foreach (var t in tokens)
        {
            int n = int.Parse(t);
            if (n < 1 || n > _menu.Count) { Console.WriteLine("  There's no " + n + " in the menu (1-" + _menu.Count + "). Type 'menu' to see it again."); return new List<string>(); }
            commands.Add(_menu[n - 1]);
        }
        return commands;
    }

    private void ShowMenu()
    {
        _menu.Clear();
        var groups = _sim.Arrived ? ArrivalGroups() : EraGroups();
        Console.WriteLine();
        Console.WriteLine("  What you can do (type a number, or several like 3 7 1; 'menu off' hides this):");
        foreach (var (title, items) in groups)
        {
            if (items.Count == 0) continue;
            var line = "  " + title + ":";
            string indent = new string(' ', line.Length);
            foreach (var (label, command) in items)
            {
                _menu.Add(command);
                string piece = " [" + _menu.Count + "] " + label;
                if (line.Length + piece.Length > 110 && line.Trim().Length > title.Length + 1)
                {
                    Console.WriteLine(line);
                    line = indent;
                }
                line += piece;
            }
            Console.WriteLine(line);
        }
    }

    private static (string, List<(string, string)>) Group(string title) => (title, new List<(string, string)>());

    private List<(string Title, List<(string Label, string Command)> Items)> ArrivalGroups()
    {
        var walk = Group("Walk around");
        foreach (var place in new[] { "market", "changers", "forges", "curia", "subura" }) walk.Item2.Add((place, "visit " + place));
        var more = Group("Then");
        more.Item2.Add(("learn more (the Index and your institutions)", "learn more"));
        if (_sim.CanJumpAgain)
        {
            double over = Math.Floor(_sim.World.Aurei - _sim.CarryAurei);
            if (_jumpArmed && over >= 1)
            {
                more.Item2.Add(("deposit " + _sim.AureiText(over) + " with the bank", "deposit " + F(over)));
                more.Item2.Add(("bury " + _sim.AureiText(over), "bury " + F(over)));
            }
            more.Item2.Add((_jumpArmed ? "jump (go now)" : "jump again", "jump"));
        }
        more.Item2.Add(("quit", "quit"));
        return new List<(string, List<(string, string)>)> { walk, more };
    }

    private List<(string Title, List<(string Label, string Command)> Items)> EraGroups()
    {
        var w = _sim.World;
        bool att = w.Attention > 0;
        string M(double gold) => _sim.Money(gold);

        // Decisions waiting for you come first.
        var decide = Group("Decide now");
        if (_sim.SeededChoiceOpen)
        {
            decide.Item2.Add(("the fountain", "choose fountain"));
            decide.Item2.Add(("the workshop", "choose workshop"));
        }
        if (w.Promise.Status == PromiseStatus.Offered)
        {
            decide.Item2.Add(("promise Demetria yes", "promise yes"));
            decide.Item2.Add(("promise no", "promise no"));
        }
        if (_sim.OutbreakAwaitingResponse)
            foreach (var resp in new[] { "quarantine", "hospice", "none" }) decide.Item2.Add(("plague: " + resp, "respond " + resp));
        if (_sim.PendingEvent is EventDef ev)
            foreach (var o in ev.Options)
                decide.Item2.Add((ev.Title.Split(' ').Take(3).Aggregate((a, b) => a + " " + b) + ": " + o.Label + (_sim.EventCost(o) > 0 ? " (" + M(_sim.EventCost(o)) + ")" : ""), "decide " + o.Id));
        foreach (var i in w.Institutions.Where(x => x.OfferedRank > 0))
        {
            decide.Item2.Add(("accept office in " + i.Def.ShortName, "office accept " + i.Key));
            decide.Item2.Add(("decline", "office decline " + i.Key));
        }

        var work = Group("Work");
        if (att && w.PersonalActionTurn != _sim.Turn)
            foreach (var kind in Simulation.WorkKinds)
                if (_sim.WorkAttention(kind) <= w.Attention) work.Item2.Add((kind + " (" + M(_sim.WorkPay(kind)) + ", " + _sim.WorkAttention(kind) + " Att.)", "work " + kind));

        var shop = Group("Workshop");
        if (att && _sim.OwnsWorkshop)
        {
            if (_sim.OrdersLeftThisSeason > 0)
                foreach (var o in _sim.OrderBoard().Where(o => o.Attention <= w.Attention)) shop.Item2.Add(("take " + o.Id + " (" + M(_sim.OrderPay(o)) + ")", "take " + o.Id));
            if (w.Apprentices < _sim.CurrentSize.ApprenticeMax) shop.Item2.Add(("hire an apprentice (" + M(_sim.ApprenticeWage()) + "/yr)", "apprentice hire"));
            if (_sim.ExpandBlocker() == null) shop.Item2.Add(("expand to " + _sim.NextSize!.Name + " (" + M(_sim.ExpandCost(_sim.NextSize)) + ")", "expand"));
        }

        var machine = Group("Machine");
        if (att && !_sim.MachineAssessed && !w.ActiveMachineSteps.Any()) machine.Item2.Add(("assess it", "assess"));
        if (att && _sim.MachineAssessed)
        {
            foreach (var system in Simulation.MachineSystems)
            {
                var step = _sim.NextMachineStep(system);
                if (step != null && w.ActiveMachineSteps.All(a => a.Def.System != system) && _sim.MachineStepGold(step) <= w.Gold)
                    machine.Item2.Add(("repair " + system + " (" + M(_sim.MachineStepGold(step)) + ")", "repair " + system));
            }
            if (_sim.MachineStepsDone >= _sim.MachineStepsTotal)
                foreach (var up in _sim.Data.Content.MachineUpgrades.Where(u => !w.MachineDone.Contains(u.Id) && w.ActiveMachineSteps.All(a => a.Def.Id != u.Id)))
                    machine.Item2.Add(("upgrade " + up.Id, "upgrade " + up.Id));
        }
        double missing = _sim.MachineGoldNeeded - _sim.MachineGoldRestored;
        if (missing >= 1)
        {
            if (w.Aurei >= 1 && _sim.MachineAssessed) machine.Item2.Add(("put " + _sim.AureiText(Math.Min(Math.Floor(w.Aurei), Math.Ceiling(missing))) + " back in", "restore " + F(Math.Min(Math.Floor(w.Aurei), Math.Ceiling(missing)))));
            double buy = Math.Ceiling(missing - w.Aurei);
            if (att && buy >= 1 && _sim.AureiCost(buy) <= w.Gold)
                machine.Item2.Add(("buy the " + _sim.AureiText(buy) + " it still needs (" + M(_sim.AureiCost(buy)) + ")", "exchange " + F(Math.Ceiling(_sim.Denarii(_sim.AureiCost(buy)))) + " denarii"));
        }

        var money = Group("Money changers");
        if (att && w.Aurei >= 1 && missing < 1 || att && w.Aurei >= 1 && _sim.MachineStepsDone < _sim.MachineStepsTotal)
        {
            if (w.Aurei >= 10) money.Item2.Add(("change 10 aurei", "exchange 10 aurei"));
            money.Item2.Add(("change all " + _sim.AureiText(Math.Floor(w.Aurei)), "exchange " + F(Math.Floor(w.Aurei)) + " aurei"));
        }

        var projects = Group("Projects");
        if (att)
            foreach (var p in _sim.AvailableProjects().Where(p => _sim.ProjectAuthorityBlocker(p) == null && _sim.ProjectGold(p) <= w.Gold && p.AttentionPerTurn <= w.Attention))
                projects.Item2.Add((p.Id + " (" + M(_sim.ProjectGold(p)) + ")", "start " + p.Id));

        var inventions = Group("Invent");
        if (att && w.ActiveInventions.Count == 0)
            foreach (var inv in _sim.Data.Content.Inventions.Where(x => _sim.InventionState(x) == "ready" && _sim.InventionGold(x) <= w.Gold))
                inventions.Item2.Add((inv.Id + " (" + M(_sim.InventionGold(inv)) + ")", "invent " + inv.Id));

        var commissions = Group("Work and invitations");
        foreach (var c in _sim.OpenCommissions())
        {
            var d = _sim.CommissionDefOf(c);
            if (c.Status == CommissionStatus.Offered && w.Attention >= d.Diagnosis.Attention) commissions.Item2.Add(("look at " + d.Client + "'s problem (unpaid)", "commission look " + d.Id));
            if (c.Status == CommissionStatus.TermsOffered)
            {
                if (w.Attention >= d.Work[0].Attention) commissions.Item2.Add(("accept " + d.Client + "'s terms", "commission accept " + d.Id));
                if (!c.Countered) commissions.Item2.Add(("ask " + d.Client + " for more", "commission counter " + d.Id));
                commissions.Item2.Add(("decline " + d.Client + "'s work", "commission decline " + d.Id));
            }
        }

        foreach (var p in w.Invitations.Where(p => p.Pending != InvitationOffer.None))
        {
            var d = _sim.InvitationPathDefFor(p.Institution)!;
            commissions.Item2.Add(("accept " + d.Inviter + "'s invitation", "invitation accept " + d.Institution));
            commissions.Item2.Add(("decline " + d.Inviter + "'s invitation", "invitation decline " + d.Institution));
        }

        var inst = Group("Institutions");
        if (att)
        {
            foreach (var i in w.Institutions.Where(x => !x.Def.IsOwn && x.Exists))
            {
                if (!i.Backed && _sim.OnInvitationPath(i)) continue;   // P1: by invitation only, nothing to buy
                if (!i.Backed)
                {
                    int first = i.Def.JoinRequirement == "deposit" ? _sim.T.GetInt("joining.bankMinFirstPercent") : 1;
                    if (_sim.JoinBlocker(i) == null && _sim.BuyCost(i, first) <= w.Gold) inst.Item2.Add(("buy into " + i.Key + ", " + first + "% (" + M(_sim.BuyCost(i, first)) + ")", "buy " + i.Key + " " + first));
                    continue;
                }
                if (_sim.BuyCost(i, 1) <= w.Gold) inst.Item2.Add((i.Key + " +1% (" + M(_sim.BuyCost(i, 1)) + ")", "buy " + i.Key + " 1"));
                if (_sim.BuyCost(i, 5) <= w.Gold) inst.Item2.Add((i.Key + " +5% (" + M(_sim.BuyCost(i, 5)) + ")", "buy " + i.Key + " 5"));
                if (i.AttendedTurn != _sim.Turn)
                    for (int c = 0; c < 2; c++) inst.Item2.Add(("attend " + i.Key + ", vote " + _sim.CampName(i, c), "attend " + i.Key + " " + i.Def.DriftPaths[c].Id));
            }
            foreach (var own in w.Institutions.Where(x => x.Def.IsOwn))
            {
                if (!own.Exists && !own.Collapsed && _sim.FoundCost(own.Def.Maintains) <= w.Gold)
                    inst.Item2.Add(("found " + own.Key + " (" + M(_sim.FoundCost(own.Def.Maintains)) + ")", "found " + own.Key));
                if (own.Exists && _sim.Controls(own) && w.Gold >= 40)
                    inst.Item2.Add(("invest " + M(Math.Floor(w.Gold / 4)) + " in " + own.Key, "invest " + own.Key + " " + F(Math.Floor(_sim.Denarii(Math.Floor(w.Gold / 4))))));
            }
            foreach (var i in w.Institutions.Where(x => x.Exists && _sim.Controls(x)))
            {
                inst.Item2.Add(("oversee " + i.Key, "oversee " + i.Key));
                if (!i.Chartered) inst.Item2.Add(("charter " + i.Key, "charter " + i.Key));
            }
        }

        var policy = Group(_sim.PolicyHold() ? "Policy" : "Advocate (no voice yet; " + M(_sim.AdvocacyCost()) + ", 2 Att. each)");
        if (att && (_sim.PolicyHold() || _sim.AdvocacyCost() <= w.Gold))
        {
            string verb = _sim.PolicyHold() ? "policy" : "advocate";
            foreach (var issue in Simulation.Issues)
                foreach (int stance in new[] { 1, -1, 0 })
                    if (_sim.Stance(issue) != stance)
                    {
                        string word = stance == 0 ? "history" : Simulation.StanceWord(issue, stance);
                        policy.Item2.Add((issue.ToString().ToLowerInvariant() + " " + word, verb + " " + issue.ToString().ToLowerInvariant() + " " + word));
                    }
        }

        var priorities = Group("Priorities");
        if (att)
            foreach (var d in DomainInfo.All.Where(_sim.HasHold))
                foreach (var p in new[] { "protect", "maintain", "accept" })
                    if (DomainInfo.TryParsePriority(p, out var pr) && w[d].Priority != pr)
                        priorities.Item2.Add((d.ToString().ToLowerInvariant() + " " + p, "priority " + d.ToString().ToLowerInvariant() + " " + p));

        var leave = Group("Before you leave");
        if (_sim.MachineReady)
        {
            if (w.Aurei >= 1) leave.Item2.Add(("deposit " + _sim.AureiText(Math.Floor(w.Aurei)) + " with the bank", "deposit " + F(Math.Floor(w.Aurei))));
            if (w.Aurei >= 1) leave.Item2.Add(("bury " + _sim.AureiText(Math.Floor(w.Aurei)), "bury " + F(Math.Floor(w.Aurei))));
            leave.Item2.Add(("jump", "jump"));
        }

        var look = Group("Look (free)");
        foreach (var (label, cmd) in new[] { ("status", "status"), ("news", "news"), ("institutions", "institutions"), ("machine", "machine"), ("workshop", "workshop"), ("inventions", "inventions"), ("projects", "projects"), ("help", "help") })
            if (cmd != "workshop" || _sim.OwnsWorkshop) look.Item2.Add((label, cmd));

        var turn = Group("Month");
        turn.Item2.Add(("End Month", "end"));
        turn.Item2.Add(("fast-forward until something needs you", "wait"));

        return new List<(string, List<(string, string)>)> { decide, commissions, work, shop, machine, money, projects, inventions, inst, policy, priorities, leave, look, turn };
    }
}
