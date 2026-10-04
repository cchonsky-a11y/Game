using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using JObject = System.Text.Json.Nodes.JsonObject;
using Xunit;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// Code-health pass (2026-10-04): content is checked when it loads. Ids that don't exist, duplicate ids, malformed
    /// requirements, unknown levels and stages fail at start-up with every problem listed, instead of silently never holding.
    /// </summary>
    public class ContentValidationTests
    {
        private static string ContentDir => Path.Combine(GameData.FindDataDirectory(AppContext.BaseDirectory), "content");

        /// <summary>A throwaway copy of the shipped content with one file edited, loaded; returns the load error (or null).</summary>
        private static string? LoadBroken(string file, Action<JsonNode> edit)
        {
            string dir = Path.Combine(Path.GetTempPath(), "butterfly-content-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                foreach (var f in Directory.GetFiles(ContentDir)) File.Copy(f, Path.Combine(dir, Path.GetFileName(f)));
                var node = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, file)))!;
                edit(node);
                File.WriteAllText(Path.Combine(dir, file), node.ToJsonString());
                try { Content.Load(dir); return null; }
                catch (FormatException e) { return e.Message; }
            }
            finally { Directory.Delete(dir, true); }
        }

        private static JsonNode ById(JsonNode root, string array, string id) =>
            root[array]!.AsArray().First(n => (string?)n!["id"] == id)!;

        [Fact]
        public void TheShippedContentHasNoProblems()
        {
            Assert.Empty(ContentValidation.Problems(TestData.Load().Content));
        }

        [Fact]
        public void TheValidatorsGrammarIsTheGrammarHoldsUnderstands()
        {
            var sim = new Simulation(TestData.Load(), 42);
            var c = sim.Data.Content;
            var samples = new[]
            {
                "commission:cellarpump:Done", "access:guild:Guest", "capability:valveseats:Reproducible", "life:felix-fever",
                "flag:baths-flow", "knows:Felix", "scene:market-day", "month:3", "monthsIn:2", "machine:assessed",
                "project:fountain:done", "regard:Felix:1", "regardBelow:Felix:2", "challenge:standards:Open", "stage:standards:1",
                "invented:" + c.Inventions[0].Id, "join:circle", "promise"
            };
            Assert.Equal(ContentValidation.RequirementPrefixes.OrderBy(x => x), samples.Select(s => s.Split(':')[0]).OrderBy(x => x));
            foreach (var s in samples) sim.Holds(s);                                      // every prefix the validator accepts, Holds parses
            Assert.Empty(ContentValidation.Problems(c));
            Assert.Throws<FormatException>(() => sim.Holds("bogus:thing"));
        }

        [Fact]
        public void AnUnknownIdInARequirementFailsAtLoad()
        {
            string? error = LoadBroken("commissions.json", n => ById(n, "commissions", "hoist")["requires"] = new JsonArray("commission:cellarpumpp:Walked"));
            Assert.NotNull(error);
            Assert.Contains("commission hoist: requirement 'commission:cellarpumpp:Walked' names an unknown commission", error);
        }

        [Fact]
        public void BadLevelsStagesAndPeopleFailAtLoad()
        {
            string? error = LoadBroken("scenes.json", n =>
            {
                ById(n, "scenes", "market-day")["requires"] = new JsonArray("capability:valveseats:Perfected", "access:guild:Friend", "knows:Feliks", "month:13");
                ById(n, "scenes", "serenus-meet")["regard"] = new JObject { ["Serenuss"] = 1 };
            });
            Assert.NotNull(error);
            Assert.Contains("has an unknown level", error);
            Assert.Contains("has an unknown access stage", error);
            Assert.Contains("'knows:Feliks' names an unknown person", error);
            Assert.Contains("needs a month 1–12", error);
            Assert.Contains("scene serenus-meet: unknown person 'Serenuss'", error);
        }

        [Fact]
        public void DuplicateIdsAndUnknownCapabilitiesFailAtLoad()
        {
            string? error = LoadBroken("people.json", n =>
            {
                var life = n["life"]!.AsArray();
                life.Add(JsonNode.Parse(life[0]!.ToJsonString()));                         // the same life event twice
                ((JObject)life[1]!)["capability"] = new JObject { ["id"] = "warpdrive", ["spread"] = "Copied" };
            });
            Assert.NotNull(error);
            Assert.Contains("duplicate life event id 'felix-fever'", error);
            Assert.Contains("unknown capability 'warpdrive'", error);
        }

        [Fact]
        public void AnInventedLeapFailsAtLoad()
        {
            string? error = LoadBroken("capabilities.json", n => n["capabilities"]!.AsArray()[0]!["leap"] = "invent");
            Assert.NotNull(error);
            Assert.Contains("leap 'invent' is not one of", error);
        }

        [Fact]
        public void MalformedJsonSaysWhere()
        {
            var e = Assert.Throws<FormatException>(() => Json.Parse("{\n  \"a\": 1,\n  \"b\" 2\n}"));
            Assert.Contains("line 3, column 7", e.Message);
        }
    }
}
