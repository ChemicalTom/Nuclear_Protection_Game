using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Voix dans le bunker (local) : chaque bunker a son propre canal vocal, rejoint par les joueurs qui y sont.
    /// Mode 4 joueurs : les deux bunkers ne s'entendent jamais, sauf par le téléphone (canal séparé).
    /// Mode 2 joueurs : bunker commun = un seul canal pour les deux joueurs.
    /// </summary>
    public class BunkerVoice : MonoBehaviour
    {
        private BunkerPlan _plan;
        private GameConfig _config;
        private string _channel;

        public static void Create(Transform parent, BunkerPlan plan, GameConfig config)
        {
            if (config.phone.voiceBackend != PhoneVoiceBackend.Vivox || !config.phone.bunkerVoiceChat) return;
            var go = new GameObject("VoixBunker");
            go.transform.SetParent(parent, false);
            var voice = go.AddComponent<BunkerVoice>();
            voice._plan = plan;
            voice._config = config;
        }

        // La liste des joueurs arrive par le réseau : on attend de connaître notre équipe.
        private void Update()
        {
            if (_channel != null || !TryGetLocalTeam(out var team)) return;
            var session = MatchSession.Instance;
            _channel = $"{_config.phone.voiceChannelPrefix}_{session.SessionId.Value}_b{_plan.BunkerOf(team)}";
            VivoxSession.Want(_channel, true, PhoneVoiceDriver.LocalPlayerName());
        }

        private void OnDestroy()
        {
            if (_channel != null) VivoxSession.Want(_channel, false, null);
        }

        private static bool TryGetLocalTeam(out TeamId team)
        {
            team = default;
            var session = MatchSession.Instance;
            if (session == null || NetworkManager.Singleton == null || session.SessionId.Value.Length == 0) return false;
            ulong me = NetworkManager.Singleton.LocalClientId;
            foreach (var p in session.Players)
                if (p.ClientId == me) { team = p.Team; return true; }
            return false;
        }
    }
}
