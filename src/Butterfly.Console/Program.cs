using System;
using System.Globalization;
using System.Linq;
using Butterfly.Core;

// The Butterfly Effect — P0 Butterfly Test, text console for human players.
// Usage: dotnet run --project src/Butterfly.Console -- --seed 42
// Scripted (automated playtests): add --inputs <file> [--checks <file>]; see playtests/ai/README.md.
// Add --continue to keep playing from the keyboard once the script's lines run out (resume a saved inputs file).
ulong seed = 42;
bool resume = args.Contains("--continue");
string? inputs = null, checks = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--seed") seed = ulong.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--inputs") inputs = args[i + 1];
    if (args[i] == "--checks") checks = args[i + 1];
}

var sim = new Simulation(GameData.LoadDefault(), seed);
var game = new ConsoleGame(sim, inputs == null ? null : new ScriptInput(inputs, sim), checks == null ? null : new Harness(sim, checks), resume);
game.Run();

internal sealed class ConsoleGame
{
    private readonly Simulation _sim;
    private readonly ScriptInput? _script;
    private readonly Harness? _harness;
    private bool _jumpArmed;
    /// <summary>End the turn by itself once a choice uses the last Attention (decided 2026-09-28). On for keyboard play;
    /// scripts turn it on with the line "@autoend on" so older scripts with explicit 'end's still replay the same.</summary>
    private bool _autoEnd;
    private static readonly string[] PrepCommands = { "paydown", "endow", "audit", "status", "s", "why", "help", "?", "exchange", "deposit", "bury", "restore", "visit", "walk" };

    /// <summary>After the script's last line, read from the keyboard (--continue).</summary>
    private readonly bool _resume;
    private bool _scriptDone;

    public ConsoleGame(Simulation sim, ScriptInput? script = null, Harness? harness = null, bool resume = false)
    {
        _resume = resume;
        _sim = sim;
        _script = script;
        _harness = harness;
        _autoEnd = script == null;
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
        while (true)
        {
            Console.Write(_sim.Arrived ? "\n(after arrival) > " : "\n> ");
            string? line = ReadCommand();
            if (line == null) break;
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1] : "";
            if (cmd == "quit" || cmd == "exit") break;
            // Jump preparation: these commands keep the jump armed.
            if (cmd != "jump" && !(_jumpArmed && PrepCommands.Contains(cmd))) _jumpArmed = false;
            if (_sim.Arrived)
            {
                AfterArrival(cmd, arg);
                _harness?.AfterCommand(line, true);
                continue;
            }
            if (cmd == "@autoend") { _autoEnd = arg != "off"; continue; }
            int attentionBefore = _sim.World.Attention;
            bool ok = Handle(cmd, arg, parts);
            _harness?.AfterCommand(line, ok);
            if (ok && _autoEnd && !_jumpArmed && !_sim.Arrived && _sim.World.Attention == 0 && (attentionBefore > 0 || cmd == "paydown"))
            {
                if (_sim.ShouldAutoEnd())
                {
                    Console.WriteLine("  (No Attention left: the turn ends.)");
                    EndTurn(wait: false);
                }
                else if (_sim.NoActionPossible())
                    Console.WriteLine("  (No Attention left. You can still pay down debt; type 'end' when you're done.)");
            }
        }
        Console.WriteLine("\nRun fingerprint (seed " + _sim.Seed + "): " + _sim.Log.Hash().Substring(0, 16));
        _harness?.Finish(_script);
    }

    private void Intro()
    {
        Console.WriteLine("THE BUTTERFLY EFFECT — P0 Butterfly Test");
        Console.WriteLine("========================================");
        Console.WriteLine("Rome, AD 155. Your time machine malfunctioned and stranded you here. You have a pouch of gold you scavenged from the");
        Console.WriteLine("machine (every bit of it must go back before it can fly), what you know, and no one who owes you anything.");
        Console.WriteLine("You don't yet know what broke. Something is coming from the East within ten years.");
        Console.WriteLine("Rome runs on silver denarii, not gold: change your aurei at the money changers (exchange <n> aurei). Gold holds its value; the denarius doesn't.");
        Console.WriteLine("Once you repair it, the machine can carry you forward, " + _sim.T.GetInt("jump.range.baseMin") + " to " + _sim.T.GetInt("jump.range.maxYears") +
                          " years depending on how well you repair it. What you leave behind will go on without you.");
        Console.WriteLine();
        Console.WriteLine("This prototype covers one era, a jump and your arrival, and then, if you like, one more jump to see how it all aged. The test ends there.");
        Console.WriteLine();
        Console.WriteLine("First, a choice. You can afford only one:");
        Console.WriteLine("  choose fountain  — " + _sim.Data.Content.Project("fountain")!.Description);
        Console.WriteLine("  choose workshop  — " + _sim.Data.Content.Project("workshop")!.Description);
        Console.WriteLine(_sim.Data.Content.Template("walk.intro.start"));
        Console.WriteLine("Type 'help' for commands.");
    }

    private void Help()
    {
        Console.WriteLine(@"Commands
  status                         where things stand
  projects                       projects you can start
  news                           what's going on in Rome: the talk of the Forum and the market (no Attention)
  start <project>                start a project (costs gold and Attention)
  priority <domain> <protect|maintain|accept>   (needs a 25% voice in an institution of that domain)
  paydown <domain> <points>      pay down debt (costs 1.5× what prevention would have)
  institutions                   who holds each domain, your stakes, and what the next step costs
  buy <inst> [percent]           buy into an established institution (2 Attention; entry fee on joining, each 1% costs more):
                                   10% counts toward influence · 25% a voice (priorities, policy) · 50% control
  attend <inst>                  attend a meeting as a member (1 Attention; twice a year earns extra seniority)
  found <school|club|house>      found your own institution: you control it, but it starts small and may fail
  invest <inst> <denarii>        build up an institution you control (2 Attention)
  charter <inst>                 write its founding principles (slows drift; needs control)
  endow <inst> [denarii|all]     give it money to hold (the minimum endowment makes it endowed)
  audit <inst>                   found an audit charter (guards its gold against corruption)
  oversee <inst>                 spend a season with its leader (1 Attention)
  policy <issue> <stance>        set economic policy through a Governance institution you have a voice in (2 Attention):
                                   coinage sound|debase · prices free|controlled · property secure|discretionary · taxes light|heavy
                                   (or 'history' to return to Rome's own practice)
  mentor <inst>                  commit Attention every turn for several turns
  work [odd|craft|consult]       your one personal action: earn about 125 / 300 / 500 denarii for 1 / 2 / 3 Attention
  choose <fountain|workshop>     the first choice
  promise <yes|no>               answer Demetria
  respond <quarantine|hospice|none>   when the pestilence breaks out
  why <thing>                    medicine, governance, economy, gold, plague, policy, promise, index, attention, or an institution
  log [n]                        the last n events
  end                            end the turn (2 months unless you change it)
  turns <1|2|3>                  set how many months a turn lasts (Attention stays 4 a turn)
  wait                           let turns pass until something needs you
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
                        Console.WriteLine("    " + idea.Id.PadRight(12) + _sim.Money(_sim.InventionGold(idea)).PadLeft(14) + ", " + idea.AttentionPerTurn + " Attention a turn for " + idea.Turns + " turns  " + idea.Name +
                                          (state == "ready" ? "" : "   (" + state + ")"));
                        if (state != "made")
                        {
                            Console.WriteLine("                " + idea.Description);
                            Console.WriteLine("                Pays: " + _sim.InventionPayoffText(idea));
                        }
                    }
                }
                return true;
            case "invent": r = _sim.Invent(arg); break;
            case "attend": r = _sim.Attend(arg); break;
            case "found": r = _sim.Found(arg); break;
            case "buy":
                if (parts.Length < 2) { Console.WriteLine("Usage: buy <institution> [percent]"); return false; }
                int pct = 1;
                if (parts.Length > 2 && !int.TryParse(parts[2].TrimEnd('%'), out pct)) { Console.WriteLine("Usage: buy <institution> [percent]"); return false; }
                r = _sim.Buy(arg, pct);
                break;
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
            case "oversee": r = _sim.Oversee(arg); break;
            case "mentor": r = _sim.Mentor(arg); break;
            case "work": r = _sim.Work(parts.Length > 1 ? arg.ToLowerInvariant() : "odd"); break;
            case "promise": r = _sim.AnswerPromise(arg.StartsWith("y", StringComparison.OrdinalIgnoreCase)); break;
            case "respond": r = _sim.RespondToPlague(arg.ToLowerInvariant()); break;
            case "turns":
                if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var months))
                {
                    Console.WriteLine("Usage: turns <1|2|3>   (now " + _sim.MonthsPerTurn + " months a turn)");
                    return false;
                }
                r = _sim.SetTurnLength(months);
                break;
            case "end": case "e": EndTurn(wait: false); return true;
            case "wait": case "w": EndTurn(wait: true); return true;
            case "jump": Jump(); return true;
            default: Console.WriteLine("Unknown command. Type 'help'."); return false;
        }
        Console.WriteLine(r.Message + (r.Ok && _sim.World.Attention < attentionBefore
            ? "  [Attention left this turn: " + _sim.World.Attention + "/" + _sim.AttentionPerTurn + "]" : ""));
        // During jump preparation, show the updated briefing after each change.
        if (_jumpArmed && r.Ok && PrepCommands.Contains(cmd)) Briefing();
        return r.Ok;
    }

    private void EndTurn(bool wait)
    {
        int from = _sim.Log.Events.Count;
        int turns = wait ? _sim.AdvanceUntilDecision() : _sim.EndTurnAndSkipIdle();
        if (turns > 1) Console.WriteLine(wait ? "  (" + turns + " turns pass; nothing needed you until now)"
                                              : "  (" + turns + " turns pass; your Attention was fully committed)");
        var shown = new[] { "project.complete", "debt.tier", "plague.warning", "plague.outbreak", "plague.toll", "plague.opening", "plague.passed",
                            "seeded.payoff", "promise.offer", "promise.kept", "commitment.complete", "income.bonus", "seeded.choice", "institution.unpaid", "year.start",
                            "machine.step", "machine.assessed", "invention.complete", "institution.stake", "institution.seniority", "rivalry.strike", "institution.collapse", "bust.warning", "bust.toll" };
        foreach (var e in _sim.Log.Events.Skip(from).Where(e => shown.Contains(e.Type)))
            Console.WriteLine("  • " + e.Text);
        var settle = _sim.Log.Events.Skip(from).LastOrDefault(e => e.Type == "gold.settle");
        if (settle != null && settle.Text.Contains("could pay only")) Console.WriteLine("  • " + settle.Text);
        Status();
    }

    private void Status()
    {
        var w = _sim.World;
        Console.WriteLine();
        Console.WriteLine("== Turn " + _sim.Turn + " · " + _sim.Now.Display + " ==  " + _sim.Money(w.Gold) + " (" + (((_sim.YearlyIncome() - _sim.YearlyUpkeepTotal()) * _sim.YearsPerTurn) >= 0 ? "+" : "−") +
                          _sim.Money(Math.Abs((_sim.YearlyIncome() - _sim.YearlyUpkeepTotal()) * _sim.YearsPerTurn)).Replace(" denarii", "") + "/turn) · " +
                          _sim.AureiText(w.Aurei) + " (1 = " + F(Math.Round(_sim.AureusInDenarii, 1)) + " den.)   Attention " + w.Attention + "/" + _sim.AttentionPerTurn + "   Index " + F(_sim.SphereIndex()));
        foreach (var d in w.Domains)
            Console.WriteLine("  " + d.Domain.ToString().PadRight(11) + F(d.Level).PadLeft(5) + "  expect " + F(_sim.Expectation(d.Domain)).PadLeft(4) +
                              "  " + (!_sim.HasHold(d.Domain) ? "(no voice)" : d.Priority.Label()).PadRight(11) + " debt " + F(d.Debt).PadLeft(5) + " " + d.Tier);
        if (w.PriceLevel > 1.0001) Console.WriteLine("  Prices: +" + F((w.PriceLevel - 1) * 100) + "% since AD 155 (" + F(_sim.InflationRate() * 100) + "% a year)");
        if (Simulation.Issues.Any(i => _sim.Stance(i) != 0))
            Console.WriteLine("  Policy: " + string.Join(", ", Simulation.Issues.Select(i => i.ToString().ToLowerInvariant() + " " + Simulation.StanceWord(i, _sim.Stance(i)))));
        if (w.Bust.Stage > 0) Console.WriteLine("  Economy: " + Simulation.BustStageText(w.Bust.Stage));
        var plague = w.Plague;
        if (plague.Stage > 0) Console.WriteLine("  Pestilence: " + Simulation.PlagueStageText(plague.Stage));
        foreach (var n in _sim.HistoryNewsThisTurn()) Console.WriteLine("  News: " + n.Text + "   (news)");
        foreach (var n in _sim.LocalNewsThisTurn()) Console.WriteLine("  On your street: " + n + "   (news)");
        foreach (var i in _sim.Backed())
            Console.WriteLine("  " + Simulation.Cap(i.Def.ShortName) + " (" + i.Leader + "): you hold " + _sim.StakePercent(i) + "%" + StakeLabel(i) + ", strength " + F(i.Strength) +
                              " (" + F(_sim.DomainShare(i) * 100) + "% of " + i.Def.Maintains + ")" + (_sim.Controls(i) ? ", loyalty " + F(i.Loyalty) : "") +
                              (i.Chartered ? ", chartered" : "") + (i.Endowed ? ", endowed" : "") + (i.AuditCharter ? ", audited" : "") +
                              (i.Holdings > 0 ? ", holds " + _sim.Money(i.Holdings) : ""));
        foreach (var p in w.ActiveProjects) Console.WriteLine("  Under way: " + p.Def.Name + " (" + p.TurnsRemaining + " turn(s) left)");
        foreach (var a in w.ActiveInventions) Console.WriteLine("  Inventing: " + a.Def.Name + " (" + a.TurnsRemaining + " turn(s) left)");
        Console.WriteLine("  Machine: " + (_sim.MachineAssessed ? _sim.MachineStepsDone + "/" + _sim.MachineStepsTotal + " repair steps" : "not yet assessed") +
                          ", gold " + F(_sim.MachineGoldRestored) + "/" + F(_sim.MachineGoldNeeded) + " aurei" +
                          string.Concat(w.ActiveMachineSteps.Select(a => "; under way: " + a.Def.Name + " (" + a.TurnsRemaining + " turn(s) left)")) +
                          (_sim.MachineReady ? " — ready to jump" : "") + "   (machine)");
        foreach (var c in w.Commitments) Console.WriteLine("  Mentoring " + c.InstitutionId + " (" + c.TurnsRemaining + " turn(s) left)");
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
            Console.WriteLine("  " + p.Id.PadRight(12) + p.Domain.ToString().PadRight(11) + _sim.Money(_sim.ProjectGold(p)).PadLeft(14) + "  " + p.Turns + "t  +" + F(p.LevelGain) + "  " + p.Name +
                              (blocked != null ? "\n                (" + blocked + ")" : p.Authority != null ? "   (public: you have the backing)" : ""));
        }
        Console.WriteLine("  Institutions: see 'institutions' (buy into one, or found your own).");
    }

    private string StakeLabel(Institution i) =>
        _sim.Controls(i) ? " (control)" : _sim.HasVoice(i) ? " (voice)" : _sim.HasInfluence(i) ? " (influence)" : " (member)";

    private void Institutions()
    {
        foreach (var d in DomainInfo.All)
        {
            Console.WriteLine(d + ": your influence " + F(_sim.Influence(d) * 100) + "%, sway " + F(_sim.Sway(d) * 100) + "%");
            foreach (var i in _sim.World.Institutions.Where(x => x.Def.Maintains == d))
            {
                string line = "  " + i.Key.PadRight(10) + Simulation.Cap(i.Def.Name) + " — " + i.Leader + " (" + i.Def.LeaderIntegrity + ")";
                if (!i.Exists)
                    line += i.Collapsed ? ": failed" : ": yours to found for " + _sim.Money(_sim.FoundCost(d)) + ", " + _sim.T.GetInt("founding.attention") +
                                                       " Attention (starts at strength " + F(_sim.T.Get("founding.startStrength")) + ")";
                else
                {
                    line += ": strength " + F(i.Strength) + " (" + F(_sim.DomainShare(i) * 100) + "%)";
                    if (i.Stake > 0) line += ", you hold " + _sim.StakePercent(i) + "%" + StakeLabel(i);
                    else if (!i.Def.IsOwn)
                        line += "; to join: " + _sim.JoinRequirementText(i) + (_sim.JoinBlocker(i) == null ? " (you qualify)" : " (not yet)");
                    int next = _sim.NextThresholdPercent(i);
                    if (!i.Def.IsOwn && next > 0)
                        line += "; next 1% " + _sim.Money(_sim.BuyCost(i, 1)) + (i.Stake <= 0 && _sim.EntryFee(i) > 0 ? " incl. " + _sim.Money(_sim.EntryFee(i)) + " entry fee" : "") +
                                ", to " + next + "% " + _sim.Money(_sim.BuyCost(i, next - _sim.StakePercent(i))) + "; dues " + _sim.Money(_sim.T.Get("joining.duesBasePerYear." + i.Key)) + "/yr + " +
                                _sim.Money(_sim.T.Get("joining.duesPerStakePercentPerYear")) + " per %";
                }
                Console.WriteLine(line);
            }
        }
    }

    private void Log(int n)
    {
        foreach (var e in _sim.Log.Events.Where(e => e.Type != "turn.start" && e.Type != "gold.settle").Reverse().Take(n).Reverse())
            Console.WriteLine("  " + e.Time.Stamp + "  " + e.Text);
    }

    private void Jump()
    {
        if (!_jumpArmed)
        {
            _jumpArmed = true;
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
        Console.WriteLine("Prepare: paydown <domain> <points> · endow <inst> <denarii|all> · audit <inst> · exchange <n> denarii · deposit <aurei> · bury <aurei>. Type 'jump' again to go, or anything else to stay.");
    }

    private void AfterArrival(string cmd, string arg)
    {
        if (cmd == "learn" || cmd == "more") Console.WriteLine(_sim.Arrival!.LearnMore());
        else if (cmd == "why") Console.WriteLine(Why.Explain(_sim, arg));
        else if (cmd == "jump" && _sim.CanJumpAgain) Jump();
        else if (cmd == "visit" || cmd == "walk") Console.WriteLine(Wrap(cmd == "walk" && arg == "" ? _sim.Data.Content.Template("walk.intro") : _sim.Visit(arg)));
        else Console.WriteLine(_sim.CanJumpAgain ? "'visit <place>', 'learn more', 'jump' to go on, or 'quit'." : "The test is over. 'visit <place>', 'learn more' or 'quit'.");
    }

    private static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.#", CultureInfo.InvariantCulture);

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
