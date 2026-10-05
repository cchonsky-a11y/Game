#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Butterfly.Unity
{
    /// <summary>
    /// Small UI Toolkit builders and the slice's placeholder look (P2). Everything is built in code, so the project needs no
    /// UXML, USS or scene setup to run; a later art pass can replace styles here and images through <see cref="ArtLibrary"/>.
    /// </summary>
    public static class Ui
    {
        // Placeholder palette: warm stone, ink and terracotta (replaceable in the art pass).
        public static readonly Color Paper = new Color(0.95f, 0.92f, 0.85f);
        public static readonly Color PaperDark = new Color(0.88f, 0.83f, 0.73f);
        public static readonly Color Ink = new Color(0.16f, 0.13f, 0.11f);
        public static readonly Color InkSoft = new Color(0.38f, 0.33f, 0.28f);
        public static readonly Color Terracotta = new Color(0.67f, 0.32f, 0.20f);
        public static readonly Color Bronze = new Color(0.55f, 0.43f, 0.22f);
        public static readonly Color Night = new Color(0.10f, 0.10f, 0.13f);
        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.55f);

        public static Font? BuiltinFont;

        public static VisualElement Box(string? name = null)
        {
            var v = new VisualElement();
            if (name != null) v.name = name;
            return v;
        }

        public static VisualElement Row(float gap = 8)
        {
            var v = new VisualElement();
            v.style.flexDirection = FlexDirection.Row;
            v.style.flexWrap = Wrap.Wrap;
            v.style.alignItems = Align.Center;
            return v;
        }

        public static VisualElement Column()
        {
            var v = new VisualElement();
            v.style.flexDirection = FlexDirection.Column;
            return v;
        }

        public static Label Text(string text, int size = 16, Color? color = null, bool bold = false)
        {
            var l = new Label(text);
            l.enableRichText = false;
            l.style.fontSize = size;
            l.style.color = color ?? Ink;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginBottom = 6;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        public static Label Heading(string text, int size = 22) => Text(text, size, Ink, true);

        public static Button Button(string text, Action onClick, bool primary = false, bool enabled = true)
        {
            var b = new Button(onClick) { text = text };
            b.enableRichText = false;
            b.SetEnabled(enabled);
            b.style.fontSize = 15;
            b.style.whiteSpace = WhiteSpace.Normal;
            b.style.unityTextAlign = TextAnchor.MiddleLeft;
            b.style.paddingLeft = 12; b.style.paddingRight = 12; b.style.paddingTop = 7; b.style.paddingBottom = 7;
            b.style.marginRight = 6; b.style.marginBottom = 6; b.style.marginLeft = 0; b.style.marginTop = 0;
            b.style.backgroundColor = primary ? Terracotta : PaperDark;
            b.style.color = primary ? Paper : Ink;
            Border(b, primary ? Terracotta : Bronze, 1, 6);
            return b;
        }

        public static void Border(VisualElement v, Color c, float width, float radius)
        {
            v.style.borderTopColor = c; v.style.borderBottomColor = c; v.style.borderLeftColor = c; v.style.borderRightColor = c;
            v.style.borderTopWidth = width; v.style.borderBottomWidth = width; v.style.borderLeftWidth = width; v.style.borderRightWidth = width;
            v.style.borderTopLeftRadius = radius; v.style.borderTopRightRadius = radius; v.style.borderBottomLeftRadius = radius; v.style.borderBottomRightRadius = radius;
        }

        public static void Pad(VisualElement v, float p)
        {
            v.style.paddingTop = p; v.style.paddingBottom = p; v.style.paddingLeft = p; v.style.paddingRight = p;
        }

        /// <summary>A paper card.</summary>
        public static VisualElement Card(Color? background = null)
        {
            var c = Column();
            c.style.backgroundColor = background ?? Paper;
            Border(c, Bronze, 1, 8);
            Pad(c, 14);
            c.style.marginBottom = 10;
            return c;
        }

        public static ScrollView Scroll()
        {
            var s = new ScrollView(ScrollViewMode.Vertical);
            s.style.flexGrow = 1;
            return s;
        }

        /// <summary>A bar from 0 to 1 (machine progress, gold restored).</summary>
        public static VisualElement Bar(float fraction, Color fill)
        {
            var back = Box();
            back.style.height = 10;
            back.style.backgroundColor = PaperDark;
            Border(back, Bronze, 1, 5);
            back.style.marginBottom = 8;
            var front = Box();
            front.style.height = Length.Percent(100);
            front.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
            front.style.backgroundColor = fill;
            back.Add(front);
            return back;
        }

        public static void Fill(VisualElement v)
        {
            v.style.flexGrow = 1;
            v.style.flexShrink = 1;
        }
    }
}
