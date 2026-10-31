using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Apparence des personnages (soldats en formes simples) et animation de marche.</summary>
    [Serializable]
    public class AvatarSettings
    {
        [Header("Uniformes par équipe")]
        public Color radarUniform = new(0.24f, 0.31f, 0.4f);
        public Color tirUniform = new(0.34f, 0.37f, 0.22f);
        [Tooltip("Écusson de poitrine et brassard (repère d'équipe).")]
        public Color radarPatch = new(0.35f, 0.75f, 1f);
        public Color tirPatch = new(1f, 0.6f, 0.15f);
        [Tooltip("Uniforme tant que l'équipe n'est pas connue.")]
        public Color unknownUniform = new(0.35f, 0.35f, 0.35f);

        [Header("Tenue")]
        [Range(0f, 1f)] public float trousersDarkening = 0.7f;
        public Color skin = new(0.86f, 0.68f, 0.55f);
        public Color helmet = new(0.22f, 0.26f, 0.17f);
        public Color boots = new(0.08f, 0.07f, 0.06f);
        public Color belt = new(0.18f, 0.14f, 0.09f);

        [Header("Animation de marche")]
        [Tooltip("Amplitude du balancement des bras et des jambes (degrés).")]
        [Range(0f, 60f)] public float swingAngle = 28f;
        [Tooltip("Pas par mètre parcouru.")]
        [Min(0.1f)] public float stepsPerMeter = 1.4f;
        [Tooltip("Vitesse à partir de laquelle le balancement est complet (m/s).")]
        [Min(0.1f)] public float fullSwingSpeed = 3f;

        [Header("Nom au-dessus de la tête")]
        public bool showNameTag = true;
        public Color nameTagColor = new(0.85f, 0.95f, 0.85f);
        [Min(0.005f)] public float nameTagSize = 0.02f;
    }
}
