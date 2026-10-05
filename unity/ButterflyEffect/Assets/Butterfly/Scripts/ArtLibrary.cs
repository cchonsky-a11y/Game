#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Butterfly.Unity
{
    /// <summary>
    /// Replaceable art (P2). A slot key ("portrait.felix", "place.forges", "site.fittings") maps to a Resources path
    /// (Assets/Butterfly/Resources/Art/portraits/felix.png and so on; see the art manifest in
    /// docs/P2_GRAPHICAL_VERTICAL_SLICE.md). If no image is there, a placeholder is drawn: a tinted panel with initials.
    /// Swapping art never touches a scene or a script.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Texture2D?> Cache = new Dictionary<string, Texture2D?>();

        public static string ResourcePath(string key)
        {
            int dot = key.IndexOf('.');
            return dot < 0 ? "Art/" + key : "Art/" + key.Substring(0, dot) + "s/" + key.Substring(dot + 1);
        }

        public static Texture2D? Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!Cache.TryGetValue(key, out var tex))
            {
                tex = Resources.Load<Texture2D>(ResourcePath(key));
                Cache[key] = tex;
            }
            return tex;
        }

        /// <summary>An art slot of the given size: the image if one exists, else a placeholder with a caption.</summary>
        public static VisualElement Slot(string key, string caption, float width, float height)
        {
            var box = Ui.Box("art:" + key);
            box.style.width = width;
            box.style.height = height;
            box.style.flexShrink = 0;
            Ui.Border(box, Ui.Bronze, 1, 6);
            box.style.overflow = Overflow.Hidden;
            var tex = Find(key);
            if (tex != null)
            {
                box.style.backgroundImage = new StyleBackground(tex);
                return box;
            }
            box.style.backgroundColor = Tint(key);
            box.style.justifyContent = Justify.Center;
            box.style.alignItems = Align.Center;
            var l = Ui.Text(Initials(caption), (int)Mathf.Clamp(height / 3f, 12, 40), Ui.Paper, true);
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(l);
            return box;
        }

        private static string Initials(string caption)
        {
            if (string.IsNullOrEmpty(caption)) return "";
            var parts = caption.Split(' ');
            return parts.Length == 1 ? caption.Substring(0, Mathf.Min(2, caption.Length)) : (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
        }

        /// <summary>A stable muted colour per key, so each placeholder is recognizable.</summary>
        private static Color Tint(string key)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in key) h = h * 31 + c;
                float hue = (h & 0xFFFF) / 65535f;
                return Color.HSVToRGB(hue, 0.35f, 0.55f);
            }
        }
    }
}
