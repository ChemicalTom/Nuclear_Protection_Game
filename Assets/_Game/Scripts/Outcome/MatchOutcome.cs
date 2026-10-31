using System;
using NuclearProtection.Aircraft;
using NuclearProtection.Calls;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Fire;
using NuclearProtection.Keys;
using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Outcome
{
    public enum MatchResult : byte
    {
        EnCours = 0,
        Victoire = 1,
        Defaite = 2
    }

    public enum DefeatReason : byte
    {
        Aucune = 0,
        EnnemisPasses = 1,
        AmisAbattus = 2,
        OrdresRates = 3,
        FauxLancement = 4
    }

    /// <summary>
    /// Serveur : compteurs cachés et fin de partie.
    /// Les compteurs ne sont JAMAIS envoyés aux clients pendant la partie (seulement au résumé, étape 11).
    /// Victoire = horloge au bout ; défaites = seuils de DefeatSettings ou faux lancement.
    /// </summary>
    public partial class MatchOutcome : NetworkBehaviour
    {
        public static MatchOutcome Instance { get; private set; }

        /// <summary>Serveur : la partie vient de se terminer.</summary>
        public static event Action<MatchResult, DefeatReason> ServerMatchEnded;

        /// <summary>Clients : un ennemi vient de frapper la base (impact = n-ième ennemi passé, pour l'intensité).</summary>
        public static event Action<int> LocalEnemyImpact;

        public readonly NetworkVariable<MatchResult> Result = new();
        public readonly NetworkVariable<DefeatReason> Reason = new();

        // ---- Compteurs cachés (serveur uniquement) ----
        public int EnemiesDowned { get; private set; }
        public int EnemiesPassed { get; private set; }
        public int FriendliesDowned { get; private set; }
        public int MissedTrueOrders { get; private set; }
        public int TrueLaunches { get; private set; }
        public int FalseLaunches { get; private set; }

        /// <summary>La partie est finie (vrai sur tous les clients).</summary>
        public static bool IsOver => Instance != null && Instance.IsSpawned && Instance.Result.Value != MatchResult.EnCours;

        private static DefeatSettings S => ConnectionManager.Instance.Config.defeat;
        private bool Playing => Result.Value == MatchResult.EnCours && GameClock.Instance != null && GameClock.Instance.IsActive;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (!IsServer) return;
            AircraftManager.ServerContactReachedBase += OnReachedBase;
            FireControl.ServerShotDown += OnShotDown;
            CallDirector.ServerTrueOrderMissed += OnTrueOrderMissed;
            KeySystem.ServerNuclearLaunched += OnNuclearLaunched;
            GameClock.ServerTimeUp += OnTimeUp;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                AircraftManager.ServerContactReachedBase -= OnReachedBase;
                FireControl.ServerShotDown -= OnShotDown;
                CallDirector.ServerTrueOrderMissed -= OnTrueOrderMissed;
                KeySystem.ServerNuclearLaunched -= OnNuclearLaunched;
                GameClock.ServerTimeUp -= OnTimeUp;
            }
            if (Instance == this) Instance = null;
        }

        // ---------- Événements de jeu (serveur) ----------

        private void OnReachedBase(RadarContact contact, bool isEnemy)
        {
            if (!isEnemy || !Playing) return;
            EnemiesPassed++;
            SpoilerLog.Log($"[Fin] Ennemi passé : {EnemiesPassed}/{S.enemiesPassedLimit}.");
            EnemyImpactRpc(EnemiesPassed);
            if (EnemiesPassed >= S.enemiesPassedLimit) Defeat(DefeatReason.EnnemisPasses);
        }

        private void OnShotDown(RadarContact contact, bool wasEnemy)
        {
            if (!Playing) return;
            if (wasEnemy) { EnemiesDowned++; return; }
            FriendliesDowned++;
            SpoilerLog.Log($"[Fin] Ami abattu : {FriendliesDowned}/{S.friendliesDownedLimit}.");
            if (FriendliesDowned >= S.friendliesDownedLimit) Defeat(DefeatReason.AmisAbattus);
        }

        private void OnTrueOrderMissed(NuclearOrder order)
        {
            if (!Playing) return;
            MissedTrueOrders++;
            SpoilerLog.Log($"[Fin] Vrai ordre raté : {MissedTrueOrders}/{S.missedTrueOrdersLimit}.");
            if (MissedTrueOrders >= S.missedTrueOrdersLimit) Defeat(DefeatReason.OrdresRates);
        }

        private void OnNuclearLaunched(bool legitimate)
        {
            if (!Playing) return;
            if (legitimate) { TrueLaunches++; return; }
            FalseLaunches++;
            _falseLaunchTimes.Add(GameClock.Instance.Elapsed);
            if (S.falseLaunchIsInstantDefeat) Defeat(DefeatReason.FauxLancement);
        }

        private void OnTimeUp()
        {
            if (Result.Value != MatchResult.EnCours) return;
            End(MatchResult.Victoire, DefeatReason.Aucune);
        }

        // ---------- Fin ----------

        private void Defeat(DefeatReason reason) => End(MatchResult.Defaite, reason);

        private void End(MatchResult result, DefeatReason reason)
        {
            Reason.Value = reason;
            Result.Value = result;
            GameClock.Instance.ServerStop();
            Debug.Log($"[Fin] {result} ({reason}).");
            SummaryRpc(BuildSummary());
            ServerMatchEnded?.Invoke(result, reason);
        }

        [Rpc(SendTo.Everyone)]
        private void EnemyImpactRpc(int impact) => LocalEnemyImpact?.Invoke(impact);

        // ---------- Tests depuis l'inspecteur (Play Mode, hôte) ----------

        [ContextMenu("Test : impact ennemi (sans compter)")]
        private void TestImpact() => EnemyImpactRpc(Mathf.Max(1, EnemiesPassed));

        /// <summary>Tests : compte un ennemi passé comme si un avion ennemi arrivait sur la base.</summary>
        public void ServerTestEnemyPassed() => OnReachedBase(default, true);
    }
}
