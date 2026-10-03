using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    public enum LedgerEntryKind
    {
        Payment,
        Expense,
        Materials,
        ProfitShare,
        InstitutionDues,
        GiftOrFavor,
        Adjustment
    }

    /// <summary>One explicit money movement. Positive amounts increase player cash; negative amounts decrease it.</summary>
    public sealed class LedgerEntry
    {
        public string Id { get; }
        public LedgerEntryKind Kind { get; }
        public double Amount { get; }
        public string Counterparty { get; }
        public string Reason { get; }
        public string ProjectId { get; }

        public LedgerEntry(string id, LedgerEntryKind kind, double amount, string counterparty, string reason, string projectId = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Ledger entry id is required.", nameof(id));
            Id = id;
            Kind = kind;
            Amount = amount;
            Counterparty = counterparty ?? string.Empty;
            Reason = reason ?? string.Empty;
            ProjectId = projectId ?? string.Empty;
        }
    }

    /// <summary>
    /// P1 money history. This is intentionally additive beside World.Gold until the legacy economy is migrated.
    /// Every substantial commission/R&amp;D project can explain who paid, who bought materials and what the player received.
    /// </summary>
    public sealed class EconomyLedger
    {
        private readonly List<LedgerEntry> _entries = new List<LedgerEntry>();

        public IReadOnlyList<LedgerEntry> Entries => _entries;
        public double Net => _entries.Sum(e => e.Amount);
        public double Income => _entries.Where(e => e.Amount > 0).Sum(e => e.Amount);
        public double Expenses => -_entries.Where(e => e.Amount < 0).Sum(e => e.Amount);

        public void Record(LedgerEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (_entries.Any(e => e.Id == entry.Id)) throw new InvalidOperationException("Ledger entry ids must be unique.");
            _entries.Add(entry);
        }

        public IReadOnlyList<LedgerEntry> ForProject(string projectId)
        {
            if (projectId == null) projectId = string.Empty;
            return _entries.Where(e => e.ProjectId == projectId).ToList();
        }
    }

    /// <summary>Connects explicit project terms to ledger entries without yet mutating legacy World.Gold.</summary>
    public static class ProjectAccounting
    {
        public static void RecordAgreement(EconomyLedger ledger, ProjectState project)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var terms = project.Terms;
            if (terms.UpfrontGold > 0)
            {
                ledger.Record(new LedgerEntry(
                    project.Id + ":upfront",
                    LedgerEntryKind.Payment,
                    terms.UpfrontGold,
                    terms.Payer,
                    "Upfront project payment.",
                    project.Id));
            }

            if (terms.PlayerMaterialCost > 0)
            {
                ledger.Record(new LedgerEntry(
                    project.Id + ":materials",
                    LedgerEntryKind.Materials,
                    -terms.PlayerMaterialCost,
                    terms.MaterialsPayer,
                    "Player-funded materials.",
                    project.Id));
            }
        }

        public static void RecordCompletion(EconomyLedger ledger, ProjectState project)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (project == null) throw new ArgumentNullException(nameof(project));

            if (project.Terms.CompletionGold <= 0) return;

            ledger.Record(new LedgerEntry(
                project.Id + ":completion",
                LedgerEntryKind.Payment,
                project.Terms.CompletionGold,
                project.Terms.Payer,
                "Project completion payment.",
                project.Id));
        }
    }
}
