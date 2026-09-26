using System.Collections.Generic;

namespace Butterfly.Core
{
    /// <summary>Hooks filled in by later slices (attention, institutions).</summary>
    public sealed partial class Simulation
    {
        internal CommandResult? CheckAttention(int amount) => null;
        internal void SpendAttention(int amount) { }
        private void OnProjectStarted(ActiveProject p) { }
        private double InstitutionUpkeepTotal() => 0;
        private void InstitutionUpkeepShortfall() { }
        private void ApplyInstitutionExtra(ProjectDef def, ProjectExtra x, int causeId) { }
        public bool IsAway => false;
        private bool HospiceAvailable() => false;
        private string AutomaticResponse() => "none";
        private double InstitutionPlagueResilience() => 0;
        private void OnPlagueResolved(int tollEventId) { }
    }
}
