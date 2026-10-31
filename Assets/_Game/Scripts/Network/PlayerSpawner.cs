using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NuclearProtection.Network
{
    /// <summary>
    /// Serveur : quand tous les joueurs ont chargé la scène de jeu, fait apparaître chaque avatar
    /// dans le bunker de son équipe, puis démarre l'horloge.
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private NetworkObject playerPrefab;
        [SerializeField] private GameClock clock;

        public override void OnNetworkSpawn()
        {
            if (IsServer) NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadCompleted;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
                NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadCompleted;
        }

        private void OnLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> completed, List<ulong> timedOut)
        {
            if (sceneName != config.network.gameSceneName || MatchSession.Instance == null) return;

            var session = MatchSession.Instance;
            var plan = new BunkerPlan(config, session.Layout.Value);
            var indexInTeam = new Dictionary<TeamId, int>();

            foreach (var p in session.Snapshot())
            {
                if (!completed.Contains(p.ClientId)) continue;
                indexInTeam.TryGetValue(p.Team, out int i);
                indexInTeam[p.Team] = i + 1;

                var player = Instantiate(playerPrefab, plan.SpawnPoint(p.Team, i), Quaternion.identity);
                player.SpawnAsPlayerObject(p.ClientId, destroyWithScene: true);
            }

            foreach (var id in timedOut) Debug.LogWarning($"[Spawn] Client {id} n'a pas chargé la scène à temps.");
            clock.ServerStart();
        }
    }
}
