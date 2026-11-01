using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>
    /// Apparition des avions, radar, registre et format des codes d'identification.
    /// Le rythme, la vitesse, les missiles et la part d'amis dépendent de la difficulté (DifficultySettings).
    /// </summary>
    [Serializable]
    public class AircraftSettings
    {
        [Header("Fréquence")]
        [Tooltip("Délai avant le tout premier avion (s).")]
        [Min(0f)] public float firstAircraftDelay = 8f;

        [Header("Trajectoire")]
        [Tooltip("Arc d'approche (degrés, 0 = nord, sens horaire). 0 à 360 = toutes directions.")]
        [Range(0f, 360f)] public float approachAngleMin = 0f;
        [Range(0f, 360f)] public float approachAngleMax = 360f;

        [Tooltip("Avion qui passe sur le côté : distance minimale / maximale à la base au plus près (0 = base, 1 = bord du radar).")]
        [Range(0.05f, 0.95f)] public float sidePassMissMin = 0.3f;
        [Range(0.05f, 0.95f)] public float sidePassMissMax = 0.8f;

        [Header("Missiles ennemis")]
        [Tooltip("Moment du tir, en fraction du trajet de l'avion (min/max).")]
        [Range(0.05f, 0.95f)] public float missileFireAtMin = 0.3f;
        [Range(0.05f, 0.95f)] public float missileFireAtMax = 0.6f;

        [Header("Radar")]
        [Tooltip("Portée affichée sur l'écran radar (km).")]
        [Min(1f)] public float radarRangeKm = 240f;
        [Tooltip("Nombre de cercles de distance sur l'écran radar.")]
        [Min(1)] public int radarRings = 6;
        [Tooltip("Taille d'un écho sur l'écran radar (pixels).")]
        [Min(2f)] public float radarBlipSize = 7f;

        [Header("Registre")]
        [Tooltip("Codes leurres en plus des avions prévus (le registre contient plus de codes que d'avions).")]
        [Min(0)] public int registryExtraCodes = 15;
        [Tooltip("Le poste Registre propose un champ de recherche.")]
        public bool registryAllowSearch = true;

        [Header("Code d'identification (unique, au moins 1 lettre et 1 chiffre)")]
        [Min(2)] public int idCodeLength = 4;

        [Tooltip("Lettres utilisables (on évite O et I, trop proches de 0 et 1 à l'oral).")]
        public string idLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";

        [Tooltip("Chiffres utilisables (on évite 0 et 1).")]
        public string idDigits = "23456789";
    }
}
