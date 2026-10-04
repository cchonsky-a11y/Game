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
        /// <summary>
        /// Scripted strategy profiles (hardening pass, 2026-10-04). Scripted profiles of the build's own API, not people:
        /// Legacy = the original validation script (asks for more on every third seed).
        /// </summary>
        public enum Profile { Legacy, Cooperative, Negotiator, Selective, Engineering, Relationship }

        public sealed class Result
        {
            public ulong Seed;
            public Profile Profile;
            public int FreeAttention0, FreeAttention1, FreeAttention2Plus, IdleAttention;
            public int StageAttentionRefusals, ProgressionPendingMonths, LongestProgressionWait, QuietMonths;
            public int UpgradesDone, RangeMin, RangeMax, Range2Min, Range2Max, Countered;
            public int ProfitSharePayments;
            public string LongestWaitId = "";
            public string StandardsRoute = "";
            public string ArchiveBy = "";
            public Dictionary<string, int> Waits = new Dictionary<string, int>();
            public double OddJobIncome, CommissionIncome, OrderIncome;
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
            // The first return (2026-10-04): measured, never optimized by the profiles.
            public bool ReturnStarted, ReturnCompleted;
            public int ReturnSites, ReturnVisited, ReturnMisattributed;
            public List<string> ReturnCategories = new List<string>(), ReturnBands = new List<string>(), ReturnThreads = new List<string>();
            public int JournalWritten, JournalSites, SitesMin;
            /// <summary>Return gate failures: the second jump offered before the return was seen.</summary>
            public int Bugs;
        }

        /// <summary>
        /// The scripted runners' minimum return protocol (infrastructure, not a strategy): visit the first sites in the order the
        /// return lists them until the requirement is met, then finish. No profile chooses or optimizes what it looks at.
        /// </summary>
        public static void FollowReturnProtocol(Simulation sim)
        {
            var r = sim.World.Return;
            if (r == null || r.Completed) return;
            for (int k = 1; k <= r.Sites.Count && !sim.ReturnCanComplete; k++) sim.VisitReturnSite(k.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sim.CompleteReturn();
        }

        private static void Count(Dictionary<SceneCategory, int> d, SceneCategory c) => d[c] = d.TryGetValue(c, out int n) ? n + 1 : 1;

        public static Result Play(GameData data, ulong seed, int departYear = 163) => Play(data, seed, Profile.Legacy, seed % 2 == 1 ? "workshop" : "fountain", departYear);

        public static Result Play(GameData data, ulong seed, Profile profile, string opening, int departYear = 163) =>
            Play(data, seed, profile, opening, departYear, null);

        /// <summary>
        /// A readable reference return for one seed (scripted, not human): the first life in brief, what was written in the
        /// journal, the departure briefing, the arrival, every return site visited and looked into, then finishing the return.
        /// </summary>
        public static string ReferenceReturn(GameData data, ulong seed, Profile profile = Profile.Legacy)
        {
            var sb = new StringBuilder();
            Play(data, seed, profile, seed % 2 == 1 ? "workshop" : "fountain", 163, sb);
            return sb.ToString();
        }

        private static Result Play(GameData data, ulong seed, Profile profile, string opening, int departYear, StringBuilder? story)
        {
            var sim = new Simulation(data, seed);
            var r = new Result { Seed = seed, Profile = profile };
            bool counter = profile == Profile.Negotiator || profile == Profile.Legacy && seed % 3 == 0;
            void Note(CommandResult res) { if (!res.Ok && res.Message.Contains("would reserve")) r.AttentionConflicts++; }
            int quiet = 0, lastScenes = 0;
            var waiting = new Dictionary<string, int>();
            double startGold = sim.World.Gold;
            r.Choice = opening;
            sim.ChooseSeeded(r.Choice);
            if (profile == Profile.Engineering) sim.SetSceneFocus("Engineering");
            if (profile == Profile.Relationship) sim.SetSceneFocus("RomanLife");
            while (!sim.EraOver)
            {
                var w = sim.World;
                if (w.Gold < 2 && w.Aurei >= 5 && sim.MachineGoldRestored < 1) sim.SellAurei(5);
                if (sim.PendingEvent is EventDef ev)
                    sim.Decide(ev.Options[profile == Profile.Relationship ? 0 : (int)(seed % (ulong)ev.Options.Count)].Id);
                foreach (var c in w.Commissions.ToList())
                {
                    if (c.Status == CommissionStatus.Offered) { if (r.FirstCommissionTurn < 0) r.FirstCommissionTurn = sim.Turn; Note(sim.LookAtCommission(c.Id)); }
                    if (c.Status == CommissionStatus.TermsOffered)
                    {
                        var d = sim.CommissionDefOf(c);
                        bool decline = profile == Profile.Selective && (d.FundingModel != ProjectFundingModel.ClientPaid || sim.ReservedInMonth(1) >= 3)
                                    || profile == Profile.Engineering && (d.Encounter.Category == SceneCategory.RomanLife || d.Encounter.Category == SceneCategory.CityHistory
                                                                          || d.Encounter.Category == SceneCategory.Personal);
                        if (decline) { sim.DeclineCommission(c.Id); continue; }
                        if (counter && !c.Countered) { sim.CounterCommission(c.Id); r.Countered++; }
                        if (c.Status == CommissionStatus.TermsOffered) Note(sim.AcceptCommission(c.Id));
                    }
                }
                foreach (var p in w.Invitations.Where(p => p.Pending != InvitationOffer.None).ToList()) sim.AcceptInvitation(p.Institution);
                bool busy = w.Commissions.Any(c => c.Status == CommissionStatus.Working);
                foreach (var ch in w.Challenges.Where(ch => ch.Status == ChallengeStatus.Open).ToList())
                {
                    if (profile == Profile.Relationship && busy) continue;                             // people and work first
                    if (profile == Profile.Selective && sim.NextStage(ch) is ChallengeStageDef st && w.Gold < 2 * sim.StageGold(st)) continue;
                    var res = sim.StartChallengeStage(ch.Id);
                    Note(res);
                    if (!res.Ok && res.Message.Contains("Attention")) r.StageAttentionRefusals++;
                }
                if (!sim.MachineAssessed) Note(sim.Assess());
                foreach (var system in Simulation.MachineSystems) Note(sim.Repair(system));
                if (profile == Profile.Engineering && sim.MachineAssessed && w.Gold > sim.Priced(60))
                    foreach (var u in data.Content.MachineUpgrades) if (!w.MachineDone.Contains(u.Id)) { Note(sim.Upgrade(u.Id)); break; }
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
                // Attention left unused as the month ends (after everything the script wanted to do).
                if (w.Attention <= 0) r.FreeAttention0++; else if (w.Attention == 1) r.FreeAttention1++; else r.FreeAttention2Plus++;
                r.IdleAttention += Math.Max(0, w.Attention);
                // Progression candidates waiting for the router: how long each waits before it is picked.
                var pending = sim.SceneCandidateIds().Where(id => !id.StartsWith("scene:")).ToList();
                if (pending.Count > 0) r.ProgressionPendingMonths++;
                foreach (var id in pending) waiting[id] = waiting.TryGetValue(id, out int n) ? n + 1 : 1;
                int routedBefore = w.RoutedScenes.Count;
                sim.EndMonth();
                r.EraMonths++;
                foreach (var picked in w.RoutedScenes.Skip(routedBefore))
                    if (waiting.TryGetValue(picked.Id, out int waited))
                    {
                        if (waited > r.LongestProgressionWait) { r.LongestProgressionWait = waited; r.LongestWaitId = picked.Id; }
                        r.Waits[picked.Id] = Math.Max(r.Waits.TryGetValue(picked.Id, out int w0) ? w0 : 0, waited);
                        waiting.Remove(picked.Id);
                    }
                int scenes = sim.World.ScenePacing.History.Count;
                quiet = scenes == lastScenes ? quiet + 1 : 0;
                if (quiet > 0) r.QuietMonths++;
                r.LongestQuietStretch = Math.Max(r.LongestQuietStretch, quiet);
                lastScenes = scenes;
            }
            // Income by source, from the log's gold effects (the ledger is built from the same events).
            foreach (var kv in waiting) r.Waits["never:" + kv.Key] = kv.Value;   // still waiting at departure
            foreach (var e in sim.Log.Events)
            {
                double gain = e.Effects.Where(f => f.Key == "gold").Sum(f => f.After - f.Before);
                if (gain <= 0) continue;
                if (e.Type == "personal.work") r.OddJobIncome += gain;
                else if (e.Type.StartsWith("workshop.")) r.OrderIncome += gain;
                else if (e.Type.StartsWith("commission.")) r.CommissionIncome += gain;
            }
            r.ProfitSharePayments = sim.Log.Events.Count(e => e.Type == "commission.share");
            r.UpgradesDone = sim.MachineUpgradesDone;
            (r.RangeMin, r.RangeMax) = sim.JumpRange();
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
            r.StandardsRoute = sim.FindChallenge("standards")?.OpenedBy ?? "";
            var archives = data.Content.MachineSteps.FirstOrDefault(m => m.Id == "archives");
            if (archives != null)
                r.ArchiveBy = sim.Log.Events.Any(e => e.Text.Contains(archives.Text)) ? "note" : sim.Log.Events.Any(e => e.Text.Contains(archives.AltDoneText)) ? "bribe" : "";
            r.LifeEvents = sim.World.LifeEventLog.Count;
            r.PeopleKnown = sim.KnownPeople().Count();
            var texts = sim.Log.Events.Where(e => e.Type.StartsWith("scene.") || e.Type.StartsWith("person.") || e.Type.StartsWith("commission.") ||
                                                  e.Type.StartsWith("invitation.") || e.Type.StartsWith("challenge.") || e.Type == "event.offer")
                                      .Select(e => e.Text).ToList();
            r.DuplicateTexts = texts.Count - texts.Distinct().Count();
            r.DuplicateList = texts.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key.Substring(0, Math.Min(90, g.Key.Length))).ToList();
            if (!sim.MachineReady) return r;
            r.Depart1 = sim.Now.Year;
            if (story != null) StoryBeforeJump(sim, story, seed, profile);
            var a1 = sim.Jump();
            if (story != null) StoryReturn(sim, a1, story);
            r.Arrive1 = a1.ArrivalYear; r.Jump1 = a1.JumpYears; r.Echoes1 = a1.P1Echoes.ToList();
            r.Institutions1 = string.Join("; ", a1.Institutions.Select(i => i.Name.Replace("the ", "") + " " + i.Outcome));
            var ret = sim.World.Return;
            r.JournalWritten = sim.World.Journal.Count;
            if (ret != null)
            {
                r.ReturnStarted = true;
                r.SitesMin = sim.T.GetInt("return.sitesMin");
                r.ReturnSites = ret.Sites.Count;
                r.ReturnCategories = ret.Sites.Select(s => s.Category.ToString()).ToList();
                r.ReturnBands = ret.Sites.Where(s => s.Band != null).Select(s => s.Person + " " + s.Band.ToString()!.ToLowerInvariant()).ToList();
                r.ReturnThreads = ret.Sites.Where(s => s.Thread.Length > 0).Select(s => s.Thread + ":" + s.Variant).ToList();
                r.ReturnMisattributed = ret.Sites.Count(s => s.Misattributed);
                r.JournalSites = ret.Sites.Count(s => s.Category == ReturnCategory.Journal);
                if (sim.CanJumpAgain) r.Bugs++;                                      // the gate must hold before the return is seen
                FollowReturnProtocol(sim);
                r.ReturnVisited = ret.Visited.Count;
                r.ReturnCompleted = ret.Completed;
            }
            if (!sim.CanJumpAgain) return r;
            (r.Range2Min, r.Range2Max) = sim.JumpRange();
            var a2 = sim.Jump();
            r.Arrive2 = a2.ArrivalYear; r.Jump2 = a2.JumpYears; r.Echoes2 = a2.P1Echoes.ToList();
            r.Institutions2 = string.Join("; ", a2.Institutions.Select(i => i.Name.Replace("the ", "") + " " + i.Outcome));
            var s1 = Sentences(a1); var s2 = Sentences(a2);
            r.RepeatedSentences = s2.Where(s1.Contains).ToList();
            r.RepeatedArrivalSentences = r.RepeatedSentences.Count;
            return r;
        }

        private static void StoryBeforeJump(Simulation sim, StringBuilder sb, ulong seed, Profile profile)
        {
            sb.AppendLine("# Reference first return: seed " + seed + " (" + profile + " profile, " + (sim.World.SeededChoice ?? "no") + " opening)");
            sb.AppendLine();
            sb.AppendLine("**Scripted, not human.** A fixed scripted player lives the first life; this file shows what its return holds, for inspection. It is not evidence that the return is engaging.");
            sb.AppendLine();
            sb.AppendLine("## The first life (AD 155–" + sim.Now.Year + ")");
            sb.AppendLine();
            sb.AppendLine("- People known: " + string.Join(", ", sim.KnownPeople().Select(p => p.Name)) + ".");
            sb.AppendLine("- Work done: " + string.Join(", ", sim.World.Commissions.Where(c => c.Status == CommissionStatus.Done).Select(c => sim.CommissionDefOf(c).Title)) + ".");
            sb.AppendLine("- Grand Challenges: " + string.Join("; ", sim.World.Challenges.Select(c => sim.ChallengeDefOf(c).Name + " " + c.Status.ToString().ToLowerInvariant() + " (stage " + c.StageIndex + ")")) + ".");
            sb.AppendLine("- Institutions: " + string.Join("; ", sim.World.Institutions.Where(i => !i.Def.IsOwn).Select(i => i.Def.ShortName + " " + sim.World.AccessTo(i.Key).Stage)) + ".");
            sb.AppendLine("- Life events: " + string.Join(", ", sim.World.LifeEventLog.Keys) + ".");
            sb.AppendLine();
            sb.AppendLine("## The journal, as written");
            sb.AppendLine();
            foreach (var l in sim.JournalLines()) sb.AppendLine("- " + l);
            if (sim.World.Journal.Count == 0) sb.AppendLine("- (nothing)");
            sb.AppendLine();
            sb.AppendLine("## The machine is ready: the departure briefing");
            sb.AppendLine();
            foreach (var l in sim.DepartureBriefing()) sb.AppendLine("- " + l);
            sb.AppendLine();
        }

        private static void StoryReturn(Simulation sim, Arrival a, StringBuilder sb)
        {
            sb.AppendLine("## Arrival: AD " + a.ArrivalYear + " (" + a.JumpYears + " years)");
            sb.AppendLine();
            foreach (var b in a.Beats) { sb.AppendLine("**" + b.Name + ".** " + b.Text); sb.AppendLine(); }
            var ret = sim.World.Return!;
            sb.AppendLine("## The return: places to look");
            sb.AppendLine();
            foreach (var l in sim.ReturnLeads()) sb.AppendLine("- " + l);
            sb.AppendLine();
            sb.AppendLine("Second jump allowed before looking: " + (sim.CanJumpAgain ? "YES (gate failure)" : "no") + ".");
            sb.AppendLine();
            for (int k = 1; k <= ret.Sites.Count; k++)
            {
                var s = ret.Sites[k - 1];
                string key = k.ToString(System.Globalization.CultureInfo.InvariantCulture);
                sb.AppendLine("### " + k + ". " + s.Place);
                sb.AppendLine();
                sb.AppendLine("*" + s.Category + (s.Band != null ? ", " + s.Person + ": " + s.Band.ToString()!.ToLowerInvariant() : "") + "; variant " + s.Variant +
                              (s.Thread.Length > 0 ? "; thread " + s.Thread : "") + "; evidence " + s.Evidence + (s.Misattributed ? "; misattributed" : "") +
                              "; grounded in log events " + string.Join(", ", s.Grounds) + " (hidden from the player).*");
                sb.AppendLine();
                sb.AppendLine("> visit " + key);
                sb.AppendLine();
                sb.AppendLine(sim.VisitReturnSite(key).Message);
                sb.AppendLine();
                sb.AppendLine("> look closer " + key);
                sb.AppendLine();
                sb.AppendLine(sim.InvestigateReturnSite(key).Message);
                sb.AppendLine();
                if (k == sim.ReturnVisitsRequired) sb.AppendLine("Second jump allowed now: " + (sim.CanJumpAgain ? "yes" : "no, not until 'done'") + ".\n");
            }
            sb.AppendLine("> done");
            sb.AppendLine();
            sb.AppendLine(sim.CompleteReturn().Message);
            sb.AppendLine();
            sb.AppendLine("Second jump allowed after finishing: " + (sim.CanJumpAgain ? "yes" : "NO (gate failure)") + ".");
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
