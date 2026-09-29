using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Butterfly.Core;

/// <summary>
/// Automated playtest checks (--checks). After every command it checks for impossible states; at the end
/// it checks that the run arrived, that the arrival shows all three Echoes in four beats with no broken
/// text, and that logged numbers match the SYSTEMS.md rules. Writes one finding per line. Reads only.
/// </summary>
internal sealed class Harness
{
    private readonly Simulation _sim;
    private readonly string _path;
    private readonly List<string> _findings = new List<string>();
    private int _commands;
    private readonly List<string> _accepted = new List<string>();

    public Harness(Simulation sim, string path)
    {
        _sim = sim;
        _path = path;
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private void Add(string severity, string text)
    {
        string entry = severity + " | " + text;
        if (!_findings.Contains(entry)) _findings.Add(entry);
    }

    public void AfterCommand(string command, bool ok)
    {
        _commands++;
        if (ok) _accepted.Add(command);
        var w = _sim.World;
        string at = " (after '" + command + "', " + _sim.Now.Stamp + ")";
        if (w.Gold < -1e-9) Add("IMPOSSIBLE", "negative gold " + F(w.Gold) + at);
        if (w.Attention < 0 || w.Attention > _sim.AttentionPerTurn) Add("IMPOSSIBLE", "Attention " + w.Attention + " outside 0–" + _sim.AttentionPerTurn + at);
        foreach (var d in w.Domains)
        {
            if (d.Debt < -1e-9 || double.IsNaN(d.Debt) || double.IsInfinity(d.Debt)) Add("IMPOSSIBLE", d.Domain + " debt " + F(d.Debt) + at);
            if (d.Level < 0 || d.Level > 100 || double.IsNaN(d.Level)) Add("IMPOSSIBLE", d.Domain + " level " + F(d.Level) + at);
        }
        foreach (var i in w.Institutions)
        {
            if (i.Holdings < -1e-9) Add("IMPOSSIBLE", i.Key + " holdings " + F(i.Holdings) + at);
            if (i.Strength < 0 || i.Strength > 100 || i.Loyalty < 0 || i.Loyalty > 100) Add("IMPOSSIBLE", i.Key + " strength/loyalty out of range" + at);
        }
    }

    public void Finish(ScriptInput? script)
    {
        if (script != null)
            foreach (var s in script.StalledLoops) Add("DEAD END", "@until " + s + " never reached its condition");
        if (!_sim.Arrived) Add("DEAD END", "the run never jumped/arrived (last time " + _sim.Now.Stamp + ", turn " + _sim.Turn + ")");
        else CheckArrival(_sim.Arrival!);
        CheckLogAgainstSystems();
        CheckText();
        var lines = new List<string> { "seed " + _sim.Seed + ", commands " + _commands + ", arrived " + _sim.Arrived + ", fingerprint " + _sim.Log.Hash().Substring(0, 16) };
        lines.AddRange(_findings.Count == 0 ? new[] { "OK | no findings" } : _findings);
        File.WriteAllLines(_path, lines);
        // The accepted commands alone replay the same run (refused commands change nothing).
        File.WriteAllLines(Path.ChangeExtension(_path, ".accepted.txt"), _accepted);
    }

    private void CheckArrival(Arrival a)
    {
        var beats = a.Beats.Select(b => b.Name).ToList();
        if (!beats.SequenceEqual(new[] { "Recognition", "Wrongness", "Personal echo", "Discovery" }))
            Add("ECHO", "arrival beats are " + string.Join(", ", beats));
        if (a.Echoes.Count != 3) Add("ECHO", "arrival has " + a.Echoes.Count + " Echoes, expected 3");
        if (!a.Echoes.Any(e => e.Id == "seeded")) Add("ECHO", "the seeded choice is not an Echo");
        foreach (var e in a.Echoes)
        {
            var beat = a.Beats.FirstOrDefault(b => b.Name == e.Beat);
            if (beat == null || string.IsNullOrWhiteSpace(beat.Text) || string.IsNullOrEmpty(e.AtArrival))
                Add("ECHO", "Echo '" + e.Name + "' is not surfaced in any beat");
        }
        // SYSTEMS §12: Index = geometric mean of sub-scores.
        double gm = Formulas.GeometricMean(DomainInfo.All.Select(d => a.SubScoresAfter[d]));
        if (Math.Abs(gm - a.IndexAfter) > 1e-6) Add("SYSTEMS", "arrival Index " + F(a.IndexAfter) + " is not the geometric mean " + F(gm));
        var jump = JumpLengthFinding(a.ArrivalYear - a.DepartureYear, a.JumpYears, _sim.T);
        if (jump != null) Add("SYSTEMS", jump);
    }

    /// <summary>
    /// SYSTEMS §11 (decided 2026-09-28): a jump carries you 25–60 years, drawn in 5-year steps. (The check once expected
    /// the old fixed 250-year absence.) Null if the absence is legal.
    /// </summary>
    internal static string? JumpLengthFinding(int absence, int drawn, Tuning t)
    {
        int min = t.GetInt("jump.range.baseMin"), max = t.GetInt("jump.range.maxYears"), step = t.GetInt("jump.range.stepYears");
        if (absence != drawn) return "absence lasted " + absence + " years, but the machine drew " + drawn;
        if (absence < min || absence > max) return "absence lasted " + absence + " years, outside the machine's " + min + "–" + max;
        if ((absence - min) % step != 0) return "absence lasted " + absence + " years, not in " + step + "-year steps";
        return null;
    }

    private void CheckLogAgainstSystems()
    {
        var t = _sim.T;
        double perPoint = t.Get("debt.preventionGoldPerPoint") * t.Get("debt.paydownMultiplier");
        foreach (var e in _sim.Log.Events)
        {
            switch (e.Type)
            {
                case "debt.paydown":
                case "debt.paidByInstitution":
                {
                    // SYSTEMS §6: paying down costs 1.5× prevention.
                    var debt = e.Effects.First(fx => fx.Key.EndsWith(".debt", StringComparison.Ordinal));
                    var gold = e.Effects.First(fx => fx.Key == "gold" || fx.Key.EndsWith(".holdings", StringComparison.Ordinal));
                    double ratio = -gold.Delta / -debt.Delta;
                    if (Math.Abs(ratio - perPoint) > 1e-6) Add("SYSTEMS", e.Type + " at " + e.Time.Stamp + " cost " + F(ratio) + " gold per point, not " + F(perPoint));
                    if (e.Type == "debt.paidByInstitution" && e.Time.Year - _sim.DepartureYear >= t.GetInt("institutions.holdings.windowYears"))
                        Add("SYSTEMS", "institution paid debt outside the 30-year window at " + e.Time.Stamp);
                    break;
                }
                case "institution.decade":
                {
                    // SYSTEMS §7: decay per decade by quality.
                    var inst = _sim.World.Institution(e.Target);
                    var s = e.Effects.First(fx => fx.Key.EndsWith(".strength", StringComparison.Ordinal));
                    if (s.Before <= 0) break;
                    double rate = 1 - s.After / s.Before;
                    if (Math.Abs(rate - _sim.DecayRate(inst.Quality)) > 1e-9) Add("SYSTEMS", e.Target + " decayed " + F(rate * 100) + "% in a decade, expected " + F(_sim.DecayRate(inst.Quality) * 100) + "%");
                    break;
                }
                case "holdings.grow":
                {
                    var h = e.Effects[0];
                    if (h.After > h.Before * Math.Pow(1 + t.Get("institutions.holdings.growthMaxRatePerYear"), 10) + 1e-6 || h.After < h.Before)
                        Add("SYSTEMS", "holdings grew " + F(h.Before) + " → " + F(h.After) + " in a decade (allowed 0–1.5%/yr)");
                    if (e.Time.Year - _sim.DepartureYear > t.GetInt("institutions.holdings.windowYears"))
                        Add("SYSTEMS", "holdings grew after the 30-year window at " + e.Time.Stamp);
                    break;
                }
                case "institution.corruption":
                    if (e.Time.Year - _sim.DepartureYear >= t.GetInt("institutions.holdings.windowYears"))
                        Add("SYSTEMS", "corruption checked outside the 30-year window at " + e.Time.Stamp);
                    break;
                case "jump.domain":
                {
                    // SYSTEMS §6: after 30 years debt no longer compounds; growth ≤ accrual only.
                    var debt = e.Effects.First(fx => fx.Key.EndsWith(".debt", StringComparison.Ordinal));
                    int yearsIn = e.Time.Year - _sim.DepartureYear;
                    if (yearsIn > 30 && debt.Before > 0 && debt.After > debt.Before * Math.Pow(1.05, 10) - 1e-6 && debt.After - debt.Before > 10 * 100)
                        Add("SYSTEMS", "debt looks compounded after the 30-year cap at " + e.Time.Stamp);
                    break;
                }
            }
        }
        // SYSTEMS §2: turns may be shortened, never longer than the stage cap.
        var turns = _sim.Log.Events.Where(e => e.Type == "turn.start").Select(e => e.Time.TotalMonths).ToList();
        for (int i = 1; i < turns.Count; i++)
            if (turns[i] - turns[i - 1] > _sim.T.GetInt("time.maxMonthsPerTurn") || turns[i] - turns[i - 1] < 1) { Add("SYSTEMS", "turn length " + (turns[i] - turns[i - 1]) + " months"); break; }
        var plague = WarningsFinding(_sim.Log.Events.Where(e => e.Type == "plague.warning").Select(e => e.Time).ToList(),
                                     _sim.Log.Events.FirstOrDefault(e => e.Type == "plague.outbreak")?.Time);
        if (plague != null) Add("SYSTEMS", plague);
    }

    /// <summary>
    /// PROTOTYPE_SCOPE: three visible warnings, in order, before the outbreak. They fall on history's dates (AD 165, 166,
    /// 166; the outbreak late in 166), so the last warning shares the outbreak's year: the order is by month, not by year.
    /// Null if they are in order (or there was no outbreak).
    /// </summary>
    internal static string? WarningsFinding(IReadOnlyList<SimTime> warnings, SimTime? outbreak)
    {
        if (outbreak == null) return null;
        if (warnings.Count != 3) return "outbreak after " + warnings.Count + " warnings, not 3";
        for (int i = 0; i < warnings.Count; i++)
        {
            var next = i + 1 < warnings.Count ? warnings[i + 1] : outbreak.Value;
            if (warnings[i].TotalMonths >= next.TotalMonths)
                return "warning " + (i + 1) + " (" + warnings[i].Stamp + ") is not before " + (i + 1 < warnings.Count ? "warning " + (i + 2) : "the outbreak") + " (" + next.Stamp + ")";
        }
        return null;
    }

    /// <summary>A word repeated back to back ("the the"), ignoring case.</summary>
    private static bool RepeatedWord(string text)
    {
        var words = text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < words.Length; i++)
            if (string.Equals(words[i], words[i - 1], StringComparison.OrdinalIgnoreCase) && char.IsLetter(words[i][0])) return true;
        return false;
    }

    private void CheckText()
    {
        foreach (var e in _sim.Log.Events)
            if (e.Text.Contains("{") || e.Text.Contains("}") || e.Text.Contains("  ") || e.Text.Contains(" .") || string.IsNullOrWhiteSpace(e.Text) || RepeatedWord(e.Text))
                Add("TEXT", "event " + e.Type + " text looks broken: \"" + e.Text + "\"");
        if (_sim.Arrival != null)
            foreach (var b in _sim.Arrival.Beats)
                if (b.Text.Contains("{") || b.Text.Contains("}") || b.Text.Contains("  ") || b.Text.Contains("..") || RepeatedWord(b.Text) || b.Text.Contains("the an "))
                    Add("TEXT", b.Name + " beat text looks broken: \"" + b.Text + "\"");
    }
}
