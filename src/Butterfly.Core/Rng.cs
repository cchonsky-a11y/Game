using System;

namespace Butterfly.Core
{
    /// <summary>
    /// The simulation's only source of randomness (SYSTEMS.md §1).
    /// SplitMix64: small, fast, and bit-identical on every platform and runtime,
    /// unlike System.Random whose algorithm is not guaranteed across runtimes.
    /// </summary>
    public sealed class Rng
    {
        private ulong _state;

        public Rng(ulong seed)
        {
            _state = seed;
        }

        public ulong NextULong()
        {
            ulong z = unchecked(_state += 0x9E3779B97F4A7C15UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            return z ^ (z >> 31);
        }

        /// <summary>Uniform double in [0, 1), using the top 53 bits.</summary>
        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / 9007199254740992.0);
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong range = (ulong)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextULong() % range);
        }

        /// <summary>True with probability p (clamped to [0, 1]).</summary>
        public bool Chance(double p)
        {
            return NextDouble() < p;
        }
    }
}
