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

    /// <summary>All authored content from data/content/.</summary>
    public sealed class Content
    {
        public IReadOnlyList<ProjectDef> Projects { get; }

        private Content(IReadOnlyList<ProjectDef> projects)
        {
            Projects = projects;
        }

        public static Content Load(string contentDirectory)
        {
            var projects = ((JsonObject)Json.Parse(File.ReadAllText(Path.Combine(contentDirectory, "projects.json")))!)
                .Arr("projects").Cast<JsonObject>().Select(o => new ProjectDef(o)).ToList();
            return new Content(projects);
        }

        public ProjectDef? Project(string id) => Projects.FirstOrDefault(p => p.Id == id);
    }
}
