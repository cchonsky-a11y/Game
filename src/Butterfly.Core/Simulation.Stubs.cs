using System.Collections.Generic;

namespace Butterfly.Core
{
    /// <summary>Hooks filled in by later slices (attention, institutions).</summary>
    public sealed partial class Simulation
    {
        internal CommandResult? CheckAttention(int amount) => null;
        internal void SpendAttention(int amount) { }
        private void OnProjectStarted(ActiveProject p) { }
        public bool IsAway => false;
        public bool PromiseKept() => false;
    }
}
