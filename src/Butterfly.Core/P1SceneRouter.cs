using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>A meaningful scene that may be selected by the P1 scene router.</summary>
    public sealed class SceneCandidate
    {
        public string Id { get; }
        public SceneCategory Category { get; }
        public double BaseWeight { get; }

        public SceneCandidate(string id, SceneCategory category, double baseWeight = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Scene id is required.", nameof(id));
            if (baseWeight < 0) throw new ArgumentOutOfRangeException(nameof(baseWeight));

            Id = id;
            Category = category;
            BaseWeight = baseWeight;
        }
    }

    /// <summary>
    /// Deterministic weighted scene selection for P1. Repeating a category beyond the soft cap is strongly
    /// deprioritized unless the player explicitly chose to remain focused. Interrupt scenes are never penalized
    /// by category repetition because the world must remain able to intrude on a focused player.
    /// </summary>
    public sealed class SceneRouter
    {
        /// <summary>Weight left to a scene that would exceed the soft cap (tuning: scenes.repeatedCategoryWeight).</summary>
        public double RepeatedCategoryWeightMultiplier { get; }

        private readonly Rng _rng;

        public SceneRouter(Rng rng, double repeatedCategoryWeightMultiplier)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (repeatedCategoryWeightMultiplier < 0 || repeatedCategoryWeightMultiplier > 1) throw new ArgumentOutOfRangeException(nameof(repeatedCategoryWeightMultiplier));
            RepeatedCategoryWeightMultiplier = repeatedCategoryWeightMultiplier;
        }

        public static SceneRouter FromTuning(Rng rng, Tuning t) => new SceneRouter(rng, t.Get("scenes.repeatedCategoryWeight"));

        public SceneCandidate? Choose(
            IEnumerable<SceneCandidate> candidates,
            ScenePacingState pacing,
            SceneCategory? explicitlyFocusedCategory = null,
            bool recordChoice = true,
            Rng? rng = null)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (pacing == null) throw new ArgumentNullException(nameof(pacing));

            var weighted = candidates
                .Where(c => c.BaseWeight > 0)
                .Select(c => new WeightedScene(c, EffectiveWeight(c, pacing, explicitlyFocusedCategory)))
                .Where(c => c.Weight > 0)
                .ToList();

            if (weighted.Count == 0) return null;

            double total = weighted.Sum(c => c.Weight);
            double roll = (rng ?? _rng).NextDouble() * total;
            double cursor = 0;

            foreach (var item in weighted)
            {
                cursor += item.Weight;
                if (roll < cursor)
                {
                    if (recordChoice) pacing.Record(item.Scene.Category);
                    return item.Scene;
                }
            }

            // Floating-point guard: the final candidate owns the closed upper edge.
            var last = weighted[weighted.Count - 1].Scene;
            if (recordChoice) pacing.Record(last.Category);
            return last;
        }

        public double EffectiveWeight(
            SceneCandidate candidate,
            ScenePacingState pacing,
            SceneCategory? explicitlyFocusedCategory = null)
        {
            if (candidate.BaseWeight <= 0) return 0;

            bool explicitFocus = explicitlyFocusedCategory == candidate.Category;
            return pacing.ShouldDeprioritize(candidate.Category, explicitFocus)
                ? candidate.BaseWeight * RepeatedCategoryWeightMultiplier
                : candidate.BaseWeight;
        }

        private sealed class WeightedScene
        {
            public SceneCandidate Scene { get; }
            public double Weight { get; }

            public WeightedScene(SceneCandidate scene, double weight)
            {
                Scene = scene;
                Weight = weight;
            }
        }
    }
}
