using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Nom affiché d'un poste (étiquette 3D dans le bunker).</summary>
    [Serializable]
    public struct StationLabel
    {
        public StationType type;
        public string text;
    }

    /// <summary>Thème de l'interface (terminal militaire) : police, couleurs de tous les écrans, étiquettes des postes.</summary>
    [Serializable]
    public class UiSettings
    {
        [Tooltip("Polices du système essayées dans l'ordre (la première trouvée est prise).")]
        public string[] fontNames = { "Consolas", "Lucida Console", "Courier New" };
        [Min(8)] public int fontSize = 15;

        [Header("Fonds")]
        public Color panel = new(0.06f, 0.09f, 0.07f, 1f);
        public Color titleBar = new(0.11f, 0.17f, 0.12f, 1f);
        public Color border = new(0.28f, 0.5f, 0.32f, 1f);
        [Tooltip("Voile derrière les menus et l'écran de fin.")]
        public Color backdrop = new(0.01f, 0.02f, 0.015f, 0.88f);
        public Color field = new(0.02f, 0.04f, 0.03f, 1f);

        [Header("Textes")]
        public Color text = new(0.78f, 0.95f, 0.8f, 1f);
        public Color dimText = new(0.45f, 0.62f, 0.48f, 1f);
        [Tooltip("Vert écran CRT : titres, valeurs importantes.")]
        public Color accent = new(0.35f, 1f, 0.5f, 1f);
        [Tooltip("Ambre : alertes, informations à remarquer.")]
        public Color warning = new(1f, 0.72f, 0.18f, 1f);
        public Color danger = new(1f, 0.32f, 0.22f, 1f);

        [Header("Boutons")]
        public Color button = new(0.12f, 0.2f, 0.14f, 1f);
        public Color buttonHover = new(0.18f, 0.3f, 0.2f, 1f);
        public Color buttonActive = new(0.32f, 0.58f, 0.37f, 1f);

        [Header("Étiquettes 3D des postes")]
        public Color worldLabelColor = new(0.95f, 0.75f, 0.35f, 1f);
        [Min(0.005f)] public float worldLabelSize = 0.022f;
        public StationLabel[] stationLabels =
        {
            new() { type = StationType.Radar, text = "RADAR" },
            new() { type = StationType.Registre, text = "REGISTRE" },
            new() { type = StationType.PupitreTir, text = "PUPITRE DE TIR" },
            new() { type = StationType.CleConv1, text = "CLÉ 1" },
            new() { type = StationType.CleConv2, text = "CLÉ 2" },
            new() { type = StationType.Telephone, text = "TÉLÉPHONE" },
            new() { type = StationType.Fusible, text = "FUSIBLES" },
            new() { type = StationType.Mallette, text = "MALLETTE" },
            new() { type = StationType.SerrureNucA, text = "SERRURE A" },
            new() { type = StationType.SerrureNucB, text = "SERRURE B" },
        };

        /// <summary>Texte de l'étiquette d'un poste (nom technique si absent de la liste).</summary>
        public string LabelFor(StationType type)
        {
            foreach (var l in stationLabels)
                if (l.type == type && !string.IsNullOrEmpty(l.text)) return l.text;
            return type.ToString();
        }
    }
}
