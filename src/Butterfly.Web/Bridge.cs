using System.Text.Json;
using Butterfly.Presentation;
using Microsoft.JSInterop;

namespace Butterfly.Web
{
    /// <summary>
    /// What the page can ask of the game: start it, read its screen models, send a command. Every answer is the models of
    /// Butterfly.Presentation as JSON. No gameplay here; the one Simulation in Butterfly.Core decides everything.
    /// </summary>
    public static class Bridge
    {
        private static GameSession? _session;
        private static Outcome? _last;
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static GameSession S => _session ?? throw new InvalidOperationException("No game started.");

        /// <summary>Starts a game from the data files the page fetched (name → text), written to the in-memory file system.</summary>
        [JSInvokable]
        public static string Start(Dictionary<string, string> files, string seed)
        {
            Directory.CreateDirectory("/data/content");
            foreach (var kv in files) File.WriteAllText(Path.Combine("/data", kv.Key), kv.Value);
            _session = GameSession.Start("/data", ulong.Parse(seed));
            _last = null;
            return State();
        }

        /// <summary>Runs one command (a GameAction's command, or end / wait / jump) and returns the new state.</summary>
        [JSInvokable]
        public static string Do(string command)
        {
            _last = S.Do(command);
            return State();
        }

        [JSInvokable] public static string OpenDeparture() { S.OpenDeparture(); return State(); }
        [JSInvokable] public static string CloseDeparture() { S.CloseDeparture(); return State(); }
        [JSInvokable] public static string FinishArrival() { S.FinishArrival(); return State(); }

        /// <summary>A place's free look-around (read-only).</summary>
        [JSInvokable] public static string Look(string place) => S.LookAround(place);

        /// <summary>Every screen model the page draws, read-only.</summary>
        [JSInvokable]
        public static string State()
        {
            var s = S;
            var phase = s.Phase;
            bool era = phase == Phase.Opening || phase == Phase.Rome || phase == Phase.Departure && !s.Sim.Arrived;
            return JsonSerializer.Serialize(new
            {
                phase = phase.ToString(),
                seed = s.Sim.Seed.ToString(),
                hud = s.Hud(),
                opening = phase == Phase.Opening ? s.Opening() : null,
                decision = s.CurrentDecision(),
                places = RomeMap.Places.Select(p => new
                {
                    p.Id, p.Name, p.Caption,
                    actions = era ? s.ActionsAt(p.Id).Select(g => new { g.Key, g.Title, items = g.Items }) : null,
                }),
                everything = phase == Phase.Ended ? null : s.Actions().Where(g => g.Items.Count > 0).Select(g => new { g.Key, g.Title, g.Place, items = g.Items }),
                people = era ? s.People() : null,
                machine = era ? s.Machine() : null,
                work = era ? s.Work() : null,
                journal = phase == Phase.Ended ? null : s.Journal(),
                readyLine = s.ReadyLine(),
                news = era ? s.News() : null,
                departure = phase == Phase.Departure ? s.Departure() : null,
                arrival = phase == Phase.Arrival ? s.Arrival() : null,
                ret = phase == Phase.Return || phase == Phase.AfterReturn ? s.Return() : null,
                learnMore = phase == Phase.AfterReturn ? s.LearnMore() : null,
                ending = s.Ending(),
                last = _last,
            }, Json);
        }
    }
}
