using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Apparition des avions, radar, registre et format des codes d'identification.</summary>
    [Serializable]
    public class AircraftSettings
    {
        [Header("Fréquence")]
        [Tooltip("Délai avant le tout premier avion (s).")]
        [Min(0f)] public float firstAircraftDelay = 8f;

        [Tooltip("Délai minimum entre deux avions (s).")]
        [Min(1f)] public float spawnIntervalMin = 10f;

        [Tooltip("Délai maximum entre deux avions (s).")]
        [Min(1f)] public float spawnIntervalMax = 25f;

        [Tooltip("Proportion d'avions amis (0 = tous ennemis, 1 = tous amis).")]
        [Range(0f, 1f)] public float friendlyRatio = 0.4f;

        [Header("Trajectoire")]
        [Tooltip("Temps entre l'apparition au bord du radar et l'arrivée sur la base (s).")]
        [Min(5f)] public float timeToReachBase = 45f;

        [Tooltip("Arc d'approche (degrés, 0 = nord, sens horaire). 0 à 360 = toutes directions.")]
        [Range(0f, 360f)] public float approachAngleMin = 0f;
        [Range(0f, 360f)] public float approachAngleMax = 360f;

        [Header("Missiles ennemis")]
        [Tooltip("Probabilité qu'un avion ennemi tire un missile vers la base.")]
        [Range(0f, 1f)] public float enemyMissileChance = 0.25f;

        [Tooltip("Moment du tir, en fraction du trajet de l'avion (min/max).")]
        [Range(0.05f, 0.95f)] public float missileFireAtMin = 0.3f;
        [Range(0.05f, 0.95f)] public float missileFireAtMax = 0.6f;

        [Tooltip("Temps de vol d'un missile ennemi depuis le bord du radar jusqu'à la base (s).")]
        [Min(3f)] public float enemyMissileFlightTime = 20f;

        [Header("Radar")]
        [Tooltip("Portée affichée sur l'écran radar (km).")]
        [Min(1f)] public float radarRangeKm = 120f;

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
