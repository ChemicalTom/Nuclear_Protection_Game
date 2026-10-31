using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>Texture du fond radar (disque sombre + cercles de distance), générée une seule fois.</summary>
    public static class RadarTextures
    {
        private static Texture2D _scope;

        public static Texture2D Scope => _scope ??= BuildScope(256);

        private static Texture2D BuildScope(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var fill = new Color(0.02f, 0.12f, 0.05f, 1f);
            var ring = new Color(0.15f, 0.55f, 0.25f, 1f);
            float half = size / 2f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                Color c = Color.clear;
                if (d <= 1f)
                {
                    c = fill;
                    // Cercles à 25/50/75/100 % et croix centrale.
                    bool onRing = Mathf.Abs(d * 4f - Mathf.Round(d * 4f)) < 0.03f && d > 0.1f;
                    bool onCross = Mathf.Abs(x - half) < 0.8f || Mathf.Abs(y - half) < 0.8f;
                    if (onRing || onCross) c = ring;
                }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}
