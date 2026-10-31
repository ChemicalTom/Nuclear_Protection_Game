using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Rooms.Visuals
{
    /// <summary>
    /// Modèle « guerre froide » de chaque type de poste, assemblé en formes simples.
    /// Repère : pied du poste au sol, face avant vers -Z (vers le joueur), taille = boîte d'interaction.
    /// Renvoie la pièce « témoin » qui prend la couleur d'état du poste (écran, porte, plaque…).
    /// </summary>
    public static class StationModels
    {
        public static Renderer Build(Transform root, StationType type, Vector3 s) => type switch
        {
            StationType.Radar => Console(root, s, true),
            StationType.PupitreTir => Console(root, s, false),
            StationType.Registre => Cabinet(root, s),
            StationType.CleConv1 or StationType.CleConv2 => KeyPost(root, s, Mat.PaintedMetal),
            StationType.SerrureNucA or StationType.SerrureNucB => KeyPost(root, s, Mat.RedPaint),
            StationType.Telephone => PhoneStand(root, s),
            StationType.Fusible => FuseBox(root, s),
            StationType.Mallette => BriefcaseTable(root, s),
            _ => Shapes.Box(root, "Corps", Vector3.up * s.y / 2f, s, MaterialKit.Get(Mat.PaintedMetal))
        };

        // Console : meuble, pupitre incliné, boîtier d'écran CRT au-dessus.
        private static Renderer Console(Transform r, Vector3 s, bool radar)
        {
            float bodyH = s.y * 0.75f;
            Shapes.Box(r, "Meuble", new Vector3(0, bodyH / 2f, 0.05f), new Vector3(s.x, bodyH, s.z - 0.1f), MaterialKit.Get(Mat.PaintedMetal));
            Shapes.Box(r, "Socle", new Vector3(0, 0.04f, 0.05f), new Vector3(s.x + 0.04f, 0.08f, s.z - 0.06f), MaterialKit.Get(Mat.DarkMetal));
            Shapes.Box(r, "Pupitre", new Vector3(0, bodyH + 0.06f, -0.08f), new Vector3(s.x, 0.06f, s.z * 0.7f), MaterialKit.Get(Mat.DarkMetal), new Vector3(-18f, 0, 0));

            // Boutons et voyants sur le pupitre.
            for (int i = 0; i < 6; i++)
            {
                float x = -s.x * 0.4f + i * s.x * 0.16f;
                var lamp = i % 3 == 0 ? Mat.LampRed : i % 3 == 1 ? Mat.LampAmber : Mat.LampGreen;
                Shapes.Cylinder(r, $"Voyant_{i}", new Vector3(x, bodyH + 0.1f, -0.25f), 0.05f, 0.03f, MaterialKit.Get(lamp), new Vector3(-18f, 0, 0));
                Shapes.Cylinder(r, $"Bouton_{i}", new Vector3(x, bodyH + 0.12f, -0.08f), 0.06f, 0.05f, MaterialKit.Get(Mat.BrushedMetal), new Vector3(-18f, 0, 0));
            }
            if (!radar) // pupitre de tir : clavier et gros bouton rouge sous capot
            {
                for (int k = 0; k < 9; k++)
                    Shapes.Box(r, $"Touche_{k}", new Vector3(s.x * 0.3f + (k % 3 - 1) * 0.07f, bodyH + 0.12f, 0.02f + (k / 3 - 1) * 0.06f), new Vector3(0.05f, 0.02f, 0.045f), MaterialKit.Get(Mat.Paper), new Vector3(-18f, 0, 0));
                Shapes.Cylinder(r, "GrosBouton", new Vector3(-s.x * 0.3f, bodyH + 0.14f, 0.02f), 0.14f, 0.05f, MaterialKit.Get(Mat.LampRed), new Vector3(-18f, 0, 0));
            }

            // Boîtier CRT à l'arrière, écran sur sa face avant.
            float housingY = bodyH + 0.42f;
            Shapes.Box(r, "BoitierEcran", new Vector3(0, housingY, s.z * 0.2f), new Vector3(s.x * 0.62f, 0.62f, 0.45f), MaterialKit.Get(Mat.PaintedMetal));
            Shapes.Box(r, "Cadre", new Vector3(0, housingY, s.z * 0.2f - 0.23f), new Vector3(s.x * 0.56f, 0.54f, 0.02f), MaterialKit.Get(Mat.DarkMetal));
            Renderer screen = radar
                ? Shapes.Cylinder(r, "EcranRadar", new Vector3(0, housingY, s.z * 0.2f - 0.245f), 0.46f, 0.01f, new Material(MaterialKit.Get(Mat.Screen)), new Vector3(-90f, 0, 0))
                : Shapes.Box(r, "Ecran", new Vector3(0, housingY, s.z * 0.2f - 0.245f), new Vector3(s.x * 0.48f, 0.42f, 0.01f), new Material(MaterialKit.Get(Mat.Screen)));
            if (radar) // réticule gravé sur l'écran
            {
                Shapes.Box(r, "ReticuleH", new Vector3(0, housingY, s.z * 0.2f - 0.252f), new Vector3(0.44f, 0.005f, 0.002f), MaterialKit.Get(Mat.DarkMetal));
                Shapes.Box(r, "ReticuleV", new Vector3(0, housingY, s.z * 0.2f - 0.252f), new Vector3(0.005f, 0.44f, 0.002f), MaterialKit.Get(Mat.DarkMetal));
            }
            return screen;
        }

        // Registre : armoire à tiroirs + grand livre ouvert dessus (le livre est le témoin).
        private static Renderer Cabinet(Transform r, Vector3 s)
        {
            Shapes.Box(r, "Armoire", new Vector3(0, s.y / 2f, 0), s, MaterialKit.Get(Mat.PaintedMetal));
            for (int i = 0; i < 3; i++)
            {
                float y = s.y * (0.2f + i * 0.28f);
                Shapes.Box(r, $"Tiroir_{i}", new Vector3(0, y, -s.z / 2f - 0.01f), new Vector3(s.x * 0.86f, s.y * 0.24f, 0.02f), MaterialKit.Get(Mat.PaintedMetal));
                Shapes.Box(r, $"Poignee_{i}", new Vector3(0, y + 0.04f, -s.z / 2f - 0.035f), new Vector3(0.18f, 0.025f, 0.025f), MaterialKit.Get(Mat.BrushedMetal));
                Shapes.Box(r, $"Etiquette_{i}", new Vector3(0, y - 0.03f, -s.z / 2f - 0.022f), new Vector3(0.12f, 0.05f, 0.004f), MaterialKit.Get(Mat.Paper));
            }
            Shapes.Box(r, "Couverture", new Vector3(0, s.y + 0.015f, 0), new Vector3(s.x * 0.8f, 0.03f, s.z * 0.75f), MaterialKit.Get(Mat.Leather));
            return Shapes.Box(r, "Pages", new Vector3(0, s.y + 0.035f, 0), new Vector3(s.x * 0.74f, 0.012f, s.z * 0.68f), new Material(MaterialKit.Get(Mat.Paper)));
        }

        // Clé (conventionnelle ou nucléaire) : colonne, tête inclinée (témoin), serrure, bande de danger au sol.
        private static Renderer KeyPost(Transform r, Vector3 s, Mat headMat)
        {
            Shapes.Box(r, "Danger", new Vector3(0, 0.005f, 0), new Vector3(s.x + 0.5f, 0.01f, s.z + 0.5f), MaterialKit.Get(Mat.Hazard));
            Shapes.Box(r, "Colonne", new Vector3(0, s.y * 0.42f, 0), new Vector3(s.x * 0.55f, s.y * 0.84f, s.z * 0.55f), MaterialKit.Get(Mat.DarkMetal));
            var head = Shapes.Box(r, "Tete", new Vector3(0, s.y * 0.9f, 0), new Vector3(s.x, s.y * 0.18f, s.z), new Material(MaterialKit.Get(headMat)), new Vector3(-25f, 0, 0));
            Shapes.Cylinder(r, "Serrure", new Vector3(0, s.y * 0.93f, -s.z * 0.3f), 0.09f, 0.06f, MaterialKit.Get(Mat.BrushedMetal), new Vector3(-115f, 0, 0));
            Shapes.Box(r, "Cle", new Vector3(0, s.y * 0.95f, -s.z * 0.38f), new Vector3(0.02f, 0.08f, 0.04f), MaterialKit.Get(Mat.BrushedMetal), new Vector3(-25f, 0, 0));
            return head;
        }

        // Téléphone : petite table, combiné noir à cadran (le corps est le témoin).
        private static Renderer PhoneStand(Transform r, Vector3 s)
        {
            float top = s.y * 0.8f;
            Shapes.Box(r, "Plateau", new Vector3(0, top, 0), new Vector3(s.x + 0.2f, 0.04f, s.z + 0.15f), MaterialKit.Get(Mat.PaintedMetal));
            Shapes.Box(r, "Pied", new Vector3(0, top / 2f, 0), new Vector3(0.08f, top, 0.08f), MaterialKit.Get(Mat.DarkMetal));
            Shapes.Box(r, "Base", new Vector3(0, 0.02f, 0), new Vector3(0.4f, 0.04f, 0.4f), MaterialKit.Get(Mat.DarkMetal));
            var body = Shapes.Box(r, "Corps", new Vector3(0, top + 0.08f, 0), new Vector3(0.24f, 0.12f, 0.26f), new Material(MaterialKit.Get(Mat.DarkMetal)), new Vector3(-12f, 0, 0));
            Shapes.Cylinder(r, "Cadran", new Vector3(0, top + 0.15f, -0.05f), 0.13f, 0.01f, MaterialKit.Get(Mat.Paper), new Vector3(-12f, 0, 0));
            Shapes.Box(r, "Combine", new Vector3(0, top + 0.18f, 0.07f), new Vector3(0.26f, 0.04f, 0.05f), MaterialKit.Get(Mat.DarkMetal));
            Shapes.Box(r, "Ecouteur", new Vector3(-0.12f, top + 0.17f, 0.07f), new Vector3(0.06f, 0.05f, 0.07f), MaterialKit.Get(Mat.DarkMetal));
            Shapes.Box(r, "Micro", new Vector3(0.12f, top + 0.17f, 0.07f), new Vector3(0.06f, 0.05f, 0.07f), MaterialKit.Get(Mat.DarkMetal));
            return body;
        }

        // Boîtier à fusible : armoire électrique, porte (témoin), plaque de danger, gaine jusqu'au plafond.
        private static Renderer FuseBox(Transform r, Vector3 s)
        {
            Shapes.Box(r, "Armoire", new Vector3(0, s.y / 2f, 0), s, MaterialKit.Get(Mat.PaintedMetal));
            var door = Shapes.Box(r, "Porte", new Vector3(0, s.y * 0.55f, -s.z / 2f - 0.01f), new Vector3(s.x * 0.85f, s.y * 0.7f, 0.02f), new Material(MaterialKit.Get(Mat.PaintedMetal)));
            Shapes.Box(r, "Danger", new Vector3(0, s.y * 0.95f, -s.z / 2f - 0.012f), new Vector3(s.x * 0.85f, 0.1f, 0.01f), MaterialKit.Get(Mat.Hazard));
            Shapes.Box(r, "Loquet", new Vector3(s.x * 0.32f, s.y * 0.55f, -s.z / 2f - 0.03f), new Vector3(0.03f, 0.12f, 0.03f), MaterialKit.Get(Mat.BrushedMetal));
            Shapes.Cylinder(r, "Gaine", new Vector3(0, s.y + 0.6f, 0), 0.08f, 1.2f, MaterialKit.Get(Mat.Pipe));
            return door;
        }

        // Mallette : table métallique, mallette en cuir (témoin) avec poignée et fermoirs.
        private static Renderer BriefcaseTable(Transform r, Vector3 s)
        {
            float top = s.y * 0.75f;
            Shapes.Box(r, "Plateau", new Vector3(0, top, 0), new Vector3(s.x + 0.3f, 0.04f, s.z + 0.2f), MaterialKit.Get(Mat.PaintedMetal));
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (s.x / 2f + 0.08f), z = (i < 2 ? -1 : 1) * (s.z / 2f + 0.03f);
                Shapes.Box(r, $"Pied_{i}", new Vector3(x, top / 2f, z), new Vector3(0.05f, top, 0.05f), MaterialKit.Get(Mat.DarkMetal));
            }
            var bag = Shapes.Box(r, "Mallette", new Vector3(0, top + 0.08f, 0), new Vector3(s.x * 0.8f, 0.12f, s.z * 0.65f), new Material(MaterialKit.Get(Mat.Leather)));
            Shapes.Box(r, "Poignee", new Vector3(0, top + 0.16f, -s.z * 0.33f), new Vector3(0.14f, 0.03f, 0.03f), MaterialKit.Get(Mat.DarkMetal));
            Shapes.Box(r, "Fermoir_G", new Vector3(-s.x * 0.25f, top + 0.1f, -s.z * 0.33f), new Vector3(0.05f, 0.03f, 0.01f), MaterialKit.Get(Mat.BrushedMetal));
            Shapes.Box(r, "Fermoir_D", new Vector3(s.x * 0.25f, top + 0.1f, -s.z * 0.33f), new Vector3(0.05f, 0.03f, 0.01f), MaterialKit.Get(Mat.BrushedMetal));
            return bag;
        }
    }
}
