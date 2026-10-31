using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Seuils des défaites (compteurs cachés pendant la partie) et retour sensoriel d'un ennemi passé.</summary>
    [Serializable]
    public class DefeatSettings
    {
        [Tooltip("Défaite quand ce nombre d'ennemis est passé (avion ou missile).")]
        [Min(1)] public int enemiesPassedLimit = 4;

        [Tooltip("Défaite (virés) quand ce nombre d'avions amis est abattu.")]
        [Min(1)] public int friendliesDownedLimit = 4;

        [Tooltip("Défaite quand ce nombre de vrais ordres a été raté (1er toléré).")]
        [Min(1)] public int missedTrueOrdersLimit = 2;

        [Tooltip("Lancer le nucléaire sans vrai ordre = mort immédiate.")]
        public bool falseLaunchIsInstantDefeat = true;

        [Header("Ennemi passé : secousse (les 2 bunkers)")]
        [Tooltip("Durée de la secousse (s).")]
        [Min(0.1f)] public float shakeDuration = 1.2f;
        [Tooltip("Amplitude du 1er impact (degrés de rotation de la caméra).")]
        [Min(0f)] public float shakeFirstAmplitude = 0.8f;
        [Tooltip("Amplitude ajoutée à chaque ennemi passé suivant.")]
        [Min(0f)] public float shakeAmplitudePerImpact = 0.9f;
        [Tooltip("Fréquence de la secousse (Hz).")]
        [Min(1f)] public float shakeFrequency = 18f;

        [Header("Ennemi passé : explosion lointaine")]
        [Tooltip("Volume du 1er impact (0 à 1).")]
        [Range(0f, 1f)] public float boomFirstVolume = 0.35f;
        [Tooltip("Volume ajouté à chaque ennemi passé suivant.")]
        [Range(0f, 1f)] public float boomVolumePerImpact = 0.2f;
        [Tooltip("Durée du grondement (s).")]
        [Min(0.3f)] public float boomDuration = 2.5f;
        [Tooltip("Son à utiliser à la place de l'explosion générée (optionnel).")]
        public AudioClip boomClipOverride;

        [Header("Écran de fin")]
        public string victoryTitle = "VICTOIRE";
        public string victoryText = "L'horloge est arrivée au bout : vous avez tenu.";
        public string defeatTitle = "DÉFAITE";
        [Tooltip("Textes des raisons de défaite.")]
        public string reasonEnemies = "Trop d'ennemis ont frappé la base.";
        public string reasonFriendlies = "Trop d'avions amis abattus : vous êtes relevés de vos fonctions.";
        public string reasonMissedOrders = "Vous n'avez pas exécuté un vrai ordre de lancement.";
        public string reasonFalseLaunch = "Lancement nucléaire sans ordre valide.";
    }
}
