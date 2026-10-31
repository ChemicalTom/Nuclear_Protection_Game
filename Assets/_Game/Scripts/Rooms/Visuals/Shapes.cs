using UnityEngine;

namespace NuclearProtection.Rooms.Visuals
{
    /// <summary>Assemblage de formes simples (sans collision) pour les modèles du décor.</summary>
    public static class Shapes
    {
        /// <summary>Pavé centré en localPos, taille en mètres.</summary>
        public static Renderer Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, Vector3 euler = default) =>
            Make(PrimitiveType.Cube, parent, name, localPos, size, mat, euler);

        /// <summary>Cylindre vertical (axe Y local) : diamètre et hauteur en mètres.</summary>
        public static Renderer Cylinder(Transform parent, string name, Vector3 localPos, float diameter, float height, Material mat, Vector3 euler = default) =>
            Make(PrimitiveType.Cylinder, parent, name, localPos, new Vector3(diameter, height / 2f, diameter), mat, euler);

        public static Renderer Sphere(Transform parent, string name, Vector3 localPos, float diameter, Material mat) =>
            Make(PrimitiveType.Sphere, parent, name, localPos, Vector3.one * diameter, mat, default);

        private static Renderer Make(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>()); // décor : seul le poste (ou le mur) a une collision
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return r;
        }
    }
}
