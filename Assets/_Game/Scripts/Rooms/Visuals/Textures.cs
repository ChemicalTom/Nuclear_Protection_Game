using UnityEngine;

namespace NuclearProtection.Rooms.Visuals
{
    /// <summary>Petites textures générées en code (en niveaux de gris, teintées par la couleur du matériau).</summary>
    public static class Textures
    {
        private const int Size = 128;

        /// <summary>Béton : bruit doux + petites taches.</summary>
        public static Texture2D Concrete(float noise) => Generate("Beton", (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.6f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.4f;
            return 1f - noise * 0.5f + (n - 0.5f) * noise;
        });

        /// <summary>Carrelage : joints sombres sur béton (une dalle par répétition de texture).</summary>
        public static Texture2D Tiles(float noise) => Generate("Dalles", (x, y) =>
        {
            bool joint = x < 3 || y < 3;
            float n = Mathf.PerlinNoise(x * 0.08f + 7f, y * 0.08f) - 0.5f;
            return joint ? 0.55f : 1f - noise * 0.3f + n * noise * 0.6f;
        });

        /// <summary>Lignes de balayage d'un écran CRT.</summary>
        public static Texture2D Scanlines() => Generate("Balayage", (x, y) => y % 4 == 0 ? 0.55f : 1f);

        /// <summary>Rayures diagonales jaune/noir.</summary>
        public static Texture2D Hazard(Color yellow, Color black)
        {
            var tex = NewTexture("Danger");
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    tex.SetPixel(x, y, ((x + y) / (Size / 4)) % 2 == 0 ? yellow : black);
            tex.Apply();
            return tex;
        }

        private static Texture2D Generate(string name, System.Func<int, int, float> shade)
        {
            var tex = NewTexture(name);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float g = Mathf.Clamp01(shade(x, y));
                    tex.SetPixel(x, y, new Color(g, g, g, 1f));
                }
            tex.Apply();
            return tex;
        }

        private static Texture2D NewTexture(string name) =>
            new(Size, Size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
    }
}
