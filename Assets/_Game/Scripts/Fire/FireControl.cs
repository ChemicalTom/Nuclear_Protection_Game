using System;
using System.Collections.Generic;
using NuclearProtection.Aircraft;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Player;
using NuclearProtection.Rooms;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Fire
{
    /// <summary>
    /// Tir conventionnel (autorité serveur) :
    /// 1. au pupitre, on saisit le code de tir d'un contact (dicté par le Radar) → le pupitre est armé ;
    /// 2. on tourne les 2 clés de tir, l'une après l'autre → le missile part ;
    /// 3. après le temps de vol, la cible est détruite si elle est encore en vol.
    /// Un code bien formé mais inconnu déclenche la pénalité réglée dans la config.
    /// </summary>
    public class FireControl : NetworkBehaviour
    {
        public static FireControl Instance { get; private set; }

        /// <summary>Serveur : un contact a été abattu par notre missile (wasEnemy = false → ami abattu).</summary>
        public static event Action<RadarContact, bool> ServerShotDown;

        /// <summary>Tous les clients : une cible vient d'être abattue (pour le bruit d'explosion).</summary>
        public static event Action<ContactKind> LocalShotDown;

        // ---- État visible par tous (affiché au pupitre et sur les clés) ----
        public readonly NetworkVariable<FixedString32Bytes> ArmedCode = new();   // vide = pas armé
        public readonly NetworkVariable<bool> Key1Turned = new();
        public readonly NetworkVariable<bool> Key2Turned = new();
        public readonly NetworkVariable<double> LockedUntil = new(-1d);          // temps serveur
        public readonly NetworkVariable<FixedString512Bytes> Status = new();

        // ---- État serveur uniquement ----
        private struct InFlight { public int TargetId; public string FireCode; public double ImpactTime; }
        private readonly List<InFlight> _inFlight = new();
        private int _armedTargetId = -1;
        private double _firstKeyTime;
        private int _consecutiveErrors;

        private static FireSettings S => ConnectionManager.Instance.Config.fire;
        private double Now => NetworkManager.ServerTime.Time;
        public bool IsArmed => ArmedCode.Value.Length > 0;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (!TryGetComponent<ShotDownSound>(out _)) gameObject.AddComponent<ShotDownSound>();
            if (IsServer)
            {
                StationRegistry.ServerInteracted += OnServerStationInteracted;
                SetStatus("Entrez le code de tir d'une cible.");
            }
            Key1Turned.OnValueChanged += (_, v) => ColorKey(StationType.CleConv1, v);
            Key2Turned.OnValueChanged += (_, v) => ColorKey(StationType.CleConv2, v);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) StationRegistry.ServerInteracted -= OnServerStationInteracted;
            if (Instance == this) Instance = null;
        }

        // Clients : une clé tournée change de couleur pour tous ceux qui la voient.
        private static void ColorKey(StationType type, bool turned)
        {
            foreach (var s in StationRegistry.All)
                if (s.Type == type) s.SetStateColor(turned ? S.keyTurnedColor : null);
        }

        // ---------- Pupitre : saisie du code ----------

        /// <summary>Client au pupitre : envoie le code saisi au serveur.</summary>
        [Rpc(SendTo.Server)]
        public void SubmitCodeRpc(FixedString32Bytes code, RpcParams rpc = default)
        {
            if (!IsPlayerAtStation(rpc.Receive.SenderClientId, StationType.PupitreTir)) return;
            if (!GameRunning() || IsUnpowered()) return; // en panne, l'écran du pupitre est éteint
            if (Now < LockedUntil.Value) return; // le pupitre affiche déjà le compte à rebours

            string c = code.ToString().Trim().ToUpperInvariant();
            if (!FireCodeGenerator.IsWellFormed(c, S))
            {
                SetStatus("Format invalide : 1 lettre puis 1 chiffre (ex. K7).");
                return;
            }

            Disarm(); // un nouveau code remplace toujours le précédent
            if (AircraftManager.Instance.TryFindByFireCode(c, out var target))
            {
                _armedTargetId = target.Id;
                _consecutiveErrors = 0;
                ArmedCode.Value = new FixedString32Bytes(c);
                SetStatus($"Code {c} accepté. Tournez les 2 clés de tir.");
            }
            else Penalize(c);
        }

        private void Penalize(string code)
        {
            _consecutiveErrors++;
            var mode = S.penaltyMode;
            float lockFor = mode != FireCodePenaltyMode.LimiteEssais ? MatchSession.Instance.CurrentDifficulty.wrongCodeCooldown : 0f;

            if (mode != FireCodePenaltyMode.Cooldown && _consecutiveErrors >= S.maxConsecutiveErrors)
            {
                lockFor = Mathf.Max(lockFor, S.errorLimitLockout);
                _consecutiveErrors = 0;
            }

            if (lockFor > 0f) LockedUntil.Value = Now + lockFor;
            SetStatus(lockFor > 0f ? $"Code {code} refusé. Pupitre bloqué {lockFor:0} s." : $"Code {code} refusé.");
        }

        // ---------- Clés de tir ----------

        private void OnServerStationInteracted(Station station, ulong clientId, bool pressed)
        {
            if (!pressed) return;
            if (station.Type == StationType.CleConv1) TurnKey(Key1Turned, Key2Turned);
            else if (station.Type == StationType.CleConv2) TurnKey(Key2Turned, Key1Turned);
        }

        // Les clés de tir sont dans la mallette de l'équipe Tir : elle doit être ouverte.
        private void TurnKey(NetworkVariable<bool> key, NetworkVariable<bool> other)
        {
            if (!GameRunning() || key.Value || IsUnpowered()) return; // clés de tir électriques : rien sans courant
            if (Keys.KeySystem.Instance != null && !Keys.KeySystem.Instance.IsBriefcaseOpen(TeamId.Tir))
            {
                SetStatus("Clés indisponibles : ouvrez d'abord la mallette Tir avec deux clés perso.");
                return;
            }
            if (!IsArmed)
            {
                SetStatus("Clé tournée sans code armé : entrez d'abord un code au pupitre.");
                return;
            }

            key.Value = true;
            if (!other.Value)
            {
                _firstKeyTime = Now;
                SetStatus($"Code {ArmedCode.Value} : 1re clé tournée, tournez la 2e.");
            }
            else Launch();
        }

        private void Launch()
        {
            string code = ArmedCode.Value.ToString();
            _inFlight.Add(new InFlight { TargetId = _armedTargetId, FireCode = code, ImpactTime = Now + S.missileFlightTime });
            Disarm();
            SetStatus($"Missile lancé sur {code}. Impact dans {S.missileFlightTime:0} s.");
            Debug.Log($"[Tir] Missile lancé sur le code {code}.");
        }

        private void Disarm()
        {
            _armedTargetId = -1;
            ArmedCode.Value = default;
            Key1Turned.Value = false;
            Key2Turned.Value = false;
        }

        // ---------- Boucle serveur ----------

        private void Update()
        {
            if (!IsServer || AircraftManager.Instance == null) return;
            var mgr = AircraftManager.Instance;

            // La cible armée a disparu (arrivée sur la base, ou abattue par un missile précédent).
            if (IsArmed && !mgr.TryGetContact(_armedTargetId, out _))
            {
                string code = ArmedCode.Value.ToString();
                Disarm();
                SetStatus($"Cible {code} perdue : plus sur le radar.");
            }

            // Délai max entre les deux clés (0 = illimité).
            float timeout = ConnectionManager.Instance.Config.keys.conventionalKeyTimeout;
            if (timeout > 0f && Key1Turned.Value != Key2Turned.Value && Now - _firstKeyTime > timeout)
            {
                Key1Turned.Value = Key2Turned.Value = false;
                SetStatus("Trop lent entre les 2 clés : retournez-les.");
            }

            ResolveImpacts(mgr);
        }

        private void ResolveImpacts(AircraftManager mgr)
        {
            for (int i = _inFlight.Count - 1; i >= 0; i--)
            {
                var m = _inFlight[i];
                if (Now < m.ImpactTime) continue;
                _inFlight.RemoveAt(i);

                if (mgr.ServerTryDestroy(m.TargetId, out var contact, out bool wasEnemy))
                {
                    SetStatus($"Cible {m.FireCode} détruite.");
                    SpoilerLog.Log($"[Tir] {contact.Kind} {contact.Code} abattu ({(wasEnemy ? "ennemi" : "AMI")}).");
                    ServerShotDown?.Invoke(contact, wasEnemy);
                    ShotDownRpc(contact.Kind);
                }
                else SetStatus($"Missile sur {m.FireCode} : cible déjà disparue, raté.");
            }
        }

        /// <summary>Test (hôte) : fait entendre l'explosion à tous sans tirer.</summary>
        public void ServerTestShotDown(ContactKind kind) => ShotDownRpc(kind);

        // Tout le monde entend l'explosion (le camp de la cible n'est pas envoyé).
        [Rpc(SendTo.ClientsAndHost)]
        private void ShotDownRpc(ContactKind kind) => LocalShotDown?.Invoke(kind);

        // ---------- Utilitaires serveur ----------

        private static bool GameRunning() => GameClock.Instance != null && GameClock.Instance.IsActive;

        // Le pupitre et les clés de tir sont dans l'espace de l'équipe Tir.
        private static bool IsUnpowered() => Fuse.FuseSystem.Instance != null && Fuse.FuseSystem.Instance.IsBlackedOut(TeamId.Tir);

        private void SetStatus(string text) => Status.Value = new FixedString512Bytes(text);

        // Anti-triche simple : l'expéditeur doit être à portée d'un poste de ce type.
        private bool IsPlayerAtStation(ulong clientId, StationType type)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null) return false;
            var interactor = client.PlayerObject.GetComponent<PlayerInteractor>();
            foreach (var s in StationRegistry.All)
                if (s.Type == type && interactor.IsInRangeOf(s)) return true;
            return false;
        }
    }
}
