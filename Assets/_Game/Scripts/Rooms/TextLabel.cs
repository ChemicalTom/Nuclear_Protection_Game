using UnityEngine;

namespace NuclearProtection.Rooms
{
    /// <summary>Crée un texte 3D simple (TextMesh, police intégrée) pour la graybox.</summary>
    public static class TextLabel
    {
        private static Font _font;

        public static TextMesh Create(Transform parent, string name, string text, float characterSize)
        {
            _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = _font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = characterSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            return tm;
        }
    }
}
