using System;
using System.Globalization;
using System.Linq;
using Butterfly.Core;

// The Butterfly Effect — P0 Butterfly Test, text console for human players.
// Usage: dotnet run --project src/Butterfly.Console -- --seed 42
// Scripted (automated playtests): add --inputs <file> [--checks <file>]; see playtests/ai/README.md.
ulong seed = 42;
string? inputs = null, checks = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--seed") seed = ulong.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--inputs") inputs = args[i + 1];
    if (args[i] == "--checks") checks = args[i + 1];
}

var sim = new Simulation(GameData.LoadDefault(), seed);
var game = new ConsoleGame(sim, inputs == null ? null : new ScriptInput(inputs, sim), checks == null ? null : new Harness(sim, checks));
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
    private static readonly string[] PrepCommands = { "paydown", "endow", "audit", "status", "s", "why", "help", "?" };

    public ConsoleGame(Simulation sim, ScriptInput? script = null, Harness? harness = null)
    {
        _sim = sim;
        _script = script;
        _harness = harness;
        _autoEnd = script == null;
    }

    /// <summary>Next command: from the script (echoed so transcripts read like a session) or from the keyboard.</summary>
    private string? ReadCommand()
    {
        if (_script == null) return Console.ReadLine();
        string? line = _script.Next();
        if (line != null) Console.WriteLine(line);
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
        Console.WriteLine("Rome, AD 155. Your time machine failed and left you here. You have a pouch of gold you scavenged from the");
        Console.WriteLine("machine (every bit of it must go back before it can fly), what you know, and no one who owes you anything.");
        Console.WriteLine("You don't yet know what broke. Something is coming from the East within ten years.");
        Console.WriteLine("Once you repair it, the machine can carry you forward, " + _sim.T.GetInt("jump.range.baseMin") + " to " + _sim.T.GetInt("jump.range.maxYears") +
                          " years depending on how well you repair it. What you leave behind will go on without you.");
        Console.WriteLine();
        Console.WriteLine("This prototype covers one era, one jump, and your arrival. When you leave Rome, you'll see what became of it, and the test ends there.");
        Console.WriteLine();
        Console.WriteLine("First, a choice. You can afford only one:");
        Console.WriteLine("  choose fountain  — " + _sim.Data.Content.Project("fountain")!.Description);
        Console.WriteLine("  choose workshop  — " + _sim.Data.Content.Project("workshop")!.Description);
        Console.WriteLine("Type 'help' for commands.");
    }

    private void Help()
    {
        Console.WriteLine(@"Commands
  status                         where things stand
  projects                       projects you can start
  start <project>                start a project (costs gold and Attention)
  priority <domain> <protect|maintain|accept>   (needs a 25% voice in an institution of that domain)
  paydown <domain> <points>      pay down debt (costs 1.5× what prevention would have)
  institutions                   who holds each domain, your stakes, and what the next step costs
  buy <inst> [percent]           buy into an established institution (2 Attention; entry fee on joining, each 1% costs more):
                                   10% counts toward influence · 25% a voice (priorities, policy) · 50% control
  attend <inst>                  attend a meeting as a member (1 Attention; twice a year earns extra seniority)
  found <school|club|house>      found your own institution: you control it, but it starts small and may fail
  invest <inst> <gold>           build up an institution you control (2 Attention)
  charter <inst>                 write its founding principles (slows drift; needs control)
  endow <inst> [gold|all]        give it gold to hold (the first 60 makes it endowed)
  audit <inst>                   found an audit charter (guards its gold against corruption)
  oversee <inst>                 spend a season with its leader (1 Attention)
  policy <issue> <stance>        set economic policy through a Governance institution you have a voice in (2 Attention):
                                   coinage sound|debase · prices free|controlled · property secure|discretionary · taxes light|heavy
                                   (or 'history' to return to Rome's own practice)
  mentor <inst>                  commit Attention every turn for several turns
  work [odd|craft|consult]       your one personal action: earn 5 / 12 / 20 gold for 1 / 2 / 3 Attention
  choose <fountain|workshop>     the first choice
  promise <yes|no>               answer Demetria
  respond <quarantine|hospice|none>   when the pestilence breaks out
  why <thing>                    medicine, governance, economy, gold, plague, policy, promise, index, attention, or an institution
  log [n]                        the last n events
  end                            end the turn (3 months)
  wait                           let turns pass until something needs you
  inventions                     things you can make from what you know, and what each needs from Rome
  invent <invention>             start work on an invention (income, standing and influence)
  machine                        the time machine: what's repaired and what's next
  assess                         assess the machine to learn what's wrong (needed before any repair)
  restore <gold>                 put scavenged gold back into the machine (all of it is needed to jump)
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
                    Console.WriteLine("Usage: paydown <domain> <points>   (costs " + F(_sim.PaydownCost(1)) + " gold per point)");
                    return false;
                }
                r = _sim.PayDown(pd, pts);
                break;
            case "institutions": case "i": Institutions(); return true;
            case "machine": case "m":
                Console.WriteLine("Machine: " + _sim.MachineStepsDone + "/" + _sim.MachineStepsTotal + " repair steps and " + F(_sim.MachineGoldRestored) + "/" +
                                  F(_sim.MachineGoldNeeded) + " gold restored (all are needed to jump).");
                foreach (var l in _sim.MachineStatus()) Console.WriteLine("  " + l);
                return true;
            case "assess": r = _sim.Assess(); break;
            case "restore":
                if (!double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var restore))
                {
                    Console.WriteLine("Usage: restore <gold>   (" + F(_sim.MachineGoldNeeded - _sim.MachineGoldRestored) + " gold still missing from the machine)");
                    return false;
                }
                r = _sim.RestoreGold(restore);
                break;
            case "repair": r = _sim.Repair(arg); break;
            case "upgrade": r = _sim.Upgrade(arg); break;
            case "inventions":
                foreach (var idea in _sim.AvailableInventions())
                    Console.WriteLine("  " + idea.Id.PadRight(12) + (_sim.InventionGold(idea) + "g").PadLeft(4) + "  " + idea.Turns + "t  " + idea.Name +
                                      (_sim.InventionRequirementMet(idea) ? "" : "   (needs " + _sim.InventionRequirementText(idea) + ")") + "\n" +
                                      "              " + idea.Description);
                foreach (var a in _sim.World.ActiveInventions) Console.WriteLine("  Under way: " + a.Def.Name + " (" + a.TurnsRemaining + " turn(s) left)");
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
                r = _sim.Invest(arg, inv);
                break;
            case "charter": r = _sim.Charter(arg); break;
            case "endow":
                if (parts.Length > 2 && parts[2].Equals("all", StringComparison.OrdinalIgnoreCase))
                    r = _sim.Endow(arg, Math.Floor(_sim.World.Gold));
                else
                    r = parts.Length > 2 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
                        ? _sim.Endow(arg, amount) : _sim.Endow(arg);
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
            case "end": case "e": EndTurn(wait: false); return true;
            case "wait": case "w": EndTurn(wait: true); return true;
            case "jump": Jump(); return true;
            default: Console.WriteLine("Unknown command. Type 'help'."); return false;
        }
        Console.WriteLine(r.Message + (r.Ok && _sim.World.Attention < attentionBefore
            ? "  [Attention left this turn: " + _sim.World.Attention + "/" + _sim.AttentionPerTurn + "]" : ""));
        // During jump preparation, show the updated briefing after each change.
        if (_jumpArmed && r.Ok && (cmd == "paydown" || cmd == "endow" || cmd == "audit")) Briefing();
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
        Console.WriteLine("== Turn " + _sim.Turn + " · " + _sim.Now.Display + " ==  Gold " + F(w.Gold) + " (" + Signed((_sim.YearlyIncome() - _sim.YearlyUpkeepTotal()) * _sim.YearsPerTurn) +
                          "/turn)   Attention " + w.Attention + "/" + _sim.AttentionPerTurn + "   Index " + F(_sim.SphereIndex()));
        foreach (var d in w.Domains)
            Console.WriteLine("  " + d.Domain.ToString().PadRight(11) + F(d.Level).PadLeft(5) + "  expect " + F(_sim.Expectation(d.Domain)).PadLeft(4) +
                              "  " + (!_sim.HasHold(d.Domain) ? "(no voice)" : d.Priority.Label()).PadRight(11) + " debt " + F(d.Debt).PadLeft(5) + " " + d.Tier);
        if (w.PriceLevel > 1.0001) Console.WriteLine("  Prices: +" + F((w.PriceLevel - 1) * 100) + "% since AD 155 (" + F(_sim.InflationRate() * 100) + "% a year)");
        if (Simulation.Issues.Any(i => _sim.Stance(i) != 0))
            Console.WriteLine("  Policy: " + string.Join(", ", Simulation.Issues.Select(i => i.ToString().ToLowerInvariant() + " " + Simulation.StanceWord(i, _sim.Stance(i)))));
        if (w.Bust.Stage > 0) Console.WriteLine("  Economy: " + Simulation.BustStageText(w.Bust.Stage));
        var plague = w.Plague;
        if (plague.Stage > 0) Console.WriteLine("  Pestilence: " + Simulation.PlagueStageText(plague.Stage));
        foreach (var i in _sim.Backed())
            Console.WriteLine("  " + Simulation.Cap(i.Def.ShortName) + " (" + i.Leader + "): you hold " + _sim.StakePercent(i) + "%" + StakeLabel(i) + ", strength " + F(i.Strength) +
                              " (" + F(_sim.DomainShare(i) * 100) + "% of " + i.Def.Maintains + ")" + (_sim.Controls(i) ? ", loyalty " + F(i.Loyalty) : "") +
                              (i.Chartered ? ", chartered" : "") + (i.Endowed ? ", endowed" : "") + (i.AuditCharter ? ", audited" : "") +
                              (i.Holdings > 0 ? ", holds " + F(i.Holdings) + " gold" : ""));
        foreach (var p in w.ActiveProjects) Console.WriteLine("  Under way: " + p.Def.Name + " (" + p.TurnsRemaining + " turn(s) left)");
        foreach (var a in w.ActiveInventions) Console.WriteLine("  Inventing: " + a.Def.Name + " (" + a.TurnsRemaining + " turn(s) left)");
        Console.WriteLine("  Machine: " + (_sim.MachineAssessed ? _sim.MachineStepsDone + "/" + _sim.MachineStepsTotal + " repair steps" : "not yet assessed") +
                          ", gold " + F(_sim.MachineGoldRestored) + "/" + F(_sim.MachineGoldNeeded) +
                          string.Concat(w.ActiveMachineSteps.Select(a => "; under way: " + a.Def.Name + " (" + a.TurnsRemaining + " turn(s) left)")) +
                          (_sim.MachineReady ? " — ready to jump" : "") + "   (machine)");
        foreach (var c in w.Commitments) Console.WriteLine("  Mentoring " + c.InstitutionId + " (" + c.TurnsRemaining + " turn(s) left)");
        if (_sim.SeededChoiceOpen) Console.WriteLine("  ► Waiting: choose fountain or choose workshop (before the end of turn " + _sim.T.GetInt("seededChoice.deadlineTurn") + ").");
        if (w.Promise.Status == PromiseStatus.Offered) Console.WriteLine("  ► Waiting: Demetria asks you to stay until the sickness has passed. promise yes / promise no");
        if (_sim.OutbreakAwaitingResponse)
            Console.WriteLine("  ► Waiting: respond " + string.Join(" / respond ", _sim.AvailablePlagueResponses()) +
                              (_sim.AvailablePlagueResponses().Contains("hospice") ? "" : "   (a hospice needs a physicians' circle of strength 30+)"));
        if (_sim.EraOver) Console.WriteLine("  ► The era's " + _sim.EraTurns + " turns are over. Jump when you're ready (you can also stay).");
    }

    private void Projects()
    {
        foreach (var p in _sim.AvailableProjects())
            Console.WriteLine("  " + p.Id.PadRight(12) + p.Domain.ToString().PadRight(11) + (_sim.ProjectGold(p) + "g").PadLeft(4) + "  " + p.Turns + "t  +" + F(p.LevelGain) + "  " + p.Name);
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
                    line += i.Collapsed ? ": failed" : ": yours to found for " + F(_sim.FoundCost(d)) + " gold, " + _sim.T.GetInt("founding.attention") +
                                                       " Attention (starts at strength " + F(_sim.T.Get("founding.startStrength")) + ")";
                else
                {
                    line += ": strength " + F(i.Strength) + " (" + F(_sim.DomainShare(i) * 100) + "%)";
                    if (i.Stake > 0) line += ", you hold " + _sim.StakePercent(i) + "%" + StakeLabel(i);
                    else if (!i.Def.IsOwn)
                        line += "; to join: " + _sim.JoinRequirementText(i) + (_sim.JoinBlocker(i) == null ? " (you qualify)" : " (not yet)");
                    int next = _sim.NextThresholdPercent(i);
                    if (!i.Def.IsOwn && next > 0)
                        line += "; next 1% " + F(_sim.BuyCost(i, 1)) + "g" + (i.Stake <= 0 && _sim.EntryFee(i) > 0 ? " incl. " + F(_sim.EntryFee(i)) + "g entry fee" : "") +
                                ", to " + next + "% " + F(_sim.BuyCost(i, next - _sim.StakePercent(i))) + "g; dues " + F(_sim.T.Get("joining.duesBasePerYear." + i.Key)) + "g/yr + " +
                                F(_sim.T.Get("joining.duesPerStakePercentPerYear")) + " per %";
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
        Console.WriteLine("Type 'learn more' for the Index and what became of your institutions, or 'quit'.");
    }

    private void Briefing()
    {
        Console.WriteLine("What you leave behind (" + F(_sim.World.Gold) + " gold in hand):");
        foreach (var line in _sim.DepartureBriefing()) Console.WriteLine("  • " + line);
        Console.WriteLine("Prepare: paydown <domain> <points> · endow <inst> <gold|all> · audit <inst>. Type 'jump' again to go, or anything else to stay.");
    }

    private void AfterArrival(string cmd, string arg)
    {
        if (cmd == "learn" || cmd == "more") Console.WriteLine(_sim.Arrival!.LearnMore());
        else if (cmd == "why") Console.WriteLine(Why.Explain(_sim, arg));
        else Console.WriteLine("The era is over. 'learn more' or 'quit'.");
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
