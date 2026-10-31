using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>
    /// Fusible par équipe, mini-jeux de réparation et règles de panne.
    /// Le fusible saute selon la CHARGE DU JEU (pas les actions des joueurs) :
    /// trop de choses en même temps (surcharge) ou trop calme trop longtemps (sous-charge, pour ne pas s'ennuyer).
    /// Charge d'une équipe = contacts sur le radar × poids + appels du jeu en attente pour l'équipe × poids.
    /// </summary>
    [Serializable]
    public class FuseSettings
    {
        [Header("Charge du jeu")]
        [Tooltip("Poids d'un contact (avion ou missile) présent sur le radar.")]
        [Min(0f)] public float loadPerContact = 1f;

        [Tooltip("Poids d'un appel du jeu (ordre, sanction) qui sonne ou attend pour l'équipe.")]
        [Min(0f)] public float loadPerGameCall = 2f;

        [Header("Surcharge (trop de choses)")]
        [Tooltip("Charge à partir de laquelle on est en surcharge.")]
        [Min(0.5f)] public float overloadThreshold = 5f;
        [Tooltip("Durée de surcharge continue avant un tirage (s).")]
        [Min(0f)] public float overloadSustain = 6f;
        [Tooltip("Probabilité que le fusible saute à chaque tirage de surcharge.")]
        [Range(0f, 1f)] public float overloadBlowChance = 0.6f;

        [Header("Sous-charge (trop calme)")]
        [Tooltip("Charge en dessous de laquelle (ou égale) le bunker est considéré calme.")]
        [Min(0f)] public float underloadThreshold = 1f;
        [Tooltip("Durée de calme continu avant un tirage (s).")]
        [Min(1f)] public float underloadSustain = 30f;
        [Tooltip("Probabilité que le fusible saute à chaque tirage de sous-charge.")]
        [Range(0f, 1f)] public float underloadBlowChance = 0.5f;

        [Header("Limites")]
        [Tooltip("Aucune panne avant ce temps de partie (s).")]
        [Min(0f)] public float firstBlackoutNotBefore = 60f;
        [Tooltip("Temps minimum entre la réparation et la panne suivante d'une même équipe (s).")]
        [Min(0f)] public float minTimeBetweenBlackouts = 90f;
        [Tooltip("Aucune nouvelle panne dans les dernières secondes de la partie (s).")]
        [Min(0f)] public float noBlackoutInLastSeconds = 30f;
        [Tooltip("Autoriser les deux équipes en panne en même temps.")]
        public bool allowSimultaneousBlackouts = false;

        [Header("Panne et ordres nucléaires")]
        [Tooltip("Aucun ordre nucléaire pendant ce temps après le début d'une panne (s).")]
        [Min(0f)] public float noNuclearOrderAfterBlackout = 20f;

        [Tooltip("Passé ce délai, si la panne dure : nombre moyen d'ordres avancés par minute (0 = jamais). 1 ≈ 50 % de risque en 40 s.")]
        [Min(0f)] public float blackoutOrderRatePerMinute = 1f;

        [Header("Ambiance")]
        public Color emergencyLightColor = new(1f, 0.08f, 0.05f);
        [Min(0f)] public float emergencyLightIntensity = 2f;
        [Tooltip("Clignotement de la lumière d'urgence (cycles/s, 0 = fixe).")]
        [Min(0f)] public float emergencyLightPulse = 0.7f;
        [Tooltip("Couleur des postes hors tension.")]
        public Color unpoweredStationColor = new(0.08f, 0.08f, 0.09f);
        [Tooltip("Couleur du boîtier quand le fusible a sauté.")]
        public Color blownFuseColor = new(1f, 0.2f, 0.1f);
        [Tooltip("Distance d'écoute du bourdonnement et de l'alarme (m). Doit rester < distance entre bunkers.")]
        [Min(1f)] public float ambienceMaxDistance = 14f;
        public PhoneTone humSound = new() { freqA = 60f, freqB = 120f, onTime = 1f, gapTime = 0f, bursts = 1, offTime = 0f, volume = 0.08f };
        public PhoneTone alarmSound = new() { freqA = 700f, freqB = 0f, onTime = 0.35f, gapTime = 0.15f, bursts = 2, offTime = 0.6f, volume = 0.35f };

        [Header("Mini-jeu : aiguille")]
        [Tooltip("Vitesse de rotation de l'aiguille (degrés/s).")]
        [Min(10f)] public float needleSpeed = 240f;
        [Tooltip("Taille de la zone verte (degrés).")]
        [Range(5f, 90f)] public float needleGreenZone = 30f;
        [Tooltip("Nombre de réussites d'affilée requises (une erreur remet à zéro).")]
        [Min(1)] public int needleSuccessesRequired = 3;

        [Header("Mini-jeu : fils")]
        [Range(2, 8)] public int wireCount = 4;
        [Tooltip("Un mauvais branchement débranche tous les fils.")]
        public bool wireMistakeResets = true;
        [Tooltip("Couleurs des fils (au moins wireCount).")]
        public Color[] wireColors =
        {
            new(0.9f, 0.15f, 0.15f), new(0.2f, 0.4f, 1f), new(1f, 0.85f, 0.1f), new(0.2f, 0.8f, 0.25f),
            new(1f, 1f, 1f), new(0.7f, 0.3f, 0.9f), new(1f, 0.55f, 0.1f), new(0.45f, 0.3f, 0.15f)
        };

        /// <summary>Nombre de fils réellement utilisé (limité par le nombre de couleurs).</summary>
        public int EffectiveWireCount => Mathf.Min(wireCount, wireColors?.Length ?? 0);
    }
}
