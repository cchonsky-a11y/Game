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
                "invented:" + c.Inventions[0].Id, "join:circle", "promise", "journal:foot", "answered:nerius:honest"
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
    
        private static Butterfly.Core.JsonObject Obj(string json) => (Butterfly.Core.JsonObject)Json.Parse(json)!;

        [Fact]
        public void EveryEffectTypeTheValidatorAcceptsIsApplied()
        {
            var sim = new Simulation(TestData.Load(), 42);
            sim.ChooseSeeded("workshop");
            foreach (var type in ContentValidation.EffectTypes)
                sim.ApplyEffects("test", new[] { new EventEffect(Obj("{\"type\":\"" + type + "\",\"value\":1,\"domain\":\"medicine\",\"institution\":\"guild\",\"person\":\"Felix\",\"text\":\"x\"}")) }, new[] { 1 });
            Assert.Throws<InvalidOperationException>(() => sim.ApplyEffects("test", new[] { new EventEffect(Obj("{\"type\":\"gild\",\"value\":1}")) }, new[] { 1 }));
            var inv = sim.Data.Content.Inventions[0];
            foreach (var type in ContentValidation.InventionEffectTypes)
                sim.ApplyInventionEffect(inv, new InventionEffect(Obj("{\"type\":\"" + type + "\",\"value\":1,\"domain\":\"medicine\",\"group\":\"guild\"}")), 1);
            Assert.Throws<InvalidOperationException>(() => sim.ApplyInventionEffect(inv, new InventionEffect(Obj("{\"type\":\"incom\",\"value\":1}")), 1));
        }

        [Fact]
        public void BadEventContentFailsAtLoad()
        {
            string? error = LoadBroken("events.json", n =>
            {
                var events = n["events"]!.AsArray();
                var withOptions = events.Where(e => e!["options"]!.AsArray().Count > 0).Take(2).ToList();
                var first = withOptions[0]!;
                first["requires"] = "memberr:guild";
                first["options"]!.AsArray()[0]!["effects"] = new JsonArray(
                    new JObject { ["type"] = "gild", ["value"] = 1 },
                    new JObject { ["type"] = "level", ["domain"] = "medecine", ["value"] = 1 },
                    new JObject { ["type"] = "loyalty", ["institution"] = "guidl", ["value"] = 1 },
                    new JObject { ["type"] = "gold", ["value"] = 1, ["if"] = "not:suburaTrsut" },
                    new JObject { ["type"] = "status", ["person"] = "Felix", ["value"] = 0 });
            });
            Assert.NotNull(error);
            Assert.Contains("unknown requirement 'memberr:guild'", error);
            Assert.Contains("effect 'gild': unknown effect type", error);
            Assert.Contains("unknown domain 'medecine'", error);
            Assert.Contains("unknown institution 'guidl'", error);
            Assert.Contains("condition 'not:suburaTrsut' names a flag nothing sets", error);
            Assert.Contains("a status effect needs its new text", error);
        }

        [Fact]
        public void BadWorkshopInventionAndMachineContentFailsAtLoad()
        {
            Assert.Contains("unknown requirement 'membr:guild'",
                LoadBroken("workshop.json", n => n["sizes"]!.AsArray()[1]!["requires"] = "membr:guild") ?? "");
            Assert.Contains("unknown domain 'Medcine'",
                LoadBroken("workshop.json", n => n["orders"]!.AsArray()[0]!["effects"] = new JsonArray(new JObject { ["type"] = "level", ["domain"] = "Medcine", ["value"] = 1 })) ?? "");
            string inv = LoadBroken("inventions.json", n =>
            {
                var first = n["inventions"]!.AsArray()[0]!;
                first["requirement"] = "workshp";
                first["effects"] = new JsonArray(new JObject { ["type"] = "level", ["domain"] = "Medcine", ["value"] = 1 },
                                                 new JObject { ["type"] = "regard", ["group"] = "guilds", ["value"] = 1 });
            }) ?? "";
            Assert.Contains("unknown requirement 'workshp'", inv);
            Assert.Contains("unknown domain 'Medcine'", inv);
            Assert.Contains("unknown group 'guilds'", inv);
            Assert.Contains("unknown requirement 'tradeMembr'",
                LoadBroken("machine.json", n => n["steps"]!.AsArray()[0]!["requirement"] = "tradeMembr") ?? "");
        }

        [Fact]
        public void EveryNotableEventTypeIsOneTheSimulationRecords()
        {
            // Fast-forward stops on these event types; a misspelled one would silently never stop it.
            string src = Path.Combine(Directory.GetParent(GameData.FindDataDirectory(AppContext.BaseDirectory))!.FullName, "src", "Butterfly.Core");
            string code = string.Join("\n", Directory.GetFiles(src, "*.cs").Where(f => !f.EndsWith("Simulation.Pacing.cs")).Select(File.ReadAllText));
            var categories = Enum.GetNames(typeof(SceneCategory)).Select(n => "scene." + n.ToLowerInvariant()).ToList();
            foreach (var type in Simulation.NotableEvents)
                Assert.True(categories.Contains(type) || code.Contains("\"" + type + "\""), "no code records '" + type + "'");
        }
    }
}
