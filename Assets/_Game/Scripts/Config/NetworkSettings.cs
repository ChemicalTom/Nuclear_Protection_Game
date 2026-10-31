using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Réglages réseau (hôte + clients par IP).</summary>
    [Serializable]
    public class NetworkSettings
    {
        [Tooltip("Port UDP par défaut.")]
        public ushort defaultPort = 7777;

        [Tooltip("Nombre de joueurs autorisés pour lancer une partie.")]
        public int[] allowedPlayerCounts = { 2, 4 };

        [Tooltip("Nombre max de joueurs connectés.")]
        [Min(2)] public int maxPlayers = 4;

        [Tooltip("Disposition à 2 joueurs (par défaut : un bunker commun avec tous les outils).")]
        public BunkerLayout twoPlayerLayout = BunkerLayout.UnBunkerCommun;

        [Tooltip("Disposition à 4 joueurs (par défaut : deux bunkers séparés).")]
        public BunkerLayout fourPlayerLayout = BunkerLayout.DeuxBunkersSepares;

        [Header("Lobby")]
        [Tooltip("Les joueurs peuvent changer d'équipe eux-mêmes (sinon répartition automatique seule).")]
        public bool playersCanChooseTeam = true;

        [Tooltip("Nom par défaut proposé dans le menu.")]
        public string defaultPlayerName = "Joueur";

        [Header("Scènes")]
        public string menuSceneName = "Menu";
        public string gameSceneName = "Game";
    }
}
