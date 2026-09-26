using System;

namespace Butterfly.Core
{
    /// <summary>The three P0 care domains (PROTOTYPE_SCOPE.md). Declaration order is iteration order.</summary>
    public enum Domain
    {
        Medicine,
        Governance,
        Economy
    }

    /// <summary>Per-domain priority (SYSTEMS.md §5).</summary>
    public enum Priority
    {
        Protect,
        Maintain,
        AcceptRisk
    }

    /// <summary>Debt tiers (SYSTEMS.md §6).</summary>
    public enum DebtTier
    {
        Stable,
        Strained,
        Fragile,
        Critical
    }

    public static class DomainInfo
    {
        public static readonly Domain[] All = { Domain.Medicine, Domain.Governance, Domain.Economy };

        /// <summary>Lowercase key used in tuning.json, content, logs and console commands.</summary>
        public static string Key(this Domain d) => d.ToString().ToLowerInvariant();

        public static string Key(this Priority p)
        {
            switch (p)
            {
                case Priority.Protect: return "protect";
                case Priority.Maintain: return "maintain";
                default: return "acceptRisk";
            }
        }

        public static string Label(this Priority p) => p == Priority.AcceptRisk ? "Accept Risk" : p.ToString();

        public static bool TryParseDomain(string text, out Domain domain)
        {
            foreach (var d in All)
            {
                if (d.Key().StartsWith(text.ToLowerInvariant(), StringComparison.Ordinal) && text.Length >= 3)
                {
                    domain = d;
                    return true;
                }
            }
            domain = Domain.Medicine;
            return false;
        }

        public static bool TryParsePriority(string text, out Priority priority)
        {
            switch (text.ToLowerInvariant())
            {
                case "protect": priority = Priority.Protect; return true;
                case "maintain": priority = Priority.Maintain; return true;
                case "accept":
                case "acceptrisk":
                case "risk": priority = Priority.AcceptRisk; return true;
            }
            priority = Priority.Maintain;
            return false;
        }
    }

    /// <summary>State of one care domain in the region.</summary>
    public sealed class DomainState
    {
        public Domain Domain { get; }
        public double Level { get; set; }
        /// <summary>The region's recent peak, fading over time (SYSTEMS.md §6).</summary>
        public double Peak { get; set; }
        public double Debt { get; set; }
        public Priority Priority { get; set; } = Priority.Maintain;
        public DebtTier Tier { get; set; } = DebtTier.Stable;

        public DomainState(Domain domain, double level)
        {
            Domain = domain;
            Level = level;
            Peak = level;
        }
    }
}
