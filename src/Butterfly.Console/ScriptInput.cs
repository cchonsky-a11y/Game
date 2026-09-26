using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Butterfly.Core;

/// <summary>
/// Scripted input for automated playtests (--inputs). One command per line; '#' starts a comment.
/// Directive: "@until <year>|era-end: cmd; cmd; ..." repeats the commands (which should include 'end')
/// until the year is reached or the era's turns are over. A test harness only; it changes no rules.
/// </summary>
internal sealed class ScriptInput
{
    public const int MaxCycles = 200;

    private readonly Simulation _sim;
    private readonly Queue<string> _lines;
    private readonly Queue<string> _pending = new Queue<string>();
    private string[]? _loop;
    private int _untilYear;
    private bool _untilEraEnd;
    private int _cycles;

    /// <summary>Directives that hit the cycle cap without reaching their condition (possible dead ends).</summary>
    public List<string> StalledLoops { get; } = new List<string>();

    public ScriptInput(string path, Simulation sim)
    {
        _sim = sim;
        _lines = new Queue<string>(File.ReadAllLines(path).Select(l => l.Split('#')[0].Trim()).Where(l => l.Length > 0));
    }

    public string? Next()
    {
        while (true)
        {
            if (_pending.Count > 0) return _pending.Dequeue();
            if (_loop != null)
            {
                bool done = _sim.Arrived || (_untilEraEnd ? _sim.EraOver : _sim.Now.Year >= _untilYear);
                if (!done && _cycles >= MaxCycles)
                {
                    StalledLoops.Add((_untilEraEnd ? "era-end" : _untilYear.ToString(CultureInfo.InvariantCulture)) + " after " + _cycles + " cycles");
                    done = true;
                }
                if (!done)
                {
                    _cycles++;
                    foreach (var c in _loop) _pending.Enqueue(c);
                    continue;
                }
                _loop = null;
            }
            if (_lines.Count == 0) return null;
            string line = _lines.Dequeue();
            if (!line.StartsWith("@until ", StringComparison.Ordinal)) return line;
            int colon = line.IndexOf(':');
            string target = line.Substring(7, colon - 7).Trim();
            _untilEraEnd = target == "era-end";
            _untilYear = _untilEraEnd ? 0 : int.Parse(target, CultureInfo.InvariantCulture);
            _loop = line.Substring(colon + 1).Split(';').Select(c => c.Trim()).Where(c => c.Length > 0).ToArray();
            _cycles = 0;
        }
    }
}
