using System.Collections.Generic;
using NuclearProtection.Core;
using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Lobby (IMGUI, thème terminal) : adresses de l'hôte, paramètres, colonnes d'équipe, bouton Lancer.
    /// Pendant la partie, affiche seulement un bouton Quitter.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        private const float Width = 680f, Gap = 16f;

        private List<string> _ips;

        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            var session = MatchSession.Instance;
            if (nm == null || !nm.IsListening) { _ips = null; return; }

            UiTheme.Begin();
            if (session == null) { GUI.Label(new Rect(0, 0, UiTheme.Size.x, UiTheme.Size.y), "Connexion…", UiTheme.Center); return; }

            if (session.HasStarted.Value)
            {
                if (GUI.Button(new Rect(10, 10, 100, 30), "Quitter")) ConnectionManager.Instance.Leave();
                return;
            }
            DrawLobby(nm, session);
        }

        private void DrawLobby(NetworkManager nm, MatchSession session)
        {
            UiTheme.DrawBackdrop();
            var size = UiTheme.Size;
            float x = (size.x - Width) / 2f, y = Mathf.Max(10f, size.y * 0.05f);

            GUI.Label(new Rect(0, y, size.x, 44), "SALLE DE BRIEFING", UiTheme.Title);
            y += 52;

            // Paramètres de la partie (et adresses à communiquer, côté hôte).
            float infoHeight = nm.IsHost ? 122f : 84f;
            var info = UiTheme.Panel(new Rect(x, y, Width, infoHeight));
            GUILayout.BeginArea(info);
            GUILayout.Label($"{UiTheme.Dim("DURÉE")} {UiTheme.Ok(session.DurationMinutes.Value + " min")}      {UiTheme.Dim("SEED")} {UiTheme.Ok(session.Seed.Value.ToString())}");
            var players = session.Snapshot();
            GUILayout.Label($"{UiTheme.Dim("BUNKER")} {UiTheme.Ok(LayoutText(TeamRules.LayoutFor(players.Count, ConnectionManager.Instance.Config.network)))}");
            if (nm.IsHost) DrawHostAddresses();
            GUILayout.EndArea();
            y += infoHeight + 10;

            float diffHeight = DifficultyLobbyPanel.Height(nm.IsHost, session.Difficulty.Value);
            DifficultyLobbyPanel.Draw(new Rect(x, y, Width, diffHeight), session, nm.IsHost);
            y += diffHeight + 10;

            float colWidth = (Width - Gap) / 2f, colHeight = Mathf.Clamp(size.y - y - 110f, 120f, 260f);
            DrawTeam(new Rect(x, y, colWidth, colHeight), TeamId.Radar, players, nm.LocalClientId, session);
            DrawTeam(new Rect(x + colWidth + Gap, y, colWidth, colHeight), TeamId.Tir, players, nm.LocalClientId, session);
            y += colHeight + 10;

            if (nm.IsHost)
            {
                string blocker = session.StartBlocker();
                GUI.enabled = blocker == null;
                if (GUI.Button(new Rect(x, y, Width - 130, 40), "LANCER LA PARTIE")) session.ServerStartGame();
                GUI.enabled = true;
                if (blocker != null) GUI.Label(new Rect(x, y + 42, Width, 40), UiTheme.Warn(blocker), UiTheme.Center);
            }
            else GUI.Label(new Rect(x, y, Width - 130, 40), UiTheme.Dim("En attente du lancement par l'hôte…"), UiTheme.Center);

            if (GUI.Button(new Rect(x + Width - 120, y, 120, 40), "Quitter")) ConnectionManager.Instance.Leave();
        }

        private static string LayoutText(BunkerLayout layout) => layout == BunkerLayout.UnBunkerCommun
            ? "un seul bunker pour tout le monde, avec tous les postes"
            : "deux bunkers scellés, une équipe dans chacun";

        private void DrawHostAddresses()
        {
            _ips ??= LocalIpUtility.GetLocalIPv4Addresses();
            ushort port = ConnectionManager.Instance.Config.network.defaultPort;
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null) port = transport.ConnectionData.Port;

            GUILayout.Label(UiTheme.Dim($"À DONNER À TES AMIS (port UDP {port})"));
            GUILayout.Label(UiTheme.Warn(string.Join("    ", _ips)));
        }

        private static void DrawTeam(Rect r, TeamId team, List<LobbyPlayer> players, ulong localId, MatchSession session)
        {
            var net = ConnectionManager.Instance.Config.network;
            var inner = UiTheme.Panel(r, $"ÉQUIPE {team.ToString().ToUpperInvariant()}   {TeamRules.Count(players, team)}/{TeamRules.MaxPerTeam(net)}");
            GUILayout.BeginArea(inner);
            GUILayout.Label(team == TeamId.Radar ? "Surveille le ciel, identifie les contacts." : "Abat les ennemis, tient les clés de lancement.", UiTheme.Small);

            bool localInTeam = false;
            foreach (var p in players)
            {
                if (p.Team != team) continue;
                bool me = p.ClientId == localId;
                localInTeam |= me;
                GUILayout.Label(me ? UiTheme.Ok($"■ {p.Name}  (toi)") : $"■ {p.Name}");
            }

            GUILayout.FlexibleSpace();
            if (net.playersCanChooseTeam && !localInTeam && TeamRules.HasRoom(players, team, net)
                && GUILayout.Button($"Rejoindre {team}", GUILayout.Height(32)))
                session.RequestTeamRpc(team);
            GUILayout.EndArea();
        }
    }
}
