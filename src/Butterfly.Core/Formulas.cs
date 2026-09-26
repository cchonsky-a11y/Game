using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Pure formulas from SYSTEMS.md, kept separate so each can be unit-tested
    /// against the reference values in BUILD_GUIDE.md §8.
    /// </summary>
    public static class Formulas
    {
        /// <summary>SYSTEMS §6: expectation = max(historical benchmark, recent peak fading over time).</summary>
        public static double Expectation(double benchmark, double fadingPeak) => Math.Max(benchmark, fadingPeak);

        /// <summary>The recent peak fades by a fixed amount per year but never below the current level.</summary>
        public static double FadePeak(double peak, double level, double fadePerYear) => Math.Max(level, peak - fadePerYear);

        /// <summary>SYSTEMS §6: yearly accrual = max(0, expectation − level) × rate.</summary>
        public static double DebtAccrual(double expectation, double level, double rate) => Math.Max(0, expectation - level) * rate;

        /// <summary>SYSTEMS §6: existing debt compounds, then this year's accrual is added.</summary>
        public static double DebtStep(double debt, double accrual, double compoundRate) => debt * (1 + compoundRate) + accrual;

        /// <summary>SYSTEMS §6 tiers by debt thresholds (thresholds are tuning values).</summary>
        public static DebtTier Tier(double debt, double strained, double fragile, double critical)
        {
            if (debt >= critical) return DebtTier.Critical;
            if (debt >= fragile) return DebtTier.Fragile;
            if (debt >= strained) return DebtTier.Strained;
            return DebtTier.Stable;
        }

        /// <summary>SYSTEMS §6: paying down debt costs 1.5× the gold prevention would have cost.</summary>
        public static double PaydownCost(double debtPoints, double preventionGoldPerPoint, double multiplier) =>
            debtPoints * preventionGoldPerPoint * multiplier;

        /// <summary>SYSTEMS §7: strength after <paramref name="decades"/> decades at <paramref name="ratePerDecade"/> decay.</summary>
        public static double Decay(double strength, double ratePerDecade, int decades) =>
            strength * Math.Pow(1 - ratePerDecade, decades);

        /// <summary>SYSTEMS §12: sub-score = world value ÷ historical value × 100.</summary>
        public static double SubScore(double worldValue, double historicalValue) => worldValue / historicalValue * 100.0;

        /// <summary>SYSTEMS §12: overall Index = geometric mean of sub-scores.</summary>
        public static double GeometricMean(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count == 0) throw new ArgumentException("No values", nameof(values));
            double logSum = 0;
            foreach (var v in list) logSum += Math.Log(Math.Max(v, 1e-9));
            return Math.Exp(logSum / list.Count);
        }

        /// <summary>Piecewise-linear interpolation over (years, values); clamps outside the range.</summary>
        public static double Interpolate(double[] years, double[] values, double year)
        {
            if (years.Length != values.Length || years.Length == 0) throw new ArgumentException("Curve mismatch");
            if (year <= years[0]) return values[0];
            for (int i = 1; i < years.Length; i++)
            {
                if (year <= years[i])
                {
                    double f = (year - years[i - 1]) / (years[i] - years[i - 1]);
                    return values[i - 1] + f * (values[i] - values[i - 1]);
                }
            }
            return values[values.Length - 1];
        }
    }
}
