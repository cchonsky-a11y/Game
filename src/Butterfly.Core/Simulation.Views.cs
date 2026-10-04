using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>One line in a section view: what it is, and the command that acts on it (empty if none).</summary>
    public sealed class ViewItem
    {
        public string Label { get; }
        public string Command { get; }
        public ViewItem(string label, string command = "") { Label = label; Command = command; }
    }

    /// <summary>
    /// A P1 menu section (decided 2026-10-02; master handoff §3): what is active, available now, blocked (and why), emerging,
    /// and archived. View models only; the console and a later Unity UI render them.
    /// </summary>
    public sealed class SectionView
    {
        public MenuSection Section { get; }
        public List<ViewItem> Active { get; } = new List<ViewItem>();
        public List<ViewItem> AvailableNow { get; } = new List<ViewItem>();
        public List<ViewItem> Blocked { get; } = new List<ViewItem>();
        public List<ViewItem> Emerging { get; } = new List<ViewItem>();
        public List<ViewItem> Archived { get; } = new List<ViewItem>();
        public SectionView(MenuSection section) => Section = section;
        public int Count => Active.Count + AvailableNow.Count + Blocked.Count + Emerging.Count + Archived.Count;
    }

    public sealed partial class Simulation
    {
        /// <summary>The view of one of the eight menu sections, from the current state of the game.</summary>
        public SectionView ViewOf(MenuSection section)
        {
            var v = new SectionView(section);
            switch (section)
            {
                case MenuSection.Now: NowView(v); break;
                case MenuSection.Projects: ProjectsView(v); break;
                case MenuSection.People: PeopleView(v); break;
                case MenuSection.Institutions: InstitutionsView(v); break;
                case MenuSection.Knowledge: KnowledgeView(v); break;
                case MenuSection.Civilization: CivilizationView(v); break;
                case MenuSection.Machine: MachineView(v); break;
                case MenuSection.Journal: JournalView(v); break;
            }
            return v;
        }

        private void NowView(SectionView v)
        {
            foreach (var part in ReservedAttentionParts()) v.Active.Add(new ViewItem(part.What + " (" + part.Attention + " Attention reserved)"));
            foreach (var d in PendingDecisions()) v.AvailableNow.Add(new ViewItem(d));
            v.AvailableNow.Add(new ViewItem("End Month", "end"));
            if (World.Attention == 0) v.Blocked.Add(new ViewItem("No Attention left this month; End Month when you're ready."));
        }

        private void ProjectsView(SectionView v)
        {
            foreach (var p in World.ActiveProjects) v.Active.Add(new ViewItem(p.Def.Name + " (" + p.TurnsRemaining + " month(s) left)", "why " + p.Def.Id));
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Working))
            {
                var d = CommissionDefOf(c);
                v.Active.Add(new ViewItem(d.Title + " for " + d.Client + ": " + StageLabel(d.Work[c.WorkIndex].Stage) + ", " + c.MonthsLeftInStage + " month(s) left", "commission"));
            }
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Offered || c.Status == CommissionStatus.TermsOffered))
            {
                var d = CommissionDefOf(c);
                v.AvailableNow.Add(c.Status == CommissionStatus.Offered
                    ? new ViewItem(d.Client + "'s problem: look at it (unpaid)", "commission look " + d.Id)
                    : new ViewItem(d.Client + "'s terms " + TermsLine(c), "commission accept " + d.Id));
            }
            foreach (var p in AvailableProjects())
            {
                string? why = ProjectAuthorityBlocker(p) ?? (ProjectGold(p) > World.Gold ? "needs " + Money(ProjectGold(p)) : null);
                if (why == null) v.AvailableNow.Add(new ViewItem(p.Name + " (" + Money(ProjectGold(p)) + ")", "start " + p.Id));
                else v.Blocked.Add(new ViewItem(p.Name + ": " + why));
            }
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.NotYet))
                if (Knows(CommissionDefOf(c).Introducer)) v.Emerging.Add(new ViewItem(CommissionDefOf(c).Introducer + " may bring work"));
            foreach (var c in World.Challenges)
            {
                var d = ChallengeDefOf(c);
                if (c.Status == ChallengeStatus.Working) v.Active.Add(new ViewItem(d.Name + ": " + NextStage(c)!.Name + ", " + c.MonthsLeft + " month(s) left", "challenge"));
                else if (c.Status == ChallengeStatus.Open)
                {
                    var s = NextStage(c)!;
                    if (StageBlocker(s) == null && StageGold(s) <= World.Gold) v.AvailableNow.Add(new ViewItem(d.Name + ": " + StageLine(s), "challenge begin " + d.Id));
                    else v.Blocked.Add(new ViewItem(d.Name + ": " + StageLine(s)));
                }
                else if (c.Status == ChallengeStatus.Done || c.Status == ChallengeStatus.Abandoned) v.Archived.Add(new ViewItem(d.Name + " (" + c.Status.ToString().ToLowerInvariant() + ")"));
            }
            foreach (var id in World.CompletedProjects) v.Archived.Add(new ViewItem(Data.Content.Projects.FirstOrDefault(p => p.Id == id)?.Name ?? id));
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Done || c.Status == CommissionStatus.Declined || c.Status == CommissionStatus.Walked || c.Status == CommissionStatus.Abandoned))
                v.Archived.Add(new ViewItem(CommissionDefOf(c).Title + " (" + c.Status.ToString().ToLowerInvariant() + ")"));
        }

        private void PeopleView(SectionView v)
        {
            foreach (var d in KnownPeople())
            {
                var p = PersonOf(d.Id)!;
                var item = new ViewItem(d.Name + ", " + d.Role + ": " + p.Status, "people");
                if (IsPersonAway(d.Id)) v.Blocked.Add(new ViewItem(d.Name + " is laid up or away for now"));
                else v.Active.Add(item);
            }
            foreach (var path in World.Invitations.Where(p => p.Pending != InvitationOffer.None))
            {
                var d = InvitationPathDefFor(path.Institution)!;
                v.AvailableNow.Add(new ViewItem(d.Inviter + "'s invitation", "invitation accept " + d.Institution));
            }
        }

        private void InstitutionsView(SectionView v)
        {
            foreach (var i in World.Institutions.Where(i => i.Exists))
            {
                var a = World.AccessTo(i.Key);
                string name = Cap(i.Def.Name);
                if (i.Backed) { v.Active.Add(new ViewItem(name + (i.Rank > Member ? ", " + OfficeTitle(i, i.Rank) : ", member"), "institutions")); continue; }
                if (OnInvitationPath(i))
                {
                    if (InvitationState(i.Key)!.Pending != InvitationOffer.None) v.AvailableNow.Add(new ViewItem(name + ": " + InvitationPathDefFor(i.Key)!.Inviter + " has invited you", "invitation accept " + i.Key));
                    else if (a.Stage >= InstitutionAccessStage.KnowsMember) v.Emerging.Add(new ViewItem(name + ": " + a.Stage + " (by invitation)"));
                    else v.Blocked.Add(new ViewItem(name + ": by invitation only; you know no one there"));
                    continue;
                }
                if (i.Def.IsOwn) { if (FoundCost(i.Def.Maintains) <= World.Gold) v.AvailableNow.Add(new ViewItem("found " + name, "found " + i.Key)); continue; }
                var blocker = JoinBlocker(i);
                if (blocker == null) v.AvailableNow.Add(new ViewItem(name, "buy " + i.Key + " 1"));
                else v.Blocked.Add(new ViewItem(name + ": " + blocker));
            }
            foreach (var i in World.Institutions.Where(i => i.Collapsed)) v.Archived.Add(new ViewItem(Cap(i.Def.Name) + " (collapsed)"));
        }

        private void KnowledgeView(SectionView v)
        {
            foreach (var a in World.ActiveInventions) v.Active.Add(new ViewItem(a.Def.Name + " (" + a.TurnsRemaining + " month(s) left)"));
            foreach (var inv in Data.Content.Inventions)
            {
                string state = InventionState(inv);
                if (state == "made") v.Archived.Add(new ViewItem(inv.Name));
                else if (state == "ready") v.AvailableNow.Add(new ViewItem(inv.Name + " (" + Money(InventionGold(inv)) + ")", "invent " + inv.Id));
                else if (state.StartsWith("needs")) v.Emerging.Add(new ViewItem(inv.Name + ": " + state));
                else if (!state.StartsWith("under way")) v.Blocked.Add(new ViewItem(inv.Name + ": " + state));
            }
        }

        /// <summary>What Rome can do: the capabilities your work has touched, and the next steps beside them (hidden network, shown where relevant).</summary>
        private void CivilizationView(SectionView v)
        {
            foreach (var c in World.Capabilities.Where(c => c.Level > CapabilityLevel.None))
                v.Active.Add(new ViewItem(Cap(CapabilityDefOf(c.Id)!.Name) + ": " + c.Level.ToString().ToLowerInvariant() + (c.Spread >= CapabilitySpread.Copied ? ", copied" + (c.Distorted ? ", badly" : "") : "")));
            var touched = new HashSet<string>(World.Capabilities.Where(c => c.Level > CapabilityLevel.None).Select(c => c.Id));
            foreach (var def in Data.Content.Capabilities.Where(d => !touched.Contains(d.Id) && d.Prerequisites.Any(touched.Contains)))
            {
                var blocker = CapabilityBlocker(def.Id, CapabilityLevel.Prototype);
                v.Emerging.Add(new ViewItem(Cap(def.Name) + ": " + def.Bottleneck + (blocker == null ? "" : " " + blocker)));
            }
            foreach (var d in DomainInfo.All) v.Active.Add(new ViewItem(d + " " + F(World[d].Level) + " (" + World[d].Tier + ")", "why " + d.ToString().ToLowerInvariant()));
        }

        private void MachineView(SectionView v)
        {
            foreach (var a in World.ActiveMachineSteps) v.Active.Add(new ViewItem(a.Def.Name + " (" + a.TurnsRemaining + " month(s) left)"));
            if (!MachineAssessed && World.ActiveMachineSteps.Count == 0) v.AvailableNow.Add(new ViewItem("assess the machine", "assess"));
            foreach (var system in MachineSystems)
            {
                var step = NextMachineStep(system);
                if (step == null) continue;
                if (MachineStepGold(step) <= World.Gold) v.AvailableNow.Add(new ViewItem(step.Name + " (" + Money(MachineStepGold(step)) + ")", "repair " + system));
                else v.Blocked.Add(new ViewItem(step.Name + ": needs " + Money(MachineStepGold(step))));
            }
            if (MachineReady) v.AvailableNow.Add(new ViewItem("The machine is ready. You can leave now, or remain in Rome and continue your work.", "jump"));
            foreach (var id in World.MachineDone) v.Archived.Add(new ViewItem(Data.Content.MachineSteps.FirstOrDefault(s => s.Id == id)?.Name ?? id));
        }

        private void JournalView(SectionView v)
        {
            var notable = new HashSet<string> { "project.complete", "commission.complete", "institution.join", "person.life", "plague.outbreak", "seeded.choice", "invention.complete", "jump.arrive" };
            foreach (var e in Log.Events.Where(e => notable.Contains(e.Type)).Reverse().Take(12)) v.Archived.Add(new ViewItem(e.Time.Display + ": " + e.Text));
            v.AvailableNow.Add(new ViewItem("ledger", "ledger"));
            v.AvailableNow.Add(new ViewItem("news", "news"));
        }
    }
}
