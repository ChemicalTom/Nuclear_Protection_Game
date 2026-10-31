using System;
using NuclearProtection.Config;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NuclearProtection.Network
{
    /// <summary>
    /// Point d'entrée réseau : Héberger / Rejoindre par IP / Quitter.
    /// Posé sur le même objet que le NetworkManager (persistant entre les scènes).
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [Tooltip("Prefab réseau de la session (lobby, durée, seed).")]
        [SerializeField] private MatchSession sessionPrefab;

        public GameConfig Config => config;
        public ConnectionApprovalHandler Approval { get; private set; }

        /// <summary>Dernier message d'erreur à afficher dans le menu.</summary>
        public string LastError { get; private set; }

        private NetworkManager _nm;
        private UnityTransport _transport;

        private void Awake()
        {
            Instance = this;
            if (config == null || sessionPrefab == null)
                Debug.LogError("[Réseau] ConnectionManager : assigne GameConfig et le prefab MatchSession dans l'Inspector.");
            _nm = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
            Approval = new ConnectionApprovalHandler(_nm, config);
            _nm.NetworkConfig.ConnectionApproval = true;
            _nm.ConnectionApprovalCallback = Approval.Approve;
            _nm.OnClientStopped += OnLocalStopped;
        }

        private void OnDestroy()
        {
            if (_nm != null) _nm.OnClientStopped -= OnLocalStopped;
        }

        /// <summary>Lance un hôte (serveur + joueur) avec la durée et la seed choisies.</summary>
        public bool Host(string playerName, ushort port, int durationMinutes, int? fixedSeed)
        {
            LastError = null;
            _transport.SetConnectionData("0.0.0.0", port, "0.0.0.0"); // Écoute sur toutes les interfaces.
            _nm.NetworkConfig.ConnectionData = ConnectionApprovalHandler.EncodePayload(playerName);

            if (!_nm.StartHost()) return Fail("Impossible d'héberger (port déjà utilisé ?).");

            // Seed générée à la création de la partie, sauf si l'hôte en impose une pour rejouer.
            int seed = fixedSeed ?? Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            var session = Instantiate(sessionPrefab);
            session.ServerInit(durationMinutes, seed);
            session.NetworkObject.Spawn(destroyWithScene: false); // Survit au changement de scène.
            return true;
        }

        /// <summary>Rejoint un hôte par IP + port.</summary>
        public bool Join(string playerName, string ip, ushort port)
        {
            LastError = null;
            if (!LocalIpUtility.IsValidIPv4(ip)) return Fail("Adresse IP invalide (ex. 192.168.1.20).");

            _transport.SetConnectionData(ip.Trim(), port);
            _nm.NetworkConfig.ConnectionData = ConnectionApprovalHandler.EncodePayload(playerName);
            return _nm.StartClient() || Fail("Impossible de démarrer le client.");
        }

        /// <summary>Quitte la partie et revient au menu.</summary>
        public void Leave()
        {
            _leavingVoluntarily = true;
            _nm.Shutdown();
        }

        private bool _leavingVoluntarily;

        // Appelé quand notre client/hôte s'arrête (départ volontaire, hôte parti, refus…).
        private void OnLocalStopped(bool wasHost)
        {
            if (!wasHost && !_leavingVoluntarily)
            {
                // Les raisons techniques de NGO commencent par « [Disconnect Event] » : on les remplace.
                string reason = _nm.DisconnectReason;
                LastError = string.IsNullOrEmpty(reason) || reason.StartsWith("[Disconnect Event]")
                    ? "Déconnecté : l'hôte a fermé la partie ou est introuvable."
                    : reason;
            }
            _leavingVoluntarily = false;
            Debug.Log($"[Réseau] Session arrêtée (hôte={wasHost}). {LastError}");

            if (SceneManager.GetActiveScene().name != config.network.menuSceneName)
                SceneManager.LoadScene(config.network.menuSceneName);
        }

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogWarning($"[Réseau] {message}");
            return false;
        }
    }
}
