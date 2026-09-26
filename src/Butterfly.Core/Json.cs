using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Butterfly.Core
{
    /// <summary>
    /// Minimal JSON reader so the core has no package dependencies (Unity compatibility).
    /// Objects become <see cref="JsonObject"/> (key order preserved), arrays become List&lt;object?&gt;,
    /// numbers become double, plus string, bool and null.
    /// </summary>
    public static class Json
    {
        public static object? Parse(string text)
        {
            int i = 0;
            object? value = ParseValue(text, ref i);
            SkipWhitespace(text, ref i);
            if (i != text.Length) throw Error(text, i, "unexpected trailing characters");
            return value;
        }

        private static object? ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) throw Error(s, i, "unexpected end of input");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { Expect(s, ref i, "true"); return true; }
            if (c == 'f') { Expect(s, ref i, "false"); return false; }
            if (c == 'n') { Expect(s, ref i, "null"); return null; }
            return ParseNumber(s, ref i);
        }

        private static JsonObject ParseObject(string s, ref int i)
        {
            var obj = new JsonObject();
            i++; // {
            SkipWhitespace(s, ref i);
            if (s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWhitespace(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (s[i] != ':') throw Error(s, i, "expected ':'");
                i++;
                obj.Add(key, ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return obj; }
                throw Error(s, i, "expected ',' or '}'");
            }
        }

        private static List<object?> ParseArray(string s, ref int i)
        {
            var list = new List<object?>();
            i++; // [
            SkipWhitespace(s, ref i);
            if (s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return list; }
                throw Error(s, i, "expected ',' or ']'");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw Error(s, i, "expected string");
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw Error(s, i, "bad escape");
                }
            }
            throw Error(s, i, "unterminated string");
        }

        private static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw Error(s, i, "unexpected character");
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw Error(s, i, "expected " + word);
            i += word.Length;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static FormatException Error(string s, int i, string message)
        {
            return new FormatException("JSON error at offset " + i + ": " + message);
        }
    }

    /// <summary>A JSON object whose keys keep file order (deterministic iteration).</summary>
    public sealed class JsonObject
    {
        private readonly List<string> _keys = new List<string>();
        private readonly Dictionary<string, object?> _values = new Dictionary<string, object?>();

        public IReadOnlyList<string> Keys => _keys;

        public void Add(string key, object? value)
        {
            if (_values.ContainsKey(key)) throw new FormatException("Duplicate JSON key: " + key);
            _keys.Add(key);
            _values[key] = value;
        }

        public bool Has(string key) => _values.ContainsKey(key);

        public object? this[string key] => _values.TryGetValue(key, out var v) ? v : throw new KeyNotFoundException("Missing JSON key: " + key);

        public string Str(string key) => (string)this[key]!;
        public string? StrOr(string key, string? fallback) => Has(key) ? (string?)this[key] : fallback;
        public double Num(string key) => (double)this[key]!;
        public double NumOr(string key, double fallback) => Has(key) ? (double)this[key]! : fallback;
        public bool BoolOr(string key, bool fallback) => Has(key) ? (bool)this[key]! : fallback;
        public JsonObject Obj(string key) => (JsonObject)this[key]!;
        public List<object?> Arr(string key) => (List<object?>)this[key]!;
    }
}
