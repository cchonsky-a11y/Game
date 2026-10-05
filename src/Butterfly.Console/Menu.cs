using System;
using System.Collections.Generic;
using System.Linq;
using Butterfly.Core;

/// <summary>
/// The action menu (decided 2026-09-28, Corey: so playtesters don't have to type commands): after each command the
/// console lists, numbered, what you can do right now; type one number to do it. P1 (Corey): one choice at a time, so the
/// player sees the result before choosing again; several numbers on one line are refused. Every number stands for an
/// ordinary command, so a game played by numbers replays exactly like one typed out.
/// </summary>
internal sealed partial class ConsoleGame
{
    private readonly List<string> _menu = new List<string>();
    private bool _menuOn;

    /// <summary>
    /// "3": the command that number stands for in the menu last shown. <paramref name="wasNumbers"/> is false if the line
    /// isn't numbers at all (an ordinary command). Several numbers ("3 7 1", "3,7") are refused: one choice at a time.
    /// </summary>
    private string? MenuCommand(string line, out bool wasNumbers)
    {
        var tokens = line.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        wasNumbers = tokens.Length > 0 && tokens.All(t => t.All(char.IsDigit));
        if (!wasNumbers) return null;
        if (tokens.Length > 1) { Console.WriteLine("  One choice at a time: type a single number, see what happens, then choose again."); return null; }
        int n = int.Parse(tokens[0]);
        if (n < 1 || n > _menu.Count) { Console.WriteLine("  There's no " + n + " in the menu (1-" + _menu.Count + "). Type 'menu' to see it again."); return null; }
        return _menu[n - 1];
    }

    private void ShowMenu()
    {
        _menu.Clear();
        var groups = _sim.Arrived ? ArrivalGroups() : EraGroups();
        Console.WriteLine();
        Console.WriteLine("  What you can do (type one number; 'menu off' hides this):");
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

    /// <summary>
    /// The groups come from the shared action catalog (P2, 2026-10-05: Butterfly.Presentation.ActionCatalog), so the console
    /// and the graphical client offer exactly the same actions.
    /// </summary>
    private List<(string Title, List<(string Label, string Command)> Items)> ArrivalGroups() => Groups();

    private List<(string Title, List<(string Label, string Command)> Items)> EraGroups() => Groups();

    private List<(string Title, List<(string Label, string Command)> Items)> Groups() =>
        Butterfly.Presentation.ActionCatalog.For(_sim, _jumpArmed)
            .Select(g => (g.Title, g.Items.Select(a => (a.Label, a.Command)).ToList())).ToList();
}
