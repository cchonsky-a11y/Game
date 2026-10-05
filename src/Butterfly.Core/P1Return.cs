using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>The kinds of evidence a return site offers (first-return prototype, 2026-10-04).</summary>
    public enum ReturnCategory { Human, Technical, Institutional, Unintended, Journal, Mystery }

    /// <summary>What a person has become by the time you return: themselves, old, carried by heirs, or a memory (PROPOSED P1-25).</summary>
    public enum HumanBand { Self, Elder, Heirs, Memory }

    /// <summary>
    /// A place in the changed city that can be found after the first jump, authored in data/content/returns.json. It exists only
    /// if its requirements held for what actually happened; its first variant that holds decides what is there.
    /// </summary>
    public sealed class ReturnSiteDef
    {
        public string Id { get; }
        public ReturnCategory Category { get; }
        public string Place { get; }
        /// <summary>For a human site: whose life it follows (their band gives the recognition).</summary>
        public string Person { get; }
        public IReadOnlyList<string> Requires { get; }
        public IReadOnlyDictionary<HumanBand, string> Bands { get; }
        public IReadOnlyList<ReturnVariantDef> Variants { get; }
        /// <summary>Arrival echo lines (kind:id) this site tells instead, left out of the first arrival's beats when it is chosen.</summary>
        public IReadOnlyList<string> Covers { get; }

        public ReturnSiteDef(JsonObject o)
        {
            Id = o.Str("id");
            Category = (ReturnCategory)System.Enum.Parse(typeof(ReturnCategory), o.Str("category"));
            Place = o.Str("place");
            Person = o.StrOr("person", "") ?? "";
            Requires = o.Arr("requires").Cast<string>().ToList();
            var bands = new Dictionary<HumanBand, string>();
            if (o.Has("bands"))
            {
                var b = o.Obj("bands");
                foreach (HumanBand band in System.Enum.GetValues(typeof(HumanBand)))
                    if (b.Has(band.ToString().ToLowerInvariant())) bands[band] = b.Str(band.ToString().ToLowerInvariant());
            }
            Bands = bands;
            Variants = o.Arr("variants").Cast<JsonObject>().Select(v => new ReturnVariantDef(v)).ToList();
            Covers = o.Has("covers") ? o.Arr("covers").Cast<string>().ToList() : new List<string>();
        }
    }

    /// <summary>One way a site can turn out: what you recognize, what contradicts it, and where to look closer.</summary>
    public sealed class ReturnVariantDef
    {
        public string Id { get; }
        public IReadOnlyList<string> Requires { get; }
        /// <summary>Overrides the site's place (the hour-one choice puts you at the workshop or the fountain).</summary>
        public string Place { get; }
        public string Recognition { get; }
        public string Contradiction { get; }
        public string Lead { get; }
        public string Investigation { get; }
        /// <summary>How far the evidence goes: obvious, plausible, contested or lost.</summary>
        public string Evidence { get; }
        public bool Misattributed { get; }
        /// <summary>The departure-sensitive thread this variant belongs to, if any (foot, copies, shaft).</summary>
        public string Thread { get; }

        public ReturnVariantDef(JsonObject o)
        {
            Id = o.Str("id");
            Requires = o.Arr("requires").Cast<string>().ToList();
            Place = o.StrOr("place", "") ?? "";
            Recognition = o.StrOr("recognition", "") ?? "";
            Contradiction = o.Str("contradiction");
            Lead = o.Str("lead");
            Investigation = o.Str("investigation");
            Evidence = o.StrOr("evidence", "plausible") ?? "plausible";
            Misattributed = o.BoolOr("misattributed", false);
            Thread = o.StrOr("thread", "") ?? "";
        }
    }

    /// <summary>A line the player writes in their journal during the first life, and what survives of it later.</summary>
    public sealed class JournalAnchorDef
    {
        public string Id { get; }
        public IReadOnlyList<string> Requires { get; }
        public string Then { get; }
        public IReadOnlyList<JournalNowDef> Now { get; }

        public JournalAnchorDef(JsonObject o)
        {
            Id = o.Str("id");
            Requires = o.Arr("requires").Cast<string>().ToList();
            Then = o.Str("then");
            Now = o.Arr("now").Cast<JsonObject>().Select(n => new JournalNowDef(n)).ToList();
        }
    }

    /// <summary>One later version of a journal line: where it is found, its words, and what looking closer shows.</summary>
    public sealed class JournalNowDef
    {
        public string Id { get; }
        public IReadOnlyList<string> Requires { get; }
        public string Where { get; }
        public string Text { get; }
        public string Lead { get; }
        public string Investigation { get; }
        public bool Misattributed { get; }

        public JournalNowDef(JsonObject o)
        {
            Id = o.Str("id");
            Requires = o.Arr("requires").Cast<string>().ToList();
            Where = o.Str("where");
            Text = o.Str("text");
            Lead = o.Str("lead");
            Investigation = o.Str("investigation");
            Misattributed = o.BoolOr("misattributed", false);
        }
    }

    /// <summary>A line in the player's journal, as written at the time (it never changes).</summary>
    public sealed class JournalEntry
    {
        public string Id { get; }
        public int Year { get; }
        public string Text { get; }
        public int EventId { get; }
        public JournalEntry(string id, int year, string text, int eventId) { Id = id; Year = year; Text = text; EventId = eventId; }
    }

    /// <summary>A site chosen for this return, its text fixed when it was chosen.</summary>
    public sealed class ReturnSite
    {
        public string Id { get; set; } = "";
        public ReturnCategory Category { get; set; }
        public string Variant { get; set; } = "";
        public string Place { get; set; } = "";
        public string Person { get; set; } = "";
        public HumanBand? Band { get; set; }
        public string Recognition { get; set; } = "";
        public string Contradiction { get; set; } = "";
        public string Lead { get; set; } = "";
        public string Investigation { get; set; } = "";
        public string Evidence { get; set; } = "";
        public bool Misattributed { get; set; }
        public string Thread { get; set; } = "";
        /// <summary>The first-life events that justify this site (hidden from the player; the log's causes for its visit).</summary>
        public List<int> Grounds { get; } = new List<int>();
        /// <summary>True if this site's thread was named in the departure briefing; false for a consequence nobody warned of.</summary>
        public bool Warned { get; set; }
        /// <summary>Arrival echo lines this site tells instead (see <see cref="ReturnSiteDef.Covers"/>).</summary>
        public List<string> Covers { get; } = new List<string>();
    }

    /// <summary>The return chapter after the first jump: the sites found, those visited and looked into, and whether it is done.</summary>
    public sealed class ReturnChapter
    {
        public int DepartureYear { get; set; }
        public int ArrivalYear { get; set; }
        public int JumpYears { get; set; }
        public List<ReturnSite> Sites { get; } = new List<ReturnSite>();
        public List<string> Visited { get; } = new List<string>();
        public List<string> Investigated { get; } = new List<string>();
        public bool Completed { get; set; }
        /// <summary>The departure threads the briefing named when you left (sharpening pass, 2026-10-04).</summary>
        public List<string> WarnedThreads { get; } = new List<string>();
    }
}
