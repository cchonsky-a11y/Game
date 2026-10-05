#nullable enable
using System;
using System.IO;
using System.Linq;
using Butterfly.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Butterfly.Unity
{
    /// <summary>The era screens reachable from the HUD.</summary>
    public enum Screen { Rome, People, Work, Machine, Journal, Everything }

    /// <summary>
    /// The graphical client (P2 vertical slice, 2026-10-05). Presentation only: it owns a <see cref="GameSession"/> (one
    /// authoritative Simulation in Butterfly.Core) and redraws from its screen models after every click. No gameplay lives
    /// here. It starts itself in any scene, so pressing Play in an empty project runs the game.
    /// </summary>
    public sealed class ButterflyApp : MonoBehaviour
    {
        public GameSession Session { get; private set; } = null!;
        public ulong Seed { get; private set; }

        // What the client is showing (never game state).
        public Screen Screen = Screen.Rome;
        public string SelectedPlace = RomeMap.Lodging;
        public string LookText = "";
        public int SelectedSite = 0;
        public string ReturnDetail = "";
        public Outcome? LastOutcome;
        public string DismissedDecision = "";
        public bool OpeningRead;
        public int ArrivalBeat;
        public bool Narrow;
        public string ReplayPath = "";

        private UIDocument _doc = null!;
        private VisualElement _root = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindAnyObjectByType<ButterflyApp>() != null) return;
            var go = new GameObject("The Butterfly Effect");
            go.SetActive(false);
            go.AddComponent<ButterflyApp>();
            DontDestroyOnLoad(go);
            go.SetActive(true);
        }

        private void Awake()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1600, 1000);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ButterflyTheme");
            _doc = gameObject.AddComponent<UIDocument>();
            _doc.panelSettings = settings;
            // Unity 6's built-in font; the default runtime theme supplies one too, so a missing font is only a fallback lost.
            try { Ui.BuiltinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (Exception e) { Debug.LogWarning("No built-in font: " + e.Message); }
            NewGame(PickSeed());
        }

        private void OnEnable()
        {
            _root = _doc.rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = Ui.Paper;
            if (Ui.BuiltinFont != null) _root.style.unityFontDefinition = FontDefinition.FromFont(Ui.BuiltinFont);
            _root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                bool narrow = e.newRect.width < 1100;
                if (narrow != Narrow) { Narrow = narrow; Refresh(); }
            });
            Refresh();
        }

        /// <summary>A seed for a new game. Shown to the player and written with the replay, so any run can be replayed.</summary>
        private static ulong PickSeed() => (ulong)(Environment.TickCount & 0x7FFFFFFF) % 100000;

        public void NewGame(ulong seed)
        {
            Seed = seed;
            string data = Path.Combine(Application.streamingAssetsPath, "data");
            Session = GameSession.Start(data, seed);
            Screen = Screen.Rome; SelectedPlace = RomeMap.Lodging; LookText = ""; LastOutcome = null; DismissedDecision = "";
            OpeningRead = false; ArrivalBeat = 0; SelectedSite = 0; ReturnDetail = "";
            ReplayPath = Path.Combine(Application.persistentDataPath, "runs", "seed-" + seed + ".txt");
            WriteReplay();
            if (_root != null) Refresh();
        }

        /// <summary>Runs one action through the session and redraws.</summary>
        public void Act(string command)
        {
            var o = Session.Do(command);
            if (o.Quit) { Application.Quit(); return; }
            LastOutcome = o;
            if (o.Navigate.Length > 0 && Enum.TryParse<Screen>(o.Navigate, true, out var s)) Screen = s;
            if (o.Navigate == "now") Screen = Screen.Rome;
            WriteReplay();
            Refresh();
        }

        /// <summary>
        /// The run as a console inputs file (seed and commands), written after every action. It is not a save: it lets an
        /// observer replay exactly what a tester did (dotnet run --project src/Butterfly.Console -- --seed N --inputs file).
        /// </summary>
        private void WriteReplay()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReplayPath)!);
                File.WriteAllLines(ReplayPath, new[] { "# seed: " + Seed }.Concat(Session.Commands));
            }
            catch (Exception e) { Debug.LogWarning("Could not write the replay: " + e.Message); }
        }

        public void Go(Screen s) { Screen = s; Refresh(); }

        public void Refresh()
        {
            if (_root == null) return;
            _root.Clear();
            var phase = Session.Phase;
            VisualElement page;
            if (phase == Phase.Opening && !OpeningRead) page = Screens.Opening(this);
            else if (phase == Phase.Departure) page = Screens.Departure(this);
            else if (phase == Phase.Arrival) page = Screens.Arrival(this);
            else if (phase == Phase.Return || phase == Phase.AfterReturn) page = Screen == Screen.Journal ? Screens.WithHud(this, Screens.Journal(this, true)) : Screens.Return(this);
            else page = Screens.WithHud(this, Screen switch
            {
                Screen.People => Screens.People(this),
                Screen.Work => Screens.Work(this),
                Screen.Machine => Screens.Machine(this),
                Screen.Journal => Screens.Journal(this, false),
                Screen.Everything => Screens.Everything(this),
                _ => Screens.Rome(this),
            });
            Ui.Fill(page);
            _root.Add(page);
            // The narrative view: a choice waiting for you, one at a time (it can be set aside and reopened from the HUD).
            if ((phase == Phase.Rome || phase == Phase.Opening && OpeningRead) && Session.CurrentDecision() is DecisionCard card && card.Key != DismissedDecision)
                _root.Add(Screens.Decision(this, card));
        }
    }
}
