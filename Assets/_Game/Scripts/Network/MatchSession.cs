using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NuclearProtection.Network
{
    /// <summary>
    /// État partagé de la partie : joueurs/équipes, durée, seed, lancement.
    /// Le serveur est seul à écrire ; les clients lisent et envoient des demandes (RPC).
    /// </summary>
    public class MatchSession : NetworkBehaviour
    {
        public static MatchSession Instance { get; private set; }

        public NetworkList<LobbyPlayer> Players { get; private set; }
        public readonly NetworkVariable<int> DurationMinutes = new();
        public readonly NetworkVariable<int> Seed = new();
        public readonly NetworkVariable<bool> HasStarted = new();
        public readonly NetworkVariable<BunkerLayout> Layout = new();

        /// <summary>Identifiant unique de cette partie (canal vocal propre, même si deux groupes jouent la même seed).</summary>
        public readonly NetworkVariable<FixedString64Bytes> SessionId = new();

        private GameConfig Config => ConnectionManager.Instance.Config;

        private void Awake() => Players = new NetworkList<LobbyPlayer>();

        // Valeurs retenues avant le Spawn (une NetworkVariable ne doit être écrite qu'une fois spawnée).
        private int _initDuration, _initSeed;

        /// <summary>À appeler par le serveur juste avant le Spawn.</summary>
        public void ServerInit(int durationMinutes, int seed)
        {
            _initDuration = durationMinutes;
            _initSeed = seed;
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (!IsServer) return;

            DurationMinutes.Value = _initDuration;
            Seed.Value = _initSeed;
            SessionId.Value = System.Guid.NewGuid().ToString("N");

            NetworkManager.OnClientConnectedCallback += AddPlayer;
            NetworkManager.OnClientDisconnectCallback += RemovePlayer;
            // L'hôte est déjà connecté au moment du spawn.
            foreach (var id in NetworkManager.ConnectedClientsIds) AddPlayer(id);
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            if (!IsServer || NetworkManager == null) return;
            NetworkManager.OnClientConnectedCallback -= AddPlayer;
            NetworkManager.OnClientDisconnectCallback -= RemovePlayer;
        }

        // ---------- Serveur ----------

        private void AddPlayer(ulong clientId)
        {
            if (IndexOf(clientId) >= 0) return;
            var name = new FixedString32Bytes();
            name.CopyFromTruncated(ConnectionManager.Instance.Approval.GetName(clientId));
            Players.Add(new LobbyPlayer { ClientId = clientId, Name = name, Team = TeamRules.LeastFilled(Snapshot()) });
        }

        private void RemovePlayer(ulong clientId)
        {
            ConnectionManager.Instance.Approval.Forget(clientId);
            int i = IndexOf(clientId);
            if (i >= 0) Players.RemoveAt(i);
        }

        /// <summary>Raison qui empêche le lancement, ou null si on peut lancer.</summary>
        public string StartBlocker() => TeamRules.ValidateStart(Snapshot(), Config.network);

        /// <summary>Lance la partie (hôte uniquement) : fige la disposition et charge la scène de jeu.</summary>
        public void ServerStartGame()
        {
            if (!IsServer || HasStarted.Value || StartBlocker() != null) return;
            Layout.Value = TeamRules.LayoutFor(Players.Count, Config.network);
            HasStarted.Value = true;
            NetworkManager.SceneManager.LoadScene(Config.network.gameSceneName, LoadSceneMode.Single);
        }

        // ---------- Demandes des clients ----------

        /// <summary>Un joueur demande à changer d'équipe (accepté s'il reste de la place).</summary>
        [Rpc(SendTo.Server)]
        public void RequestTeamRpc(TeamId team, RpcParams rpc = default)
        {
            if (HasStarted.Value || !Config.network.playersCanChooseTeam) return;
            int i = IndexOf(rpc.Receive.SenderClientId);
            if (i < 0 || Players[i].Team == team) return;
            if (!TeamRules.HasRoom(Snapshot(), team, Config.network)) return;

            var p = Players[i];
            p.Team = team;
            Players[i] = p;
        }

        // ---------- Utilitaires ----------

        public List<LobbyPlayer> Snapshot()
        {
            var list = new List<LobbyPlayer>(Players.Count);
            foreach (var p in Players) list.Add(p);
            return list;
        }

        private int IndexOf(ulong clientId)
        {
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].ClientId == clientId) return i;
            return -1;
        }
    }
}
