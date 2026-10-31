using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>
    /// Clés et mallettes. Chaque équipe a une mallette, ouverte avec les clés perso
    /// de ses joueurs ; elle contient les clés de lancement.
    /// Conventionnel : le bunker Tir tourne ses 2 clés l'une après l'autre, à chaque missile.
    /// Nucléaire : dans chaque bunker, les 2 clés tournées ensemble = 1 vote ; il faut les votes des 2 bunkers.
    /// </summary>
    [Serializable]
    public class KeySettings
    {
        [Header("Mallette")]
        [Tooltip("Nombre de clés de lancement dans chaque mallette.")]
        [Min(2)] public int keysPerBriefcase = 2;

        [Tooltip("Nombre de clés perso nécessaires pour ouvrir la mallette (joueurs différents ; à 2 joueurs, les deux).")]
        [Min(1)] public int personalKeysToOpen = 2;

        [Tooltip("Écart max entre les clés perso pour ouvrir la mallette (s).")]
        [Min(0.1f)] public float briefcaseOpenTolerance = 2f;

        [Tooltip("Couleur d'une mallette ouverte.")]
        public Color briefcaseOpenColor = new(0.3f, 0.9f, 1f);

        [Header("Conventionnel")]
        [Tooltip("Temps max entre la 1re et la 2e clé avant annulation (0 = pas de limite).")]
        [Min(0f)] public float conventionalKeyTimeout = 0f;

        [Header("Nucléaire")]
        [Tooltip("Écart max entre les 2 clés d'un même bunker pour compter comme « en même temps » (s).")]
        [Min(0.05f)] public float nuclearSimultaneityTolerance = 1f;

        [Tooltip("Distance entre les 2 serrures nucléaires d'un bunker (m). ~12 pieds.")]
        [Min(1f)] public float nuclearLockDistance = 3.6f;

        [Tooltip("Écart max entre le vote du 1er bunker et celui du 2e (s). Au-delà, le vote expire.")]
        [Min(1f)] public float bunkerVoteWindow = 30f;

        [Tooltip("Couleur des serrures nucléaires d'un bunker dont le vote est en cours.")]
        public Color nuclearVoteColor = new(1f, 0.2f, 0.2f);
    }
}
