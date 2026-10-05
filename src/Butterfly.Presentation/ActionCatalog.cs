using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Butterfly.Core;

namespace Butterfly.Presentation
{
    /// <summary>One thing the player can do now: what it says, and the ordinary command it runs.</summary>
    public sealed class GameAction
    {
        public string Label { get; }
        /// <summary>The command, in the console's grammar, so a game played by clicks replays exactly like one typed out.</summary>
        public string Command { get; }

        public GameAction(string label, string command)
        {
            Label = label;
            Command = command;
        }

        public override string ToString() => Label + " → " + Command;
    }

    /// <summary>A group of actions: one of the eight sections, and the place in Rome where it happens (for the map).</summary>
    public sealed class ActionGroup
    {
        public string Key { get; }
        public string Title { get; }
        public MenuSection Section { get; }
        /// <summary>A <see cref="RomeMap"/> place id, or "" where the group belongs to no one place.</summary>
        public string Place { get; }
        public List<GameAction> Items { get; } = new List<GameAction>();

        public ActionGroup(string key, string title, MenuSection section, string place)
        {
            Key = key;
            Title = title;
            Section = section;
            Place = place;
        }

        internal void Add(string label, string command) => Items.Add(new GameAction(label, command));
    }

    /// <summary>
    /// What the player can do right now, grouped (P2, 2026-10-05). This is the console's action menu (decided 2026-09-28,
    /// Corey; P1 one choice at a time), moved here so the console and the graphical client offer exactly the same actions
    /// from one place. It decides only what to offer; the simulation decides what happens.
    /// </summary>
    public static class ActionCatalog
    {
        internal static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        /// <summary>The groups for the game as it stands: the era, or after an arrival.</summary>
        /// <param name="jumpArmed">The client has shown the departure briefing (the console's armed jump).</param>
        public static List<ActionGroup> For(Simulation sim, bool jumpArmed) => sim.Arrived ? Arrival(sim, jumpArmed) : Era(sim);

        public static List<ActionGroup> Arrival(Simulation sim, bool jumpArmed)
        {
            var look = new ActionGroup("return", "The return · places to look", MenuSection.Journal, "");
            var ret = sim.World.Return;
            if (ret != null)
            {
                for (int k = 0; k < ret.Sites.Count; k++)
                {
                    var s = ret.Sites[k];
                    if (!ret.Visited.Contains(s.Id)) look.Add(s.Place, "visit " + (k + 1));
                    else if (!ret.Investigated.Contains(s.Id)) look.Add("look closer: " + s.Lead, "look closer " + (k + 1));
                }
                if (sim.World.Journal.Count > 0) look.Add("your journal", "journal");
                if (sim.ReturnCanComplete) look.Add("done looking", "done");
            }
            var walk = new ActionGroup("walk", "Walk around", MenuSection.Civilization, "");
            foreach (var place in new[] { "market", "changers", "forges", "curia", "subura" }) walk.Add(place, "visit " + place);
            var more = new ActionGroup("then", "Then", MenuSection.Machine, RomeMap.Lodging);
            more.Add("learn more (the Index and your institutions)", "learn more");
            if (sim.CanJumpAgain)
            {
                double over = Math.Floor(sim.World.Aurei - sim.CarryAurei);
                if (jumpArmed && over >= 1)
                {
                    more.Add("deposit " + sim.AureiText(over) + " with the bank", "deposit " + F(over));
                    more.Add("bury " + sim.AureiText(over), "bury " + F(over));
                }
                more.Add(jumpArmed ? "jump (go now)" : "jump again", "jump");
            }
            more.Add("quit", "quit");
            var groups = new List<ActionGroup>();
            if (look.Items.Count > 0) groups.Add(look);
            groups.Add(walk);
            groups.Add(more);
            return groups;
        }

        public static List<ActionGroup> Era(Simulation sim)
        {
            var w = sim.World;
            bool att = w.Attention > 0;
            string M(double gold) => sim.Money(gold);

            // Decisions waiting for you come first.
            var decide = new ActionGroup("decide", "Now · decide", MenuSection.Now, "");
            if (sim.SeededChoiceOpen)
            {
                decide.Add("the fountain", "choose fountain");
                decide.Add("the workshop", "choose workshop");
            }
            if (w.Promise.Status == PromiseStatus.Offered)
            {
                decide.Add("promise Demetria yes", "promise yes");
                decide.Add("promise no", "promise no");
            }
            if (sim.OutbreakAwaitingResponse)
                foreach (var resp in new[] { "quarantine", "hospice", "none" }) decide.Add("plague: " + resp, "respond " + resp);
            if (sim.PendingEvent is EventDef ev)
                foreach (var o in ev.Options)
                    decide.Add(ev.Title.Split(' ').Take(3).Aggregate((a, b) => a + " " + b) + ": " + o.Label + (sim.EventCost(o) > 0 ? " (" + M(sim.EventCost(o)) + ")" : ""), "decide " + o.Id);
            foreach (var i in w.Institutions.Where(x => x.OfferedRank > 0))
            {
                decide.Add("accept office in " + i.Def.ShortName, "office accept " + i.Key);
                decide.Add("decline", "office decline " + i.Key);
            }

            // P1 (Corey, 2026-10-04): generic work is a fallback, not the loop. The menu offers only odd jobs, and only while no
            // commission or Grand Challenge stage is under way; craft and consult stay typed commands (help) for compatibility.
            var work = new ActionGroup("work", "Now · get by", MenuSection.Projects, RomeMap.Market);
            bool realWork = w.Commissions.Any(c => c.Status == CommissionStatus.Working) || w.Challenges.Any(c => c.Status == ChallengeStatus.Working);
            if (att && w.PersonalActionTurn != sim.Turn && !realWork && sim.WorkAttention("odd") <= w.Attention)
                work.Add("odd jobs to get by (" + M(sim.WorkPay("odd")) + ", " + sim.WorkAttention("odd") + " Att.)", "work odd");

            var shop = new ActionGroup("shop", "Projects · workshop", MenuSection.Projects, RomeMap.Forges);
            if (att && sim.OwnsWorkshop)
            {
                if (sim.OrdersLeftThisSeason > 0)
                    foreach (var o in sim.OrderBoard().Where(o => o.Attention <= w.Attention)) shop.Add("take " + o.Id + " (" + M(sim.OrderPay(o)) + ")", "take " + o.Id);
                if (w.Apprentices < sim.CurrentSize.ApprenticeMax) shop.Add("hire an apprentice (" + M(sim.ApprenticeWage()) + "/yr)", "apprentice hire");
                if (sim.ExpandBlocker() == null) shop.Add("expand to " + sim.NextSize!.Name + " (" + M(sim.ExpandCost(sim.NextSize)) + ")", "expand");
            }

            var machine = new ActionGroup("machine", "Machine", MenuSection.Machine, RomeMap.Lodging);
            if (att && !sim.MachineAssessed && !w.ActiveMachineSteps.Any()) machine.Add("assess it", "assess");
            if (att && sim.MachineAssessed)
            {
                foreach (var system in Simulation.MachineSystems)
                {
                    var step = sim.NextMachineStep(system);
                    if (step != null && w.ActiveMachineSteps.All(a => a.Def.System != system) && sim.MachineStepGold(step) <= w.Gold)
                        machine.Add("repair " + system + " (" + M(sim.MachineStepGold(step)) + ")", "repair " + system);
                }
                if (sim.MachineStepsDone >= sim.MachineStepsTotal)
                    foreach (var up in sim.Data.Content.MachineUpgrades.Where(u => !w.MachineDone.Contains(u.Id) && w.ActiveMachineSteps.All(a => a.Def.Id != u.Id)))
                        machine.Add("upgrade " + up.Id, "upgrade " + up.Id);
            }
            if (att && sim.CanListen) machine.Add("open the reference channel", "listen");
            double missing = sim.MachineGoldNeeded - sim.MachineGoldRestored;
            if (missing >= 1)
            {
                if (w.Aurei >= 1 && sim.MachineAssessed) machine.Add("put " + sim.AureiText(Math.Min(Math.Floor(w.Aurei), Math.Ceiling(missing))) + " back in", "restore " + F(Math.Min(Math.Floor(w.Aurei), Math.Ceiling(missing))));
                double buy = Math.Ceiling(missing - w.Aurei);
                if (att && buy >= 1 && sim.AureiCost(buy) <= w.Gold)
                    machine.Add("buy the " + sim.AureiText(buy) + " it still needs (" + M(sim.AureiCost(buy)) + ")", "exchange " + F(Math.Ceiling(sim.Denarii(sim.AureiCost(buy)))) + " denarii");
            }

            var money = new ActionGroup("money", "Now · money changers", MenuSection.Now, RomeMap.Changers);
            if (att && w.Aurei >= 1 && missing < 1 || att && w.Aurei >= 1 && sim.MachineStepsDone < sim.MachineStepsTotal)
            {
                if (w.Aurei >= 10) money.Add("change 10 aurei", "exchange 10 aurei");
                money.Add("change all " + sim.AureiText(Math.Floor(w.Aurei)), "exchange " + F(Math.Floor(w.Aurei)) + " aurei");
            }

            var projects = new ActionGroup("projects", "Projects · public works", MenuSection.Projects, RomeMap.Curia);
            if (att)
                foreach (var p in sim.AvailableProjects().Where(p => sim.ProjectAuthorityBlocker(p) == null && sim.ProjectGold(p) <= w.Gold && p.AttentionPerTurn <= w.Attention))
                    projects.Add(p.Id + " (" + M(sim.ProjectGold(p)) + ")", "start " + p.Id);

            var inventions = new ActionGroup("inventions", "Knowledge · practical projects", MenuSection.Knowledge, RomeMap.Lodging);
            if (att && w.ActiveInventions.Count == 0)
                foreach (var inv in sim.Data.Content.Inventions.Where(x => sim.InventionState(x) == "ready" && sim.InventionGold(x) <= w.Gold))
                    inventions.Add(inv.Id + " (" + M(sim.InventionGold(inv)) + ")", "invent " + inv.Id);

            var commissions = new ActionGroup("commissions", "Projects · commissions", MenuSection.Projects, RomeMap.Market);
            foreach (var c in sim.OpenCommissions())
            {
                var d = sim.CommissionDefOf(c);
                if (c.Status == CommissionStatus.Offered && w.Attention >= d.Diagnosis.Attention) commissions.Add("look at " + d.Client + "'s problem (unpaid)", "commission look " + d.Id);
                if (c.Status == CommissionStatus.TermsOffered)
                {
                    if (w.Attention >= d.Work[0].Attention) commissions.Add("accept " + d.Client + "'s terms", "commission accept " + d.Id);
                    if (!c.Countered) commissions.Add("ask " + d.Client + " for more", "commission counter " + d.Id);
                    commissions.Add("decline " + d.Client + "'s work", "commission decline " + d.Id);
                }
            }

            var challenges = new ActionGroup("challenges", "Projects · grand challenge", MenuSection.Projects, RomeMap.Forges);
            foreach (var c in w.Challenges.Where(c => c.Status == ChallengeStatus.Open))
            {
                var s = sim.NextStage(c)!;
                if (sim.StageBlocker(s) == null && sim.StageGold(s) <= w.Gold && s.Attention <= w.Attention)
                    challenges.Add(s.Name.ToLowerInvariant() + " (" + M(sim.StageGold(s)) + ")", "challenge begin " + c.Id);
            }

            var invitations = new ActionGroup("invitations", "Institutions · invitations", MenuSection.Institutions, RomeMap.Curia);
            foreach (var p in w.Invitations.Where(p => p.Pending != InvitationOffer.None))
            {
                var d = sim.InvitationPathDefFor(p.Institution)!;
                invitations.Add("accept " + d.Inviter + "'s invitation", "invitation accept " + d.Institution);
                invitations.Add("decline " + d.Inviter + "'s invitation", "invitation decline " + d.Institution);
            }

            var inst = new ActionGroup("institutions", "Institutions", MenuSection.Institutions, RomeMap.Curia);
            if (att)
            {
                foreach (var i in w.Institutions.Where(x => !x.Def.IsOwn && x.Exists))
                {
                    if (!i.Backed && sim.OnInvitationPath(i)) continue;   // P1: by invitation only, nothing to buy
                    if (sim.PatronageOnly(i) && !i.Backed) continue;      // P1: clients come by introduction, not purchase; no members yet
                    if (sim.TakesGifts(i))
                    {
                        if (sim.JoinBlocker(i) == null && w.Attention >= sim.T.GetInt("stakes.buyAttention") && sim.GiftCost(i) + (i.Backed ? 0 : sim.EntryFee(i)) <= w.Gold) inst.Add("give the " + i.Def.ShortName.Replace("the ", "") + " a gift (" + M(sim.GiftCost(i)) + ")", "give " + i.Key);
                        if (i.Backed && i.AttendedTurn != sim.Turn)
                            for (int c = 0; c < 2; c++) inst.Add("attend " + i.Key + ", vote " + sim.CampName(i, c), "attend " + i.Key + " " + i.Def.DriftPaths[c].Id);
                        continue;
                    }
                    if (!i.Backed)
                    {
                        if (sim.AccessRefusal(i) != null) continue;
                        int first = i.Def.JoinRequirement == "deposit" ? sim.T.GetInt("joining.bankMinFirstPercent") : 1;
                        if (sim.JoinBlocker(i) == null && sim.BuyCost(i, first) <= w.Gold) inst.Add("buy into " + i.Key + ", " + first + "% (" + M(sim.BuyCost(i, first)) + ")", "buy " + i.Key + " " + first);
                        continue;
                    }
                    if (sim.AccessRefusal(i) == null)   // only shares really for sale (the bank); the simulation decides
                    {
                        if (sim.BuyCost(i, 1) <= w.Gold) inst.Add(i.Key + " +1% (" + M(sim.BuyCost(i, 1)) + ")", "buy " + i.Key + " 1");
                        if (sim.BuyCost(i, 5) <= w.Gold) inst.Add(i.Key + " +5% (" + M(sim.BuyCost(i, 5)) + ")", "buy " + i.Key + " 5");
                    }
                    if (i.AttendedTurn != sim.Turn)
                        for (int c = 0; c < 2; c++) inst.Add("attend " + i.Key + ", vote " + sim.CampName(i, c), "attend " + i.Key + " " + i.Def.DriftPaths[c].Id);
                }
                foreach (var own in w.Institutions.Where(x => x.Def.IsOwn))
                {
                    if (!own.Exists && !own.Collapsed && sim.FoundCost(own.Def.Maintains) <= w.Gold)
                        inst.Add("found " + own.Key + " (" + M(sim.FoundCost(own.Def.Maintains)) + ")", "found " + own.Key);
                    if (own.Exists && sim.Controls(own) && w.Gold >= 40)
                        inst.Add("invest " + M(Math.Floor(w.Gold / 4)) + " in " + own.Key, "invest " + own.Key + " " + F(Math.Floor(sim.Denarii(Math.Floor(w.Gold / 4)))));
                }
                foreach (var i in w.Institutions.Where(x => x.Exists && sim.Controls(x)))
                {
                    inst.Add("oversee " + i.Key, "oversee " + i.Key);
                    if (!i.Chartered) inst.Add("charter " + i.Key, "charter " + i.Key);
                }
            }

            var policy = new ActionGroup("policy", sim.PolicyHold() ? "Civilization · policy" : "Civilization · advocate (no voice yet; " + M(sim.AdvocacyCost()) + ", 2 Att. each)", MenuSection.Civilization, RomeMap.Curia);
            if (att && (sim.PolicyHold() || sim.AdvocacyCost() <= w.Gold))
            {
                string verb = sim.PolicyHold() ? "policy" : "advocate";
                foreach (var issue in Simulation.Issues)
                    foreach (int stance in new[] { 1, -1, 0 })
                        if (sim.Stance(issue) != stance)
                        {
                            string word = stance == 0 ? "history" : Simulation.StanceWord(issue, stance);
                            policy.Add(issue.ToString().ToLowerInvariant() + " " + word, verb + " " + issue.ToString().ToLowerInvariant() + " " + word);
                        }
            }

            var priorities = new ActionGroup("priorities", "Civilization · priorities", MenuSection.Civilization, RomeMap.Curia);
            if (att)
                foreach (var d in DomainInfo.All.Where(sim.HasHold))
                    foreach (var p in new[] { "protect", "maintain", "accept" })
                        if (DomainInfo.TryParsePriority(p, out var pr) && w[d].Priority != pr)
                            priorities.Add(d.ToString().ToLowerInvariant() + " " + p, "priority " + d.ToString().ToLowerInvariant() + " " + p);

            var leave = new ActionGroup("leave", "Machine · before you leave", MenuSection.Machine, RomeMap.Lodging);
            if (sim.MachineReady)
            {
                if (w.Aurei >= 1) leave.Add("deposit " + sim.AureiText(Math.Floor(w.Aurei)) + " with the bank", "deposit " + F(Math.Floor(w.Aurei)));
                if (w.Aurei >= 1) leave.Add("bury " + sim.AureiText(Math.Floor(w.Aurei)), "bury " + F(Math.Floor(w.Aurei)));
                leave.Add("jump", "jump");
            }

            var look = new ActionGroup("look", "Journal · look (free)", MenuSection.Journal, "");
            foreach (var (label, cmd) in new[] { ("status", "status"), ("news", "news"), ("people", "people"), ("ledger", "ledger"), ("sections", "view"), ("institutions", "institutions"), ("machine", "machine"), ("workshop", "workshop"), ("inventions", "inventions"), ("projects", "projects"), ("help", "help") })
                if (cmd != "workshop" || sim.OwnsWorkshop) look.Add(label, cmd);

            var turn = new ActionGroup("month", "Now · month", MenuSection.Now, "");
            turn.Add("End Month", "end");
            turn.Add("fast-forward until something needs you", "wait");

            // Grouped by the eight P1 sections (Now, Projects, People, Institutions, Knowledge, Civilization, Machine, Journal).
            return new List<ActionGroup> { decide, money, commissions, challenges, work, shop, projects, invitations, inst, inventions, policy, priorities, machine, leave, look, turn };
        }
    }
}
