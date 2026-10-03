using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P1 narrative rules (master handoff §7, decided 2026-10-02): say it once, with no narrator commentary after a line, and
    /// Romans speak concretely (no modern managerial words in their mouths). Every authored string in data/content is checked.
    /// </summary>
    public class NarrativeRulesTests
    {
        private static readonly string[] BannedNarration =
        {
            "That lands", "That gets his attention", "That gets her attention", "That's a shift", "There it is", "That changes things",
            "That changes the value", "This isn't just", "That matters", "This matters", "That tells you something", "The real issue is",
            "Not a question", "No flourish", "Just yes", "That is enough", "He understands", "She understands", "You can tell he means it",
            "You can tell she means it", "The room changes", "That sits between them", "Something in his voice tells you",
            "Something in her voice tells you", "He looks at you differently", "She looks at you differently",
        };

        private static readonly string[] ModernWordsRomansDontUse =
        {
            "rollout", "implementation", "performance", "process", "capacity", "metrics", "success criteria", "workflow", "scalable",
            "shutdown procedure", "inspection standard",
        };

        private static IEnumerable<string> AuthoredStrings()
        {
            var dir = Path.Combine(GameData.FindDataDirectory(System.AppContext.BaseDirectory), "content");
            foreach (var file in Directory.GetFiles(dir, "*.json"))
                foreach (var s in Strings(Json.Parse(File.ReadAllText(file))))
                    yield return s;
        }

        private static IEnumerable<string> Strings(object? node)
        {
            if (node is string s) yield return s;
            else if (node is JsonObject o) { foreach (var k in o.Keys) foreach (var x in Strings(o[k])) yield return x; }
            else if (node is IEnumerable<object?> list) foreach (var item in list) foreach (var x in Strings(item)) yield return x;
        }

        [Fact]
        public void NoAuthoredTextUsesTheBannedNarratorPatterns()
        {
            Assert.True(AuthoredStrings().Count() > 500);                     // the scan really reads the content
            var hits = AuthoredStrings().SelectMany(s => BannedNarration.Where(b => s.IndexOf(b, System.StringComparison.OrdinalIgnoreCase) >= 0).Select(b => b + " | " + s)).ToList();
            Assert.True(hits.Count == 0, string.Join("\n", hits));
        }

        [Fact]
        public void RomansDoNotSpeakInModernManagerialWords()
        {
            var hits = new List<string>();
            foreach (var s in AuthoredStrings())
                foreach (Match q in Regex.Matches(s, "\"([^\"]{3,})\""))
                    foreach (var w in ModernWordsRomansDontUse)
                        if (Regex.IsMatch(q.Groups[1].Value, @"\b" + w + @"\b", RegexOptions.IgnoreCase)) hits.Add(w + " | " + q.Value);
            Assert.True(hits.Count == 0, string.Join("\n", hits));
        }

        [Fact]
        public void TheOpeningTellsTheStoryFirst()
        {
            // Locked opening: the line, the non-travel test, Latin; no plague, jump ranges or Echoes up front.
            var t = TestData.Load().Content;
            string opening = t.Template("opening.scene") + t.Template("opening.gold") + t.Template("opening.money");
            Assert.StartsWith("The machine stops screaming before you do.", opening);
            Assert.Contains("systems test", opening);
            Assert.Contains("Ancient Rome. Not ruins. Alive.", opening);
            Assert.Contains("contacts and couplings", opening);                  // the gold is the machine's own
            foreach (var w in new[] { "plague", "pestilence", "East", "years depending", "Echo", "jump" })
                Assert.DoesNotContain(w, opening);
        }
    }
}
