using System.Collections.Generic;
using NuclearProtection.Core;
using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Lobby graybox (IMGUI) : IP/port de l'hôte, durée, seed, joueurs par équipe, bouton Lancer.
    /// Pendant la partie, affiche seulement un bouton Quitter.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        private List<string> _ips;

        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            var session = MatchSession.Instance;
            if (nm == null || !nm.IsListening) { _ips = null; return; }

            GUI.matrix = Matrix4x4.Scale(Vector3.one * DisplayControl.UiScale);
            if (session == null) { GUILayout.Label("Connexion…"); return; }

            if (session.HasStarted.Value)
            {
                if (GUI.Button(new Rect(10, 10, 90, 28), "Quitter")) ConnectionManager.Instance.Leave();
                return;
            }
            DrawLobby(nm, session);
        }

        private void DrawLobby(NetworkManager nm, MatchSession session)
        {
            var rich = new GUIStyle(GUI.skin.label) { richText = true };
            GUILayout.BeginArea(new Rect(20, 20, 460, 640), GUI.skin.box);
            GUILayout.Label("<b>LOBBY</b>", rich);

            if (nm.IsHost) DrawHostAddresses(rich);
            GUILayout.Label($"Durée : {session.DurationMinutes.Value} min    Seed : {session.Seed.Value}");

            var players = session.Snapshot();
            DrawTeam(TeamId.Radar, players, nm.LocalClientId, session, rich);
            DrawTeam(TeamId.Tir, players, nm.LocalClientId, session, rich);

            GUILayout.Space(10);
            if (nm.IsHost)
            {
                string blocker = session.StartBlocker();
                GUI.enabled = blocker == null;
                if (GUILayout.Button("Lancer la partie")) session.ServerStartGame();
                GUI.enabled = true;
                if (blocker != null) GUILayout.Label($"<color=#ffb040>{blocker}</color>", rich);
            }
            else GUILayout.Label("En attente du lancement par l'hôte…");

            if (GUILayout.Button("Quitter")) ConnectionManager.Instance.Leave();
            GUILayout.EndArea();
        }

        private void DrawHostAddresses(GUIStyle rich)
        {
            _ips ??= LocalIpUtility.GetLocalIPv4Addresses();
            ushort port = ConnectionManager.Instance.Config.network.defaultPort;
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null) port = transport.ConnectionData.Port;

            GUILayout.Label($"<b>À donner à tes amis</b> (port UDP {port}) :", rich);
            foreach (var ip in _ips) GUILayout.Label("  " + ip);
        }

        private static void DrawTeam(TeamId team, List<LobbyPlayer> players, ulong localId, MatchSession session, GUIStyle rich)
        {
            var net = ConnectionManager.Instance.Config.network;
            GUILayout.Label($"<b>Équipe {team}</b> ({TeamRules.Count(players, team)}/{TeamRules.MaxPerTeam(net)})", rich);

            bool localInTeam = false;
            foreach (var p in players)
            {
                if (p.Team != team) continue;
                bool me = p.ClientId == localId;
                localInTeam |= me;
                GUILayout.Label($"  • {p.Name}{(me ? "  (toi)" : "")}");
            }

            if (net.playersCanChooseTeam && !localInTeam && TeamRules.HasRoom(players, team, net)
                && GUILayout.Button($"Rejoindre {team}"))
                session.RequestTeamRpc(team);
        }
    }
}
