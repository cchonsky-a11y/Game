using System.Collections.Generic;
using System.Linq;
using Butterfly.Core;

namespace Butterfly.Presentation
{
    /// <summary>One replaceable art slot: its key, what it shows, and where a client looks for it.</summary>
    public sealed class ArtSlot
    {
        public string Key { get; }
        public string Kind { get; }
        public string Describes { get; }

        public ArtSlot(string key, string kind, string describes)
        {
            Key = key;
            Kind = kind;
            Describes = describes;
        }

        /// <summary>The Unity Resources path ("Art/portraits/felix"): drop an image there and it replaces the placeholder.</summary>
        public string ResourcePath => "Art/" + Key.Substring(0, Key.IndexOf('.')) + "s/" + Key.Substring(Key.IndexOf('.') + 1);
    }

    /// <summary>
    /// Every art slot the slice uses (P2): portraits for the people, the places on the map, the return's places, the map and
    /// the machine. Derived from content, so a new person or site gets a slot without code. Art is swapped by file, never by
    /// rewriting a scene.
    /// </summary>
    public static class ArtManifest
    {
        /// <summary>A person's portrait slot ("portrait.felix"): ids are lower-cased so file names are predictable.</summary>
        public static string Portrait(string personId) => "portrait." + personId.ToLowerInvariant();

        public static List<ArtSlot> Slots(GameData data)
        {
            var slots = new List<ArtSlot>
            {
                new ArtSlot("map.rome", "map", "the stylized map of Rome behind the places"),
                new ArtSlot("machine.core", "machine", "the machine: the Temporal Field Core and its support systems"),
                new ArtSlot("scene.opening", "scene", "the opening: dirt, a mule cart, Rome alive"),
                new ArtSlot("scene.jump", "scene", "the jump: decades passing in the dark"),
            };
            slots.AddRange(data.Content.People.Select(p => new ArtSlot(Portrait(p.Id), "portrait", p.Name + ", " + p.Role)));
            slots.AddRange(RomeMap.Places.Select(p => new ArtSlot(p.ArtKey, "place", p.Name + ": " + p.Caption)));
            slots.AddRange(data.Content.ReturnSites.Select(s => new ArtSlot("site." + s.Id.ToLowerInvariant(), "site", "a place in the changed city: " + s.Place)));
            return slots;
        }
    }
}
