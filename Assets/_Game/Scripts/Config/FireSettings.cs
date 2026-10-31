using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Code de tir (1 lettre + 1 chiffre, un par contact, lu au radar) et pénalités en cas d'erreur.</summary>
    [Serializable]
    public class FireSettings
    {
        [Header("Format du code de tir")]
        public string fireLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        public string fireDigits = "23456789";

        [Header("Pénalité d'erreur (code bien formé mais ne correspondant à aucun contact)")]
        public FireCodePenaltyMode penaltyMode = FireCodePenaltyMode.Cooldown;

        [Tooltip("Blocage du pupitre après chaque mauvais code (s). Modes Cooldown et LesDeux.")]
        [Min(0f)] public float wrongCodeCooldown = 5f;

        [Tooltip("Erreurs consécutives tolérées avant le gros blocage. Modes LimiteEssais et LesDeux.")]
        [Min(1)] public int maxConsecutiveErrors = 3;

        [Tooltip("Durée du gros blocage quand la limite d'erreurs est atteinte (s).")]
        [Min(0f)] public float errorLimitLockout = 20f;

        [Header("Missile conventionnel")]
        [Tooltip("Temps de vol de notre missile jusqu'à la cible (s). Si la cible atteint la base avant, c'est raté.")]
        [Min(0.5f)] public float missileFlightTime = 4f;

        [Tooltip("Couleur d'une clé de tir déjà tournée.")]
        public Color keyTurnedColor = new(0.2f, 1f, 0.3f);
    }
}
