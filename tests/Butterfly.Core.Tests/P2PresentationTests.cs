using System.Linq;
using Butterfly.Core;
using Butterfly.Presentation;

namespace Butterfly.Core.Tests
{
    /// <summary>
    /// P2 graphical vertical slice (2026-10-05): the presentation adapter the Unity client uses. A whole first life and the
    /// first return played by clicking only what the screens offer, the same game as the console, and views that never
    /// change the game. Scripted, not human.
    /// </summary>
    [Collection("Console")]
    public class P2PresentationTests
    {
        /// <summary>
        /// A simple click-through player: only actions the screens offer (decision cards, the catalog, the departure and
        /// return screens), never a Simulation call. Returns the commands it clicked, in order.
        /// </summary>
        internal static List<string> ClickThrough(GameSession s, int stayMonthsAfterReady = 2, int maxMonths = 240)
        {
            var clicked = new List<string>();
            void Click(string c)
            {
                clicked.Add(c);
                s.Do(c);
            }
            bool Try(string groupKey, System.Func<GameAction, bool>? which = null)
            {
                var g = s.Actions().FirstOrDefault(x => x.Key == groupKey);
                var a = g?.Items.FirstOrDefault(which ?? (_ => true));
                if (a == null) return false;
                Click(a.Command);
                return true;
            }

            int readyFor = 0;
            for (int month = 0; month < maxMonths && !s.Sim.Arrived; month++)
            {
                for (int guard = 0; guard < 12; guard++)
                {
                    var card = s.CurrentDecision();
                    if (card != null)
                    {
                        // The fountain at hour one; otherwise the first offered answer (accepting terms and invitations).
                        var pick = card.Choices.FirstOrDefault(c => c.Command == "choose fountain") ?? card.Choices[0];
                        Click(pick.Command);
                        continue;
                    }
                    // Gold back into the machine first (it is the only gold that never debases), then the repairs.
                    if (Try("machine", a => a.Command.StartsWith("restore "))) continue;
                    if (Try("machine", a => a.Command == "assess" || a.Command.StartsWith("repair ") || a.Command.StartsWith("exchange "))) continue;
                    if (Try("commissions", a => a.Command.StartsWith("commission look "))) continue;
                    if (Try("challenges")) continue;
                    if (Try("work")) continue;
                    break;
                }
                if (s.Sim.MachineReady && ++readyFor > stayMonthsAfterReady)
                {
                    // The player opens the departure screen, reads it, and leaves.
                    Assert.True(s.OpenDeparture());
                    var dep = s.Departure();
                    Assert.True(dep.CanLeave);
                    Assert.NotEmpty(dep.Range);
                    s.Do("jump");          // with the screen open, the second 'jump' is the console's armed jump
                    break;
                }
                Click("end");
            }
            if (!s.Sim.Arrived) return clicked;
            Assert.Equal(Phase.Arrival, s.Phase);
            Assert.NotEmpty(s.Arrival()!.Beats);
            s.FinishArrival();
            Assert.Equal(Phase.Return, s.Phase);
            // The return: visit places, looking closer at each, then finish when the screen allows.
            while (!s.Return()!.CanFinish)
            {
                var site = s.Return()!.Sites.First(x => x.State == SiteState.Unvisited);
                Click(site.VisitCommand);
                Click(site.LookCloserCommand);
            }
            Click("journal");
            Click("done");
            s.LearnMore();
            return clicked;
        }

        [Theory]
        [InlineData(42UL)]
        [InlineData(1UL)]
        [InlineData(2UL)]
        [InlineData(3UL)]
        [InlineData(100UL)]
        [InlineData(2026UL)]
        public void AFirstLifeAndTheFirstReturnCanBePlayedByClickingAlone(ulong seed)
        {
            var s = new GameSession(new Simulation(TestData.Load(), seed));
            Assert.Equal(Phase.Opening, s.Phase);
            Assert.StartsWith("The machine stops screaming before you do.", s.Opening().Paragraphs[0]);
            var card = s.CurrentDecision()!;
            Assert.Equal(2, card.Choices.Count);
            ClickThrough(s);
            Assert.True(s.Sim.Arrived, "the machine was never ready by clicking; now " + s.Hud().Date + ", machine " + s.Hud().Machine);
            Assert.True(s.Sim.World.Return!.Completed);
            Assert.Equal(Phase.AfterReturn, s.Phase);
            Assert.True(s.Sim.World.Return.Visited.Count >= s.Sim.ReturnVisitsRequired);
        }

        [Fact]
        public void ClickingIsTheSameGameAsTypingIntoTheConsole()
        {
            var s = new GameSession(new Simulation(TestData.Load(), 7));
            var clicked = ClickThrough(s);
            Assert.True(s.Sim.Arrived, s.Hud().Date + " machine " + s.Hud().Machine + " gold " + s.Hud().Money + " " + s.Hud().Aurei + " | " + string.Join(" / ", s.Machine().Panel));
            var console = new Simulation(TestData.Load(), 7);
            Assert.Equal(clicked, s.Commands.Where(c => c != "jump"));
            ConsoleTests.Play(console, s.Commands.ToArray());
            Assert.Equal(console.Log.Hash(), s.Sim.Log.Hash());
        }

        [Fact]
        public void LookingAtScreensNeverChangesTheGame()
        {
            var s = new GameSession(new Simulation(TestData.Load(), 3));
            s.Do("choose workshop");
            for (int k = 0; k < 6; k++) s.Do("end");
            string before = s.Sim.Log.Hash();
            var rngBefore = s.Sim.World.Gold;
            s.Hud(); s.Actions(); s.CurrentDecision(); s.News(); s.ReadyLine(); s.Opening(); s.People(); s.Journal(); s.Machine(); s.Work(); s.Departure(); s.Return(); s.Arrival();
            foreach (var p in RomeMap.Places) s.LookAround(p.Id);
            foreach (var p in RomeMap.Places) s.ActionsAt(p.Id);
            Assert.Equal(before, s.Sim.Log.Hash());
            Assert.Equal(rngBefore, s.Sim.World.Gold);
            // And the game goes on exactly as one where nobody looked.
            var other = new GameSession(new Simulation(TestData.Load(), 3));
            other.Do("choose workshop");
            for (int k = 0; k < 6; k++) other.Do("end");
            s.Do("end"); other.Do("end");
            Assert.Equal(other.Sim.Log.Hash(), s.Sim.Log.Hash());
        }

        [Fact]
        public void TheDepartureScreenOpensOnlyWhenThePlayerOpensItAndCanBeClosed()
        {
            var s = new GameSession(new Simulation(TestData.Load(), 11));
            Assert.False(s.OpenDeparture());                        // not ready: nothing to open
            s.Sim.World.Gold = 1000;
            s.Sim.MarkAssessedForTests();
            for (int guard = 0; guard < 60 && !s.Sim.MachineReady; guard++)
            {
                foreach (var system in Simulation.MachineSystems) s.Sim.Repair(system);
                if (s.Sim.MachineStepsDone >= s.Sim.MachineStepsTotal) s.Sim.RestoreGold(s.Sim.MachineGoldNeeded);
                s.Do("end");
            }
            Assert.True(s.Sim.MachineReady);
            Assert.Equal(Phase.Rome, s.Phase);                      // ready, and still in Rome: never pushed
            s.Do("end");
            Assert.Equal(Phase.Rome, s.Phase);
            Assert.True(s.OpenDeparture());
            Assert.Equal(Phase.Departure, s.Phase);
            var d = s.Departure();
            Assert.Contains("You can't come back", d.Range);
            s.CloseDeparture();
            Assert.Equal(Phase.Rome, s.Phase);
            Assert.False(s.Sim.Arrived);
            // Any other action also closes it, as any other command disarms the console's jump.
            s.Do("jump");
            Assert.Equal(Phase.Departure, s.Phase);
            s.Do("end");
            Assert.Equal(Phase.Rome, s.Phase);
            Assert.False(s.Sim.Arrived);
        }

        [Fact]
        public void ReturnSitesShowOnlyWhatThePlayerHasDoneThere()
        {
            // Never true, false, player-caused or important (P2 rule): the model carries no such field.
            var names = typeof(ReturnSiteModel).GetProperties().Select(p => p.Name).ToList();
            foreach (var hidden in new[] { "Category", "Misattributed", "Thread", "Warned", "Grounds", "Evidence", "Variant", "Band", "Important", "Correct" })
                Assert.DoesNotContain(hidden, names);
            var s = new GameSession(new Simulation(TestData.Load(), 42));
            ClickThrough(s);
            var r = s.Return()!;
            Assert.All(r.Sites.Where(x => x.State == SiteState.Unvisited), x => Assert.Equal("", x.Recognition + x.Contradiction + x.Lead + x.Finding));
            Assert.All(r.Sites.Where(x => x.State == SiteState.LookedCloser), x => Assert.NotEqual("", x.Finding));
        }

        [Fact]
        public void TheSecondJumpWaitsForTheReturnAsInTheConsole()
        {
            var s = new GameSession(new Simulation(TestData.Load(), 5));
            s.Sim.World.Gold = 1000;
            s.Sim.MarkAssessedForTests();
            for (int guard = 0; guard < 60 && !s.Sim.MachineReady; guard++)
            {
                foreach (var system in Simulation.MachineSystems) s.Sim.Repair(system);
                if (s.Sim.MachineStepsDone >= s.Sim.MachineStepsTotal) s.Sim.RestoreGold(s.Sim.MachineGoldNeeded);
                s.Do("end");
            }
            s.Do("jump"); s.Do("jump");
            Assert.True(s.Sim.Arrived);
            s.FinishArrival();
            Assert.False(s.OpenDeparture());
            Assert.False(s.Do("jump").Ok);
            Assert.False(s.Do("done").Ok);                          // not enough seen yet
            Assert.Equal(1, s.Sim.JumpsMade);
        }

        [Fact]
        public void EveryOfferedActionIsUnderstoodByTheSession()
        {
            // Whatever the catalog offers, the session runs (never "That isn't something you can do here").
            var s = new GameSession(new Simulation(TestData.Load(), 42));
            for (int month = 0; month < 12; month++)
            {
                foreach (var a in s.Actions().SelectMany(g => g.Items).ToList())
                {
                    if (a.Command == "quit" || a.Command == "end" || a.Command == "wait" || a.Command == "jump") continue;
                    var probe = new GameSession(CloneByReplay(s));
                    Assert.NotEqual("That isn't something you can do here.", probe.Do(a.Command).Message);
                }
                var card = s.CurrentDecision();
                if (card != null) s.Do(card.Choices[0].Command);
                s.Do("end");
            }
        }

        /// <summary>The same game as <paramref name="s"/>, rebuilt from the same seed and commands (no save system exists).</summary>
        [Fact]
        public void EveryPersonPlaceAndReturnSiteHasAnArtSlot()
        {
            var data = TestData.Load();
            var slots = ArtManifest.Slots(data);
            Assert.Equal(slots.Count, slots.Select(x => x.Key).Distinct().Count());
            foreach (var p in data.Content.People) Assert.Contains(slots, x => x.Key == ArtManifest.Portrait(p.Id));
            foreach (var id in new[] { "gaius", "marcus", "livia", "aulus", "felix", "cassianus", "serenus" }) Assert.Contains(slots, x => x.Key == "portrait." + id);
            foreach (var s in data.Content.ReturnSites) Assert.Contains(slots, x => x.Key == "site." + s.Id.ToLowerInvariant());
            foreach (var p in RomeMap.Places) Assert.Contains(slots, x => x.Key == p.ArtKey);
            Assert.Equal("Art/portraits/felix", slots.First(x => x.Key == "portrait.felix").ResourcePath);
            // Only places the simulation can describe are on the map (and the lodging, where the machine stands).
            Assert.All(RomeMap.Places.Where(p => p.Id != RomeMap.Lodging), p => Assert.Contains(p.Id, Simulation.WalkPlaces));
        }

        /// <summary>
        /// Where the Unity client shows each catalog group (unity/…/Screens.cs). A new group must be given a home here and there,
        /// so no P1 action is reachable only from the console.
        /// </summary>
        internal static readonly Dictionary<string, string> GraphicalHome = new Dictionary<string, string>
        {
            { "decide", "decision card" }, { "invitations", "decision card, Everything" }, { "commissions", "decision card (terms), map: market, Work" },
            { "money", "map: changers, Everything" }, { "challenges", "map: forges, Work" }, { "work", "map: market, Work" }, { "shop", "map: forges, Work" },
            { "projects", "map: curia, Work" }, { "institutions", "map: curia, Everything" }, { "inventions", "map: lodging, Work" },
            { "policy", "map: curia, Everything" }, { "priorities", "map: curia, Everything" }, { "machine", "map: lodging, Machine" },
            { "leave", "Prepare to leave → Departure" }, { "look", "HUD sections" }, { "month", "HUD: End Month, Fast-forward" },
            { "return", "Return screen" }, { "walk", "Return screen: walk around" }, { "then", "Return screen: learn more (the second jump is not offered: the slice ends with the return)" },
        };

        [Fact]
        public void EveryActionGroupHasAGraphicalHome()
        {
            var s = new GameSession(new Simulation(TestData.Load(), 42));
            var keys = new HashSet<string>(s.Actions().Select(g => g.Key));
            ClickThrough(s);
            keys.UnionWith(s.Actions().Select(g => g.Key));
            Assert.Empty(keys.Where(k => !GraphicalHome.ContainsKey(k)));
            // Groups tied to a place are on the map; the map shows only supported places.
            Assert.All(ActionCatalog.Era(new Simulation(TestData.Load(), 1)).Where(g => g.Place.Length > 0), g => Assert.NotNull(RomeMap.Find(g.Place)));
        }

        private static Simulation CloneByReplay(GameSession s)
        {
            var copy = new GameSession(new Simulation(TestData.Load(), s.Sim.Seed));
            foreach (var c in s.Commands) copy.Do(c);
            return copy.Sim;
        }
    }
}
