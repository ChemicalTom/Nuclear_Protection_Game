using System.Collections.Generic;
using NuclearProtection.Config;
using UnityEngine;

namespace NuclearProtection.Rooms.Visuals
{
    /// <summary>Familles de matériaux du décor.</summary>
    public enum Mat { Concrete, Wainscot, Floor, Ceiling, PaintedMetal, DarkMetal, BrushedMetal, RedPaint, Hazard, Pipe, Neon, LampRed, LampAmber, LampGreen, Paper, Leather, Glass, Screen }

    /// <summary>
    /// Crée une seule fois (par machine) les matériaux du bunker à partir du matériau de base URP Lit,
    /// avec des textures générées en code (béton bruité, carrelage, rayures de danger). Aucun fichier image.
    /// </summary>
    public static class MaterialKit
    {
        private static readonly Dictionary<Mat, Material> Cache = new();
        private static Material _base;
        private static VisualSettings _v;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        public static void Init(Material baseMaterial, VisualSettings v)
        {
            if (_base == baseMaterial && _v == v) return;
            _base = baseMaterial;
            _v = v;
            Cache.Clear();
        }

        public static Material Get(Mat kind)
        {
            if (!Cache.TryGetValue(kind, out var m)) Cache[kind] = m = Create(kind);
            return m;
        }

        /// <summary>Copie d'un matériau avec une répétition de texture propre (murs de tailles différentes).</summary>
        public static Material Tiled(Mat kind, Vector2 tiling)
        {
            var m = new Material(Get(kind));
            m.SetTextureScale(BaseMap, tiling);
            return m;
        }

        /// <summary>Rend un matériau lumineux (écrans, néons, voyants).</summary>
        public static void SetGlow(Material m, Color color, float intensity)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor(Emission, color * intensity);
        }

        private static Material Create(Mat kind)
        {
            var v = _v;
            return kind switch
            {
                Mat.Concrete => Make(v.concreteColor, 0.15f, 0f, Textures.Concrete(v.concreteNoise)),
                Mat.Wainscot => Make(v.wainscotColor, 0.35f, 0f, Textures.Concrete(v.concreteNoise * 0.5f)),
                Mat.Floor => Make(v.floorColor, 0.3f, 0f, Textures.Tiles(v.concreteNoise)),
                Mat.Ceiling => Make(v.ceilingColor, 0.1f, 0f, Textures.Concrete(v.concreteNoise)),
                Mat.PaintedMetal => Make(v.paintedMetalColor, 0.45f, 0.3f, Textures.Concrete(0.15f)),
                Mat.DarkMetal => Make(v.darkMetalColor, 0.5f, 0.6f, null),
                Mat.BrushedMetal => Make(v.brushedMetalColor, 0.7f, 0.9f, null),
                Mat.RedPaint => Make(v.redPaintColor, 0.5f, 0.2f, null),
                Mat.Hazard => Make(Color.white, 0.3f, 0f, Textures.Hazard(v.hazardYellow, v.hazardBlack)),
                Mat.Pipe => Make(v.pipeColor, 0.4f, 0.5f, null),
                Mat.Neon => Glow(v.neonColor, v.neonGlow),
                Mat.LampRed => Glow(new Color(1f, 0.15f, 0.1f), v.lampGlow),
                Mat.LampAmber => Glow(new Color(1f, 0.6f, 0.1f), v.lampGlow),
                Mat.LampGreen => Glow(new Color(0.2f, 1f, 0.3f), v.lampGlow),
                Mat.Paper => Make(new Color(0.9f, 0.87f, 0.75f), 0.05f, 0f, null),
                Mat.Leather => Make(new Color(0.3f, 0.18f, 0.1f), 0.35f, 0f, null),
                Mat.Glass => Make(new Color(0.6f, 0.75f, 0.7f), 0.95f, 0f, null),
                Mat.Screen => Make(new Color(0.02f, 0.03f, 0.02f), 0.85f, 0f, Textures.Scanlines()),
                _ => Make(Color.magenta, 0f, 0f, null)
            };
        }

        private static Material Make(Color color, float smoothness, float metallic, Texture2D tex)
        {
            var m = new Material(_base) { name = $"NP_{color}" };
            m.SetColor(BaseColor, color);
            m.SetFloat(Smoothness, smoothness);
            m.SetFloat(Metallic, metallic);
            if (tex != null) m.SetTexture(BaseMap, tex);
            return m;
        }

        private static Material Glow(Color color, float intensity)
        {
            var m = Make(color, 0.6f, 0f, null);
            SetGlow(m, color, intensity);
            return m;
        }
    }
}
