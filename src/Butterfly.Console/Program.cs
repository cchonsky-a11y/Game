using System;
using System.Globalization;
using System.Linq;
using Butterfly.Core;

// The Butterfly Effect — P1 playable game structure, text console.
// Usage: dotnet run --project src/Butterfly.Console -- --seed 42
// Scripted (automated playtests): add --inputs <file> [--checks <file>]; see playtests/ai/README.md.
// Add --continue to keep playing from the keyboard once the script's lines run out (resume a saved inputs file).
// The numbered action menu is on when you play from the keyboard; --menu turns it on for a script, --no-menu off.
ulong seed = 42;
bool resume = args.Contains("--continue");
bool? menu = args.Contains("--no-menu") ? false : args.Contains("--menu") ? true : (bool?)null;
string? inputs = null, checks = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--seed") seed = ulong.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--inputs") inputs = args[i + 1];
    if (args[i] == "--checks") checks = args[i + 1];
}

var sim = new Simulation(GameData.LoadDefault(), seed);
var game = new ConsoleGame(sim, inputs == null ? null : new ScriptInput(inputs, sim), checks == null ? null : new Harness(sim, checks), resume, menu);
game.Run();

internal sealed partial class ConsoleGame
{
    private readonly Simulation _sim;
    private readonly ScriptInput? _script;
    private readonly Harness? _harness;
    private bool _jumpArmed;
    private static readonly string[] PrepCommands = { "paydown", "endow", "audit", "orders", "status", "s", "why", "help", "?", "exchange", "deposit", "bury", "restore", "visit", "walk" };

    /// <summary>After the script's last line, read from the keyboard (--continue).</summary>
    private readonly bool _resume;
    private bool _scriptDone;

    public ConsoleGame(Simulation sim, ScriptInput? script = null, Harness? harness = null, bool resume = false, bool? menu = null)
    {
        _menuOn = menu ?? (script == null || resume);
        _resume = resume;
        _sim = sim;
        _script = script;
        _harness = harness;
    }

    /// <summary>Next command: from the script (echoed so transcripts read like a session) or from the keyboard.</summary>
    private string? ReadCommand()
    {
        if (_script == null || _scriptDone) return Console.ReadLine();
        string? line = _script.Next();
        if (line != null) Console.WriteLine(line);
        else if (_resume) { _scriptDone = true; return Console.ReadLine(); }
        return line;
    }

    private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    public void Run()
    {
        Intro();
        Status();
        if (_menuOn) ShowMenu();
        while (true)
        {
            Console.Write(_sim.Arrived ? "\n(after arrival) > " : "\n> ");
            string? line = ReadCommand();
            if (line == null) break;
            // A number picks one item from the menu (P1: one choice at a time; you see the result before choosing again).
            var picked = MenuCommand(line, out bool wasNumbers);
            if (!wasNumbers) { if (!ProcessLine(line)) break; }
            else if (picked != null)
            {
                Console.WriteLine("  → " + picked);
                if (!ProcessLine(picked)) break;
            }
        }
        Console.WriteLine("\nRun fingerprint (seed " + _sim.Seed + "): " + _sim.Log.Hash().Substring(0, 16));
        _harness?.Finish(_script);
    }

    private static readonly string[] LookCommands = { "help", "?", "status", "s", "news", "n", "why", "log", "ledger", "people", "view", "focus", "challenge", "challenges", "commissions", "inventions", "institutions", "i", "machine", "m", "workshop", "projects", "p" };

    /// <summary>One command line. False means quit.</summary>
    private bool ProcessLine(string line, bool showMenu = true)
    {
        var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return true;
        string cmd = parts[0].ToLowerInvariant();
        string arg = parts.Length > 1 ? parts[1] : "";
        if (cmd == "quit" || cmd == "exit") return false;
        if (cmd == "menu")
        {
            if (arg == "off") { _menuOn = false; Console.WriteLine("  Menu off: type commands, or 'menu' to see it once."); }
            else { if (arg == "on") _menuOn = true; ShowMenu(); }
            return true;
        }
        // Jump preparation: these commands keep the jump armed.
        if (cmd != "jump" && !(_jumpArmed && PrepCommands.Contains(cmd))) _jumpArmed = false;
        if (_sim.Arrived)
        {
            AfterArrival(cmd, arg);
            _harness?.AfterCommand(line, true);
            if (showMenu && _menuOn) ShowMenu();
            return true;
        }
        // "@autoend" (P0 scripts): auto-end is gone in P1 (decided 2026-10-02), so the directive is accepted and ignored.
        if (cmd == "@autoend") return true;
        int committedBefore = _sim.AttentionCommittedNextTurn();
        bool ok = Handle(cmd, arg, parts);
        _harness?.AfterCommand(line, ok);
        // L3 (tester 2): say so the moment all of next month's Attention is pledged.
        if (ok && !_sim.Arrived && committedBefore < _sim.AttentionPerTurn && _sim.AttentionCommittedNextTurn() >= _sim.AttentionPerTurn)
            Console.WriteLine("  (All your Attention is pledged for the months ahead, until that work finishes.)");
        // P1 (decided 2026-10-02): spending the last Attention never ends the month; only 'end' does.
        if (ok && !_sim.Arrived && _sim.World.Attention == 0 && cmd != "end" && cmd != "e" && cmd != "wait" && cmd != "w")
            Console.WriteLine("  (No Attention left this month. Type 'end' to end the month.)");
        if (showMenu && _menuOn)
        {
            if (LookCommands.Contains(cmd)) Console.WriteLine("  (Pick a number from the menu above, or type 'menu' to see it again.)");
            else ShowMenu();
        }
        return true;
    }

    private void Intro()
    {
        // The locked P1 opening (decided 2026-10-02): story first; nothing about plague, jump ranges or Echoes up front.
        var text = _sim.Data.Content;
        Console.WriteLine("THE BUTTERFLY EFFECT");
        Console.WriteLine();
        foreach (var para in text.Template("opening.scene").Split(new[] { "\n\n" }, StringSplitOptions.None)) { Console.WriteLine(Wrap(para)); Console.WriteLine(); }
        Console.WriteLine(Wrap(text.Template("opening.gold", new System.Collections.Generic.Dictionary<string, string> { { "aurei", F(_sim.MachineGoldNeeded) } })));
        Console.WriteLine(Wrap(text.Template("opening.money")));
        Console.WriteLine();
        Console.WriteLine(text.Template("opening.choice"));
        Console.WriteLine("  choose fountain  — " + _sim.Data.Content.Project("fountain")!.Description);
        Console.WriteLine("  choose workshop  — " + _sim.Data.Content.Project("workshop")!.Description);
        Console.WriteLine(_sim.Data.Content.Template("walk.intro.start"));
        Console.WriteLine("Type 'help' for commands.");
    }

    private void Help()
    {
        Console.WriteLine(@"Commands
  <number>                       do what that number in the menu lists (one choice at a time)
  menu | menu on | menu off      show the numbered menu, or turn it on or off (on when you play from the keyboard)
  status                         where things stand
  projects                       projects you can start
  news                           what's going on in Rome: the talk of the Forum and the market (no Attention)
  start <project>                start a project (costs gold and Attention)
  priority <domain> <protect|maintain|accept>   (needs a 25% voice in an institution of that domain)
  paydown <domain> <points>      pay down debt (costs 1.5× what prevention would have)
  institutions                   who holds each domain, your stakes, and what the next step costs
  buy <inst> [percent]           buy into an established institution (2 Attention; entry fee on joining, each 1% costs more):
                                   10% counts toward influence · 25% a voice (priorities, policy) · 50% control
  attend <inst> [camp]           attend a meeting and vote for one of its two camps (1 Attention; twice a year earns extra seniority)
  office accept|decline <inst>   answer an offer of office (offices weigh more in votes and cost Attention in duties)
  resign <inst>                  step down from an office
  decide <option>                answer a choice Rome puts to you (it passes you by after a few turns)
  workshop                       your workshop: this season's orders, apprentices, the smith (no Attention)
  take <order>                   take an order at the workshop (1-2 Attention; one a season, two with 2+ apprentices)
  apprentice hire|dismiss        take on or let go a free, paid apprentice (1 Attention to hire; wages each year)
  expand                         enlarge the workshop to its next size: smithy → yard → works on the river → foundry
                                   (gold, Attention, a few turns; each needs something from Rome and costs upkeep a year)
  orders <inst> <camp> [1|2]     your last orders: the camp to back when you leave (and, from the head's seat, who succeeds you)
  found <school|club|house>      found your own institution: you control it, but it starts small and may fail
  invest <inst> <denarii>        build up an institution you control (2 Attention)
  charter <inst>                 write its founding principles (slows drift; needs control)
  endow <inst> [denarii|all]     give it money to hold (the minimum endowment makes it endowed)
  audit <inst>                   found an audit charter (guards its gold against corruption)
  oversee <inst>                 spend a month working with its leader (1 Attention)
  advocate <issue> <stance>      without a voice: argue for a stance in pamphlets and at dinners (gold, 2 Attention; small sway,
                                   more with memberships and a faction office; lasts only while you're in Rome)
  policy <issue> <stance>        set economic policy through a Governance institution you have a voice in (2 Attention):
                                   coinage sound|debase · prices free|controlled · property secure|discretionary · taxes light|heavy
                                   (or 'history' to return to Rome's own practice)
  mentor <inst>                  commit Attention every month for several months
  work [odd|craft|consult]       a fallback when no one has brought you work: about 125 / 300 / 500 denarii for 1 / 2 / 3 Attention
  choose <fountain|workshop>     the first choice
  promise <yes|no>               answer Demetria
  respond <quarantine|hospice|none>   when the pestilence breaks out
  why <thing>                    medicine, governance, economy, gold, plague, policy, promise, index, attention, or an institution
  listen                         reopen the machine's reference channel (once the panel shows R-17 ACTIVE; 1 Attention)
  give <inst>                    a gift to an institution that takes gifts (the Tiber Island sanctuary): you become a benefactor
  focus <kind|off>               stay with one kind of scene (Engineering, Personal, RomanLife, WorkEconomy, MachineMystery,
                                 CityHistory, Exploration, InstitutionsPolitics); the world can still interrupt
  view [section]                 the eight sections: now, projects, people, institutions, knowledge, civilization, machine, journal
  challenge [begin <id>]         Grand Challenges: the question, the next stage and what holds it up; begin the next stage
  people                         the people you know, and how their lives stand now
  commission                     work people bring you: look at the problem (unpaid), then accept, counter or decline the terms
  log [n]                        the last n events
  ledger [n]                     your money: the last n entries (who paid, what for) and the totals
  end                            End Month: the calendar moves one month (only this ends a month)
  wait                           fast-forward: months pass until something needs you
  inventions                     the invention tree: what you can make, what each needs first and from Rome
  invent <invention>             start work on an invention (income, standing and influence)
  machine                        the time machine: what's repaired and what's next
  assess                         assess the machine to learn what's wrong (needed before any repair)
  restore <aurei>                put scavenged gold back into the machine (all of it is needed to jump)
  exchange <n> aurei|denarii     change money at the money changers (1 Attention, a fee each way)
  visit <place>                  walk around Rome: market, changers, forges, curia, subura (no Attention)
  deposit <aurei> / bury <aurei> keep gold for your return: the machine carries only a small purse
  repair <coil|coolant|chronometer>   start the next repair step (all 9 steps are needed to jump)
  upgrade <contacts|lens|flywheel>    optional: each upgrade lets the machine carry you further
  jump                           prepare to leave (then pay down, endow, audit, or 'jump' again)
  quit");
    }

    /// <summary>Runs one command. Returns false only when a game command was refused (used by the playtest harness).</summary>
    private bool Handle(string cmd, string arg, string[] parts)
    {
        int attentionBefore = _sim.World.Attention;
        CommandResult? r = null;
        switch (cmd)
        {
            case "help": case "?": Help(); return true;
            case "status": case "s": Status(); return true;
            case "projects": case "p": Projects(); return true;
            case "why": Console.WriteLine(Why.Explain(_sim, arg)); return true;
            case "news": case "n": foreach (var l in _sim.News()) Console.WriteLine(l); return true;
            case "log": Log(parts.Length > 1 && int.TryParse(arg, out var n) ? n : 12); return true;
            case "people": case "who": People(); return true;
            case "view": case "section": View(arg); return true;
            case "focus": r = _sim.SetSceneFocus(arg); break;
            case "give": r = _sim.Give(arg); break;
            case "listen": r = _sim.Listen(); break;
            case "challenge": case "challenges":
                if (arg.ToLowerInvariant() == "begin") { r = _sim.StartChallengeStage(parts.Length > 2 ? parts[2] : ""); break; }
                Challenges();
                return true;
            case "ledger": Ledger(parts.Length > 1 && int.TryParse(arg, out var ln) ? ln : 12); return true;
            case "commission": case "commissions":
            {
                string verb = arg.ToLowerInvariant(), id = parts.Length > 2 ? parts[2] : "";
                if (verb == "" ) { Commissions(); return true; }
                r = verb == "look" ? _sim.LookAtCommission(id) : verb == "accept" ? _sim.AcceptCommission(id)
                  : verb == "counter" ? _sim.CounterCommission(id) : verb == "decline" ? _sim.DeclineCommission(id)
                  : CommandResult.Fail("Usage: commission [look|accept|counter|decline] <id>");
                break;
            }
            case "start": r = _sim.StartProject(arg.ToLowerInvariant()); break;
            case "choose": r = _sim.ChooseSeeded(arg.ToLowerInvariant()); break;
            case "priority":
                if (parts.Length < 3 || !DomainInfo.TryParseDomain(parts[1], out var d) || !DomainInfo.TryParsePriority(parts[2], out var p))
                {
                    Console.WriteLine("Usage: priority <medicine|governance|economy> <protect|maintain|accept>");
                    return false;
                }
                r = _sim.SetPriority(d, p);
                break;
            case "paydown":
                if (parts.Length < 3 || !DomainInfo.TryParseDomain(parts[1], out var pd) || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pts))
                {
                    Console.WriteLine("Usage: paydown <domain> <points>   (costs " + _sim.Money(_sim.PaydownCost(1)) + " per point)");
                    return false;
                }
                r = _sim.PayDown(pd, pts);
                break;
            case "institutions": case "i": Institutions(); return true;
            case "machine": case "m":
                Console.WriteLine("Machine: " + _sim.MachineStepsDone + "/" + _sim.MachineStepsTotal + " repair steps and " + F(_sim.MachineGoldRestored) + "/" +
                                  F(_sim.MachineGoldNeeded) + " aurei restored (all are needed to jump).");
                foreach (var l in _sim.MachineStatus()) Console.WriteLine("  " + l);
                return true;
            case "assess": r = _sim.Assess(); break;
            case "restore":
                if (!double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var restore))
                {
                    Console.WriteLine("Usage: restore <aurei>   (" + F(_sim.MachineGoldNeeded - _sim.MachineGoldRestored) + " aurei still missing from the machine; you hold " + _sim.AureiText(_sim.World.Aurei) + ")");
                    return false;
                }
                r = _sim.RestoreGold(restore);
                break;
            case "visit": case "walk":
                Console.WriteLine(Wrap(cmd == "walk" && arg == "" ? _sim.Data.Content.Template("walk.intro") : _sim.Visit(arg)));
                return true;
            case "exchange":
            {
                double exn = 0;
                string unit = parts.Length > 2 ? parts[2].ToLowerInvariant() : "";
                if (parts.Length < 3 || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out exn) ||
                    !(unit.StartsWith("aure") || unit.StartsWith("gold") || unit.StartsWith("den")))
                {
                    Console.WriteLine("Usage: exchange <n> aurei   (sell gold for denarii)   or   exchange <n> denarii   (buy gold with them)");
                    Console.WriteLine("  An aureus is worth " + F(Math.Round(_sim.AureusInDenarii, 1)) + " denarii now; the changers take " +
                                      F(_sim.T.Get("currency.exchangeFee") * 100) + "% each way, and a trip costs " + _sim.T.GetInt("currency.exchangeAttention") + " Attention.");
                    return false;
                }
                r = unit.StartsWith("den") ? _sim.BuyAurei(Math.Floor(_sim.FromDenarii(exn) / _sim.AureiCost(1) + 1e-9)) : _sim.SellAurei(exn);
                break;
            }
            case "deposit":
                r = double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var dep) ? _sim.Deposit(dep)
                    : CommandResult.Fail("Usage: deposit <aurei>   (with the banking house: a little interest in gold, and a risk it fails)");
                break;
            case "bury":
                r = double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var bur) ? _sim.Bury(bur)
                    : CommandResult.Fail("Usage: bury <aurei>   (a jar only you know about: no interest, and someone may find it)");
                break;
            case "repair": r = _sim.Repair(arg); break;
            case "upgrade": r = _sim.Upgrade(arg); break;
            case "inventions":
                foreach (var branch in Simulation.InventionBranches)
                {
                    Console.WriteLine("  " + char.ToUpperInvariant(branch[0]) + branch.Substring(1) + ":");
                    foreach (var idea in _sim.Data.Content.Inventions.Where(x => x.Branch == branch))
                    {
                        string state = _sim.InventionState(idea);
                        Console.WriteLine("    " + idea.Id.PadRight(12) + _sim.Money(_sim.InventionGold(idea)).PadLeft(14) + ", " + idea.AttentionPerTurn + " Attention a month for " + idea.DurationMonths + " months  " + idea.Name +
                                          (state == "ready" ? "" : "   (" + state + ")"));
                        if (state != "made")
                        {
                            Console.WriteLine("                " + idea.Description);
                            if (idea.Baseline.Length > 0) Console.WriteLine("                Rome already: " + idea.Baseline);
                            Console.WriteLine("                Pays: " + _sim.InventionPayoffText(idea));
                        }
                    }
                }
                return true;
            case "invent": r = _sim.Invent(arg); break;
            case "attend": r = _sim.Attend(arg, parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : null); break;
            case "office":
                r = parts.Length < 3 || !(arg == "accept" || arg == "decline")
                    ? CommandResult.Fail("Usage: office accept <inst>  or  office decline <inst>")
                    : _sim.AnswerOffice(parts[2], arg == "accept");
                break;
            case "resign": r = _sim.Resign(arg); break;
            case "decide": r = _sim.Decide(arg); break;
            case "workshop": foreach (var l in _sim.WorkshopLines()) Console.WriteLine(l); return true;
            case "take": r = _sim.TakeOrder(arg); break;
            case "expand": r = _sim.Expand(); break;
            case "apprentice":
                r = arg.StartsWith("h", StringComparison.OrdinalIgnoreCase) ? _sim.HireApprentice()
                  : arg.StartsWith("d", StringComparison.OrdinalIgnoreCase) ? _sim.DismissApprentice()
                  : CommandResult.Fail("Usage: apprentice hire | apprentice dismiss");
                break;
            case "orders":
                r = parts.Length < 3 ? CommandResult.Fail("Usage: orders <inst> <camp> [1|2]   (the camp to back when you leave; from the head's seat, the successor to name)")
                    : _sim.Orders(arg, parts[2], parts.Length > 3 && int.TryParse(parts[3], out var succ) ? succ : (int?)null);
                break;
            case "found": r = _sim.Found(arg); break;
            case "buy":
                if (parts.Length < 2) { Console.WriteLine("Usage: buy <institution> [percent]"); return false; }
                // L8 (tester 6): with no percent, a first purchase buys what joining takes (the bank's first is 5%, not 1%).
                var target = _sim.FindInstitution(arg);
                // P1 (decided 2026-10-02): an institution on an invitation path doesn't sell seats.
                if (target != null && _sim.OnInvitationPath(target)) { Console.WriteLine(Simulation.Cap(target.Def.ShortName) + " doesn't sell seats: members bring you in. " + AccessText(target)); return false; }
                if (target != null && _sim.PatronageOnly(target)) { Console.WriteLine(Simulation.Cap(target.Def.ShortName) + " doesn't sell places: a senator's following takes clients through a patron's introduction. (" + Simulation.Cap(_sim.PatronageStanding(target)) + ".)"); return false; }
                if (target != null && _sim.TakesGifts(target)) { Console.WriteLine(Simulation.Cap(target.Def.ShortName) + " doesn't sell shares; it takes gifts. Type 'give " + target.Key + "' (" + _sim.Money(_sim.GiftCost(target)) + ")."); return false; }
                int pct = target != null && !target.Backed && target.Def.JoinRequirement == "deposit" ? _sim.T.GetInt("joining.bankMinFirstPercent") : 1;
                if (parts.Length > 2 && !int.TryParse(parts[2].TrimEnd('%'), out pct)) { Console.WriteLine("Usage: buy <institution> [percent]"); return false; }
                r = _sim.Buy(arg, pct);
                break;
            case "invitation": case "invite":
            {
                string verb = arg.ToLowerInvariant(), inst = parts.Length > 2 ? parts[2] : "";
                r = verb == "accept" ? _sim.AcceptInvitation(inst) : verb == "decline" ? _sim.DeclineInvitation(inst) : CommandResult.Fail("Usage: invitation accept|decline <institution>");
                break;
            }
            case "invest":
                if (parts.Length < 3 || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var inv))
                {
                    Console.WriteLine("Usage: invest <institution> <gold>");
                    return false;
                }
                r = _sim.Invest(arg, _sim.FromDenarii(inv));
                break;
            case "charter": r = _sim.Charter(arg); break;
            case "endow":
                if (parts.Length > 2 && parts[2].Equals("all", StringComparison.OrdinalIgnoreCase))
                    r = _sim.Endow(arg, Math.Floor(_sim.World.Gold));
                else
                    r = parts.Length > 2 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
                        ? _sim.Endow(arg, _sim.FromDenarii(amount)) : _sim.Endow(arg);
                break;
            case "audit": r = _sim.Audit(arg); break;
            case "policy":
                if (parts.Length < 3 || !Simulation.TryParsePolicy(parts[1], parts[2], out var issue, out var stance))
                {
                    Console.WriteLine("Usage: policy coinage sound|debase|history · policy prices free|controlled|history · " +
                                      "policy property secure|discretionary|history · policy taxes light|heavy|history");
                    return false;
                }
                r = _sim.SetPolicy(issue, stance);
                break;
            case "advocate":
                if (parts.Length < 3 || !Simulation.TryParsePolicy(parts[1], parts[2], out var aIssue, out var aStance))
                {
                    Console.WriteLine("Usage: advocate <issue> <stance>, e.g. advocate coinage sound (without a voice; " + _sim.Money(_sim.AdvocacyCost()) +
                                      ", " + _sim.T.GetInt("policy.advocacy.attention") + " Attention; sway " + Math.Round(_sim.AdvocacySway() * 100) + "% while you're in Rome)");
                    return false;
                }
                r = _sim.Advocate(aIssue, aStance);
                break;
            case "oversee": r = _sim.Oversee(arg); break;
            case "mentor": r = _sim.Mentor(arg); break;
            case "work": r = _sim.Work(parts.Length > 1 ? arg.ToLowerInvariant() : "odd"); break;
            case "promise": r = _sim.AnswerPromise(arg.StartsWith("y", StringComparison.OrdinalIgnoreCase)); break;
            case "respond": r = _sim.RespondToPlague(arg.ToLowerInvariant()); break;
            case "turns": Console.WriteLine("Each turn is one month (fixed). 'end' ends the month; 'wait' fast-forwards until something needs you."); return false;
            case "end": case "e": EndTurn(wait: false); return true;
            case "wait": case "w": EndTurn(wait: true); return true;
            case "jump": Jump(); return true;
            default: Console.WriteLine("Unknown command. Type 'help'."); return false;
        }
        Console.WriteLine(r.Message + (r.Ok && _sim.World.Attention < attentionBefore
            ? "  [Attention left this month: " + _sim.World.Attention + "/" + _sim.AttentionPerTurn + "]" : ""));
        // During jump preparation, show the updated briefing after each change.
        if (_jumpArmed && r.Ok && PrepCommands.Contains(cmd)) Briefing();
        return r.Ok;
    }

    private void EndTurn(bool wait)
    {
        int from = _sim.Log.Events.Count;
        int months = 1;
        if (wait) months = _sim.AdvanceUntilDecision();
        else _sim.EndMonth();
        if (months > 1) Console.WriteLine("  (" + months + " months pass; nothing needed you until now)");
        var shown = new[] { "project.complete", "debt.tier", "plague.warning", "plague.outbreak", "plague.toll", "plague.opening", "plague.passed",
                            "seeded.payoff", "promise.offer", "promise.kept", "commitment.complete", "income.bonus", "seeded.choice", "institution.unpaid", "year.start",
                            "machine.step", "machine.assessed", "invention.complete", "institution.stake", "institution.seniority", "rivalry.strike", "institution.collapse", "bust.warning", "bust.toll",
                            // L4 (tester 2): apprentices who leave over unpaid wages, and a workshop that grows or shrinks, were only in the log.
                            "workshop.apprentice", "workshop.size",
                            // P1 commissions: the scenes as they happen.
                            "commission.encounter", "commission.stage", "commission.complete", "commission.referral", "institution.access",
                            "invitation.offer", "institution.join", "invitation.wait",
                            "person.life", "person.return", "challenge.open", "challenge.stage", "challenge.complete" };
        foreach (var e in _sim.Log.Events.Skip(from).Where(e => shown.Contains(e.Type) || e.Type.StartsWith("scene.", StringComparison.Ordinal)))
            Console.WriteLine("  • " + e.Text);
        var settle = _sim.Log.Events.Skip(from).LastOrDefault(e => e.Type == "gold.settle");
        if (settle != null && settle.Text.Contains("could pay only")) Console.WriteLine("  • " + settle.Text);
        Status();
    }

    /// <summary>P1 header (decided 2026-10-02): "Attention: 2 free / 4 total · 2 reserved — Machine Assessment".</summary>
    private string AttentionHeader()
    {
        var reserved = _sim.ReservedAttentionParts();
        int held = reserved.Sum(p => p.Attention);
        return "Attention: " + _sim.World.Attention + " free / " + _sim.AttentionPerTurn + " total" +
               (held > 0 ? " · " + held + " reserved — " + string.Join(", ", reserved.Select(p => p.What)) : "");
    }

    private void Status()
    {
        var w = _sim.World;
        Console.WriteLine();
        Console.WriteLine("== Month " + _sim.Turn + " · " + _sim.Now.Display + " ==  " + _sim.Money(w.Gold) + " (" + (((_sim.YearlyIncome() - _sim.YearlyUpkeepTotal()) * _sim.YearsPerTurn) >= 0 ? "+" : "−") +
                          _sim.Money(Math.Abs((_sim.YearlyIncome() - _sim.YearlyUpkeepTotal()) * _sim.YearsPerTurn)).Replace(" denarii", "") + "/month) · " +
                          _sim.AureiText(w.Aurei) + " (1 = " + F(Math.Round(_sim.AureusInDenarii, 1)) + " den.)   Index " + F(_sim.SphereIndex()));
        Console.WriteLine("  " + AttentionHeader());
        foreach (var d in w.Domains)
            Console.WriteLine("  " + d.Domain.ToString().PadRight(11) + F(d.Level).PadLeft(5) + "  expect " + F(_sim.Expectation(d.Domain)).PadLeft(4) +
                              "  " + (!_sim.HasHold(d.Domain) ? "(no voice)" : d.Priority.Label()).PadRight(11) + " debt " + F(d.Debt).PadLeft(5) + " " + d.Tier);
        if (w.PriceLevel > 1.0001) Console.WriteLine("  Prices: +" + F((w.PriceLevel - 1) * 100) + "% since AD 155 (" + F(_sim.InflationRate() * 100) + "% a year)");
        if (Simulation.Issues.Any(i => _sim.Stance(i) != 0))
            Console.WriteLine("  Policy: " + string.Join(", ", Simulation.Issues.Select(i => i.ToString().ToLowerInvariant() + " " + Simulation.StanceWord(i, _sim.Stance(i)))) +
                              (!_sim.PolicyHold() && w.Advocating ? "   (your advocacy: sway " + F(_sim.AdvocacySway() * 100) + "%, only while you're here)" : ""));
        if (w.Bust.Stage > 0) Console.WriteLine("  Economy: " + Simulation.BustStageText(w.Bust.Stage));
        var plague = w.Plague;
        if (plague.Stage > 0) Console.WriteLine("  Pestilence: " + Simulation.PlagueStageText(plague.Stage));
        foreach (var n in _sim.HistoryNewsThisTurn()) Console.WriteLine("  News: " + n.Text + "   (news)");
        if (_sim.PendingEvent is EventDef ev)
        {
            // A decision event or a leader's request (P0-33, P0-32).
            Console.WriteLine("  ► " + ev.Title + ". " + ev.Text);
            foreach (var o in ev.Options)
                Console.WriteLine("      decide " + o.Id.PadRight(8) + " " + o.Label + (_sim.EventCost(o) > 0 ? " (" + _sim.Money(_sim.EventCost(o)) + ")" : ""));
        }
        foreach (var n in _sim.LocalNewsThisTurn()) Console.WriteLine("  On your street: " + n + "   (news)");
        if (_sim.OwnsWorkshop && _sim.OrdersLeftThisSeason > 0 && _sim.OrderBoard().Any())
            Console.WriteLine("  Workshop orders: " + string.Join("; ", _sim.OrderBoard().Select(o => "take " + o.Id + " (" + _sim.Money(_sim.OrderPay(o)) + ", " + o.Attention + " Att.)")) +
                              (w.Apprentices > 0 ? "   apprentices " + w.Apprentices : "") + "   (workshop)");
        foreach (var i in _sim.Backed())
            Console.WriteLine("  " + Simulation.Cap(i.Def.ShortName) + " (" + i.Leader + "): you hold " + _sim.StakePercent(i) + "%" + StakeLabel(i) + ", strength " + F(i.Strength) +
                              " (" + F(_sim.DomainShare(i) * 100) + "% of " + i.Def.Maintains + ")" + (_sim.Controls(i) ? ", loyalty " + F(i.Loyalty) : "") +
                              (i.Chartered ? ", chartered" : "") + (i.Endowed ? ", endowed" : "") + (i.AuditCharter ? ", audited" : "") +
                              (i.Holdings > 0 ? ", holds " + _sim.Money(i.Holdings) : ""));
        foreach (var p in w.ActiveProjects) Console.WriteLine("  Under way: " + p.Def.Name + " (" + p.TurnsRemaining + " month(s) left)");
        foreach (var a in w.ActiveInventions) Console.WriteLine("  Inventing: " + a.Def.Name + " (" + a.TurnsRemaining + " month(s) left)");
        foreach (var p in w.Invitations.Where(p => p.Pending != InvitationOffer.None))
            Console.WriteLine("  ► " + _sim.InvitationPathDefFor(p.Institution)!.Inviter + "'s invitation: invitation accept|decline " + p.Institution + "   (institutions)");
        foreach (var c in _sim.OpenCommissions())
        {
            var d = _sim.CommissionDefOf(c);
            Console.WriteLine("  " + (c.Status == CommissionStatus.Working ? "Commission: " + d.Title + " (" + Simulation.StageLabel(d.Work[c.WorkIndex].Stage) + ", " + c.MonthsLeftInStage + " month(s) left in this stage)"
                : "► " + d.Client + ": " + (c.Status == CommissionStatus.Offered ? "commission look " + d.Id + " (unpaid)" : "terms on the table: commission accept|counter|decline " + d.Id)) + "   (commission)");
        }
        if (w.WorkshopBuildTurns > 0) Console.WriteLine("  Enlarging the workshop (" + w.WorkshopBuildTurns + " month(s) left)   (workshop)");
        Console.WriteLine("  Machine: " + (_sim.MachineAssessed ? _sim.MachineStepsDone + "/" + _sim.MachineStepsTotal + " repair steps" : "not yet assessed") +
                          ", gold " + F(_sim.MachineGoldRestored) + "/" + F(_sim.MachineGoldNeeded) + " aurei" +
                          string.Concat(w.ActiveMachineSteps.Select(a => "; under way: " + a.Def.Name + " (" + a.TurnsRemaining + " month(s) left)")) +
                          (_sim.MachineReady ? " — ready to jump" : "") + "   (machine)");
        foreach (var c in w.Commitments) Console.WriteLine("  Mentoring " + c.InstitutionId + " (" + c.TurnsRemaining + " month(s) left)");
        if (_sim.SeededChoiceOpen) Console.WriteLine("  ► Waiting: choose fountain or choose workshop (before the end of turn " + _sim.T.GetInt("seededChoice.deadlineTurn") + ").");
        if (w.Promise.Status == PromiseStatus.Offered) Console.WriteLine("  ► Waiting: Demetria asks you to stay until the sickness has passed. promise yes / promise no");
        if (_sim.OutbreakAwaitingResponse)
            Console.WriteLine("  ► Waiting: respond " + string.Join(" / respond ", _sim.AvailablePlagueResponses()) +
                              (!_sim.CanDirectPlagueResponse() ? "   (no one will take orders from you: a response needs 10% of a Medicine or Governance institution)"
                               : _sim.AvailablePlagueResponses().Contains("hospice") ? "" : "   (a hospice needs a physicians' circle of strength 30+)"));
        if (_sim.EraOver) Console.WriteLine("  ► The era's " + _sim.EraYears + " years are over. Jump when you're ready (you can also stay).");
    }

    private void Projects()
    {
        foreach (var p in _sim.AvailableProjects())
        {
            string? blocked = _sim.ProjectAuthorityBlocker(p);
            Console.WriteLine("  " + p.Id.PadRight(12) + p.Domain.ToString().PadRight(11) + _sim.Money(_sim.ProjectGold(p)).PadLeft(14) + "  " + p.DurationMonths + "t  +" + F(p.LevelGain) + "  " + p.Name +
                              (blocked != null ? "\n                (" + blocked + ")" : p.Authority != null ? "   (public: you have the backing)" : ""));
        }
        Console.WriteLine("  Institutions: see 'institutions' (buy into one, or found your own).");
    }

    private string StakeLabel(Institution i) =>
        _sim.Controls(i) ? " (control)" : _sim.HasVoice(i) ? " (voice)" : _sim.HasInfluence(i) ? " (influence)" : " (member)";

    /// <summary>Where you stand with an institution that takes members by invitation (P1).</summary>
    private string AccessText(Institution i)
    {
        var a = _sim.World.AccessTo(i.Key);
        var d = _sim.InvitationPathDefFor(i.Key)!;
        string stage = a.Stage switch
        {
            InstitutionAccessStage.Unaware => "you know no one there",
            InstitutionAccessStage.Aware => "you know of it, but no one in it",
            InstitutionAccessStage.KnowsMember => "you know " + a.KnownMemberId + ", a member",
            InstitutionAccessStage.Guest => "you have been a guest at a supper",
            InstitutionAccessStage.InvitedBack => "you have been asked back",
            InstitutionAccessStage.SponsoredCandidate => a.SponsorId + " has put your name forward",
            InstitutionAccessStage.Member => "you are a member",
            _ => "you hold an office",
        };
        return "membership by invitation: " + stage + ".";
    }

    private void Institutions()
    {
        foreach (var d in DomainInfo.All)
        {
            Console.WriteLine(d + ": your influence " + F(_sim.Influence(d) * 100) + "%, sway " + F(_sim.Sway(d) * 100) + "%");
            foreach (var i in _sim.World.Institutions.Where(x => x.Def.Maintains == d))
            {
                string line = "  " + i.Key.PadRight(10) + Simulation.Cap(i.Def.Name) + " — " + i.Leader + (i.Leader == "you" ? "" : " (" + i.Integrity + ")");
                if (!i.Exists)
                    line += i.Collapsed ? ": failed" : ": yours to found for " + _sim.Money(_sim.FoundCost(d)) + ", " + _sim.T.GetInt("founding.attention") +
                                                       " Attention (starts at strength " + F(_sim.T.Get("founding.startStrength")) + ")";
                else
                {
                    line += ": strength " + F(i.Strength) + " (" + F(_sim.DomainShare(i) * 100) + "%)";
                    if (_sim.OnInvitationPath(i)) line += "; " + AccessText(i);
                    if (_sim.TakesGifts(i)) line += i.Stake > 0 ? "; you are among its benefactors" : "; it takes gifts (give " + i.Key + ", " + _sim.Money(_sim.GiftCost(i)) + ")";
                    else if (_sim.PatronageOnly(i)) line += "; " + _sim.PatronageStanding(i);
                    else if (i.Stake > 0) line += ", you hold " + _sim.StakePercent(i) + "%" + StakeLabel(i);
                    else if (_sim.OnInvitationPath(i)) { }
                    else if (!i.Def.IsOwn)
                        line += "; to join: " + _sim.JoinRequirementText(i) + (_sim.JoinBlocker(i) == null ? " (you qualify)" : " (not yet)");
                    int next = _sim.NextThresholdPercent(i);
                    if (!i.Def.IsOwn && next > 0 && !_sim.OnInvitationPath(i) && !_sim.TakesGifts(i) && !_sim.PatronageOnly(i))
                        line += "; next 1% " + _sim.Money(_sim.BuyCost(i, 1)) + (i.Stake <= 0 && _sim.EntryFee(i) > 0 ? " incl. " + _sim.Money(_sim.EntryFee(i)) + " entry fee" : "") +
                                ", to " + next + "% " + _sim.Money(_sim.BuyCost(i, next - _sim.StakePercent(i))) + "; dues " + _sim.Money(_sim.T.Get("joining.duesBasePerYear." + i.Key)) + "/yr + " +
                                _sim.Money(_sim.T.Get("joining.duesPerStakePercentPerYear")) + " per %";
                }
                Console.WriteLine(line);
                if (i.Exists && i.Def.DriftPaths.Count >= 2)
                {
                    // Camps and offices (P0-32).
                    var lead = _sim.LeadingCamp(i);
                    string camps = "            camps: " + string.Join(" vs ", i.Def.DriftPaths.Select(p => p.Id + " (" + p.Name + ")")) + " — " +
                                   (lead == null ? "neither leads" : i.Def.DriftPaths[lead.Value].Id + " lead") +
                                   (i.Votes[0] + i.Votes[1] > 0 ? "; your votes " + i.Votes[0] + "–" + i.Votes[1] : "");
                    Console.WriteLine(camps);
                    if (i.Stake > 0 || i.Def.IsOwn)
                    {
                        string office = "            " + (i.Def.IsOwn ? (i.Stake > 0 ? "you lead it as " + _sim.OfficeTitle(i, Simulation.Head) + " (duties " + _sim.DutyAttention(Simulation.Head, true) + " a turn)" : "")
                            : "your office: " + _sim.OfficeTitle(i, i.Rank) + (i.Rank >= Simulation.Officer ? " (duties " + _sim.DutyAttention(i.Rank, false) + " a turn)" : ""));
                        if (!i.Def.IsOwn)
                        {
                            int nextRank = Math.Max(Simulation.Member, i.Rank) + 1;
                            if (i.OfferedRank > 0) office += "; OFFERED: " + _sim.OfficeTitle(i, i.OfferedRank) + " (office accept|decline " + i.Key + ")";
                            else if (nextRank <= Simulation.Head) office += "; next: " + _sim.OfficeTitle(i, nextRank) + " — " + (_sim.OfficeBlocker(i, nextRank) ?? "you qualify; the offer comes at the new year");
                        }
                        if (i.OrderCamp >= 0) office += "; last orders: back " + i.Def.DriftPaths[i.OrderCamp].Id + " (" + Simulation.OrdersBand(_sim.LastOrderForce(i)) + " weight)";
                        if (office.Trim().Length > 0) Console.WriteLine(office);
                    }
                }
            }
        }
    }

    /// <summary>P1 commissions: what's offered, what's on the table, what's under way.</summary>
    private void Commissions()
    {
        var open = _sim.OpenCommissions().ToList();
        if (open.Count == 0) { Console.WriteLine("No one has brought you work yet."); return; }
        foreach (var c in open)
        {
            var d = _sim.CommissionDefOf(c);
            string where = c.Status == CommissionStatus.Offered ? "offered: commission look " + d.Id + " (" + d.Diagnosis.Attention + " Attention · Pay: none initially · may lead to paid commission)"
                         : c.Status == CommissionStatus.TermsOffered ? "terms on the table " + _sim.TermsLine(c)
                         : "under way: " + Simulation.StageLabel(d.Work[c.WorkIndex].Stage) + ", " + c.MonthsLeftInStage + " month(s) left in this stage";
            Console.WriteLine("  " + d.Title + " (" + d.Client + ", " + d.ClientRole + ") — " + where);
        }
    }

    /// <summary>P1 people: who you know and how they stand now (present conditions only).</summary>
    private void People()
    {
        var known = _sim.KnownPeople().ToList();
        if (known.Count == 0) { Console.WriteLine("You don't know anyone here by name yet."); return; }
        foreach (var d in known)
        {
            var p = _sim.PersonOf(d.Id)!;
            Console.WriteLine("  " + d.Name + ", " + d.Role + ". " + Capitalize(p.Status) + "." + (_sim.IsPersonAway(d.Id) ? " Laid up or away for now." : ""));
            Console.WriteLine("      household: " + d.Household + "; cares about " + d.CaresAbout + "; wants " + d.Goal + ".");
        }
    }

    /// <summary>P1 Grand Challenges: the question, how far it has come, and the next stage with what holds it up.</summary>
    private void Challenges()
    {
        var open = _sim.World.Challenges.Where(c => c.Status != ChallengeStatus.NotYet).ToList();
        if (open.Count == 0) { Console.WriteLine("No Grand Challenge has come up yet. Your work will raise one."); return; }
        foreach (var c in open)
        {
            var d = _sim.ChallengeDefOf(c);
            Console.WriteLine("  " + d.Name + " — " + d.Question + "   [" + c.StageIndex + "/" + d.Stages.Count + " stages]");
            if (c.Status == ChallengeStatus.Working) Console.WriteLine("    Under way: " + _sim.NextStage(c)!.Name + ", " + c.MonthsLeft + " month(s) left.");
            else if (c.Status == ChallengeStatus.Open) Console.WriteLine("    Next: " + _sim.StageLine(_sim.NextStage(c)!) + "   (challenge begin " + d.Id + ")");
            else Console.WriteLine("    " + c.Status + ".");
        }
    }

    /// <summary>P1 sections: active, available now, blocked (and why), emerging, archived.</summary>
    private void View(string arg)
    {
        if (!Enum.TryParse<MenuSection>(arg, true, out var section) || arg.Length == 0)
        {
            Console.WriteLine("  Sections: " + string.Join(" · ", Enum.GetValues(typeof(MenuSection)).Cast<MenuSection>().Select(m => m.ToString().ToLowerInvariant() + " (" + _sim.ViewOf(m).Count + ")")));
            Console.WriteLine("  Type 'view <section>', e.g. view people.");
            return;
        }
        var v = _sim.ViewOf(section);
        Console.WriteLine("== " + section);
        void Part(string title, List<ViewItem> items)
        {
            if (items.Count == 0) return;
            Console.WriteLine("  " + title + ":");
            foreach (var i in items) Console.WriteLine("    " + i.Label + (i.Command.Length > 0 && i.Command != section.ToString().ToLowerInvariant() ? "   (" + i.Command + ")" : ""));
        }
        Part("Active", v.Active);
        Part("Available now", v.AvailableNow);
        Part("Blocked", v.Blocked);
        Part("Emerging", v.Emerging);
        Part("Archived", v.Archived);
        if (v.Count == 0) Console.WriteLine("  Nothing here yet.");
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    /// <summary>The P1 ledger: every change to your money, with who paid and why.</summary>
    private void Ledger(int n)
    {
        var l = _sim.World.Ledger;
        foreach (var e in l.Entries.Skip(Math.Max(0, l.Entries.Count - n)))
            Console.WriteLine("  " + (e.Amount >= 0 ? "+" : "−") + _sim.Money(Math.Abs(e.Amount)).PadLeft(16) + "  " + e.Kind.ToString().PadRight(15) +
                              (e.Counterparty.Length > 0 ? e.Counterparty + ": " : "") + e.Reason);
        Console.WriteLine("  In: " + _sim.Money(l.Income) + " · out: " + _sim.Money(l.Expenses) + " · now: " + _sim.Money(_sim.World.Gold) + ".");
    }

    private void Log(int n)
    {
        foreach (var e in _sim.Log.Events.Where(e => e.Type != "turn.start" && e.Type != "gold.settle").Reverse().Take(n).Reverse())
            Console.WriteLine("  " + e.Time.Stamp + "  " + e.Text);
    }

    private void Jump()
    {
        // Armed only when the machine can go: a second 'jump' on an unready machine crashed the game.
        if (!_jumpArmed || !_sim.MachineReady)
        {
            _jumpArmed = _sim.MachineReady;
            if (!_sim.MachineReady)
            {
                Console.WriteLine("The machine isn't ready: " + _sim.MachineStepsDone + " of " + _sim.MachineStepsTotal + " repair steps done.");
                foreach (var l in _sim.MachineStatus()) Console.WriteLine("  " + l);
                return;
            }
            var (lo, hi) = _sim.JumpRange();
            Console.WriteLine("You will leave AD " + _sim.Now.Year + " and arrive somewhere between AD " + (_sim.Now.Year + lo) + " and AD " + (_sim.Now.Year + hi) +
                              ": the machine's range is " + _sim.JumpRangeText() + ". You can't come back.");
            Briefing();
            return;
        }
        var arrival = _sim.Jump();
        // The jump is spent: a 'jump' at the arrival starts a new preparation, never a second jump at once
        // (testers 2 and 6 jumped twice with one extra 'jump' and never saw their first arrival).
        _jumpArmed = false;
        Console.WriteLine("\nThe machine shudders. Decades pass in the dark... It carries you " + arrival.JumpYears + " years.\n");
        foreach (var beat in arrival.Beats)
        {
            Console.WriteLine("— " + beat.Name + " —");
            Console.WriteLine(Wrap(beat.Text));
            Console.WriteLine();
            if (!Console.IsInputRedirected && _script == null)
            {
                Console.Write("(press Enter)");
                Console.ReadLine();
            }
        }
        Console.WriteLine(_sim.Data.Content.Template("walk.intro"));
        Console.WriteLine("Type 'learn more' for the Index and what became of your institutions" +
                          (_sim.CanJumpAgain ? ", 'jump' to go on another " + _sim.JumpRangeText().Split(' ')[0] + " years," : "") + " or 'quit'.");
    }

    private void Briefing()
    {
        Console.WriteLine("What you leave behind (" + _sim.Money(_sim.World.Gold) + " and " + _sim.AureiText(_sim.World.Aurei) + " in hand):");
        foreach (var line in _sim.DepartureBriefing()) Console.WriteLine("  • " + line);
        // After an arrival there is no era to act in: only your gold can still be put away (tester 7 was offered paydown,
        // endow, audit and exchange there, and 'bury' was refused).
        Console.WriteLine(_sim.Arrived
            ? "Prepare: deposit <aurei> · bury <aurei>. Type 'jump' again to go, or anything else to stay."
            : "Prepare: paydown <domain> <points> · endow <inst> <denarii|all> · audit <inst> · exchange <n> denarii · deposit <aurei> · bury <aurei>. Type 'jump' again to go, or anything else to stay.");
    }

    private void AfterArrival(string cmd, string arg)
    {
        if (cmd == "learn" || cmd == "more") Console.WriteLine(_sim.Arrival!.LearnMore());
        else if (cmd == "why") Console.WriteLine(Why.Explain(_sim, arg));
        else if (cmd == "jump" && _sim.CanJumpAgain) Jump();
        else if ((cmd == "deposit" || cmd == "bury") && _sim.CanJumpAgain)
        {
            if (!double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) { Console.WriteLine("Usage: " + cmd + " <aurei>"); return; }
            var r = cmd == "deposit" ? _sim.Deposit(n) : _sim.Bury(n);
            Console.WriteLine(r.Message);
            if (_jumpArmed && r.Ok) Briefing();
        }
        else if (cmd == "visit" || cmd == "walk") Console.WriteLine(Wrap(cmd == "walk" && arg == "" ? _sim.Data.Content.Template("walk.intro") : _sim.Visit(arg)));
        else Console.WriteLine(_sim.CanJumpAgain ? "'visit <place>', 'learn more', 'jump' to go on (then 'deposit' or 'bury' gold you can't carry), or 'quit'." : "The test is over. 'visit <place>', 'learn more' or 'quit'.");
    }

    private static string Wrap(string text, int width = 100)
    {
        var words = text.Split(' ');
        var sb = new System.Text.StringBuilder();
        int col = 0;
        foreach (var w in words)
        {
            if (col + w.Length > width) { sb.Append('\n'); col = 0; }
            else if (col > 0) { sb.Append(' '); col++; }
            sb.Append(w);
            col += w.Length;
        }
        return sb.ToString();
    }
}
