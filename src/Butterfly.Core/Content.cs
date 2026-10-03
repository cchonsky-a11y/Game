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
        public int DurationMonths { get; }
        public double LevelGain { get; }
        /// <summary>Who must back it (decided 2026-09-28): null for a private project, "public" or "plague".</summary>
        public string? Authority { get; }
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
            DurationMonths = (int)o.Num("durationMonths");
            LevelGain = o.Num("levelGain");
            Authority = o.Has("authority") ? o.Str("authority") : null;
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

    /// <summary>A candidate to lead an institution after you (P0-32).</summary>
    public sealed class SuccessorDef
    {
        public string Name { get; }
        public string Integrity { get; }
        public string Note { get; }

        public SuccessorDef(JsonObject o)
        {
            Name = o.Str("name");
            Integrity = o.Str("integrity");
            Note = o.Str("note");
        }
    }

    /// <summary>An institution template (SYSTEMS §7), authored in data/content/institutions.json.</summary>
    public sealed class InstitutionDef
    {
        public string Id { get; }
        /// <summary>"established" (exists from the start; you buy into it) or "own" (you found it yourself).</summary>
        public string Origin { get; }
        public bool IsOwn => Origin == "own";
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
        /// <summary>Shown when you found it (own institutions) or first buy into it (established ones).</summary>
        public string FoundText { get; }
        public IReadOnlyList<DriftPathDef> DriftPaths { get; }
        /// <summary>Office titles from member to head (P0-32), and the highest a foreigner can reach.</summary>
        public IReadOnlyList<string> Offices { get; }
        public int OfficeCeiling { get; }
        /// <summary>Candidates you can name to succeed you, from the head's seat or in your own institution.</summary>
        public IReadOnlyList<SuccessorDef> Successors { get; }
        /// <summary>What you must have done before your first purchase (decided 2026-09-28).</summary>
        public string JoinRequirement { get; }
        /// <summary>A rival that won't share a member with this one, if any.</summary>
        public string? ExclusiveWith { get; }

        public InstitutionDef(JsonObject o)
        {
            Id = o.Str("id");
            Origin = o.Str("origin");
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
            FoundText = o.StrOr(Origin == "own" ? "foundText" : "joinText", null) ?? "";
            JoinRequirement = o.StrOr("joinRequirement", "none") ?? "none";
            ExclusiveWith = o.StrOr("exclusiveWith", null);
            DriftPaths = o.Arr("driftPaths").Cast<JsonObject>().Select(x => new DriftPathDef(x)).ToList();
            Offices = o.Has("offices") ? o.Arr("offices").Cast<string>().ToList() : new List<string> { "member", "officer", "deputy", "head" };
            OfficeCeiling = o.Has("officeCeiling") ? (int)o.Num("officeCeiling") : 3;
            Successors = o.Has("successors") ? o.Arr("successors").Cast<JsonObject>().Select(x => new SuccessorDef(x)).ToList() : new List<SuccessorDef>();
        }
    }

    /// <summary>One small step of the P0 time machine repair track (data/content/machine.json).</summary>
    public sealed class MachineStepDef
    {
        public string Id { get; }
        public string System { get; }
        public string Name { get; }
        public int Gold { get; }
        public int AttentionPerTurn { get; }
        public int DurationMonths { get; }
        /// <summary>What Rome must give you for this step (tradeMember, medicineWork, factionMember), if anything.</summary>
        public string? Requirement { get; }
        /// <summary>Gold price instead, if the requirement isn't met.</summary>
        public int AltGold { get; }
        public string AltText { get; }
        public string Text { get; }
        /// <summary>Completion text when you paid the gold instead of getting help from Rome.</summary>
        public string AltDoneText { get; }

        public MachineStepDef(JsonObject o)
        {
            Id = o.Str("id");
            System = o.Str("system");
            Name = o.Str("name");
            Gold = (int)o.Num("gold");
            AttentionPerTurn = (int)o.Num("attentionPerTurn");
            DurationMonths = (int)o.Num("durationMonths");
            Requirement = o.StrOr("requirement", null);
            AltGold = o.Has("altGold") ? (int)o.Num("altGold") : 0;
            AltText = o.StrOr("altText", "") ?? "";
            Text = o.Str("text");
            AltDoneText = o.StrOr("altDoneText", null) ?? Text;
        }
    }

    /// <summary>One effect of an invention (income, consultBonus, loyalty, stake, level, plagueResilience).</summary>
    public sealed class InventionEffect
    {
        public string Type { get; }
        public double Value { get; }
        /// <summary>Which institutions it touches: trade (guild or bank), medicine (Circle or sanctuary), faction (either faction), guild.</summary>
        public string? Group { get; }
        public Domain? Domain { get; }

        public InventionEffect(JsonObject o)
        {
            Type = o.Str("type");
            Value = o.Num("value");
            Group = o.StrOr("group", null);
            if (o.Has("domain") && DomainInfo.TryParseDomain(o.Str("domain"), out var d)) Domain = d;
        }
    }

    /// <summary>An invention (data/content/inventions.json): made once, needs something from Rome.</summary>
    public sealed class InventionDef
    {
        public string Id { get; }
        public string Name { get; }
        public int Gold { get; }
        public int AttentionPerTurn { get; }
        public int DurationMonths { get; }
        public string Requirement { get; }
        /// <summary>The invention tree (decided 2026-09-28): its branch, and the invention it needs first (null for a branch's first).</summary>
        public string Branch { get; }
        public string? Prerequisite { get; }
        public IReadOnlyList<InventionEffect> Effects { get; }
        public string Description { get; }
        public string CompletionText { get; }

        public InventionDef(JsonObject o)
        {
            Id = o.Str("id");
            Name = o.Str("name");
            Gold = (int)o.Num("gold");
            AttentionPerTurn = (int)o.Num("attentionPerTurn");
            DurationMonths = (int)o.Num("durationMonths");
            Requirement = o.Str("requirement");
            Branch = o.Has("branch") ? o.Str("branch") : "";
            Prerequisite = o.Has("prerequisite") ? o.Str("prerequisite") : null;
            Effects = o.Arr("effects").Cast<JsonObject>().Select(x => new InventionEffect(x)).ToList();
            Description = o.Str("description");
            CompletionText = o.Str("completionText");
        }
    }

    /// <summary>A dated piece of Rome's news (data/content/news.json): what history had happening; text only.</summary>
    public sealed class NewsDef
    {
        public int Year { get; }
        /// <summary>1-12.</summary>
        public int Month { get; }
        public string Text { get; }
        public SimTime Time => SimTime.FromYear(Year, Month - 1);

        public NewsDef(JsonObject o)
        {
            Year = (int)o.Num("year");
            Month = (int)o.Num("month");
            Text = o.Str("text");
        }
    }

    /// <summary>A piece of local talk (data/content/news.json "local"): invented street-level news; text only.</summary>
    public sealed class LocalNewsDef
    {
        public string Text { get; }
        /// <summary>What must be true of the player's world for it to be heard ("always" if not given).</summary>
        public string When { get; }
        public int From { get; }
        public int Until { get; }
        /// <summary>A festival's month (1-12), or 0.</summary>
        public int Month { get; }

        public LocalNewsDef(JsonObject o)
        {
            Text = o.Str("text");
            When = o.Has("when") ? o.Str("when") : "always";
            From = o.Has("from") ? (int)o.Num("from") : 0;
            Until = o.Has("until") ? (int)o.Num("until") : int.MaxValue;
            Month = o.Has("month") ? (int)o.Num("month") : 0;
        }
    }

    /// <summary>One effect of a decision event's option (data/content/events.json).</summary>
    public sealed class EventEffect
    {
        public string Type { get; }
        public double Value { get; }
        public string? Domain { get; }
        public string? Institution { get; }
        /// <summary>Only when this holds (P0-33, answers that stack): a flag set by an earlier answer, or "own:&lt;id&gt;" for an institution you founded that stands.</summary>
        public string? If { get; }

        public EventEffect(JsonObject o)
        {
            If = o.StrOr("if", null);
            Type = o.Str("type");
            Value = o.Num("value");
            Domain = o.StrOr("domain", null);
            Institution = o.StrOr("institution", null);
        }
    }

    public sealed class EventOptionDef
    {
        public string Id { get; }
        public string Label { get; }
        public string Text { get; }
        public IReadOnlyList<EventEffect> Effects { get; }
        /// <summary>What this choice leaves in Rome, shown at the first and a later arrival (P0-33); null leaves no mark.</summary>
        public string? Mark { get; }
        public string? Mark2 { get; }
        /// <summary>The institution the mark lives in: it shows only while that institution stands, else MarkGone.</summary>
        public string? MarkInstitution { get; }
        public string? MarkGone { get; }
        public string? MarkGone2 { get; }
        /// <summary>What kind of answer this is (generous, profit, principled, loyal, aloof), and the flags it leaves for later choices.</summary>
        public string Style { get; }
        public IReadOnlyList<string> Sets { get; }

        public EventOptionDef(JsonObject o)
        {
            Style = o.StrOr("style", "aloof") ?? "aloof";
            Sets = o.Has("sets") ? o.Arr("sets").Cast<object>().Select(x => x.ToString()!).ToList() : new List<string>();
            Id = o.Str("id");
            Label = o.Str("label");
            Text = o.Str("text");
            Effects = o.Arr("effects").Cast<JsonObject>().Select(x => new EventEffect(x)).ToList();
            Mark = o.StrOr("mark", null);
            Mark2 = o.StrOr("mark2", null);
            MarkInstitution = o.StrOr("markInstitution", null);
            MarkGone = o.StrOr("markGone", null);
            MarkGone2 = o.StrOr("markGone2", null);
        }
    }

    /// <summary>A dated decision (P0-33) or a leader's request (P0-32): a choice with costs, on its date if its requirement holds.</summary>
    public sealed class EventDef
    {
        public string Id { get; }
        public int Year { get; }
        public int Month { get; }
        public string Requires { get; }
        public string Title { get; }
        public string Text { get; }
        public IReadOnlyList<EventOptionDef> Options { get; }
        public SimTime Time => SimTime.FromYear(Year, Month - 1);

        public EventDef(JsonObject o)
        {
            Id = o.Str("id");
            Year = (int)o.Num("year");
            Month = (int)o.Num("month");
            Requires = o.StrOr("requires", "any") ?? "any";
            Title = o.Str("title");
            Text = o.Str("text");
            Options = o.Arr("options").Cast<JsonObject>().Select(x => new EventOptionDef(x)).ToList();
        }
    }

    /// <summary>A kind of workshop order (P0-34): pay, Attention and effects in the decision events' terms.</summary>
    public sealed class OrderDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Text { get; }
        public int Attention { get; }
        public double Pay { get; }
        /// <summary>The smallest workshop that gets this order (1 = any).</summary>
        public int MinSize { get; }
        public IReadOnlyList<EventEffect> Effects { get; }

        public OrderDef(JsonObject o)
        {
            MinSize = o.Has("minSize") ? (int)o.Num("minSize") : 1;
            Id = o.Str("id");
            Name = o.Str("name");
            Text = o.Str("text");
            Attention = (int)o.Num("attention");
            Pay = o.Num("pay");
            Effects = o.Arr("effects").Cast<JsonObject>().Select(x => new EventEffect(x)).ToList();
        }
    }

    /// <summary>A size of the workshop (decided 2026-09-28, Corey: upgrade the workshop, a ladder of sizes).</summary>
    public sealed class WorkshopSizeDef
    {
        public int Size { get; }
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string BuildText { get; }
        public double Cost { get; }
        public int Attention { get; }
        public int DurationMonths { get; }
        public string Requires { get; }
        public string RequiresText { get; }
        public double Upkeep { get; }
        public int Offered { get; }
        public int PerSeason { get; }
        public int ApprenticeMax { get; }
        public double Output { get; }

        public WorkshopSizeDef(JsonObject o)
        {
            Size = (int)o.Num("size");
            Id = o.Str("id");
            Name = o.Str("name");
            Description = o.Str("description");
            BuildText = o.StrOr("buildText", "") ?? "";
            Cost = o.Num("cost");
            Attention = (int)o.Num("attention");
            DurationMonths = (int)o.Num("durationMonths");
            Requires = o.Str("requires");
            RequiresText = o.StrOr("requiresText", "") ?? "";
            Upkeep = o.Num("upkeep");
            Offered = (int)o.Num("offered");
            PerSeason = (int)o.Num("perSeason");
            ApprenticeMax = (int)o.Num("apprenticeMax");
            Output = o.Num("output");
        }
    }

    /// <summary>All authored content from data/content/.</summary>
    public sealed class Content
    {
        public IReadOnlyList<ProjectDef> Projects { get; }
        public IReadOnlyList<InstitutionDef> Institutions { get; }
        public IReadOnlyList<MachineStepDef> MachineSteps { get; }
        /// <summary>Optional machine upgrades: not needed to jump; each lengthens the jump.</summary>
        public IReadOnlyList<MachineStepDef> MachineUpgrades { get; }
        /// <summary>The full assessment of the machine that must come before any repair (decided 2026-09-28).</summary>
        public MachineStepDef? MachineAssessment { get; }
        public IReadOnlyList<InventionDef> Inventions { get; }
        /// <summary>Rome's news, in date order (optional file).</summary>
        public IReadOnlyList<NewsDef> News { get; }
        /// <summary>Local talk, in authored order (optional).</summary>
        public IReadOnlyList<LocalNewsDef> LocalNews { get; }
        /// <summary>Decision events and leaders' requests, in date order (optional file).</summary>
        public IReadOnlyList<EventDef> Events { get; private set; } = new List<EventDef>();
        /// <summary>The workshop (P0-34): the smith's name and the kinds of order.</summary>
        public string Smith { get; private set; } = "the smith";
        public IReadOnlyList<OrderDef> Orders { get; private set; } = new List<OrderDef>();
        public IReadOnlyList<WorkshopSizeDef> WorkshopSizes { get; private set; } = new List<WorkshopSizeDef>();
        /// <summary>Text templates keyed "section.key", e.g. "recognition.fountain.runs".</summary>
        public IReadOnlyDictionary<string, string> Text { get; }

        private Content(IReadOnlyList<ProjectDef> projects, IReadOnlyList<InstitutionDef> institutions, IReadOnlyList<MachineStepDef> machine,
            IReadOnlyList<MachineStepDef> upgrades, MachineStepDef? assessment, IReadOnlyList<InventionDef> inventions, IReadOnlyList<NewsDef> news, IReadOnlyList<LocalNewsDef> local, IReadOnlyDictionary<string, string> text)
        {
            News = news;
            LocalNews = local;
            MachineAssessment = assessment;
            MachineUpgrades = upgrades;
            Inventions = inventions;
            Projects = projects;
            Institutions = institutions;
            MachineSteps = machine;
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
            var machineObj = Read(contentDirectory, "machine.json");
            var machine = machineObj.Arr("steps").Cast<JsonObject>().Select(o => new MachineStepDef(o)).ToList();
            var upgrades = machineObj.Has("upgrades")
                ? machineObj.Arr("upgrades").Cast<JsonObject>().Select(o => new MachineStepDef(o)).ToList() : new List<MachineStepDef>();
            var assessment = machineObj.Has("assessment") ? new MachineStepDef(machineObj.Obj("assessment")) : null;
            var inventions = Read(contentDirectory, "inventions.json").Arr("inventions").Cast<JsonObject>().Select(o => new InventionDef(o)).ToList();
            var newsObj = File.Exists(Path.Combine(contentDirectory, "news.json")) ? Read(contentDirectory, "news.json") : null;
            var news = newsObj != null && newsObj.Has("news")
                ? newsObj.Arr("news").Cast<JsonObject>().Select(o => new NewsDef(o)).OrderBy(n => n.Time.TotalMonths).ToList()
                : new List<NewsDef>();
            var local = newsObj != null && newsObj.Has("local")
                ? newsObj.Arr("local").Cast<JsonObject>().Select(o => new LocalNewsDef(o)).ToList()
                : new List<LocalNewsDef>();
            var textObj = Read(contentDirectory, "text.json");
            var text = new Dictionary<string, string>();
            foreach (var section in textObj.Keys)
            {
                if (section.StartsWith("_")) continue;
                var obj = textObj.Obj(section);
                foreach (var key in obj.Keys) text[section + "." + key] = obj.Str(key);
            }
            var content = new Content(projects, institutions, machine, upgrades, assessment, inventions, news, local, text);
            if (File.Exists(Path.Combine(contentDirectory, "events.json")))
                content.Events = Read(contentDirectory, "events.json").Arr("events").Cast<JsonObject>().Select(o => new EventDef(o)).OrderBy(e => e.Time.TotalMonths).ToList();
            if (File.Exists(Path.Combine(contentDirectory, "workshop.json")))
            {
                var w = Read(contentDirectory, "workshop.json");
                content.Smith = w.Str("smith");
                content.Orders = w.Arr("orders").Cast<JsonObject>().Select(o => new OrderDef(o)).ToList();
                content.WorkshopSizes = w.Arr("sizes").Cast<JsonObject>().Select(o => new WorkshopSizeDef(o)).OrderBy(x => x.Size).ToList();
            }
            return content;
        }

        public ProjectDef? Project(string id) => Projects.FirstOrDefault(p => p.Id == id);
        public InstitutionDef? Institution(string id) => Institutions.FirstOrDefault(i => i.Id == id);
    }
}
