using System;
using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;

namespace NuclearProtection.Network
{
    /// <summary>
    /// Règles pures (sans réseau) de répartition des équipes et de validation du lancement.
    /// </summary>
    public static class TeamRules
    {
        /// <summary>Nombre max de joueurs par équipe (la moitié du max).</summary>
        public static int MaxPerTeam(NetworkSettings net) => Math.Max(1, net.maxPlayers / 2);

        /// <summary>Compte les joueurs d'une équipe.</summary>
        public static int Count(IEnumerable<LobbyPlayer> players, TeamId team)
        {
            int n = 0;
            foreach (var p in players) if (p.Team == team) n++;
            return n;
        }

        /// <summary>Équipe la moins remplie (Radar en cas d'égalité).</summary>
        public static TeamId LeastFilled(IEnumerable<LobbyPlayer> players)
        {
            var list = new List<LobbyPlayer>(players);
            return Count(list, TeamId.Tir) < Count(list, TeamId.Radar) ? TeamId.Tir : TeamId.Radar;
        }

        /// <summary>Peut-on rejoindre cette équipe ?</summary>
        public static bool HasRoom(IEnumerable<LobbyPlayer> players, TeamId team, NetworkSettings net) =>
            Count(players, team) < MaxPerTeam(net);

        /// <summary>
        /// Vérifie qu'on peut lancer : nombre de joueurs autorisé et équipes équilibrées.
        /// Renvoie null si OK, sinon la raison à afficher.
        /// </summary>
        public static string ValidateStart(IReadOnlyList<LobbyPlayer> players, NetworkSettings net)
        {
            int total = players.Count;
            if (Array.IndexOf(net.allowedPlayerCounts, total) < 0)
                return $"Il faut {string.Join(" ou ", net.allowedPlayerCounts)} joueurs (actuellement {total}).";

            int half = total / 2;
            if (Count(players, TeamId.Radar) != half || Count(players, TeamId.Tir) != half)
                return $"Équipes déséquilibrées : il faut {half} Radar et {half} Tir.";

            return null;
        }

        /// <summary>Disposition des bunkers selon le nombre de joueurs.</summary>
        public static BunkerLayout LayoutFor(int playerCount, NetworkSettings net) =>
            playerCount <= 2 ? net.twoPlayerLayout : net.fourPlayerLayout;
    }
}
