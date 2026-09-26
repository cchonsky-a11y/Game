using System.Linq;

namespace Butterfly.Core
{
    /// <summary>The Index (SYSTEMS §12): per-domain sub-scores against history, combined by geometric mean.</summary>
    public sealed partial class Simulation
    {
        public double SubScore(Domain d) => Formulas.SubScore(World[d].Level, Benchmark(d, Now.YearFraction));

        /// <summary>Sphere Index for the single P0 region.</summary>
        public double SphereIndex() => Formulas.GeometricMean(DomainInfo.All.Select(SubScore));
    }
}
