using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Durées proposées à l'hôte et bornes du champ « personnalisé ».</summary>
    [Serializable]
    public class MatchDurationSettings
    {
        [Tooltip("Durées prédéfinies proposées dans le menu Héberger (minutes).")]
        public int[] presetMinutes = { 7, 15 };

        [Tooltip("Durée minimale autorisée pour une partie personnalisée (minutes).")]
        [Min(1)] public int customMinMinutes = 3;

        [Tooltip("Durée maximale autorisée pour une partie personnalisée (minutes).")]
        [Min(1)] public int customMaxMinutes = 60;

        [Tooltip("Heure affichée au début de la partie (00:00 = minuit). L'horloge avance en temps réel.")]
        [Range(0, 23)] public int clockStartHour = 0;

        [Tooltip("Afficher aussi les secondes (HH:MM:SS) sur l'horloge.")]
        public bool clockShowSeconds = false;

        /// <summary>Ramène une durée personnalisée dans les bornes autorisées.</summary>
        public int ClampCustom(int minutes) => Mathf.Clamp(minutes, customMinMinutes, customMaxMinutes);
    }
}
