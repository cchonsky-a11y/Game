using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Butterfly.Core;

namespace Butterfly.Batch
{
    /// <summary>
    /// Executable P1 validation (2026-10-04): a scripted player runs the real simulation through an era and two jumps and
    /// the run is measured. This is an automated, scripted run of the actual build: not a human playtest and not persona
    /// feedback. Usage: --p1-validate 1,2,3,4,5 [--out file].
    /// </summary>
    public static class P1Campaign
    {
        public sealed class Result
        {
            public ulong Seed;
            public string Choice = "";
            public int FirstCommissionTurn = -1, FirstPaidTurn = -1;
            public int OddJobMonths, EraMonths, AttentionConflicts;
            public Dictionary<SceneCategory, int> Routed = new Dictionary<SceneCategory, int>();
            public Dictionary<SceneCategory, int> AllScenes = new Dictionary<SceneCategory, int>();
            public int LongestRoutedRun, LongestSceneRun;
            public string LongestRunCategory = "";
            public int InvitationsOffered, InvitationsAccepted, Members;
            public int CommissionsOffered, CommissionsAccepted, CommissionsDone, CommissionsWalked, CommissionsDeclined;
            public int ChallengeStages, ChallengesDone;
            public int LifeEvents, PeopleKnown;
            public double MinGold = double.MaxValue, GoldAtDeparture;
            public bool LedgerReconciles;
            public int Depart1, Arrive1, Jump1, Arrive2, Jump2;
            public List<string> Echoes1 = new List<string>(), Echoes2 = new List<string>();
            public int DuplicateTexts, RepeatedArrivalSentences;
            public List<string> RepeatedSentences = new List<string>();
            public List<string> DuplicateList = new List<string>();
            public int LongestQuietStretch;
            public bool Warned;
            public string R17Reached = "none";
            public string Institutions1 = "", Institutions2 = "";
        }

        private static void Count(Dictionary<SceneCategory, int> d, SceneCategory c) => d[c] = d.TryGetValue(c, out int n) ? n + 1 : 1;

        public static Result Play(GameData data, ulong seed, int departYear = 163)
        {
            var sim = new Simulation(data, seed);
            var r = new Result { Seed = seed };
            bool counter = seed % 3 == 0;
            void Note(CommandResult res) { if (!res.Ok && res.Message.Contains("would reserve")) r.AttentionConflicts++; }
            int quiet = 0, lastScenes = 0;
            double startGold = sim.World.Gold;
            r.Choice = seed % 2 == 1 ? "workshop" : "fountain";
            sim.ChooseSeeded(r.Choice);
            while (!sim.EraOver)
            {
                var w = sim.World;
                if (w.Gold < 2 && w.Aurei >= 5 && sim.MachineGoldRestored < 1) sim.SellAurei(5);
                if (sim.PendingEvent is EventDef ev) sim.Decide(ev.Options[(int)(seed % (ulong)ev.Options.Count)].Id);
                foreach (var c in w.Commissions.ToList())
                {
                    if (c.Status == CommissionStatus.Offered) { if (r.FirstCommissionTurn < 0) r.FirstCommissionTurn = sim.Turn; Note(sim.LookAtCommission(c.Id)); }
                    if (c.Status == CommissionStatus.TermsOffered)
                    {
                        if (counter && !c.Countered) sim.CounterCommission(c.Id);
                        if (c.Status == CommissionStatus.TermsOffered) Note(sim.AcceptCommission(c.Id));
                    }
                }
                foreach (var p in w.Invitations.Where(p => p.Pending != InvitationOffer.None).ToList()) sim.AcceptInvitation(p.Institution);
                foreach (var ch in w.Challenges.Where(ch => ch.Status == ChallengeStatus.Open).ToList()) Note(sim.StartChallengeStage(ch.Id));
                if (!sim.MachineAssessed) Note(sim.Assess());
                foreach (var system in Simulation.MachineSystems) Note(sim.Repair(system));
                if (sim.MachineStepsDone >= sim.MachineStepsTotal && sim.MachineGoldRestored < sim.MachineGoldNeeded)
                {
                    double missing = Math.Ceiling(sim.MachineGoldNeeded - sim.MachineGoldRestored);
                    if (w.Aurei < missing) { double can = Math.Floor(w.Gold / sim.AureiCost(1)); if (can >= 1) sim.BuyAurei(Math.Min(missing - w.Aurei, can)); }
                    if (w.Aurei >= 1) sim.RestoreGold(Math.Min(w.Aurei, missing));
                }
                if (sim.CanListen) sim.Listen();
                if (sim.OwnsWorkshop && sim.OrdersLeftThisSeason > 0 && sim.OrderBoard().Any()) sim.TakeOrder(sim.OrderBoard().First().Id);
                bool working = w.Commissions.Any(c => c.Status == CommissionStatus.Working);
                if (!working && w.Gold < sim.Priced(30) && w.Attention >= 1 && sim.Work("odd").Ok) r.OddJobMonths++;
                r.MinGold = Math.Min(r.MinGold, w.Gold);
                if (sim.MachineReady && sim.Now.Year >= departYear) break;
                sim.EndMonth();
                r.EraMonths++;
                int scenes = sim.World.ScenePacing.History.Count;
                quiet = scenes == lastScenes ? quiet + 1 : 0;
                r.LongestQuietStretch = Math.Max(r.LongestQuietStretch, quiet);
                lastScenes = scenes;
            }
            r.GoldAtDeparture = sim.World.Gold;
            var l = sim.World.Ledger;
            r.LedgerReconciles = Math.Abs(startGold + l.Income - l.Expenses - sim.World.Gold) < 0.01;
            r.Warned = sim.World.Flags.Contains("r17-warned");
            r.R17Reached = sim.World.ScenesSeen.Where(id => id.StartsWith("r17-") && id != "r17-mill").DefaultIfEmpty("none").Last();
            r.FirstPaidTurn = sim.Log.Events.Where(e => e.Type == "commission.complete" && e.Effects.Any(f => f.Key == "gold")).Select(e => (int?)e.Time.TotalMonths).FirstOrDefault() is int t
                ? t - sim.Log.Events.First().Time.TotalMonths + 1 : -1;
            foreach (var p in sim.World.RoutedScenes) Count(r.Routed, p.Category);
            foreach (var c in sim.World.ScenePacing.History) Count(r.AllScenes, c);
            r.LongestRoutedRun = LongestRun(sim.World.RoutedScenes.Select(p => p.Category).ToList(), out _);
            r.LongestSceneRun = LongestRun(sim.World.ScenePacing.History, out var cat);
            r.LongestRunCategory = cat.ToString();
            r.InvitationsOffered = sim.Log.Events.Count(e => e.Type == "invitation.offer");
            r.InvitationsAccepted = sim.Log.Events.Count(e => e.Type == "invitation.guest" || e.Type == "invitation.sponsor");
            r.Members = sim.Log.Events.Count(e => e.Type == "institution.join");
            r.CommissionsOffered = sim.World.Commissions.Count(c => c.Status != CommissionStatus.NotYet);
            r.CommissionsAccepted = sim.Log.Events.Count(e => e.Type == "commission.agreed");
            r.CommissionsDone = sim.World.Commissions.Count(c => c.Status == CommissionStatus.Done);
            r.CommissionsWalked = sim.World.Commissions.Count(c => c.Status == CommissionStatus.Walked);
            r.CommissionsDeclined = sim.World.Commissions.Count(c => c.Status == CommissionStatus.Declined);
            r.ChallengeStages = sim.Log.Events.Count(e => e.Type == "challenge.stage");
            r.ChallengesDone = sim.World.Challenges.Count(c => c.Status == ChallengeStatus.Done);
            r.LifeEvents = sim.World.LifeEventLog.Count;
            r.PeopleKnown = sim.KnownPeople().Count();
            var texts = sim.Log.Events.Where(e => e.Type.StartsWith("scene.") || e.Type.StartsWith("person.") || e.Type.StartsWith("commission.") ||
                                                  e.Type.StartsWith("invitation.") || e.Type.StartsWith("challenge.") || e.Type == "event.offer")
                                      .Select(e => e.Text).ToList();
            r.DuplicateTexts = texts.Count - texts.Distinct().Count();
            r.DuplicateList = texts.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key.Substring(0, Math.Min(90, g.Key.Length))).ToList();
            if (!sim.MachineReady) return r;
            r.Depart1 = sim.Now.Year;
            var a1 = sim.Jump();
            r.Arrive1 = a1.ArrivalYear; r.Jump1 = a1.JumpYears; r.Echoes1 = a1.P1Echoes.ToList();
            r.Institutions1 = string.Join("; ", a1.Institutions.Select(i => i.Name.Replace("the ", "") + " " + i.Outcome));
            if (!sim.CanJumpAgain) return r;
            var a2 = sim.Jump();
            r.Arrive2 = a2.ArrivalYear; r.Jump2 = a2.JumpYears; r.Echoes2 = a2.P1Echoes.ToList();
            r.Institutions2 = string.Join("; ", a2.Institutions.Select(i => i.Name.Replace("the ", "") + " " + i.Outcome));
            var s1 = Sentences(a1); var s2 = Sentences(a2);
            r.RepeatedSentences = s2.Where(s1.Contains).ToList();
            r.RepeatedArrivalSentences = r.RepeatedSentences.Count;
            return r;
        }

        private static HashSet<string> Sentences(Arrival a) =>
            new HashSet<string>(a.Beats.SelectMany(b => b.Text.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries)).Select(x => x.Trim()).Where(x => x.Length > 40));

        private static int LongestRun(IReadOnlyList<SceneCategory> list, out SceneCategory category)
        {
            int best = 0, run = 0; category = default;
            for (int i = 0; i < list.Count; i++)
            {
                run = i > 0 && list[i] == list[i - 1] ? run + 1 : 1;
                if (run > best) { best = run; category = list[i]; }
            }
            return best;
        }

        public static string Report(IReadOnlyList<Result> results)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("# P1 executable two-jump validation");
            sb.AppendLine();
            sb.AppendLine("**These are automated, scripted runs of the actual build (`dotnet run --project src/Butterfly.Batch -- --p1-validate ...`), not human playtests and not persona feedback.** The scripted player: picks the workshop on odd seeds and the fountain on even ones; looks at and accepts every commission (and asks for more on every third seed); accepts every invitation; starts every Grand Challenge stage it can; assesses and repairs the machine, buys back and restores its gold; opens the R-17 channel when it can; answers Rome's choices by seed; takes a workshop order when offered; does odd jobs only when no commission is under way and money is short; jumps once the machine is ready and the year is at least AD 163, then jumps again.");
            sb.AppendLine();
            sb.AppendLine("| Seed | Choice | First commission (month) | First paid (month) | Odd-job months / era months | Commissions offered / accepted / done / walked | Invitations offered / accepted / joined | Challenge stages / done | Life events | People known | Attention conflicts | Min gold / at departure (aurei) | Ledger reconciles | R-17 warned |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var r in results)
                sb.AppendLine("| " + r.Seed + " | " + r.Choice + " | " + r.FirstCommissionTurn + " | " + r.FirstPaidTurn + " | " + r.OddJobMonths + " / " + r.EraMonths + " | " +
                              r.CommissionsOffered + " / " + r.CommissionsAccepted + " / " + r.CommissionsDone + " / " + r.CommissionsWalked + " | " +
                              r.InvitationsOffered + " / " + r.InvitationsAccepted + " / " + r.Members + " | " + r.ChallengeStages + " / " + r.ChallengesDone + " | " +
                              r.LifeEvents + " | " + r.PeopleKnown + " | " + r.AttentionConflicts + " | " +
                              r.MinGold.ToString("0.0", ci) + " / " + r.GoldAtDeparture.ToString("0.0", ci) + " | " + (r.LedgerReconciles ? "yes" : "NO") + " | " + (r.Warned ? "yes" : "no") + " |");
            sb.AppendLine();
            sb.AppendLine("## Jumps and echoes");
            sb.AppendLine();
            sb.AppendLine("| Seed | First jump | Second jump | Echoes, first arrival | Echoes, second arrival | Institutions found (1st; 2nd) | Repeated arrival sentences | Duplicate scene texts in the era | R-17 reached |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var r in results)
                sb.AppendLine("| " + r.Seed + " | AD " + r.Depart1 + " → " + r.Arrive1 + " (" + r.Jump1 + " yrs) | AD " + r.Arrive1 + " → " + r.Arrive2 + " (" + r.Jump2 + " yrs) | " +
                              Kinds(r.Echoes1) + " | " + Kinds(r.Echoes2) + " | " + (r.Institutions1.Length == 0 ? "none" : r.Institutions1) + "; " + (r.Institutions2.Length == 0 ? "none" : r.Institutions2) +
                              " | " + r.RepeatedArrivalSentences + " | " + r.DuplicateTexts + " | " + r.R17Reached + (r.Warned ? ", warned" : "") + " |");
            foreach (var r in results)
                foreach (var dup in r.DuplicateList) sb.AppendLine("- Seed " + r.Seed + " shows twice in the era: \"" + dup + "…\"");
            foreach (var r in results.Where(r => r.RepeatedSentences.Count > 0))
                foreach (var s in r.RepeatedSentences) sb.AppendLine("- Seed " + r.Seed + " repeats on both arrivals: \"" + s + "\"");
            sb.AppendLine();
            sb.AppendLine("## Scene categories (era)");
            sb.AppendLine();
            var cats = Enum.GetValues(typeof(SceneCategory)).Cast<SceneCategory>().ToList();
            sb.AppendLine("| Seed | " + string.Join(" | ", cats) + " | Routed total | Longest routed run | Longest run, all scenes | Longest quiet stretch (months) |");
            sb.AppendLine("|---|" + string.Concat(cats.Select(_ => "---|")) + "---|---|---|---|");
            foreach (var r in results)
                sb.AppendLine("| " + r.Seed + " | " + string.Join(" | ", cats.Select(c => (r.Routed.TryGetValue(c, out int n) ? n : 0) + " (" + (r.AllScenes.TryGetValue(c, out int m) ? m : 0) + ")")) +
                              " | " + r.Routed.Values.Sum() + " | " + r.LongestRoutedRun + " | " + r.LongestSceneRun + " " + r.LongestRunCategory + " | " + r.LongestQuietStretch + " |");
            sb.AppendLine();
            sb.AppendLine("Category cells: routed optional scenes (all meaningful scenes, including the player's own work and interruptions, in brackets).");
            return sb.ToString();
        }

        private static string Kinds(List<string> echoes) =>
            echoes.Count == 0 ? "none" : string.Join(", ", echoes.GroupBy(e => e.Split(':')[0]).Select(g => g.Key + " " + g.Count()));
    }
}
