using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Le « directeur » corrige les séries injustes sans supprimer le hasard.</summary>
    [Serializable]
    public class DirectorSettings
    {
        [Tooltip("Nombre max d'ordres faux d'affilée.")]
        [Min(1)] public int maxConsecutiveFalseOrders = 2;

        [Tooltip("Nombre max d'ordres vrais d'affilée.")]
        [Min(1)] public int maxConsecutiveTrueOrders = 2;

        [Tooltip("Nombre max de fois d'affilée où la même équipe est appelée en premier.")]
        [Min(1)] public int maxSameTeamCalledFirst = 2;
        // Le nombre max d'ennemis simultanés dépend de la difficulté (DifficultyValues).

        [Tooltip("Nombre max d'avions du même camp d'affilée.")]
        [Min(1)] public int maxConsecutiveSameSide = 4;

        [Tooltip("Quand le directeur retient un avion (trop d'ennemis en vol), délai avant de réessayer (s).")]
        [Min(0.5f)] public float spawnRetryDelay = 2f;
    }
}
