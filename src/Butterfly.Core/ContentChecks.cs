using System;
using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>
    /// Automated content checks for the SYSTEMS §14 hard constraints. Run by the test suite
    /// over everything in data/content, so a violating project or text can never ship.
    /// </summary>
    public static class ContentChecks
    {
        /// <summary>Tags that no player-facing project, verb or institution may carry.</summary>
        public static readonly string[] ForbiddenTags =
        {
            "enslaved-labor", "slavery", "atrocity", "massacre", "ethnic-cleansing",
            "religious-founder", "weapon-of-mass-destruction", "exploitation-gain"
        };

        /// <summary>Phrases that must not appear in player-verb text (projects, actions).</summary>
        public static readonly string[] ForbiddenPhrases =
        {
            "slave", "enslave", "massacre", "ethnic cleansing", "exterminat", "plague as a weapon", "poison the"
        };

        public static IReadOnlyList<string> Check(Content content)
        {
            var problems = new List<string>();
            foreach (var p in content.Projects)
                CheckVerb("project " + p.Id, p.Tags, new[] { p.Name, p.Description, p.CompletionText }, problems);
            foreach (var i in content.Institutions)
            {
                var texts = new List<string> { i.Name, i.FoundingIdentity, i.FoundText };
                texts.AddRange(i.DriftPaths.Select(d => d.Name + " " + d.Description));
                CheckVerb("institution " + i.Id, i.Tags, texts, problems);
            }
            foreach (var kv in content.Text)
                CheckVerb("text " + kv.Key, new string[0], new[] { kv.Value }, problems);
            return problems;
        }

        public static void CheckVerb(string what, IEnumerable<string> tags, IEnumerable<string> texts, List<string> problems)
        {
            foreach (var tag in tags)
                if (ForbiddenTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    problems.Add(what + ": forbidden tag '" + tag + "' (SYSTEMS §14)");
            foreach (var text in texts)
                foreach (var phrase in ForbiddenPhrases)
                    if (text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                        problems.Add(what + ": forbidden phrase '" + phrase + "' (SYSTEMS §14)");
        }
    }
}
