using NuclearProtection.Network;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NuclearProtection.Player
{
    /// <summary>
    /// Interaction : le propriétaire vise un poste et envoie appui/relâchement au serveur,
    /// qui vérifie la distance puis déclenche StationRegistry.ServerInteracted.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerInteractor : NetworkBehaviour
    {
        /// <summary>Invite affichée par le HUD du joueur local (null = rien à viser).</summary>
        public static string LocalPrompt { get; private set; }

        /// <summary>Le joueur contrôlé sur cette machine.</summary>
        public static PlayerInteractor Local { get; private set; }

        private PlayerMotor _motor;
        private Station _aimed;
        private int _heldId = -1;

        private Config.PlayerControlSettings S => ConnectionManager.Instance.Config.player;

        public override void OnNetworkSpawn()
        {
            _motor = GetComponent<PlayerMotor>();
            if (IsOwner) Local = this;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            LocalPrompt = null;
            if (Local == this) Local = null;
        }

        /// <summary>Le joueur local est-il assez près de ce poste ? (fermeture auto des écrans)</summary>
        public static bool LocalIsInRangeOf(Station station) =>
            Local != null && station != null && Local.InRange(station);

        /// <summary>Serveur : ce joueur est-il à portée du poste ? (validation des RPC de pupitre)</summary>
        public bool IsInRangeOf(Station station) => station != null && InRange(station);

        // Même règle de distance côté client et côté serveur.
        private bool InRange(Station station)
        {
            var col = station.GetComponent<Collider>();
            Vector3 eye = _motor.Head.position;
            Vector3 closest = col != null ? col.ClosestPoint(eye) : station.transform.position;
            return Vector3.Distance(eye, closest) <= S.interactRange + S.serverRangeTolerance;
        }

        private void Update()
        {
            if (!IsOwner || _motor.Controls == null) return;
            if (UI.Panels.StationPanelHost.IsOpen || Outcome.MatchOutcome.IsOver) { LocalPrompt = null; return; }

            _aimed = FindAimedStation();
            string key = _motor.Controls.Interact.GetBindingDisplayString();
            LocalPrompt = _aimed != null ? $"[{key}] {_aimed.DisplayName}" : null;

            if (_motor.Controls.Interact.WasPressedThisFrame() && _aimed != null) RequestInteract(_aimed, true);
            if (_motor.Controls.Interact.WasReleasedThisFrame() && _heldId >= 0) RequestInteract(null, false);
        }

        /// <summary>
        /// Propriétaire : demande au serveur d'appuyer sur un poste (pressed=true)
        /// ou de relâcher le poste tenu (pressed=false).
        /// </summary>
        public void RequestInteract(Station station, bool pressed)
        {
            if (!IsOwner) return;
            if (pressed && station != null)
            {
                _heldId = station.Id;
                InteractRpc(_heldId, true);
            }
            else if (!pressed && _heldId >= 0)
            {
                InteractRpc(_heldId, false);
                _heldId = -1;
            }
        }

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        // Premier objet touché qui n'est pas notre propre corps : s'il s'agit d'un poste, on le vise.
        private Station FindAimedStation()
        {
            var ray = new Ray(_motor.Head.position, _motor.Head.forward);
            int n = Physics.RaycastNonAlloc(ray, _hits, S.interactRange, ~0, QueryTriggerInteraction.Ignore);

            RaycastHit? nearest = null;
            for (int i = 0; i < n; i++)
            {
                if (_hits[i].collider.transform.IsChildOf(transform)) continue;
                if (nearest == null || _hits[i].distance < nearest.Value.distance) nearest = _hits[i];
            }
            return nearest?.collider.GetComponentInParent<Station>();
        }

        // ---------- Serveur ----------

        [Rpc(SendTo.Server)]
        private void InteractRpc(int stationId, bool pressed)
        {
            if (!StationRegistry.TryGet(stationId, out var station)) return;
            if (Outcome.MatchOutcome.IsOver) return; // plus aucune action après la fin
            if (!InRange(station)) return; // anti-triche simple : il faut être à portée

            StationRegistry.RaiseServerInteracted(station, OwnerClientId, pressed);
            if (!pressed) return;
            FlashStationRpc(stationId);
            OpenPanelRpc(stationId);
        }

        /// <summary>Le serveur autorise ce joueur à ouvrir l'écran du poste (s'il en a un).</summary>
        [Rpc(SendTo.Owner)]
        private void OpenPanelRpc(int stationId)
        {
            if (StationRegistry.TryGet(stationId, out var s) && UI.Panels.StationPanelHost.Instance != null)
                UI.Panels.StationPanelHost.Instance.Open(s);
        }

        /// <summary>Retour visuel sur toutes les machines : preuve que le serveur a validé.</summary>
        [Rpc(SendTo.Everyone)]
        private void FlashStationRpc(int stationId)
        {
            if (StationRegistry.TryGet(stationId, out var s)) s.Flash(S.interactFeedbackDuration);
        }
    }
}
