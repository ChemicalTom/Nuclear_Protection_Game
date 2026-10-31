using UnityEngine;
using UnityEngine.Rendering;

namespace NuclearProtection.Rooms
{
    /// <summary>Crée un texte 3D simple (TextMesh, police intégrée), caché par les objets placés devant.</summary>
    public static class TextLabel
    {
        private static Font _font;
        private static Material _material;

        public static TextMesh Create(Transform parent, string name, string text, float characterSize)
        {
            _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_material == null)
            {
                // Le matériau de police par défaut se dessine par-dessus tout : on lui rend le test de profondeur.
                _material = new Material(_font.material);
                _material.SetFloat("unity_GUIZTestMode", (float)CompareFunction.LessEqual);
            }

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
            go.GetComponent<MeshRenderer>().sharedMaterial = _material;
            return tm;
        }
    }
}
