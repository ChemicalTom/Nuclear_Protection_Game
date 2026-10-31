using System;
using System.Collections.Generic;
using NuclearProtection.Aircraft;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Phone;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Fuse
{
    /// <summary>
    /// Fusible de chaque équipe (autorité serveur). Il saute selon la charge du jeu :
    /// surcharge (trop de contacts / d'appels en même temps) ou sous-charge (trop calme trop longtemps).
    /// Pendant la panne : postes éteints, téléphone coupé entre les bunkers, appels du jeu manqués.
    /// La réparation (mini-jeux) est dans FuseSystem.Repair.
    /// </summary>
    public partial class FuseSystem : NetworkBehaviour
    {
        public static FuseSystem Instance { get; private set; }

        /// <summary>Serveur : le fusible d'une équipe vient de sauter.</summary>
        public static event Action<TeamId> ServerBlackoutStarted;

        /// <summary>Serveur : le fusible d'une équipe vient d'être remis.</summary>
        public static event Action<TeamId> ServerRepaired;

        private readonly NetworkVariable<FuseState> _radar = new();
        private readonly NetworkVariable<FuseState> _tir = new();

        // ---- État serveur uniquement ----
        private readonly List<BlackoutRecord> _records = new();
        private readonly Dictionary<TeamId, float> _overSince = new(), _underSince = new(), _lastRepair = new();
        private System.Random _rng;

        public IReadOnlyList<BlackoutRecord> Records => _records;
        private static FuseSettings S => ConnectionManager.Instance.Config.fuse;
        private static GameClock Clock => GameClock.Instance;
        private static readonly TeamId[] Teams = { TeamId.Radar, TeamId.Tir };

        public FuseState State(TeamId team) => Var(team).Value;
        private NetworkVariable<FuseState> Var(TeamId team) => team == TeamId.Radar ? _radar : _tir;
        private static TeamId Other(TeamId team) => team == TeamId.Radar ? TeamId.Tir : TeamId.Radar;

        /// <summary>Le fusible de l'équipe a-t-il sauté ? (serveur et clients)</summary>
        public bool IsBlackedOut(TeamId team) => IsSpawned && State(team).Blown;

        /// <summary>Depuis combien de secondes l'équipe est en panne (-1 si pas en panne).</summary>
        public float BlackoutAge(TeamId team) =>
            IsBlackedOut(team) ? (float)(NetworkManager.ServerTime.Time - State(team).Since) : -1f;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            foreach (var team in Teams)
            {
                _overSince[team] = _underSince[team] = -1f;
                _lastRepair[team] = float.NegativeInfinity;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- Déclenchement (serveur) ----------

        private void Update()
        {
            if (!IsServer || Clock == null || !Clock.IsActive) return;
            _rng ??= SeededRandom.Create(MatchSession.Instance.Seed.Value, SeededRandom.SaltFuse);
            float now = Clock.Elapsed;

            foreach (var team in Teams)
            {
                if (IsBlackedOut(team)) continue;
                float load = Load(team);
                Check(team, load >= S.overloadThreshold, _overSince, S.overloadSustain, S.overloadBlowChance, BlowCause.Surcharge, now);
                Check(team, load <= S.underloadThreshold, _underSince, S.underloadSustain, S.underloadBlowChance, BlowCause.SousCharge, now);
            }
        }

        /// <summary>Charge actuelle d'une équipe : contacts sur le radar + appels du jeu qui l'attendent.</summary>
        public float Load(TeamId team)
        {
            int contacts = AircraftManager.Instance != null ? AircraftManager.Instance.Contacts.Count : 0;
            int calls = PhoneSystem.Instance != null ? PhoneSystem.Instance.PendingGameCalls(team) : 0;
            return contacts * S.loadPerContact + calls * S.loadPerGameCall;
        }

        // Condition tenue pendant « sustain » secondes → un tirage ; raté, on recommence une nouvelle durée.
        private void Check(TeamId team, bool active, Dictionary<TeamId, float> since, float sustain, float chance, BlowCause cause, float now)
        {
            if (!active) { since[team] = -1f; return; }
            if (since[team] < 0f) since[team] = now;
            if (now - since[team] < sustain) return;

            since[team] = now;
            if (CanBlow(team, now) && _rng.NextDouble() < chance) ServerBlow(team, cause);
        }

        private bool CanBlow(TeamId team, float now) =>
            now >= S.firstBlackoutNotBefore
            && Clock.Remaining > S.noBlackoutInLastSeconds
            && now - _lastRepair[team] >= S.minTimeBetweenBlackouts
            && (S.allowSimultaneousBlackouts || !IsBlackedOut(Other(team)));

        /// <summary>Serveur : fait sauter le fusible (public pour les tests).</summary>
        public void ServerBlow(TeamId team, BlowCause cause)
        {
            if (!IsServer || IsBlackedOut(team)) return;
            _rng ??= SeededRandom.Create(MatchSession.Instance.Seed.Value, SeededRandom.SaltFuse);
            Var(team).Value = new FuseState
            {
                Blown = true,
                Since = NetworkManager.ServerTime.Time,
                Game = _rng.Next(2) == 0 ? FuseMiniGame.Aiguille : FuseMiniGame.Fils,
                Seed = _rng.Next(),
                Progress = 0
            };
            _overSince[team] = _underSince[team] = -1f;
            _records.Add(new BlackoutRecord { Team = team, Cause = cause, StartTime = Clock != null ? Clock.Elapsed : 0f });
            Debug.Log($"[Fusible] Panne {team} ({cause}), mini-jeu {State(team).Game}.");
            ServerBlackoutStarted?.Invoke(team);
        }

        private void Repair(TeamId team)
        {
            float now = Clock != null ? Clock.Elapsed : 0f;
            Var(team).Value = default;
            _lastRepair[team] = now;
            for (int i = _records.Count - 1; i >= 0; i--)
                if (_records[i].Team == team && _records[i].RepairTime == null) { _records[i].RepairTime = now; break; }
            Debug.Log($"[Fusible] {team} réparé.");
            ServerRepaired?.Invoke(team);
        }

        /// <summary>Résumé de fin (étape 11) : nombre de pannes et temps moyen de réparation (s, -1 si aucune réparée).</summary>
        public void GetStats(out int count, out float averageRepair)
        {
            count = _records.Count;
            float total = 0f;
            int repaired = 0;
            foreach (var r in _records)
                if (r.Duration is float d) { total += d; repaired++; }
            averageRepair = repaired > 0 ? total / repaired : -1f;
        }

        // ---------- Tests depuis l'inspecteur (Play Mode, hôte) ----------

        [ContextMenu("Test : panne Radar")]
        private void TestBlowRadar() => ServerBlow(TeamId.Radar, BlowCause.Test);

        [ContextMenu("Test : panne Tir")]
        private void TestBlowTir() => ServerBlow(TeamId.Tir, BlowCause.Test);

        [ContextMenu("Test : tout réparer")]
        private void TestRepairAll()
        {
            foreach (var team in Teams) if (IsBlackedOut(team)) Repair(team);
        }
    }
}
