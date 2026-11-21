using UnityEngine;
using static LastSignal.EditorTools.BuilderKit;

namespace LastSignal.EditorTools
{
    /// <summary>Shared "Brass &amp; Signal" station materials (see Documentation/ArtDirection.md). Create() loads or updates them.</summary>
    public static class StationMaterials
    {
        public static Material TrimRondel, TealPanel, Floor, Wall, Ceiling, Brass, Terracotta, SlateDark, Ceramic, GlassTeal, CyanStrip, AmberLamp, LampOff, RedLamp, StatusLight, WindowGlow, Rubber;

        public static void Create()
        {
            Floor = Lit("M_Floor_SlateTile", Color.Lerp(Navy, MetalGrey, 0.3f), 0.42f, 0.2f);
            Wall = Lit("M_Wall_PaintedMetal", Color.Lerp(Slate, MetalGrey, 0.26f), 0.3f, 0.12f);
            Ceiling = Lit("M_Ceiling_Navy", Navy, 0.15f);
            SlateDark = Lit("M_Slate_Dark", Color.Lerp(Navy, Slate, 0.5f), 0.3f, 0.2f);
            Brass = Lit("M_Brass_Polished", Hex("#D19445"), 0.58f, 0.75f);
            Terracotta = Lit("M_Terracotta_Enamel", Color.Lerp(BuilderKit.Terracotta, Navy, 0.18f), 0.42f);
            Ceramic = Lit("M_Ceramic_White", new Color(0.86f, 0.84f, 0.78f), 0.55f);
            Rubber = Lit("M_Rubber_Black", new Color(0.05f, 0.06f, 0.07f), 0.1f);
            GlassTeal = Glass("M_Glass_Teal", Teal, 0.45f, 0.6f);
            WindowGlow = Emissive("M_Window_DeepSpace", new Color(0.04f, 0.11f, 0.16f), 0.8f, Navy * 0.25f);
            WindowGlow.SetFloat("_Smoothness", 0.7f);
            CyanStrip = Emissive("M_Emissive_SignalCyan", SignalCyan, 0.9f, SignalCyan * 0.5f);
            AmberLamp = Emissive("M_Emissive_AmberLamp", Amber, 1.8f, Amber);
            LampOff = Lit("M_Lamp_Off", new Color(0.18f, 0.17f, 0.15f), 0.6f);
            RedLamp = Emissive("M_Emissive_EmergencyRed", EmergencyRed, 1.1f, EmergencyRed * 0.5f);
            StatusLight = Emissive("M_Emissive_Status", Color.white, 1f, Color.white);

            // painted surface detail (art pass): hue from _BaseColor, detail + normals from the generated maps
            StationTextures.Apply(Wall, StationTextures.PaintedMetal());
            StationTextures.Apply(Floor, StationTextures.FloorPlate());
            StationTextures.Apply(Ceiling, StationTextures.CeilingPanel());
            StationTextures.Apply(Brass, StationTextures.Brass(), 0.6f);
            var enamel = StationTextures.Enamel();
            StationTextures.Apply(Terracotta, enamel, 0.8f);
            StationTextures.Apply(SlateDark, enamel, 0.6f);
            StationTextures.Apply(Ceramic, enamel, 0.5f);

            // ornament (reference A): rondel trim band and teal enamel inset panels
            TrimRondel = Lit("M_Trim_Rondel", Color.white, 0.45f, 0.15f);
            StationTextures.Apply(TrimRondel, StationTextures.RondelTrim());
            TealPanel = Lit("M_Enamel_Teal", Color.Lerp(Teal, Navy, 0.47f), 0.5f, 0.05f);
            StationTextures.Apply(TealPanel, enamel, 0.6f);
        }

        /// <summary>Gives a builder's own material the same painted detail (e.g. a sector-tinted wall).</summary>
        public static Material Painted(Material mat, StationTextures.Maps maps, float normal = 1f)
        {
            StationTextures.Apply(mat, maps, normal);
            return mat;
        }
    }
}
