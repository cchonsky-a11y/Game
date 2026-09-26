using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Butterfly.Core
{
    /// <summary>A state delta recorded on an event: key, value before, value after.</summary>
    public sealed class Effect
    {
        public string Key { get; }
        public double Before { get; }
        public double After { get; }

        public Effect(string key, double before, double after)
        {
            Key = key;
            Before = before;
            After = after;
        }

        public double Delta => After - Before;

        public override string ToString() =>
            Key + " " + Fmt(Before) + " -> " + Fmt(After);

        internal static string Fmt(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// One meaningful state change (SYSTEMS.md §1): id, time, type, target,
    /// immediate causes (event ids), actors, effects, plus human-readable text.
    /// </summary>
    public sealed class GameEvent
    {
        public int Id { get; }
        public SimTime Time { get; }
        public string Type { get; }
        public string Target { get; }
        public IReadOnlyList<int> ImmediateCauses { get; }
        public IReadOnlyList<string> Actors { get; }
        public IReadOnlyList<Effect> Effects { get; }
        public string Text { get; }

        public GameEvent(int id, SimTime time, string type, string target,
            IEnumerable<int> causes, IEnumerable<string> actors, IEnumerable<Effect> effects, string text)
        {
            Id = id;
            Time = time;
            Type = type;
            Target = target;
            // Sorted and de-duplicated so the log never depends on collection order.
            ImmediateCauses = causes.Where(c => c > 0).Distinct().OrderBy(c => c).ToList();
            Actors = actors.ToList();
            Effects = effects.ToList();
            Text = text;
        }

        /// <summary>Canonical single-line form used for hashing and debugging.</summary>
        public string Canonical()
        {
            var sb = new StringBuilder();
            sb.Append(Id).Append('|').Append(Time.Stamp).Append('|').Append(Type).Append('|').Append(Target).Append('|');
            sb.Append(string.Join(",", ImmediateCauses.Select(c => c.ToString(CultureInfo.InvariantCulture)))).Append('|');
            sb.Append(string.Join(",", Actors)).Append('|');
            sb.Append(string.Join(";", Effects.Select(e => e.ToString()))).Append('|');
            sb.Append(Text);
            return sb.ToString();
        }
    }

    /// <summary>Append-only event log. Its hash is the determinism fingerprint of a run.</summary>
    public sealed class EventLog
    {
        private readonly List<GameEvent> _events = new List<GameEvent>();

        public IReadOnlyList<GameEvent> Events => _events;

        public int NextId => _events.Count + 1;

        public GameEvent Record(SimTime time, string type, string target,
            IEnumerable<int>? causes, IEnumerable<string>? actors, IEnumerable<Effect>? effects, string text)
        {
            var e = new GameEvent(NextId, time, type, target,
                causes ?? Enumerable.Empty<int>(),
                actors ?? Enumerable.Empty<string>(),
                effects ?? Enumerable.Empty<Effect>(),
                text);
            _events.Add(e);
            return e;
        }

        public GameEvent Get(int id) => _events[id - 1];

        /// <summary>SHA-256 over the canonical form of every event, as lowercase hex.</summary>
        public string Hash()
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(string.Join("\n", _events.Select(e => e.Canonical())));
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }
}
