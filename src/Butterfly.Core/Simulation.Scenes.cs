using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// The P1 scene router in play (Corey, locked; implemented 2026-10-04). Each month every system offers its optional
    /// meaningful scenes as candidates: a commission's encounter, an inviter's next invitation, a Grand Challenge whose
    /// question the work has raised, a person's offscreen development, Roman life, the machine mystery. The router picks at
    /// most <c>scenes.optionalPerMonth</c> of them. A category that has had two scenes in a row is left out while any other
    /// category could happen; only if nothing else can is it allowed, and then at the deprioritized weight. The player's
    /// explicit focus lifts that for its category. World interruptions (illness, fire, Rome's dated events, the plague) are
    /// never routed or delayed: they happen and count for pacing. A candidate not picked waits for a later month.
    /// </summary>
    public sealed partial class Simulation
    {
        private sealed class RoutedScene
        {
            public SceneCandidate Candidate { get; }
            public Action Fire { get; }
            public RoutedScene(string id, SceneCategory category, double weight, Action fire)
            {
                Candidate = new SceneCandidate(id, category, weight);
                Fire = fire;
            }
        }

        /// <summary>Stay with one kind of scene (P1: the player may explicitly remain focused), or null to let pacing run.</summary>
        public CommandResult SetSceneFocus(string category)
        {
            string c = (category ?? "").Trim();
            if (c.Length == 0 || c.Equals("off", StringComparison.OrdinalIgnoreCase) || c.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                World.SceneFocus = null;
                return CommandResult.Success("No focus: the month brings whatever comes.");
            }
            if (!Enum.TryParse<SceneCategory>(c, true, out var cat))
                return CommandResult.Fail("Focus on one of: " + string.Join(", ", Enum.GetNames(typeof(SceneCategory))) + " (or 'focus off').");
            World.SceneFocus = cat;
            return CommandResult.Success("You keep your mind on " + cat + " for now; the world can still interrupt.");
        }

        /// <summary>Every optional scene that could happen this month, in a fixed order (deterministic).</summary>
        private List<RoutedScene> SceneCandidates()
        {
            double w = T.Get("scenes.baseWeight");
            // Scenes that move the game on (work, invitations, challenges, people's lives) outweigh texture, so a rich Rome
            // doesn't starve the plot; texture still wins most months when nothing is waiting (P1-11, revised 2026-10-04).
            double pw = T.Get("scenes.progressionWeight");
            var list = new List<RoutedScene>();
            foreach (var c in CommissionsDue().ToList())
                list.Add(new RoutedScene("commission:" + c.Id, CommissionDefOf(c).Encounter.Category, Aged(pw, "commission:" + c.Id), () => OpenCommission(c)));
            foreach (var (d, offer) in InvitationOffersDue().ToList())
                list.Add(new RoutedScene("invitation:" + d.Institution, OfferCategory(d, offer), Aged(pw, "invitation:" + d.Institution), () => OfferInvitation(d, offer)));
            foreach (var c in ChallengesDue().ToList())
            {
                var route = OpeningRoute(c)!;
                list.Add(new RoutedScene("challenge:" + c.Id, route.Category, Aged(pw, "challenge:" + c.Id), () => OpenChallenge(c, route)));
            }
            foreach (var id in World.ReadyLife.ToList())
            {
                var e = Data.Content.Lives.First(l => l.Id == id);
                list.Add(new RoutedScene("life:" + id, e.Category, Aged(pw, "life:" + id), () => Happen(e, PersonOf(e.Person)!)));
            }
            foreach (var s in Data.Content.Scenes.Where(s => !s.Interrupt && !World.ScenesSeen.Contains(s.Id) && Rested(s) && s.Requires.All(Holds)))
                list.Add(new RoutedScene("scene:" + s.Id, s.Category, s.Weight > 0 ? s.Weight : w, () => PlayAuthoredScene(s)));
            return list;
        }

        /// <summary>At the start of each month: the router picks the optional scenes this month brings.</summary>
        private void RouteScenes()
        {
            // Critical authored moments interrupt: they happen the month they become possible, outside the router.
            foreach (var s in Data.Content.Scenes.Where(s => s.Interrupt && !World.ScenesSeen.Contains(s.Id) && s.Requires.All(Holds)).ToList())
                PlayAuthoredScene(s);
            NoteEligibleRoutes();
            int slots = T.GetInt("scenes.optionalPerMonth");
            for (int n = 0; n < slots; n++)
            {
                var candidates = SceneCandidates();
                TrackWaiting(candidates);
                var chosen = ChooseScene(candidates);
                if (chosen == null) return;
                World.RoutedScenes.Add((Turn, chosen.Candidate.Category, chosen.Candidate.Id));
                World.CandidateSince.Remove(chosen.Candidate.Id);
                chosen.Fire();
            }
        }

        /// <summary>
        /// A progression candidate's weight, raised by <c>scenes.progressionAgePerMonth</c> for each month it has waited
        /// (P1 polish pass: the long waits came from a crowded pool, not from category exclusion). At the default 0 the
        /// weight is exactly the progression weight, so routing is unchanged until Corey decides (PROPOSED P1-23).
        /// </summary>
        private double Aged(double weight, string id)
        {
            double perMonth = T.Get("scenes.progressionAgePerMonth");
            if (perMonth <= 0 || !World.CandidateSince.TryGetValue(id, out int since)) return weight;
            return weight * (1 + perMonth * (Turn - since));
        }

        /// <summary>Remembers when each progression candidate began waiting; forgets those no longer waiting.</summary>
        private void TrackWaiting(List<RoutedScene> candidates)
        {
            var waiting = candidates.Select(c => c.Candidate.Id).Where(id => !id.StartsWith("scene:", StringComparison.Ordinal)).ToList();
            foreach (var id in World.CandidateSince.Keys.Where(k => !waiting.Contains(k)).ToList()) World.CandidateSince.Remove(id);
            foreach (var id in waiting) if (!World.CandidateSince.ContainsKey(id)) World.CandidateSince[id] = Turn;
        }

        /// <summary>
        /// The routing rule: drop a category that would make a third scene in a row whenever another category is available
        /// (unless the player is focused on it); then a seeded weighted pick (SceneRouter, which still deprioritizes it if
        /// it is all there is).
        /// </summary>
        /// <param name="rng">Draw from this instead of the game's generator (a preview passes a copy).</param>
        private RoutedScene? ChooseScene(List<RoutedScene> candidates, Rng? rng = null)
        {
            if (candidates.Count == 0) return null;
            var pacing = World.ScenePacing;
            var focus = World.SceneFocus;
            var fresh = candidates.Where(c => !pacing.ShouldDeprioritize(c.Candidate.Category, focus == c.Candidate.Category)).ToList();
            var pool = fresh.Count > 0 ? fresh : candidates;
            var picked = Scenes.Choose(pool.Select(c => c.Candidate), pacing, focus, recordChoice: false, rng: rng);
            return picked == null ? null : pool.First(c => c.Candidate.Id == picked.Id);
        }

        /// <summary>
        /// What the router would pick from the candidates as they stand, without firing it. Read-only: it draws from a copy of
        /// the generator, so previewing never changes what actually happens (P1 correctness pass, 2026-10-04).
        /// </summary>
        internal string? PeekRoutedScene() => ChooseScene(SceneCandidates(), Rng.Clone())?.Candidate.Id;

        public IReadOnlyList<string> SceneCandidateIds() => SceneCandidates().Select(c => c.Candidate.Id).ToList();

        /// <summary>A reusable scene comes back only after its cooldown.</summary>
        private bool Rested(AuthoredSceneDef s) =>
            !World.SceneLastTurn.TryGetValue(s.Id, out int last) || Turn - last >= s.CooldownMonths;

        private void PlayAuthoredScene(AuthoredSceneDef s)
        {
            // Reusable scenes play their text, then each variant once; when the words run out the scene is done.
            World.ScenePlays.TryGetValue(s.Id, out int plays);
            string text = plays == 0 ? s.Text : s.Variants[plays - 1];
            World.ScenePlays[s.Id] = plays + 1;
            World.SceneLastTurn[s.Id] = Turn;
            if (plays >= s.Variants.Count) World.ScenesSeen.Add(s.Id);
            World.ScenePacing.Record(s.Category);
            var effects = new List<Effect>();
            var actors = new List<string> { "player" };
            foreach (var kv in s.Regard)
            {
                var p = PersonOf(kv.Key) ?? throw new FormatException("Scene " + s.Id + " names an unknown person: " + kv.Key);
                effects.Add(new Effect("person." + kv.Key + ".regard", p.Regard, p.Regard + kv.Value));
                p.Regard += kv.Value;
                actors.Add(kv.Key);
            }
            foreach (var kv in s.StatusChanges)
            {
                var p = PersonOf(kv.Key) ?? throw new FormatException("Scene " + s.Id + " names an unknown person: " + kv.Key);
                p.Status = kv.Value;
                if (!actors.Contains(kv.Key)) actors.Add(kv.Key);
            }
            if (s.Gold != 0)
            {
                double before = World.Gold, amount = Priced(s.Gold);
                World.Gold = Math.Max(0, World.Gold + amount);
                effects.Add(new Effect(GoldKey, before, World.Gold));
            }
            var played = Record("scene." + s.Category.ToString().ToLowerInvariant(), "scene:" + s.Id, null, actors, effects.Count > 0 ? effects : null, text);
            if (s.DelaysChallenge.Length > 0 && FindChallenge(s.DelaysChallenge) is ChallengeState ch && ch.Status == ChallengeStatus.Working)
            {
                int before = ch.MonthsLeft;
                ch.MonthsLeft += s.DelayMonths;
                Record("challenge.delayed", ch.ProjectId, new[] { played.Id }, actors, new[] { new Effect(ch.ProjectId + ".monthsLeft", before, ch.MonthsLeft) },
                    NextStage(ch)!.Name + " slips " + s.DelayMonths + " month" + (s.DelayMonths == 1 ? "" : "s") + ".");
            }
            if (s.Triggers.Length > 0) World.TriggeredEvents.Add(s.Triggers);
            if (s.KnowsInstitution.Length > 0)
            {
                var access = World.AccessTo(s.KnowsInstitution);
                var before = access.Stage;
                access.RecordMemberRelationship(s.KnowsMember);
                if (access.Stage != before)
                    Record("institution.access", s.KnowsInstitution, new[] { played.Id }, new[] { "player", s.KnowsMember },
                        new[] { new Effect("access." + s.KnowsInstitution, (int)before, (int)access.Stage) },
                        "You know " + s.KnowsMember + ", a member of " + World.Institution(s.KnowsInstitution).Def.Name + ".");
            }
        }
    }
}
