using System.Collections.Generic;
using System.Text;
using NuclearProtection.Config;
using Unity.Netcode;

namespace NuclearProtection.Network
{
    /// <summary>
    /// Côté serveur : accepte ou refuse les connexions (partie pleine / déjà lancée)
    /// et retient le nom envoyé par chaque joueur.
    /// </summary>
    public class ConnectionApprovalHandler
    {
        private readonly NetworkManager _nm;
        private readonly GameConfig _config;
        private readonly Dictionary<ulong, string> _names = new();

        public ConnectionApprovalHandler(NetworkManager nm, GameConfig config)
        {
            _nm = nm;
            _config = config;
        }

        /// <summary>Encode le nom du joueur dans les données de connexion.</summary>
        public static byte[] EncodePayload(string playerName) => Encoding.UTF8.GetBytes(playerName ?? "");

        /// <summary>Nom retenu pour ce client (ou nom par défaut numéroté).</summary>
        public string GetName(ulong clientId) =>
            _names.TryGetValue(clientId, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n
                : $"{_config.network.defaultPlayerName} {clientId + 1}";

        public void Forget(ulong clientId) => _names.Remove(clientId);

        public void Approve(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
        {
            bool full = _nm.ConnectedClientsIds.Count >= _config.network.maxPlayers;
            bool started = MatchSession.Instance != null && MatchSession.Instance.HasStarted.Value;

            res.Approved = !full && !started;
            res.CreatePlayerObject = false; // Les avatars arrivent à l'étape 3.
            res.Reason = full ? "Partie pleine." : started ? "La partie a déjà commencé." : string.Empty;

            if (res.Approved)
            {
                // La troncature à la taille réseau est faite par MatchSession.
                _names[req.ClientNetworkId] = req.Payload != null ? Encoding.UTF8.GetString(req.Payload).Trim() : "";
            }
        }
    }
}
