using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Every tuning value, loaded from data/tuning.json (CLAUDE.md rule 2).
    /// Each leaf is { "value": ..., "ref": "SYSTEMS §n" | "PROPOSED P0-xx" } so every number
    /// traces to SYSTEMS.md or to a pending proposal in docs/P0_PROPOSALS.md.
    /// Keys are dotted paths, e.g. "debt.compoundRate".
    /// </summary>
    public sealed class Tuning
    {
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>();
        private readonly Dictionary<string, string> _refs = new Dictionary<string, string>();
        private readonly List<string> _keys = new List<string>();

        public IReadOnlyList<string> Keys => _keys;

        public static Tuning Parse(string json)
        {
            var t = new Tuning();
            t.Flatten("", (JsonObject)Json.Parse(json)!);
            return t;
        }

        private void Flatten(string prefix, JsonObject obj)
        {
            foreach (var key in obj.Keys)
            {
                if (key.StartsWith("_")) continue; // comments
                string path = prefix.Length == 0 ? key : prefix + "." + key;
                var child = obj[key] as JsonObject;
                if (child == null)
                    throw new FormatException("Tuning leaf '" + path + "' must be an object with 'value' and 'ref'.");
                if (child.Has("value"))
                {
                    _keys.Add(path);
                    _values[path] = child["value"];
                    _refs[path] = child.StrOr("ref", "") ?? "";
                }
                else
                {
                    Flatten(path, child);
                }
            }
        }

        public bool Has(string key) => _values.ContainsKey(key);

        public string Ref(string key) => _refs[key];

        public double Get(string key)
        {
            if (!_values.TryGetValue(key, out var v)) throw new KeyNotFoundException("Missing tuning key: " + key);
            return (double)v!;
        }

        public int GetInt(string key) => (int)Math.Round(Get(key));

        public bool GetBool(string key)
        {
            if (!_values.TryGetValue(key, out var v)) throw new KeyNotFoundException("Missing tuning key: " + key);
            return (bool)v!;
        }

        public double[] GetArray(string key)
        {
            if (!_values.TryGetValue(key, out var v)) throw new KeyNotFoundException("Missing tuning key: " + key);
            return ((List<object?>)v!).Select(x => (double)x!).ToArray();
        }
    }
}
