using System;
using System.Globalization;
using System.Linq;
using Butterfly.Core;

// The Butterfly Effect — P0 Butterfly Test, text console for human players.
// Usage: dotnet run --project src/Butterfly.Console -- --seed 42
ulong seed = 42;
for (int i = 0; i < args.Length - 1; i++)
    if (args[i] == "--seed") seed = ulong.Parse(args[i + 1], CultureInfo.InvariantCulture);

var game = new ConsoleGame(new Simulation(GameData.LoadDefault(), seed));
game.Run();

internal sealed class ConsoleGame
{
    private readonly Simulation _sim;
    private bool _jumpArmed;
    private static readonly string[] PrepCommands = { "paydown", "endow", "audit", "status", "s", "why", "help", "?" };

    public ConsoleGame(Simulation sim)
    {
        _sim = sim;
    }

    private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    public void Run()
    {
        Intro();
        Status();
        while (true)
        {
            Console.Write(_sim.Arrived ? "\n(after arrival) > " : "\n> ");
            string? line = Console.ReadLine();
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
                continue;
            }
            Handle(cmd, arg, parts);
        }
        Console.WriteLine("\nRun fingerprint (seed " + _sim.Seed + "): " + _sim.Log.Hash().Substring(0, 16));
    }

    private void Intro()
    {
        Console.WriteLine("THE BUTTERFLY EFFECT — P0 Butterfly Test");
        Console.WriteLine("========================================");
        Console.WriteLine("Rome, AD 155. Your time machine failed and left you here. You have a little gold, what you know,");
        Console.WriteLine("and a small circle of people willing to listen. Something is coming from the East in a few years.");
        Console.WriteLine("When you're ready, the machine can carry you 250 years forward. What you leave behind will go on without you.");
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
  priority <domain> <protect|maintain|accept>
  paydown <domain> <points>      pay down debt (costs 1.5× what prevention would have)
  found <circle|faction>         found an institution
  charter <inst>                 write its founding principles (slows drift)
  endow <inst> [gold]            give it gold to hold (the first 60 makes it endowed)
  audit <inst>                   found an audit charter (guards its gold against corruption)
  oversee <inst>                 spend a season with its leader (1 Attention)
  mentor <inst>                  commit Attention every turn for several turns
  work                           your one personal action: earn gold
  choose <fountain|workshop>     the first choice
  promise <yes|no>               answer Demetria
  respond <quarantine|hospice|none>   when the pestilence breaks out
  why <thing>                    medicine, governance, economy, gold, plague, circle, faction, promise, index, attention
  log [n]                        the last n events
  end                            end the turn (6 months); quiet turns pass on their own
  jump                           prepare to leave for AD +250 (then pay down, endow, audit, or 'jump' again)
  quit");
    }

    private void Handle(string cmd, string arg, string[] parts)
    {
        CommandResult? r = null;
        switch (cmd)
        {
            case "help": case "?": Help(); return;
            case "status": case "s": Status(); return;
            case "projects": case "p": Projects(); return;
            case "why": Console.WriteLine(Why.Explain(_sim, arg)); return;
            case "log": Log(parts.Length > 1 && int.TryParse(arg, out var n) ? n : 12); return;
            case "start": r = _sim.StartProject(arg.ToLowerInvariant()); break;
            case "choose": r = _sim.ChooseSeeded(arg.ToLowerInvariant()); break;
            case "priority":
                if (parts.Length < 3 || !DomainInfo.TryParseDomain(parts[1], out var d) || !DomainInfo.TryParsePriority(parts[2], out var p))
                {
                    Console.WriteLine("Usage: priority <medicine|governance|economy> <protect|maintain|accept>");
                    return;
                }
                r = _sim.SetPriority(d, p);
                break;
            case "paydown":
                if (parts.Length < 3 || !DomainInfo.TryParseDomain(parts[1], out var pd) || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pts))
                {
                    Console.WriteLine("Usage: paydown <domain> <points>   (costs " + F(_sim.PaydownCost(1)) + " gold per point)");
                    return;
                }
                r = _sim.PayDown(pd, pts);
                break;
            case "found": r = _sim.Found(arg); break;
            case "charter": r = _sim.Charter(arg); break;
            case "endow":
                r = parts.Length > 2 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
                    ? _sim.Endow(arg, amount) : _sim.Endow(arg);
                break;
            case "audit": r = _sim.Audit(arg); break;
            case "oversee": r = _sim.Oversee(arg); break;
            case "mentor": r = _sim.Mentor(arg); break;
            case "work": r = _sim.Work(); break;
            case "promise": r = _sim.AnswerPromise(arg.StartsWith("y", StringComparison.OrdinalIgnoreCase)); break;
            case "respond": r = _sim.RespondToPlague(arg.ToLowerInvariant()); break;
            case "end": case "e": EndTurn(); return;
            case "jump": Jump(); return;
            default: Console.WriteLine("Unknown command. Type 'help'."); return;
        }
        Console.WriteLine(r.Message);
    }

    private void EndTurn()
    {
        int from = _sim.Log.Events.Count;
        int turns = _sim.AdvanceUntilDecision();
        if (turns > 1) Console.WriteLine("  (" + turns + " turns pass; nothing needed you until now)");
        var shown = new[] { "project.complete", "debt.tier", "plague.warning", "plague.outbreak", "plague.toll", "plague.opening", "plague.passed",
                            "seeded.payoff", "promise.offer", "promise.kept", "commitment.complete", "income.bonus", "seeded.choice", "institution.unpaid", "year.start" };
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
                              "  " + d.Priority.Label().PadRight(11) + " debt " + F(d.Debt).PadLeft(5) + " " + d.Tier);
        var plague = w.Plague;
        if (plague.Stage > 0) Console.WriteLine("  Pestilence: " + Simulation.PlagueStageText(plague.Stage));
        foreach (var i in w.Institutions.Where(i => i.Founded))
            Console.WriteLine("  " + Simulation.Cap(i.Def.ShortName) + " (" + i.Leader + "): strength " + F(i.Strength) + ", loyalty " + F(i.Loyalty) +
                              (i.Chartered ? ", chartered" : "") + (i.Endowed ? ", endowed" : "") + (i.AuditCharter ? ", audited" : "") +
                              (i.Holdings > 0 ? ", holds " + F(i.Holdings) + " gold" : ""));
        foreach (var p in w.ActiveProjects) Console.WriteLine("  Under way: " + p.Def.Name + " (" + p.TurnsRemaining + " turn(s) left)");
        foreach (var c in w.Commitments) Console.WriteLine("  Mentoring " + c.InstitutionId + " (" + c.TurnsRemaining + " turn(s) left)");
        if (_sim.SeededChoiceOpen) Console.WriteLine("  ► Waiting: choose fountain or choose workshop (before the end of turn 2).");
        if (w.Promise.Status == PromiseStatus.Offered) Console.WriteLine("  ► Waiting: Demetria asks you to stay until the sickness has passed. promise yes / promise no");
        if (_sim.OutbreakAwaitingResponse) Console.WriteLine("  ► Waiting: respond " + string.Join(" / respond ", _sim.AvailablePlagueResponses()));
        if (_sim.EraOver) Console.WriteLine("  ► The era's " + _sim.EraTurns + " turns are over. Jump when you're ready (you can also stay).");
    }

    private void Projects()
    {
        foreach (var p in _sim.AvailableProjects())
            Console.WriteLine("  " + p.Id.PadRight(12) + p.Domain.ToString().PadRight(11) + (p.Gold + "g").PadLeft(4) + "  " + p.Turns + "t  +" + F(p.LevelGain) + "  " + p.Name);
        if (!_sim.World.Institution("circle").Founded || !_sim.World.Institution("faction").Founded)
            Console.WriteLine("  Institutions: found circle / found faction (" + F(_sim.T.Get("institutions.foundGold")) + "g, 1 Attention)");
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
            Console.WriteLine("You will leave AD " + _sim.Now.Year + " for AD " + (_sim.Now.Year + 250) + ". You can't come back. What you leave behind:");
            foreach (var line in _sim.DepartureBriefing()) Console.WriteLine("  • " + line);
            Console.WriteLine("Prepare: paydown <domain> <points> · endow <inst> <gold> · audit <inst>. Type 'jump' again to go, or anything else to stay.");
            return;
        }
        var arrival = _sim.Jump();
        Console.WriteLine("\nThe machine shudders. Decades pass in the dark...\n");
        foreach (var beat in arrival.Beats)
        {
            Console.WriteLine("— " + beat.Name + " —");
            Console.WriteLine(Wrap(beat.Text));
            Console.WriteLine();
            if (!Console.IsInputRedirected)
            {
                Console.Write("(press Enter)");
                Console.ReadLine();
            }
        }
        Console.WriteLine("Type 'learn more' for the Index and what became of your institutions, or 'quit'.");
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
