using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>
    /// Small sprites for the start screen, drawn in code once per session (no image assets): a bevelled frame
    /// (fill + outline, 9-sliced), a tileable hex grid for the selected item, and the glowing arc over the logo.
    /// </summary>
    public static class MenuArt
    {
        private static Sprite _bevelFill, _bevelLine, _hex, _arc, _hudFill, _hudLine, _dots, _hatch, _slant;

        public static Sprite BevelFill => _bevelFill != null ? _bevelFill : _bevelFill = Bevel(false);
        public static Sprite BevelLine => _bevelLine != null ? _bevelLine : _bevelLine = Bevel(true);
        public static Sprite Hex => _hex != null ? _hex : _hex = HexTile();
        public static Sprite Arc => _arc != null ? _arc : _arc = ArcSprite();

        /// <summary>Glass HUD panel (big cuts top-left and bottom-right, small elsewhere): fill fades downwards.</summary>
        public static Sprite HudFill => _hudFill != null ? _hudFill : _hudFill = Hud(false);
        public static Sprite HudLine => _hudLine != null ? _hudLine : _hudLine = Hud(true);
        /// <summary>Tileable row of small dots (horizontal).</summary>
        public static Sprite Dots => _dots != null ? _dots : _dots = DotTile();
        /// <summary>Tileable diagonal hazard hatching.</summary>
        public static Sprite Hatch => _hatch != null ? _hatch : _hatch = HatchTile();
        /// <summary>Solid tab with a 45-degree slanted right end (accent bars on the frame).</summary>
        public static Sprite Slant => _slant != null ? _slant : _slant = SlantTab();

        private const int HudSize = 128, HudBig = 30, HudSmall = 8;

        private static Sprite Hud(bool outline)
        {
            const int n = HudSize;
            var t = NewTexture(n, n, TextureWrapMode.Clamp);
            const float s2 = 1.41421356f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f, m = n;
                    float d = Mathf.Min(Mathf.Min(px, m - px), Mathf.Min(py, m - py));
                    d = Mathf.Min(d, (px + (m - py) - HudBig) / s2);        // top-left
                    d = Mathf.Min(d, ((m - px) + (m - py) - HudSmall) / s2); // top-right
                    d = Mathf.Min(d, (px + py - HudSmall) / s2);             // bottom-left
                    d = Mathf.Min(d, ((m - px) + py - HudBig) / s2);         // bottom-right
                    float a;
                    if (outline) a = Mathf.Clamp01(d + 0.5f) * Mathf.Clamp01(2.5f - d);
                    else a = Mathf.Clamp01(d + 0.5f) * Mathf.Lerp(0.72f, 1f, py / m); // glass: a touch denser at the top
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            const float b = HudBig + 4;
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        private static Sprite DotTile()
        {
            const int w = 14, h = 10;
            var t = NewTexture(w, h, TextureWrapMode.Repeat);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w / 2f, h / 2f));
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(3.2f - d)));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite HatchTile()
        {
            const int n = 12;
            var t = NewTexture(n, n, TextureWrapMode.Repeat);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = Mathf.Repeat(x + y + 1f, n);           // 45-degree stripes, period 12 px
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Min(u, 6f - u) + 0.5f) * (u < 6f ? 1f : 0f)));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite SlantTab()
        {
            const int w = 64, h = 16;
            var t = NewTexture(w, h, TextureWrapMode.Clamp);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float edge = (w - h) + (y + 0.5f) - (x + 0.5f); // right end slants
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(edge + 0.5f)));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 0, h + 2, 0));
        }

        private const int BevelSize = 64, Cut = 14;

        /// <summary>Rectangle with all four corners cut at 45 degrees; the outline variant is a 2 px border.</summary>
        private static Sprite Bevel(bool outline)
        {
            const int n = BevelSize;
            var t = NewTexture(n, n, TextureWrapMode.Clamp);
            const float s2 = 1.41421356f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f, m = n;
                    float d = Mathf.Min(Mathf.Min(px, m - px), Mathf.Min(py, m - py));
                    d = Mathf.Min(d, (px + py - Cut) / s2);
                    d = Mathf.Min(d, (m - px + py - Cut) / s2);
                    d = Mathf.Min(d, (px + m - py - Cut) / s2);
                    d = Mathf.Min(d, (m - px + m - py - Cut) / s2);
                    float a = outline ? Mathf.Clamp01(d + 0.5f) * Mathf.Clamp01(2.5f - d) : Mathf.Clamp01(d + 0.5f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(Cut + 4, Cut + 4, Cut + 4, Cut + 4));
        }

        /// <summary>Pointy-top hexagon outlines, tileable (18 x 30 px tile, two hex rows).</summary>
        private static Sprite HexTile()
        {
            const int w = 18, h = 30;
            const float inradius = 8.2f;
            var t = NewTexture(w, h, TextureWrapMode.Repeat);
            var centres = new[] { new Vector2(0, 0), new Vector2(w, 0), new Vector2(0, h), new Vector2(w, h), new Vector2(w / 2f, h / 2f) };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float best = float.MaxValue;
                    foreach (var c in centres)
                    {
                        float dx = Mathf.Abs(p.x - c.x), dy = Mathf.Abs(p.y - c.y);
                        best = Mathf.Min(best, Mathf.Max(dx, dx * 0.5f + dy * 0.866f));
                    }
                    float a = Mathf.Clamp01(best - (inradius - 1.2f));
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>A thin glowing arc (part of a large circle), fading towards both ends.</summary>
        private static Sprite ArcSprite()
        {
            const int w = 512, h = 96;
            var t = NewTexture(w, h, TextureWrapMode.Clamp);
            var centre = new Vector2(w * 0.5f, -620f);
            const float radius = 700f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Abs(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) - radius);
                    float core = Mathf.Clamp01(2.2f - d);
                    float glow = Mathf.Exp(-d * d / 60f) * 0.45f;
                    float ends = Mathf.Sin(Mathf.PI * (x + 0.5f) / w);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow) * ends * ends));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTexture(int w, int h, TextureWrapMode wrap)
            => new(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
    }
}
