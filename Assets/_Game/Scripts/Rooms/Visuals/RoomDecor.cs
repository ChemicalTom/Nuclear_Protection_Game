using NuclearProtection.Config;
using UnityEngine;

namespace NuclearProtection.Rooms.Visuals
{
    /// <summary>
    /// Décor d'un bunker (sans collision, purement visuel) : soubassement peint, néons, tuyaux,
    /// chemin de câbles, porte blindée scellée et panneau « accès restreint ».
    /// </summary>
    public static class RoomDecor
    {
        public static void Build(Transform root, BunkerInfo b, RoomSettings r, VisualSettings v, bool shared)
        {
            float w = b.Size.x, d = b.Size.y, h = r.wallHeight;
            Wainscot(root, w, d, v.wainscotHeight);
            Neons(root, w, d, h);
            Pipes(root, w, d, h);
            BlastDoor(root, w, 1f);  // mur est
            BlastDoor(root, w, -1f); // mur ouest : une porte de chaque côté
            Sign(root, d, h);
            if (shared) // bunker commun : ligne jaune au sol entre l'espace Radar et l'espace Tir
                Shapes.Box(root, "LigneCentrale", new Vector3(0, 0.004f, 0), new Vector3(0.12f, 0.008f, d), MaterialKit.Get(Mat.Hazard));
        }

        // Bas des murs peint en vert militaire, avec une baguette sombre.
        private static void Wainscot(Transform root, float w, float d, float height)
        {
            if (height <= 0f) return;
            const float e = 0.02f;
            var paint = MaterialKit.Get(Mat.Wainscot);
            var trim = MaterialKit.Get(Mat.DarkMetal);
            Panel(root, "Nord", new Vector3(0, 0, d / 2f - e), new Vector3(w, height, e));
            Panel(root, "Sud", new Vector3(0, 0, -d / 2f + e), new Vector3(w, height, e));
            Panel(root, "Est", new Vector3(w / 2f - e, 0, 0), new Vector3(e, height, d));
            Panel(root, "Ouest", new Vector3(-w / 2f + e, 0, 0), new Vector3(e, height, d));

            void Panel(Transform p, string side, Vector3 pos, Vector3 size)
            {
                Shapes.Box(p, $"Soubassement_{side}", pos + Vector3.up * height / 2f, size, paint);
                Shapes.Box(p, $"Baguette_{side}", pos + Vector3.up * height, new Vector3(Mathf.Max(size.x, 0.04f), 0.05f, Mathf.Max(size.z, 0.04f)), trim);
            }
        }

        // Rangées de tubes néon au plafond (environ un tous les 3 m).
        private static void Neons(Transform root, float w, float d, float h)
        {
            int cols = Mathf.Max(1, Mathf.RoundToInt(w / 3f));
            for (int c = 0; c < cols; c++)
                for (int row = -1; row <= 1; row += 2)
                {
                    var pos = new Vector3(-w / 2f + (c + 0.5f) * w / cols, h - 0.06f, row * d / 4f);
                    Shapes.Box(root, $"Neon_{c}_{row}", pos + Vector3.up * 0.03f, new Vector3(1.3f, 0.05f, 0.22f), MaterialKit.Get(Mat.DarkMetal));
                    Shapes.Box(root, $"Tube_{c}_{row}", pos - Vector3.up * 0.01f, new Vector3(1.2f, 0.04f, 0.06f), MaterialKit.Get(Mat.Neon));
                }
        }

        // Trois tuyaux le long du mur nord, un chemin de câbles le long du mur sud.
        private static void Pipes(Transform root, float w, float d, float h)
        {
            var pipe = MaterialKit.Get(Mat.Pipe);
            for (int i = 0; i < 3; i++)
            {
                var pos = new Vector3(0, h - 0.25f - i * 0.05f, d / 2f - 0.2f - i * 0.16f);
                Shapes.Cylinder(root, $"Tuyau_{i}", pos, 0.1f + i * 0.03f, w, pipe, new Vector3(0, 0, 90f));
                for (float x = -w / 2f + 1f; x < w / 2f; x += 2.5f) // colliers de fixation
                    Shapes.Box(root, $"Collier_{i}_{x:0}", new Vector3(x, pos.y + 0.12f, pos.z), new Vector3(0.04f, 0.24f, 0.04f), MaterialKit.Get(Mat.DarkMetal));
            }
            Shapes.Box(root, "CheminCables", new Vector3(0, h - 0.3f, -d / 2f + 0.2f), new Vector3(w, 0.06f, 0.25f), MaterialKit.Get(Mat.DarkMetal));
            Shapes.Box(root, "Cables", new Vector3(0, h - 0.25f, -d / 2f + 0.2f), new Vector3(w, 0.05f, 0.18f), MaterialKit.Get(Mat.DarkMetal));
        }

        // Porte blindée ronde (scellée : les bunkers sont isolés) ; side = +1 mur est, -1 mur ouest.
        private static void BlastDoor(Transform root, float w, float side)
        {
            var door = new GameObject(side > 0f ? "PorteBlindee_Est" : "PorteBlindee_Ouest").transform;
            door.SetParent(root, false);
            door.localPosition = new Vector3(side * (w / 2f - 0.05f), 1.2f, 0);
            door.localRotation = Quaternion.Euler(0, side * 90f, 0); // -Z local = vers l'intérieur du bunker
            Shapes.Cylinder(door, "Cadre", Vector3.zero, 2.3f, 0.12f, MaterialKit.Get(Mat.DarkMetal), new Vector3(90f, 0, 0));
            Shapes.Cylinder(door, "Battant", new Vector3(0, 0, -0.08f), 2f, 0.12f, MaterialKit.Get(Mat.PaintedMetal), new Vector3(90f, 0, 0));
            Shapes.Cylinder(door, "Moyeu", new Vector3(0, 0, -0.2f), 0.2f, 0.15f, MaterialKit.Get(Mat.BrushedMetal), new Vector3(90f, 0, 0));
            for (int i = 0; i < 3; i++) // volant à 3 rayons
                Shapes.Box(door, $"Rayon_{i}", new Vector3(0, 0, -0.24f), new Vector3(0.9f, 0.05f, 0.05f), MaterialKit.Get(Mat.BrushedMetal), new Vector3(0, 0, i * 60f));
            Shapes.Box(door, "Danger", new Vector3(0, -1.25f, -0.05f), new Vector3(2.3f, 0.12f, 0.02f), MaterialKit.Get(Mat.Hazard));
        }

        // Panneau jaune « accès restreint » sur le mur nord, à côté de l'horloge.
        private static void Sign(Transform root, float d, float h)
        {
            var pos = new Vector3(-2.2f, h - 0.75f, d / 2f - 0.03f);
            Shapes.Box(root, "Panneau", pos, new Vector3(1.5f, 0.45f, 0.02f), MaterialKit.Get(Mat.Hazard));
            Shapes.Box(root, "PanneauFond", pos - Vector3.forward * 0.012f, new Vector3(1.36f, 0.33f, 0.005f), MaterialKit.Get(Mat.Paper));
            var text = TextLabel.Create(root, "PanneauTexte", "ACCÈS RESTREINT\nDÉFENSE AÉRIENNE", 0.022f);
            text.color = new Color(0.1f, 0.1f, 0.1f);
            text.transform.localPosition = pos - Vector3.forward * 0.02f; // comme l'horloge : lisible depuis l'intérieur
        }
    }
}
