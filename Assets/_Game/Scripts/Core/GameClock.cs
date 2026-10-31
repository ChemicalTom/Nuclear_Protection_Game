using System;
using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Core
{
    /// <summary>
    /// Horloge de partie : part de minuit (00:00) et monte jusqu'à la durée choisie (ex. 00:07).
    /// Le serveur fixe l'instant de départ ; chaque client calcule l'heure à partir du temps serveur.
    /// </summary>
    public class GameClock : NetworkBehaviour
    {
        public static GameClock Instance { get; private set; }

        /// <summary>Serveur : la durée est écoulée (victoire, voir MatchOutcome).</summary>
        public static event Action ServerTimeUp;

        private readonly NetworkVariable<double> _startServerTime = new(-1d);
        private readonly NetworkVariable<double> _stopServerTime = new(-1d); // fin anticipée (défaite)
        private bool _ended;

        public bool Running => IsSpawned && _startServerTime.Value >= 0d;

        /// <summary>La partie est terminée (horloge arrivée au bout ou défaite).</summary>
        public bool Stopped => IsSpawned && _stopServerTime.Value >= 0d;

        /// <summary>La partie se joue en ce moment : démarrée, pas finie. Tous les systèmes de jeu s'y fient.</summary>
        public bool IsActive => Running && !Stopped && Remaining > 0f;

        /// <summary>Durée totale de la partie (s).</summary>
        public float DurationSeconds => MatchSession.Instance != null ? MatchSession.Instance.DurationMinutes.Value * 60f : 0f;

        /// <summary>Temps écoulé depuis le départ (s), borné à la durée ; figé à l'arrêt.</summary>
        public float Elapsed => Running
            ? Mathf.Clamp((float)((Stopped ? _stopServerTime.Value : NetworkManager.ServerTime.Time) - _startServerTime.Value), 0f, DurationSeconds)
            : 0f;

        public float Remaining => DurationSeconds - Elapsed;

        /// <summary>Heure affichée, ex. « 00:07 » (ou « 00:07:00 » si secondes activées).</summary>
        public string Formatted
        {
            get
            {
                var d = ConnectionManager.Instance.Config.duration;
                int total = d.clockStartHour * 3600 + Mathf.FloorToInt(Elapsed);
                int hh = total / 3600 % 24, mm = total / 60 % 60, ss = total % 60;
                return d.clockShowSeconds ? $"{hh:00}:{mm:00}:{ss:00}" : $"{hh:00}:{mm:00}";
            }
        }

        public override void OnNetworkSpawn() => Instance = this;

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Serveur : démarre l'horloge (quand tous les joueurs ont chargé la scène).</summary>
        public void ServerStart()
        {
            if (!IsServer || Running) return;
            _startServerTime.Value = NetworkManager.ServerTime.Time;
            Debug.Log($"[Horloge] Départ, durée {DurationSeconds / 60f} min.");
        }

        /// <summary>Serveur : arrête la partie (l'horloge se fige sur l'heure actuelle).</summary>
        public void ServerStop()
        {
            if (!IsServer || !Running || Stopped) return;
            _stopServerTime.Value = NetworkManager.ServerTime.Time;
        }

        private void Update()
        {
            if (!IsServer || !Running || Stopped || _ended || Elapsed < DurationSeconds) return;
            _ended = true;
            Debug.Log("[Horloge] Fin de la durée.");
            ServerTimeUp?.Invoke();
        }
    }
}
