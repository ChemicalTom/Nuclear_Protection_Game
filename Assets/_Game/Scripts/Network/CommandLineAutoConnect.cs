using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Network
{
    /// <summary>
    /// Outil de test : démarre automatiquement selon les arguments de lancement.
    ///   -np-join 192.168.1.20   rejoindre cette IP
    ///   -np-host 7               héberger une partie de 7 minutes
    ///   -np-port 7777  -np-name Bob  -np-team Tir
    /// Sans argument, ne fait rien (menu normal).
    /// </summary>
    public class CommandLineAutoConnect : MonoBehaviour
    {
        private string _wantedTeam;

        private void Start()
        {
            var cm = ConnectionManager.Instance;
            if (cm == null || cm.Config == null) return;

            string name = Arg("-np-name") ?? cm.Config.network.defaultPlayerName;
            ushort port = ushort.TryParse(Arg("-np-port"), out var p) ? p : cm.Config.network.defaultPort;
            _wantedTeam = Arg("-np-team");

            if (Arg("-np-join") is { } ip) cm.Join(name, ip, port);
            else if (int.TryParse(Arg("-np-host"), out var minutes)) cm.Host(name, port, minutes, null);
        }

        // Demande l'équipe voulue dès que la session est disponible.
        private void Update()
        {
            if (_wantedTeam == null || MatchSession.Instance == null) return;
            if (Enum.TryParse(_wantedTeam, true, out TeamId team)) MatchSession.Instance.RequestTeamRpc(team);
            _wantedTeam = null;
        }

        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
