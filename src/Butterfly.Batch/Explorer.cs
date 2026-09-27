using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>
    /// A randomized player for exploratory runs (Corey, 2026-09-28): each run draws a persona (the hour-one choice, which
    /// institutions to join or found, policy, priorities, plague response, the promise, spending habits, how to carry gold,
    /// when to jump) and then picks actions at random each turn, weighted by that persona. It plays through both jumps and
    /// walks around after each arrival. The player's randomness is its own seeded generator, separate from the simulation's,
    /// so every run is repeatable.
    /// </summary>
    public sealed class Persona
    {
        public string Seeded = "";
        public bool Promise;
        public string[] PlagueResponse = Array.Empty<string>();
        public List<string> Join = new List<string>();
        public string? Found;
        public int TargetStake;
        public string PolicyStyle = "";
        public int[] Stances = new int[4];
        public Priority[] Priorities = new Priority[3];
        public double[] DomainWeight = new double[3];
        public double InventRate, ProjectRate, AttendRate, PaydownRate, ExchangeEarly;
        public string WorkStyle = "";
        public double JumpYear;
        public double DepositShare, BuryShare;
        public bool EndowAtJump, AuditAtJump;
        public double TurnLengthChangeRate;
        public int MachineStartYear;

        public string Engagement => Join.Count + (Found != null ? 1 : 0) == 0 ? "none" : Join.Count + (Found != null ? 1 : 0) <= 2 ? "light" : "heavy";

        public static Persona Draw(Rng r)
        {
            var p = new Persona();
            p.Seeded = r.Chance(0.45) ? "fountain" : r.Chance(0.9) ? "workshop" : "neither";
            p.Promise = r.Chance(0.6);
            var responses = new[] { new[] { "quarantine", "hospice" }, new[] { "hospice", "quarantine" }, new[] { "none" } };
            p.PlagueResponse = responses[r.Chance(0.15) ? 2 : r.NextInt(0, 2)];
            var established = new[] { "circle", "sanctuary", "faction", "junian", "guild", "bank" };
            int joins = r.Chance(0.12) ? 0 : r.NextInt(1, 5);
            var pool = established.ToList();
            for (int k = 0; k < joins && pool.Count > 0; k++) { int i = r.NextInt(0, pool.Count); p.Join.Add(pool[i]); pool.RemoveAt(i); }
            if (r.Chance(0.3)) p.Found = new[] { "school", "club", "house" }[r.NextInt(0, 3)];
            p.TargetStake = new[] { 1, 10, 25, 50 }[r.NextInt(0, 4)];
            p.PolicyStyle = new[] { "history", "freemarket", "interventionist", "mixed" }[r.NextInt(0, 4)];
            for (int i = 0; i < 4; i++)
                p.Stances[i] = p.PolicyStyle == "history" ? 0 : p.PolicyStyle == "freemarket" ? 1 : p.PolicyStyle == "interventionist" ? -1 : r.NextInt(-1, 2);
            for (int d = 0; d < 3; d++)
            {
                p.Priorities[d] = new[] { Priority.Protect, Priority.Maintain, Priority.AcceptRisk }[r.NextInt(0, 3)];
                p.DomainWeight[d] = 0.2 + r.NextDouble();
            }
            p.InventRate = r.NextDouble();
            p.ProjectRate = r.NextDouble();
            p.AttendRate = r.NextDouble();
            p.PaydownRate = r.NextDouble() * 0.5;
            p.ExchangeEarly = r.NextDouble();
            p.WorkStyle = new[] { "consult", "craft", "odd", "mixed" }[r.NextInt(0, 4)];
            p.JumpYear = 162 + r.NextDouble() * 18;           // AD 162-180: as soon as ready, mid-era, at the era's end or after
            p.DepositShare = r.NextDouble();
            p.BuryShare = r.NextDouble() * (1 - p.DepositShare);
            p.EndowAtJump = r.Chance(0.6);
            p.AuditAtJump = r.Chance(0.5);
            p.TurnLengthChangeRate = r.Chance(0.3) ? 0.02 : 0;
            p.MachineStartYear = 155 + r.NextInt(0, 10);
            return p;
        }

        public string Describe() =>
            Seeded + ", joins " + (Join.Count == 0 ? "nothing" : string.Join("/", Join)) + (Found != null ? " + founds " + Found : "") + " (to " + TargetStake + "%), policy " + PolicyStyle +
            ", priorities " + string.Join("/", Priorities.Select(x => x.ToString())) + ", plague " + PlagueResponse[0] + ", promise " + (Promise ? "yes" : "no") + ", work " + WorkStyle +
            ", jump ~AD " + JumpYear.ToString("0", CultureInfo.InvariantCulture);
    }

    public sealed class ExploreResult
    {
        public int Run;
        public ulong Seed;
        public Persona Persona = new Persona();
        public List<string> Bugs = new List<string>();
        public int Jump1Year, Arrival1Year, Arrival2Year;
        public double IndexDeparture, IndexArrival1, IndexArrival2;
        public double[] Sub1 = new double[3], Sub2 = new double[3], SubDeparture = new double[3];
        public double PlagueDeadShare;
        public string PlagueLabel = "";
        public PromiseStatus Promise;
        public bool Bust;
        public double PriceLevelAtDeparture, SilverArrival1, SilverArrival2;
        public double AureiAfter1, AureiAfter2;
        public string[] Beats1 = new string[4], Beats2 = new string[4];
        public string Walk1 = "", Walk2 = "";
        public Dictionary<string, string> Outcomes1 = new Dictionary<string, string>();
        public Dictionary<string, string> Outcomes2 = new Dictionary<string, string>();
        public string PlagueResponse = "";
        public int Actions, Refused;
        public string LogHash = "";
        public bool Stuck;
        public int BustsInAbsence;
        public string PolicyInForce = "";
        public int MaxStake;
        public bool BothFactions;
    }

    public static class Explorer
    {
        private static readonly Regex Digits = new Regex(@"[\d][\d,.]*");

        /// <summary>Text with its numbers blanked, so two arrivals that differ only in figures count as the same story.</summary>
        public static string Shape(string text) => Digits.Replace(text, "#");

        /// <summary>When set, each turn's state is written here (--trace).</summary>
        public static Action<string>? Trace;

        public static ExploreResult Play(GameData data, int run, ulong seed)
        {
            var res = new ExploreResult { Run = run, Seed = seed };
            var r = new Rng(seed * 7919UL + 104729UL);
            var p = Persona.Draw(r);
            res.Persona = p;
            var sim = new Simulation(data, seed);
            try
            {
                if (p.Seeded != "neither") sim.ChooseSeeded(p.Seeded);
                while (!(sim.MachineReady && sim.Now.YearFraction >= p.JumpYear))
                {
                    if (sim.Now.Year >= 195) { res.Stuck = true; res.Bugs.Add("Machine never ready by AD 195 (" + sim.MachineStepsDone + "/9 steps, " + sim.MachineGoldRestored + "/60 aurei)"); break; }
                    int logFrom = sim.Log.Events.Count;
                    PlayTurn(sim, p, r, res);
                    Check(sim, res, "turn " + sim.Turn);
                    Trace?.Invoke("T" + sim.Turn + " " + sim.Now.Display + " money " + F(sim.World.Gold, "0") + " aurei " + F(sim.World.Aurei, "0") + " att " + sim.World.Attention +
                                  " machine " + sim.MachineStepsDone + "/9 gold " + F(sim.MachineGoldRestored, "0") + " idx " + F(sim.SphereIndex()) +
                                  " E " + F(sim.World[Domain.Economy].Level) + " stakes " + string.Join(",", sim.Backed().Select(i => i.Key + sim.StakePercent(i))) +
                                  " | " + string.Join(" / ", sim.Log.Events.Skip(logFrom).Where(e => e.Type != "gold.settle").Select(e => e.Type)));
                    sim.EndTurn();
                }
                if (res.Stuck) return Finish(sim, res);
                BeforeJump(sim, p, r, res);
                for (int d = 0; d < 3; d++) res.SubDeparture[d] = sim.SubScore(DomainInfo.All[d]);
                res.PriceLevelAtDeparture = sim.World.PriceLevel;
                res.PlagueDeadShare = sim.World.Plague.Deaths / Math.Max(1, sim.World.Plague.Deaths + sim.World.Population);
                res.PlagueLabel = sim.World.Plague.SeverityLabel;
                res.Promise = sim.World.Promise.Status;
                res.Bust = sim.World.Bust.Busts > 0;
                res.Jump1Year = sim.Now.Year;
                int austrian = Simulation.Issues.Count(i => sim.Stance(i) > 0), interv = Simulation.Issues.Count(i => sim.Stance(i) < 0);
                res.PolicyInForce = !sim.World.Institutions.Any(i => i.Def.Maintains == Domain.Governance && sim.HasVoice(i)) ? "no voice (history)" :
                    austrian > 0 && interv == 0 ? "free market" : interv > 0 && austrian == 0 ? "interventionist" : austrian > 0 ? "mixed" : "voice, as history";
                res.MaxStake = sim.World.Institutions.Select(i => sim.StakePercent(i)).DefaultIfEmpty(0).Max();
                res.BothFactions = sim.StakePercent(sim.World.Institution("faction")) >= 10 && sim.StakePercent(sim.World.Institution("junian")) >= 10;
                var a1 = sim.Jump();
                res.IndexDeparture = a1.IndexBefore;
                res.IndexArrival1 = a1.IndexAfter;
                res.Arrival1Year = a1.ArrivalYear;
                for (int d = 0; d < 3; d++) res.Sub1[d] = a1.SubScoresAfter[DomainInfo.All[d]];
                res.Beats1 = Beats(a1, res, "arrival 1");
                res.Walk1 = Walk(sim, res, "arrival 1");
                res.SilverArrival1 = sim.CoinSilverNow();
                res.AureiAfter1 = sim.World.Aurei;
                foreach (var i in a1.Institutions) res.Outcomes1[i.Name] = i.Outcome.ToString();
                // The plague may have struck while the inventor was away: measure it after the first jump.
                res.PlagueDeadShare = sim.World.Plague.Deaths / Math.Max(1, sim.World.Plague.Deaths + sim.World.Population);
                res.PlagueLabel = sim.World.Plague.SeverityLabel;
                res.PlagueResponse = sim.World.Plague.Response ?? "none";
                Texts(res, "learn more 1", a1.LearnMore());
                Trace?.Invoke("ARRIVAL 1\n" + string.Join("\n", a1.Beats.Select(b => b.Name + ": " + b.Text)) + "\n" + a1.LearnMore());
                foreach (var t in Why.Topics) Texts(res, "why " + t, Why.Explain(sim, t));
                if (!sim.CanJumpAgain) res.Bugs.Add("Can't jump a second time after the first arrival");
                else
                {
                    var a2 = sim.Jump();
                    res.IndexArrival2 = a2.IndexAfter;
                    res.Arrival2Year = a2.ArrivalYear;
                    for (int d = 0; d < 3; d++) res.Sub2[d] = a2.SubScoresAfter[DomainInfo.All[d]];
                    res.Beats2 = Beats(a2, res, "arrival 2");
                    res.Walk2 = Walk(sim, res, "arrival 2");
                    res.SilverArrival2 = sim.CoinSilverNow();
                    res.AureiAfter2 = sim.World.Aurei;
                    Texts(res, "learn more 2", a2.LearnMore());
                    Trace?.Invoke("ARRIVAL 2\n" + string.Join("\n", a2.Beats.Select(b => b.Name + ": " + b.Text)) + "\n" + a2.LearnMore());
                    foreach (var i in a2.Institutions) res.Outcomes2[i.Name] = i.Outcome.ToString();
                    res.BustsInAbsence = sim.Log.Events.Count(e => e.Type == "bust.outbreak" && e.Time.Year > res.Jump1Year);
                    if (sim.CanJumpAgain) res.Bugs.Add("A third jump is offered (P0 allows two)");
                }
                Check(sim, res, "end");
            }
            catch (Exception e)
            {
                res.Bugs.Add("Exception at turn " + sim.Turn + " (AD " + sim.Now.Year + "): " + e.GetType().Name + ": " + e.Message + " @ " + (e.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim());
            }
            return Finish(sim, res);
        }

        private static ExploreResult Finish(Simulation sim, ExploreResult res)
        {
            res.LogHash = sim.Log.Hash();
            foreach (var e in sim.Log.Events) if (e.Text.Contains("{") || e.Text.Contains("}")) { res.Bugs.Add("Unfilled placeholder in log: " + e.Text); break; }
            return res;
        }

        private static string[] Beats(Arrival a, ExploreResult res, string where)
        {
            if (a.Beats.Count != 4) res.Bugs.Add(where + ": " + a.Beats.Count + " beats, not 4");
            if (a.Echoes.Count != 3) res.Bugs.Add(where + ": " + a.Echoes.Count + " Echoes, not 3");
            foreach (var b in a.Beats) Texts(res, where + " " + b.Name, b.Text);
            return a.Beats.Select(b => b.Text).Concat(Enumerable.Repeat("", 4)).Take(4).ToArray();
        }

        private static string Walk(Simulation sim, ExploreResult res, string where)
        {
            var sb = new StringBuilder();
            foreach (var place in Simulation.WalkPlaces)
            {
                string t = sim.Visit(place);
                Texts(res, where + " visit " + place, t);
                sb.AppendLine(t);
            }
            Texts(res, where + " news", string.Join("\n", sim.News()));
            return sb.ToString();
        }

        private static void Texts(ExploreResult res, string where, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) res.Bugs.Add(where + ": empty text");
            else if (text.Contains("{") || text.Contains("}")) res.Bugs.Add(where + ": unfilled placeholder: " + text.Substring(Math.Max(0, text.IndexOf('{') - 40), Math.Min(120, text.Length - Math.Max(0, text.IndexOf('{') - 40))));
            else if (text.Contains("NaN") || text.Contains("∞") || text.Contains("Infinity")) res.Bugs.Add(where + ": NaN/Infinity in text");
        }

        private static void Check(Simulation sim, ExploreResult res, string where)
        {
            var w = sim.World;
            void Bad(string what) { if (res.Bugs.Count < 40) res.Bugs.Add(where + " (AD " + sim.Now.Year + "): " + what); }
            if (double.IsNaN(w.Gold) || double.IsInfinity(w.Gold)) Bad("gold is " + w.Gold);
            if (w.Gold < -0.01) Bad("negative money: " + w.Gold);
            if (w.Aurei < -0.01) Bad("negative aurei: " + w.Aurei);
            if (w.Attention < 0) Bad("negative Attention: " + w.Attention);
            foreach (var d in w.Domains)
            {
                if (double.IsNaN(d.Level) || double.IsNaN(d.Debt)) Bad(d.Domain + " level or debt is NaN");
                if (d.Level < 0 || d.Level > 200) Bad(d.Domain + " level out of range: " + d.Level);
                if (d.Debt < -0.01) Bad(d.Domain + " negative debt: " + d.Debt);
            }
            foreach (var i in w.Institutions)
            {
                if (i.Stake < -1e-9 || i.Stake > 1 + 1e-9) Bad(i.Key + " stake out of range: " + i.Stake);
                if (double.IsNaN(i.Strength) || i.Strength < -0.01) Bad(i.Key + " strength " + i.Strength);
                if (i.Loyalty < -0.01 || i.Loyalty > 100.01) Bad(i.Key + " loyalty out of range: " + i.Loyalty);
                if (i.Holdings < -0.01) Bad(i.Key + " negative holdings: " + i.Holdings);
            }
            double idx = sim.SphereIndex();
            if (double.IsNaN(idx) || idx <= 0) Bad("Index is " + idx);
        }

        private static bool Do(Simulation sim, ExploreResult res, Func<CommandResult> action)
        {
            res.Actions++;
            var c = action();
            if (!c.Ok) res.Refused++;
            return c.Ok;
        }

        private static double Reserve(Simulation sim) => 12 * sim.World.PriceLevel;   // about a year's dues and upkeep

        private static void PlayTurn(Simulation sim, Persona p, Rng r, ExploreResult res)
        {
            var w = sim.World;
            // Prompts first.
            if (w.Promise.Status == PromiseStatus.Offered) Do(sim, res, () => sim.AnswerPromise(p.Promise));
            if (sim.OutbreakAwaitingResponse)
            {
                bool done = false;
                foreach (var resp in p.PlagueResponse) if (!done && Do(sim, res, () => sim.RespondToPlague(resp))) done = true;
                if (!done) Do(sim, res, () => sim.RespondToPlague("none"));
            }
            if (p.TurnLengthChangeRate > 0 && r.Chance(p.TurnLengthChangeRate)) Do(sim, res, () => sim.SetTurnLength(r.NextInt(1, 4)));
            // Live on the scavenged gold until the machine needs it back.
            if (w.Aurei >= 1 && sim.MachineGoldRestored < 1 && (w.Gold < 20 || r.Chance(0.3))) Do(sim, res, () => sim.SellAurei(w.Aurei));

            var actions = new List<(double Weight, Func<bool> Act)>();
            // A player who means to leave saves for the machine as the planned jump nears.
            bool saving = !sim.MachineReady && sim.Now.YearFraction >= p.JumpYear - 2;
            // The machine.
            if (sim.Now.Year >= p.MachineStartYear || sim.Now.Year >= 163)
            {
                if (!sim.MachineAssessed) actions.Add((3, () => Do(sim, res, sim.Assess)));
                foreach (var system in Simulation.MachineSystems)
                {
                    var step = sim.NextMachineStep(system);
                    if (step != null && sim.MachineStepGold(step) <= w.Gold - Reserve(sim) / 2 && step.AttentionPerTurn <= w.Attention)
                        actions.Add((2, () => Do(sim, res, () => sim.Repair(system))));
                }
                if (sim.MachineStepsDone >= sim.MachineStepsTotal)
                {
                    foreach (var up in new[] { "contacts", "lens", "flywheel" })
                        if (r.Chance(0.3)) actions.Add((0.5, () => Do(sim, res, () => sim.Upgrade(up))));
                    if (sim.MachineGoldRestored < sim.MachineGoldNeeded && (saving || sim.Now.YearFraction > p.JumpYear - 3 || r.Chance(p.ExchangeEarly * 0.3)))
                        actions.Add((2, () =>
                        {
                            double missing = sim.MachineGoldNeeded - sim.MachineGoldRestored - w.Aurei;
                            double can = Math.Floor(Math.Max(0, w.Gold - Reserve(sim)) / sim.AureiCost(1));
                            if (missing >= 1 && can >= 1) Do(sim, res, () => sim.BuyAurei(Math.Min(missing, can)));
                            return w.Aurei >= 1 && Do(sim, res, () => sim.RestoreGold(Math.Min(w.Aurei, sim.MachineGoldNeeded - sim.MachineGoldRestored)));
                        }));
                }
            }
            // Institutions.
            foreach (var id in saving ? new List<string>() : p.Join)
            {
                var i = w.Institution(id);
                if (!i.Exists) continue;
                if (i.Stake <= 0)
                {
                    int first = id == "bank" ? 5 : 1;
                    if (sim.JoinBlocker(i) == null && sim.BuyCost(i, first) <= w.Gold - Reserve(sim)) actions.Add((2, () => Do(sim, res, () => sim.Buy(id, first))));
                }
                else
                {
                    if (sim.StakePercent(i) < p.TargetStake && sim.BuyCost(i, 1) <= w.Gold - Reserve(sim))
                    {
                        int pts = 1 + r.NextInt(0, 3);
                        while (pts > 1 && sim.BuyCost(i, pts) > w.Gold - Reserve(sim)) pts--;
                        actions.Add((1, () => Do(sim, res, () => sim.Buy(id, pts))));
                    }
                    if (i.MeetingsThisYear < 2 && r.Chance(p.AttendRate)) actions.Add((1.5, () => Do(sim, res, () => sim.Attend(id))));
                }
            }
            if (p.Found != null)
            {
                var own = w.Institution(p.Found);
                if (!own.Exists && !own.Collapsed && sim.FoundCost(own.Def.Maintains) <= w.Gold - Reserve(sim)) actions.Add((2, () => Do(sim, res, () => sim.Found(p.Found))));
                if (own.Exists && sim.Controls(own) && own.Strength < 40 && w.Gold > 3 * Reserve(sim)) actions.Add((1, () => Do(sim, res, () => sim.Invest(p.Found, Math.Floor((w.Gold - Reserve(sim)) / 2)))));
            }
            foreach (var i in w.Institutions.Where(x => x.Exists && sim.Controls(x)).ToList())
            {
                if (i.Loyalty < 70) actions.Add((1, () => Do(sim, res, () => sim.Oversee(i.Key))));
                if (!i.Chartered && r.Chance(0.3)) actions.Add((0.7, () => Do(sim, res, () => sim.Charter(i.Key))));
                if (sim.CommitmentsEnabled && w.Commitments.Count == 0 && r.Chance(0.05)) actions.Add((0.5, () => Do(sim, res, () => sim.Mentor(i.Key))));
            }
            // Voice: policy and priorities.
            for (int k = 0; k < 4; k++)
            {
                var issue = Simulation.Issues[k];
                int stance = p.Stances[k];
                if (sim.Stance(issue) != stance && r.Chance(0.5)) actions.Add((1.5, () => Do(sim, res, () => sim.SetPolicy(issue, stance))));
            }
            for (int d = 0; d < 3; d++)
            {
                var dom = DomainInfo.All[d];
                var pr = p.Priorities[d];
                if (sim.HasHold(dom) && w[dom].Priority != pr) actions.Add((1.5, () => Do(sim, res, () => sim.SetPriority(dom, pr))));
            }
            // Projects, weighted by the persona's interest in each domain.
            if (!saving && r.Chance(p.ProjectRate))
                foreach (var proj in sim.AvailableProjects().Where(x => sim.ProjectAuthorityBlocker(x) == null && sim.ProjectGold(x) <= w.Gold - Reserve(sim)).ToList())
                    actions.Add((p.DomainWeight[Array.IndexOf(DomainInfo.All, proj.Domain)], () => Do(sim, res, () => sim.StartProject(proj.Id))));
            // Inventions.
            if (!saving && r.Chance(p.InventRate) && w.ActiveInventions.Count == 0)
                foreach (var inv in sim.Data.Content.Inventions.Where(x => sim.InventionState(x) == "ready" && sim.InventionGold(x) <= w.Gold - Reserve(sim)).ToList())
                    actions.Add((0.6, () => Do(sim, res, () => sim.Invent(inv.Id))));
            // Paying down debt.
            foreach (var d in w.Domains.Where(d => d.Debt >= 1).ToList())
                if (!saving && r.Chance(p.PaydownRate))
                {
                    double pts = Math.Floor(Math.Min(d.Debt, Math.Max(0, w.Gold - Reserve(sim)) / sim.PaydownCost(1)));
                    if (pts >= 1) actions.Add((1, () => Do(sim, res, () => sim.PayDown(d.Domain, pts))));
                }

            // Work comes first when money is short; otherwise it takes its chance with everything else.
            bool worked = false;
            bool Work()
            {
                if (worked) return false;
                worked = true;
                string kind = p.WorkStyle == "mixed" ? Simulation.WorkKinds[r.NextInt(0, 3)] : p.WorkStyle;
                foreach (var k in new[] { kind, "craft", "odd" })
                    if (sim.WorkAttention(k) <= w.Attention && Do(sim, res, () => sim.Work(k))) return true;
                return false;
            }
            if (w.Gold < 2 * Reserve(sim)) Work();
            actions.Add((2.5, Work));
            // Weighted random order.
            while (actions.Count > 0 && w.Attention > 0)
            {
                double total = actions.Sum(a => a.Weight), pick = r.NextDouble() * total;
                int idx = 0;
                while (idx < actions.Count - 1 && pick > actions[idx].Weight) { pick -= actions[idx].Weight; idx++; }
                var act = actions[idx];
                actions.RemoveAt(idx);
                act.Act();
            }
            if (w.Attention > 0 && !worked) Work();
        }

        private static void BeforeJump(Simulation sim, Persona p, Rng r, ExploreResult res)
        {
            var w = sim.World;
            if (p.PaydownRate > 0.25)
                foreach (var d in w.Domains.Where(d => d.Debt >= 1).OrderByDescending(d => d.Debt).ToList())
                {
                    double pts = Math.Floor(Math.Min(d.Debt, w.Gold / sim.PaydownCost(1)));
                    if (pts >= 1) Do(sim, res, () => sim.PayDown(d.Domain, pts));
                }
            foreach (var i in w.Institutions.Where(x => x.Exists && sim.Controls(x)).ToList())
            {
                if (p.AuditAtJump) Do(sim, res, () => sim.Audit(i.Key));
                if (p.EndowAtJump && w.Gold >= 1) Do(sim, res, () => sim.Endow(i.Key, Math.Floor(w.Gold / 2)));
            }
            // Everything else into gold: carry what the machine takes, bank and bury the rest as the persona likes.
            double can = Math.Floor(w.Gold / sim.AureiCost(1));
            if (can >= 1) Do(sim, res, () => sim.BuyAurei(can));
            double spare = Math.Max(0, w.Aurei - sim.CarryAurei);
            double dep = Math.Floor(spare * p.DepositShare), bury = Math.Floor(spare * p.BuryShare);
            if (dep >= 1) Do(sim, res, () => sim.Deposit(dep));
            if (bury >= 1) Do(sim, res, () => sim.Bury(bury));
        }

        // ---- report ---------------------------------------------------------

        private static string F(double v, string f = "0.0") => v.ToString(f, CultureInfo.InvariantCulture);

        private static (double Mean, double Sd, double Min, double Median, double Max) Stats(IEnumerable<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            if (v.Count == 0) return (0, 0, 0, 0, 0);
            double mean = v.Average();
            double sd = Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / Math.Max(1, v.Count - 1));
            return (mean, sd, v[0], v[v.Count / 2], v[v.Count - 1]);
        }

        public static string Report(List<ExploreResult> results, List<string> determinism)
        {
            var ok = results.Where(x => !x.Stuck && x.IndexArrival2 > 0).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("# Exploration: " + results.Count + " randomized runs through two jumps");
            sb.AppendLine();
            sb.AppendLine("Each run draws a random player (persona) and random choices each turn, with its own seed; the simulation seed also differs per run. Generated by `dotnet run --project src/Butterfly.Batch -- --explore " + results.Count + "`.");
            sb.AppendLine();
            sb.AppendLine("## Bugs and anomalies");
            var bugs = results.SelectMany(x => x.Bugs.Select(b => (x.Run, Bug: b))).ToList();
            if (bugs.Count == 0) sb.AppendLine("None found.");
            foreach (var g in bugs.GroupBy(b => Shape(b.Bug)).OrderByDescending(g => g.Count()))
                sb.AppendLine("- " + g.Count() + " run(s), e.g. run " + g.First().Run + ": " + g.First().Bug);
            sb.AppendLine("- Runs holding 10%+ of both Senate factions at departure (the rule says holding 10% of one shuts you out of the other): " + results.Count(x => x.BothFactions) + ".");
            var busty = ok.Where(x => x.BustsInAbsence > 0).ToList();
            sb.AppendLine("- Economic busts while away: " + busty.Count + " runs, " + busty.Sum(x => x.BustsInAbsence) + " busts (up to " + (busty.Count > 0 ? busty.Max(x => x.BustsInAbsence) : 0) + " in one run); their mean second-arrival Index " + (busty.Count > 0 ? F(busty.Average(x => x.IndexArrival2)) : "-") + " vs " + F(ok.Where(x => x.BustsInAbsence == 0).Average(x => x.IndexArrival2)) + " without.");
            var dup = ok.Count(x => { var w = x.Beats1.Length > 1 ? x.Beats1[1] : ""; return w.Contains("copper washed in silver") && w.Contains("bronze washed thin"); });
            sb.AppendLine("- Wrongness beat describing the debased coin twice (\"copper washed in silver\" and \"bronze washed thin\"): " + dup + " first arrivals.");
            foreach (var rich in results.Where(x => x.AureiAfter1 > 300).OrderByDescending(x => x.AureiAfter1).Take(3))
                sb.AppendLine("- Large purse at the first arrival: run " + rich.Run + " holds " + F(rich.AureiAfter1, "0") + " aurei (" + rich.Persona.Describe() + ").");
            sb.AppendLine("- Determinism: " + (determinism.Count == 0 ? "10 runs replayed with identical log hashes." : string.Join("; ", determinism)));
            sb.AppendLine("- Refused commands: " + results.Sum(x => x.Refused) + " of " + results.Sum(x => x.Actions) + " (random players try things they can't do; not bugs by themselves).");
            sb.AppendLine();

            sb.AppendLine("## How far apart the outcomes end up");
            sb.AppendLine("| Stage | Mean | SD | Min | Median | Max |");
            sb.AppendLine("|---|---|---|---|---|---|");
            void Row(string name, IEnumerable<double> v) { var s = Stats(v); sb.AppendLine("| " + name + " | " + F(s.Mean) + " | " + F(s.Sd) + " | " + F(s.Min) + " | " + F(s.Median) + " | " + F(s.Max) + " |"); }
            Row("Index at departure", ok.Select(x => x.IndexDeparture));
            var noBust = ok.Where(x => x.BustsInAbsence == 0).ToList();
            Row("Index at departure (no bust while away)", noBust.Select(x => x.IndexDeparture));
            Row("Index at first arrival (no bust while away)", noBust.Select(x => x.IndexArrival1));
            Row("Index at second arrival (no bust while away)", noBust.Select(x => x.IndexArrival2));
            Row("Index at first arrival", ok.Select(x => x.IndexArrival1));
            Row("Index at second arrival", ok.Select(x => x.IndexArrival2));
            for (int d = 0; d < 3; d++)
            {
                Row(DomainInfo.All[d] + " at departure", ok.Select(x => x.SubDeparture[d]));
                Row(DomainInfo.All[d] + " at first arrival", ok.Select(x => x.Sub1[d]));
                Row(DomainInfo.All[d] + " at second arrival", ok.Select(x => x.Sub2[d]));
            }
            Row("Plague dead (% of Rome)", ok.Select(x => x.PlagueDeadShare * 100));
            Row("Coin silver at first arrival (%)", ok.Select(x => x.SilverArrival1 * 100));
            Row("Coin silver at second arrival (%)", ok.Select(x => x.SilverArrival2 * 100));
            Row("Aurei in hand at first arrival", ok.Select(x => x.AureiAfter1));
            Row("Departure year", ok.Select(x => (double)x.Jump1Year));
            Row("First arrival year", ok.Select(x => (double)x.Arrival1Year));
            Row("Second arrival year", ok.Select(x => (double)x.Arrival2Year));
            sb.AppendLine();

            sb.AppendLine("## Do big choices lead to different places?");
            sb.AppendLine("Mean Index (and SD) by group. **Effect** = the gap between the best and worst group means divided by the overall SD at that stage; below 0.3 the choice barely shows in the outcome.");
            sb.AppendLine();
            void Group(string title, Func<ExploreResult, string> key)
            {
                sb.AppendLine("**" + title + "**");
                sb.AppendLine();
                sb.AppendLine("| Group | Runs | Departure | First arrival | Second arrival |");
                sb.AppendLine("|---|---|---|---|---|");
                var groups = ok.GroupBy(key).OrderBy(g => g.Key).ToList();
                foreach (var g in groups)
                {
                    var a = Stats(g.Select(x => x.IndexDeparture)); var b = Stats(g.Select(x => x.IndexArrival1)); var c = Stats(g.Select(x => x.IndexArrival2));
                    sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + F(a.Mean) + " (" + F(a.Sd) + ") | " + F(b.Mean) + " (" + F(b.Sd) + ") | " + F(c.Mean) + " (" + F(c.Sd) + ") |");
                }
                string Effect(Func<ExploreResult, double> f)
                {
                    var big = groups.Where(g => g.Count() >= 5).Select(g => g.Average(f)).ToList();
                    double sd = Stats(ok.Select(f)).Sd;
                    return big.Count < 2 || sd <= 0 ? "n/a" : F((big.Max() - big.Min()) / sd, "0.00");
                }
                sb.AppendLine("| **Effect** | | " + Effect(x => x.IndexDeparture) + " | " + Effect(x => x.IndexArrival1) + " | " + Effect(x => x.IndexArrival2) + " |");
                // The same, leaving out runs where a bust struck while away (they dominate the spread).
                var calm = ok.Where(x => x.BustsInAbsence == 0).ToList();
                var cg = calm.GroupBy(key).Where(g => g.Count() >= 5).ToList();
                string CalmEffect(Func<ExploreResult, double> f)
                {
                    double sd = Stats(calm.Select(f)).Sd;
                    var m = cg.Select(g => g.Average(f)).ToList();
                    return m.Count < 2 || sd <= 0 ? "n/a" : F((m.Max() - m.Min()) / sd, "0.00") + " (gap " + F(m.Max() - m.Min()) + ")";
                }
                sb.AppendLine("| Effect without busts | | " + CalmEffect(x => x.IndexDeparture) + " | " + CalmEffect(x => x.IndexArrival1) + " | " + CalmEffect(x => x.IndexArrival2) + " |");
                sb.AppendLine();
            }
            Group("Hour-one choice", x => x.Persona.Seeded);
            Group("Economic policy style", x => x.Persona.PolicyStyle);
            Group("Engagement (institutions joined or founded)", x => x.Persona.Engagement);
            Group("Economic policy in force at departure", x => x.PolicyInForce);
            Group("Highest stake held at departure", x => x.MaxStake == 0 ? "0 none" : x.MaxStake < 10 ? "1 under 10%" : x.MaxStake < 25 ? "2 10-24%" : x.MaxStake < 50 ? "3 25-49%" : "4 50%+");
            Group("Plague response actually made", x => x.PlagueResponse);
            Group("Promise to Demetria", x => x.Promise.ToString());
            Group("Founded an institution", x => x.Persona.Found ?? "none");
            Group("First jump", x => x.Jump1Year < 167 ? "before the plague (<167)" : x.Jump1Year < 175 ? "mid (167-174)" : "late (175+)");
            Group("Work style", x => x.Persona.WorkStyle);

            sb.AppendLine("## Do the arrivals read differently?");
            sb.AppendLine("Distinct texts per beat (numbers blanked, so only different stories count) out of " + ok.Count + " runs.");
            sb.AppendLine();
            sb.AppendLine("| Beat | First arrival | Second arrival | Most common first-arrival text (share) |");
            sb.AppendLine("|---|---|---|---|");
            string[] names = { "Recognition", "Wrongness", "Personal echo", "Discovery" };
            for (int k = 0; k < 4; k++)
            {
                var s1 = ok.Select(x => Shape(x.Beats1[k])).ToList();
                var s2 = ok.Select(x => Shape(x.Beats2[k])).ToList();
                var top = s1.GroupBy(s => s).OrderByDescending(g => g.Count()).First();
                string sample = top.Key.Length > 90 ? top.Key.Substring(0, 90) + "…" : top.Key;
                sb.AppendLine("| " + names[k] + " | " + s1.Distinct().Count() + " | " + s2.Distinct().Count() + " | " + F(100.0 * top.Count() / ok.Count, "0") + "%: " + sample.Replace("|", "/") + " |");
            }
            sb.AppendLine("| Walk (all five places) | " + ok.Select(x => Shape(x.Walk1)).Distinct().Count() + " | " + ok.Select(x => Shape(x.Walk2)).Distinct().Count() + " | |");
            for (int k = 0; k < 4; k++)
                sb.AppendLine("| " + names[k] + ": second arrival repeats the first word for word | " + F(100.0 * ok.Count(x => Shape(x.Beats1[k]) == Shape(x.Beats2[k])) / ok.Count, "0") + "% of runs | | |");
            sb.AppendLine("| Whole arrival (4 beats) | " + ok.Select(x => string.Join("|", x.Beats1.Select(Shape))).Distinct().Count() + " | " + ok.Select(x => string.Join("|", x.Beats2.Select(Shape))).Distinct().Count() + " | |");
            sb.AppendLine();
            sb.AppendLine("Institution outcomes (institutions held at 10%+): first arrival " +
                          string.Join(", ", ok.SelectMany(x => x.Outcomes1.Values).GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) +
                          "; second arrival " + string.Join(", ", ok.SelectMany(x => x.Outcomes2.Values).GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) + ".");
            sb.AppendLine();

            sb.AppendLine("## Runs");
            sb.AppendLine("| Run | Seed | Persona | Left | Index dep → 1st → 2nd | Plague | Bugs |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var x in results)
                sb.AppendLine("| " + x.Run + " | " + x.Seed + " | " + x.Persona.Describe() + " | AD " + x.Jump1Year + " → " + x.Arrival1Year + " → " + x.Arrival2Year + " | " +
                              F(x.IndexDeparture) + " → " + F(x.IndexArrival1) + " → " + F(x.IndexArrival2) + " | " + F(x.PlagueDeadShare * 100) + "% " + x.PlagueLabel + " | " + x.Bugs.Count + " |");
            return sb.ToString();
        }

        public static List<ExploreResult> RunAll(GameData data, int runs, out List<string> determinism)
        {
            var results = new List<ExploreResult>();
            for (int k = 1; k <= runs; k++) results.Add(Play(data, k, 1000UL + (ulong)k));
            determinism = new List<string>();
            foreach (var x in results.Take(10))
            {
                var again = Play(data, x.Run, x.Seed);
                if (again.LogHash != x.LogHash) determinism.Add("run " + x.Run + " replayed differently");
            }
            return results;
        }
    }
}
