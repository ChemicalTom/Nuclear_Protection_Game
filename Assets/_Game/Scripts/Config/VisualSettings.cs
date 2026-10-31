using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>
    /// Habillage Â« bunker guerre froide Â» : couleurs des matÃ©riaux, dÃ©cor et post-traitement.
    /// Purement visuel (aucun effet sur les rÃ¨gles), construit localement sur chaque machine.
    /// </summary>
    [Serializable]
    public class VisualSettings
    {
        [Tooltip("Ajoute le dÃ©cor (tuyaux, nÃ©ons, porte blindÃ©e, panneaux). DÃ©cocher = salle nue.")]
        public bool buildDecor = true;

        [Header("Murs, sol, plafond")]
        public Color concreteColor = new(0.62f, 0.6f, 0.55f);
        [Tooltip("Bas des murs peint (vert militaire).")]
        public Color wainscotColor = new(0.24f, 0.32f, 0.27f);
        [Min(0f)] public float wainscotHeight = 1.1f;
        public Color floorColor = new(0.33f, 0.34f, 0.33f);
        [Min(0.2f)] public float floorTileSize = 1f;
        public Color ceilingColor = new(0.42f, 0.42f, 0.4f);
        [Range(0f, 1f)] public float concreteNoise = 0.35f;

        [Header("MÃ©tal et consoles")]
        public Color paintedMetalColor = new(0.33f, 0.4f, 0.34f);
        public Color darkMetalColor = new(0.14f, 0.15f, 0.16f);
        public Color brushedMetalColor = new(0.6f, 0.6f, 0.58f);
        public Color redPaintColor = new(0.55f, 0.08f, 0.06f);
        public Color hazardYellow = new(0.95f, 0.75f, 0.05f);
        public Color hazardBlack = new(0.08f, 0.08f, 0.08f);
        public Color pipeColor = new(0.36f, 0.3f, 0.24f);

        [Header("LumiÃ¨res et Ã©crans")]
        [Tooltip("IntensitÃ© de l'Ã©mission des Ã©crans CRT (multipliÃ©e par la couleur du poste).")]
        [Min(0f)] public float screenGlow = 1.6f;
        public Color neonColor = new(0.9f, 0.95f, 1f);
        [Min(0f)] public float neonGlow = 2.5f;
        [Tooltip("Petits voyants (boutons) des consoles.")]
        [Min(0f)] public float lampGlow = 3f;
        [Tooltip("Ombres de la lumiÃ¨re principale de chaque espace.")]
        public bool roomLightShadows = true;
        public Color ambientColor = new(0.34f, 0.35f, 0.33f);
        [Tooltip("Multiplie l'intensitÃ© de la lumiÃ¨re normale de chaque espace (RoomSettings.roomLightIntensity).")]
        [Min(0f)] public float roomLightBoost = 2.5f;

        [Header("Post-traitement")]
        public bool postProcessing = true;
        [Min(0f)] public float bloomIntensity = 0.9f;
        [Min(0f)] public float bloomThreshold = 0.95f;
        [Range(0f, 1f)] public float vignette = 0.3f;
        [Range(0f, 1f)] public float filmGrain = 0.25f;
        [Range(-100f, 100f)] public float saturation = -15f;
        [Range(-100f, 100f)] public float contrast = 12f;
        [Tooltip("Brouillard lÃ©ger (densitÃ© exponentielle, 0 = aucun).")]
        [Min(0f)] public float fogDensity = 0.02f;
        public Color fogColor = new(0.12f, 0.13f, 0.12f);
    }
}
