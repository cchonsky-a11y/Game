using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// P1 commissions (decided 2026-10-02, Corey): work grows from an encounter, through an unpaid look at the problem, to
    /// terms that say who pays and who buys the materials, then staged work and a paid completion that leads somewhere
    /// (a referral into an institution's circle). Negotiation can fail. Every payment goes through the event log, so the
    /// ledger shows it against the commission's project. Each scene counts for the scene router's pacing.
    /// </summary>
    public sealed partial class Simulation
    {
        public CommissionDef CommissionDefOf(CommissionState c) => Data.Content.Commissions.First(d => d.Id == c.Id);

        /// <summary>A project stage in plain words for the player.</summary>
        public static string StageLabel(ProjectStage s) => s switch
        {
            ProjectStage.Prototype => "making a first one",
            ProjectStage.Failure => "finding what failed",
            ProjectStage.Refinement => "improving it",
            ProjectStage.CraftAdaptation => "teaching the shop to make it",
            ProjectStage.Repeatability => "proving it works every time",
            ProjectStage.Adoption => "putting it to use",
            ProjectStage.Spread => "seeing it spread",
            ProjectStage.Observation => "watching the problem",
            _ => s.ToString().ToLowerInvariant(),
        };

        public CommissionState? FindCommission(string id) =>
            World.Commissions.FirstOrDefault(c => c.Id == (id ?? "").Trim().ToLowerInvariant());

        /// <summary>Commissions the player can see now: offered, with terms on the table, or under way.</summary>
        public IEnumerable<CommissionState> OpenCommissions() =>
            World.Commissions.Where(c => c.Status == CommissionStatus.Offered || c.Status == CommissionStatus.TermsOffered || c.Status == CommissionStatus.Working);

        private void InitCommissions()
        {
            foreach (var d in Data.Content.Commissions) World.Commissions.Add(new CommissionState(d.Id));
        }

        /// <summary>Commissions whose time has come (their introducer about), waiting for the scene router.</summary>
        private IEnumerable<CommissionState> CommissionsDue() =>
            World.Commissions.Where(c => c.Status == CommissionStatus.NotYet).Where(c =>
            {
                var d = CommissionDefOf(c);
                return MonthsSinceStart >= d.OpensAfterMonths && d.Requires.All(Holds) && !(d.Introducer.Length > 0 && IsPersonAway(d.Introducer));
            });

        /// <summary>The encounter: someone brings you the problem.</summary>
        private void OpenCommission(CommissionState c)
        {
            var d = CommissionDefOf(c);
            c.Status = CommissionStatus.Offered;
            Scene(d.Encounter.Category, "commission.encounter", c, d.Encounter.Text + " " + PayLine(d));
        }

        /// <summary>The explicit money rule before the first step: looking is unpaid, and may lead to a paid commission.</summary>
        private string PayLine(CommissionDef d) =>
            "(commission look " + d.Id + ": " + d.Diagnosis.Attention + " Attention · Pay: none initially · may lead to paid commission)";

        /// <summary>The terms, stated before any work: who pays, how much and when, who buys the materials.</summary>
        public string TermsLine(CommissionState c)
        {
            var d = CommissionDefOf(c);
            int months = d.Work.Sum(w => w.DurationMonths);
            int att = d.Work.Max(w => w.Attention);
            string pay = d.FundingModel == ProjectFundingModel.SelfFundedResearch ? "self-funded: no one pays you"
                : d.FundingModel == ProjectFundingModel.Favor ? "a favor: no one pays you"
                : d.Payer + " pays " + (d.Upfront > 0 ? Money(Priced(d.Upfront)) + " now, " : "") + Money(Priced(c.CompletionPay)) + " on completion" +
                  (d.FundingModel == ProjectFundingModel.ProfitShare ? " and a share of what it earns" : "");
            string materials = d.MaterialsPayer == "player" ? "you buy the materials" : d.MaterialsPayer + " buys the materials";
            return "(" + pay + "; " + materials + ". Work: about " + months + " month" + (months == 1 ? "" : "s") + ", " + att + " Attention a month. " +
                   "commission accept " + d.Id + (c.Countered ? "" : " / counter " + d.Id) + " / decline " + d.Id + ")";
        }

        /// <summary>Look at the problem (unpaid): the diagnosis, then the client names terms.</summary>
        public CommandResult LookAtCommission(string id)
        {
            var c = FindCommission(id);
            if (c == null || c.Status != CommissionStatus.Offered) return CommandResult.Fail("No one is waiting for you to look at that.");
            var d = CommissionDefOf(c);
            var attention = CheckAttention(d.Diagnosis.Attention);
            if (attention != null) return attention;
            SpendAttention(d.Diagnosis.Attention);
            c.CompletionPay = d.Completion;
            c.Status = CommissionStatus.TermsOffered;
            var look = Scene(d.Diagnosis.Category, "commission.diagnosis", c, d.Diagnosis.Text);
            Scene(d.TermsScene.Category, "commission.terms", c, d.TermsScene.Text + " " + TermsLine(c), look.Id);
            return CommandResult.Success(d.Diagnosis.Text + "\n" + d.TermsScene.Text + " " + TermsLine(c));
        }

        /// <summary>Push for more. The client may agree, stand firm, or walk away; once only (he remembers).</summary>
        public CommandResult CounterCommission(string id)
        {
            var c = FindCommission(id);
            if (c == null || c.Status != CommissionStatus.TermsOffered) return CommandResult.Fail("There are no terms on the table to argue over.");
            if (c.Countered) return CommandResult.Fail("You've already pushed once; pushing again would cost you the work.");
            var d = CommissionDefOf(c);
            c.Countered = true;
            double total = d.AcceptWeight + d.StandFirmWeight + d.WalkWeight, roll = Rng.NextDouble() * total;
            if (roll < d.AcceptWeight)
            {
                c.CompletionPay = d.CounterCompletion;
                Scene(SceneCategory.WorkEconomy, "commission.counter", c, d.AcceptText + " " + TermsLine(c));
                return CommandResult.Success(d.AcceptText + " " + TermsLine(c));
            }
            if (roll < d.AcceptWeight + d.StandFirmWeight)
            {
                Scene(SceneCategory.WorkEconomy, "commission.counter", c, d.StandFirmText + " " + TermsLine(c));
                return CommandResult.Success(d.StandFirmText + " " + TermsLine(c));
            }
            c.Status = CommissionStatus.Walked;
            Scene(SceneCategory.WorkEconomy, "commission.walk", c, d.WalkText);
            return CommandResult.Success(d.WalkText);
        }

        public CommandResult DeclineCommission(string id)
        {
            var c = FindCommission(id);
            if (c == null || (c.Status != CommissionStatus.Offered && c.Status != CommissionStatus.TermsOffered)) return CommandResult.Fail("There's nothing to decline.");
            var d = CommissionDefOf(c);
            c.Status = CommissionStatus.Declined;
            Scene(SceneCategory.WorkEconomy, "commission.decline", c, d.DeclineText);
            return CommandResult.Success(d.DeclineText);
        }

        /// <summary>Agree the terms: the project starts with them written down, the upfront payment arrives, the first stage begins.</summary>
        public CommandResult AcceptCommission(string id)
        {
            var c = FindCommission(id);
            if (c == null || c.Status != CommissionStatus.TermsOffered) return CommandResult.Fail("No terms are waiting for your answer.");
            var d = CommissionDefOf(c);
            var first = d.Work[0];
            int months = d.Work.Sum(w => w.DurationMonths);
            var attention = CheckAttention(first.Attention)
                ?? CheckFutureAttention(Enumerable.Range(1, Math.Max(0, months - 1)).Select(k => StageAttentionAt(d, 0, first.DurationMonths, k)));
            if (attention != null) return attention;
            SpendAttention(first.Attention);
            var terms = new ProjectTerms(d.FundingModel, d.Payer, d.MaterialsPayer, Priced(d.Upfront), Priced(c.CompletionPay));
            var project = new ProjectState(c.ProjectId, d.Title, "Commission for " + d.Client + ", " + d.ClientRole, d.Client, terms,
                d.Work.Sum(w => w.DurationMonths), ProjectStage.Agreed);
            project.Collaborators.Add(d.Client);
            if (d.Introducer.Length > 0) project.Collaborators.Add(d.Introducer);
            World.Projects.Add(project);
            c.Status = CommissionStatus.Working;
            c.WorkIndex = 0;
            c.MonthsLeftInStage = first.DurationMonths;
            c.ReservedFromTurn = Turn + 1;
            project.SetStage(first.Stage);
            double before = World.Gold;
            World.Gold += terms.UpfrontGold;
            var agreed = Record("commission.agreed", c.ProjectId, null, new[] { "player", d.Client },
                terms.UpfrontGold > 0 ? new[] { new Effect(GoldKey, before, World.Gold) } : null,
                "You and " + d.Client + " agree terms: " + TermsSummary(terms) + (terms.UpfrontGold > 0 ? " " + d.Client + " pays " + Money(terms.UpfrontGold) + " now." : ""));
            return CommandResult.Success("Agreed. " + TermsSummary(terms) + (terms.UpfrontGold > 0 ? " You receive " + Money(terms.UpfrontGold) + " now." : ""));
        }

        private string TermsSummary(ProjectTerms t) =>
            (t.FundingModel == ProjectFundingModel.Favor ? "No one pays: it's a favor" : (t.Payer.Length > 0 ? t.Payer + " pays " : "") + Money(t.UpfrontGold + t.CompletionGold) + " in all") +
            (t.UpfrontGold > 0 ? " (" + Money(t.UpfrontGold) + " now, " + Money(t.CompletionGold) + " on completion)" : "") +
            "; " + (t.MaterialsPayer == "player" ? "you buy" : t.MaterialsPayer + " buys") + " the materials.";

        /// <summary>At the end of each month: the work stage advances; when the last is done, the client pays and refers you on.</summary>
        private void ProgressCommissions()
        {
            // A finished profit share pays its later shares on schedule, each once, while you are in Rome.
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Done && c.SharesLeft > 0 && Turn >= c.NextShareTurn))
            {
                var d = CommissionDefOf(c);
                double amount = Priced(d.ShareAmount), before = World.Gold;
                World.Gold += amount;
                string text = d.ShareTexts[d.SharePayments - c.SharesLeft];
                c.SharesLeft--;
                c.NextShareTurn = Turn + d.ShareEveryMonths;
                Record("commission.share", c.ProjectId, new[] { c.CompletedEventId }, new[] { "player", d.Payer },
                    new[] { new Effect(GoldKey, before, World.Gold) }, text + " (" + Money(amount) + ")");
            }
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Working).ToList())
            {
                var d = CommissionDefOf(c);
                var project = World.Projects.First(p => p.Id == c.ProjectId);
                project.AdvanceMonth();
                if (--c.MonthsLeftInStage > 0) continue;
                var stage = d.Work[c.WorkIndex];
                var done = Scene(stage.Category, "commission.stage", c, stage.Text);
                string cap = stage.Capability.Length > 0 ? stage.Capability : d.Capability;
                var to = stage.CapabilityTo ?? CapabilityLevelFor(stage.Stage);
                if (cap.Length > 0 && to > CapabilityLevel.None)
                    AdvanceCapability(cap, to, new[] { done.Id }, "Rome's " + CapabilityDefOf(cap)!.Name + " now stand at " + to.ToString().ToLowerInvariant() + ".");
                c.WorkIndex++;
                if (c.WorkIndex < d.Work.Count)
                {
                    c.MonthsLeftInStage = d.Work[c.WorkIndex].DurationMonths;
                    project.SetStage(d.Work[c.WorkIndex].Stage);
                    continue;
                }
                Complete(c, d, project, done.Id);
            }
        }

        private void Complete(CommissionState c, CommissionDef d, ProjectState project, int causeId)
        {
            double before = World.Gold;
            World.Gold += project.Terms.CompletionGold;
            project.Complete();
            c.Status = CommissionStatus.Done;
            c.SharesLeft = d.SharePayments;
            c.NextShareTurn = Turn + d.ShareEveryMonths;
            var paid = Record("commission.complete", c.ProjectId, new[] { causeId }, new[] { "player", d.Client },
                project.Terms.CompletionGold > 0 ? new[] { new Effect(GoldKey, before, World.Gold) } : null,
                (project.Terms.CompletionGold > 0 ? d.Client + " pays " + Money(project.Terms.CompletionGold) + ": " : "") + Cap(d.Title) + " is done.");
            c.CompletedEventId = paid.Id;
            // What the work leaves besides money (regard, standing, flags), logged with the payment as cause.
            foreach (var kv in d.OnCompleteRegard)
            {
                var p = PersonOf(kv.Key) ?? throw new FormatException("Commission " + d.Id + " names an unknown person: " + kv.Key);
                int rb = p.Regard;
                p.Regard += kv.Value;
                Record("person.regard", "person." + p.Id, new[] { paid.Id }, new[] { "player", p.Id }, new[] { new Effect("person." + p.Id + ".regard", rb, p.Regard) },
                    Cap(d.Title) + ": " + PersonDefOf(p.Id)!.Name + (kv.Value >= 0 ? " thinks better of you." : " thinks less of you."));
            }
            foreach (var kv in d.OnCompleteStatus) (PersonOf(kv.Key) ?? throw new FormatException("Commission " + d.Id + " names an unknown person: " + kv.Key)).Status = kv.Value;
            foreach (var f in d.OnCompleteSets) World.Flags.Add(f);
            if (d.ReferralInstitution.Length == 0)
            {
                if (d.ReferralText.Length > 0) Scene(SceneCategory.Personal, "commission.referral", c, d.ReferralText, paid.Id);
                return;
            }
            // The referral (P1 institutions): the work earns a relationship with a member, the first step on the invitation path.
            var access = World.Access.FirstOrDefault(a => a.InstitutionId == d.ReferralInstitution);
            if (access != null)
            {
                var before2 = access.Stage;
                access.RecordMemberRelationship(d.ReferralMember);
                Scene(SceneCategory.Personal, "commission.referral", c, d.ReferralText, paid.Id);
                if (access.Stage != before2)
                    Record("institution.access", d.ReferralInstitution, new[] { paid.Id }, new[] { "player", d.ReferralMember },
                        new[] { new Effect("access." + d.ReferralInstitution, (int)before2, (int)access.Stage) },
                        "You know " + d.ReferralMember + ", a member of " + World.Institution(d.ReferralInstitution).Def.Name + ".");
            }
        }

        /// <summary>A commission scene: logged, and counted for the scene router's pacing (two-in-a-row soft cap).</summary>
        private GameEvent Scene(SceneCategory category, string type, CommissionState c, string text, int? cause = null)
        {
            World.ScenePacing.Record(category);
            return Record(type, c.ProjectId, cause.HasValue ? new[] { cause.Value } : null, new[] { "player", CommissionDefOf(c).Client }, null, text);
        }

        /// <summary>Attention the current work stages hold this month (from the month after they were agreed).</summary>
        private int ReservedCommissionAttention() =>
            World.Commissions.Where(c => c.Status == CommissionStatus.Working && Turn >= c.ReservedFromTurn).Sum(c => CommissionDefOf(c).Work[c.WorkIndex].Attention);

        /// <summary>Attention the commissions will hold k months from now: whichever stage is running then.</summary>
        private int CommissionAttentionInMonth(int k) =>
            World.Commissions.Where(c => c.Status == CommissionStatus.Working).Sum(c => StageAttentionAt(CommissionDefOf(c), c.WorkIndex, c.MonthsLeftInStage, k));

        /// <summary>The Attention the stage running k months on holds, from stage <paramref name="index"/> with <paramref name="left"/> months left.</summary>
        private static int StageAttentionAt(CommissionDef d, int index, int left, int k)
        {
            while (k >= left)
            {
                k -= left;
                if (++index >= d.Work.Count) return 0;
                left = d.Work[index].DurationMonths;
            }
            return d.Work[index].Attention;
        }

        private void AbandonCommissionsOnDeparture(int departId)
        {
            // A share still owing stops when you leave: no one carries it to you across the years.
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Done && c.SharesLeft > 0))
            {
                c.SharesLeft = 0;
                Record("commission.share.lapsed", c.ProjectId, new[] { departId }, new[] { "player", CommissionDefOf(c).Payer }, null,
                    CommissionDefOf(c).Payer + "'s share stops when you leave.");
            }
            foreach (var c in World.Commissions.Where(c => c.Status == CommissionStatus.Working || c.Status == CommissionStatus.Offered || c.Status == CommissionStatus.TermsOffered))
            {
                bool working = c.Status == CommissionStatus.Working;
                c.Status = CommissionStatus.Abandoned;
                if (!working) continue;
                World.Projects.First(p => p.Id == c.ProjectId).Abandon();
                Record("commission.abandoned", c.ProjectId, new[] { departId }, new[] { "player" }, null, CommissionDefOf(c).Title + " is left unfinished.");
            }
        }
    }
}
