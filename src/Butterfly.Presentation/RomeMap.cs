using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Presentation
{
    /// <summary>A place on the stylized map of Rome.</summary>
    public sealed class MapPlace
    {
        public string Id { get; }
        public string Name { get; }
        /// <summary>A short caption under the name (what kind of place it is), never what will happen there.</summary>
        public string Caption { get; }
        /// <summary>Position on the map, 0..1 from the left and from the top, so any map art can be dropped in behind it.</summary>
        public float X { get; }
        public float Y { get; }
        /// <summary>The key of this place's art in the art manifest (docs/P2_GRAPHICAL_VERTICAL_SLICE.md).</summary>
        public string ArtKey => "place." + Id;

        public MapPlace(string id, string name, string caption, float x, float y)
        {
            Id = id;
            Name = name;
            Caption = caption;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// The places the game supports (P2): the walk's five places (Simulation.WalkPlaces) and the inventor's lodging, where
    /// the machine stands. Looking around a walk place is free and changes nothing (Simulation.Visit).
    /// </summary>
    public static class RomeMap
    {
        public const string Lodging = "lodging";
        public const string Market = "market";
        public const string Changers = "changers";
        public const string Forges = "forges";
        public const string Curia = "curia";
        public const string Subura = "subura";

        public static readonly IReadOnlyList<MapPlace> Places = new List<MapPlace>
        {
            new MapPlace(Lodging, "Your lodging", "where the machine stands", 0.30f, 0.30f),
            new MapPlace(Subura, "The Subura", "streets, tenements, the district fountain", 0.42f, 0.22f),
            new MapPlace(Curia, "The Forum and the Curia", "the Senate house, the guilds, the great houses", 0.50f, 0.50f),
            new MapPlace(Market, "The market", "stalls, prices, people who need things fixed", 0.62f, 0.42f),
            new MapPlace(Changers, "The money changers", "aurei into denarii, and back", 0.55f, 0.62f),
            new MapPlace(Forges, "The forges", "smiths, workshops, the river's mills", 0.30f, 0.70f),
        };

        public static MapPlace? Find(string id) => Places.FirstOrDefault(p => p.Id == id);

        /// <summary>True for the places Simulation.Visit describes (the lodging has no walk scene).</summary>
        public static bool IsWalkPlace(string id) => id != Lodging && Places.Any(p => p.Id == id);
    }
}
