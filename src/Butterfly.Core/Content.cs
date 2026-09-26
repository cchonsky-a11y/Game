using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Butterfly.Core
{
    /// <summary>A special effect attached to a project, beyond its domain level gain.</summary>
    public sealed class ProjectExtra
    {
        public string Type { get; }
        public double Value { get; }
        public string? Institution { get; }

        public ProjectExtra(string type, double value, string? institution)
        {
            Type = type;
            Value = value;
            Institution = institution;
        }
    }

    /// <summary>A templated project (SYSTEMS §5), authored in data/content/projects.json.</summary>
    public sealed class ProjectDef
    {
        public string Id { get; }
        public Domain Domain { get; }
        public string Name { get; }
        public int Gold { get; }
        public int AttentionPerTurn { get; }
        public int Turns { get; }
        public double LevelGain { get; }
        public IReadOnlyList<ProjectExtra> Extras { get; }
        public IReadOnlyList<string> Tags { get; }
        public string Description { get; }
        public string CompletionText { get; }

        public ProjectDef(JsonObject o)
        {
            Id = o.Str("id");
            DomainInfo.TryParseDomain(o.Str("domain"), out var d);
            Domain = d;
            Name = o.Str("name");
            Gold = (int)o.Num("gold");
            AttentionPerTurn = (int)o.Num("attentionPerTurn");
            Turns = (int)o.Num("turns");
            LevelGain = o.Num("levelGain");
            Extras = o.Has("extras")
                ? o.Arr("extras").Cast<JsonObject>().Select(x => new ProjectExtra(x.Str("type"), x.Num("value"), x.StrOr("institution", null))).ToList()
                : new List<ProjectExtra>();
            Tags = o.Has("tags") ? o.Arr("tags").Cast<string>().ToList() : new List<string>();
            Description = o.Str("description");
            CompletionText = o.Str("completionText");
        }
    }

    /// <summary>A pre-authored drift path (PROTOTYPE_SCOPE: 2 per institution, no general identity engine).</summary>
    public sealed class DriftPathDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Identity { get; }
        public string Condition { get; }
        public string Description { get; }
        public string ArrivalLeader { get; }

        public DriftPathDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Identity = o.Str("identity");
            Condition = o.Str("condition");
            Description = o.Str("description");
            ArrivalLeader = o.Str("arrivalLeader");
        }
    }

    /// <summary>An institution template (SYSTEMS §7), authored in data/content/institutions.json.</summary>
    public sealed class InstitutionDef
    {
        public string Id { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string Type { get; }
        public Domain Maintains { get; }
        public string Leader { get; }
        public string LeaderRole { get; }
        /// <summary>Pre-authored leader integrity: honest, average or venal.</summary>
        public string LeaderIntegrity { get; }
        public string FoundingIdentity { get; }
        public IReadOnlyList<string> Tags { get; }
        public string FoundText { get; }
        public IReadOnlyList<DriftPathDef> DriftPaths { get; }

        public InstitutionDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            ShortName = o.Str("shortName");
            Type = o.Str("type");
            DomainInfo.TryParseDomain(o.Str("maintains"), out var d);
            Maintains = d;
            Leader = o.Str("leader");
            LeaderRole = o.Str("leaderRole");
            LeaderIntegrity = o.Str("leaderIntegrity");
            FoundingIdentity = o.Str("foundingIdentity");
            Tags = o.Arr("tags").Cast<string>().ToList();
            FoundText = o.Str("foundText");
            DriftPaths = o.Arr("driftPaths").Cast<JsonObject>().Select(x => new DriftPathDef(x)).ToList();
        }
    }

    /// <summary>All authored content from data/content/.</summary>
    public sealed class Content
    {
        public IReadOnlyList<ProjectDef> Projects { get; }
        public IReadOnlyList<InstitutionDef> Institutions { get; }
        /// <summary>Text templates keyed "section.key", e.g. "recognition.fountain.runs".</summary>
        public IReadOnlyDictionary<string, string> Text { get; }

        private Content(IReadOnlyList<ProjectDef> projects, IReadOnlyList<InstitutionDef> institutions, IReadOnlyDictionary<string, string> text)
        {
            Projects = projects;
            Institutions = institutions;
            Text = text;
        }

        /// <summary>Fills a template's {placeholders}. Unknown placeholders are left visible so tests can catch them.</summary>
        public string Template(string key, IDictionary<string, string>? values = null)
        {
            if (!Text.TryGetValue(key, out var template)) throw new KeyNotFoundException("Missing text template: " + key);
            if (values == null) return template;
            foreach (var kv in values) template = template.Replace("{" + kv.Key + "}", kv.Value);
            return template;
        }

        private static JsonObject Read(string dir, string file) =>
            (JsonObject)Json.Parse(File.ReadAllText(Path.Combine(dir, file)))!;

        public static Content Load(string contentDirectory)
        {
            var projects = Read(contentDirectory, "projects.json").Arr("projects").Cast<JsonObject>().Select(o => new ProjectDef(o)).ToList();
            var institutions = Read(contentDirectory, "institutions.json").Arr("institutions").Cast<JsonObject>().Select(o => new InstitutionDef(o)).ToList();
            var textObj = Read(contentDirectory, "text.json");
            var text = new Dictionary<string, string>();
            foreach (var section in textObj.Keys)
            {
                if (section.StartsWith("_")) continue;
                var obj = textObj.Obj(section);
                foreach (var key in obj.Keys) text[section + "." + key] = obj.Str(key);
            }
            return new Content(projects, institutions, text);
        }

        public ProjectDef? Project(string id) => Projects.FirstOrDefault(p => p.Id == id);
        public InstitutionDef? Institution(string id) => Institutions.FirstOrDefault(i => i.Id == id);
    }
}
