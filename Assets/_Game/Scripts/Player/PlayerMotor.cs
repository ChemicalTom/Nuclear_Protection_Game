using NuclearProtection.Config;
using NuclearProtection.Network;
using NuclearProtection.UI.Panels;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Player
{
    /// <summary>
    /// Déplacement à la première personne (seul le propriétaire le pilote ;
    /// la position est diffusée par un NetworkTransform en mode Owner).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : NetworkBehaviour
    {
        [Tooltip("Pivot de la caméra (tête).")]
        [SerializeField] private Transform head;
        [Tooltip("Corps visible par les autres (caché pour soi).")]
        [SerializeField] private Renderer body;

        public PlayerControls Controls { get; private set; }
        public Transform Head => head;

        private PlayerControlSettings _s;
        private CharacterController _cc;
        private Camera _cam;
        private float _pitch, _verticalSpeed;
        private bool _physicsSynced;

        public override void OnNetworkSpawn()
        {
            _s = ConnectionManager.Instance.Config.player;
            _cc = GetComponent<CharacterController>();
            head.localPosition = new Vector3(0f, _s.eyeHeight, 0f);
            PlayerAvatar.Attach(gameObject, body, OwnerClientId, IsOwner, ConnectionManager.Instance.Config.avatar);
            if (!IsOwner) return;

            Controls = new PlayerControls(_s);

            // On récupère la caméra de la scène et on la fixe sur notre tête.
            _cam = Camera.main;
            if (_cam != null)
            {
                _cam.transform.SetParent(head, false);
                _cam.transform.localPosition = Vector3.zero;
                _cam.transform.localRotation = Quaternion.identity;
            }
            CursorControl.SetLocked(true);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            // On détache la caméra pour qu'elle ne soit pas détruite avec le joueur.
            if (_cam != null) _cam.transform.SetParent(null, true);
            Controls?.Dispose();
            Controls = null;
            CursorControl.SetLocked(false);
        }

        private void Update()
        {
            if (!IsOwner || Controls == null) return;
            if (Outcome.MatchOutcome.IsOver) return; // partie finie : on ne bouge plus, l'écran de fin a le curseur

            // Écran de poste ouvert : on ne bouge pas, Échap le ferme.
            if (StationPanelHost.IsOpen)
            {
                if (Controls.ToggleCursor.WasPressedThisFrame()) StationPanelHost.Instance.Close();
                return;
            }

            if (Controls.ToggleCursor.WasPressedThisFrame())
                CursorControl.SetLocked(!CursorControl.IsLocked);

            if (CursorControl.IsLocked) Look();
            Move();
        }

        private void Look()
        {
            Vector2 d = Controls.Look.ReadValue<Vector2>() * _s.mouseSensitivity;
            transform.Rotate(0f, d.x, 0f);
            _pitch = Mathf.Clamp(_pitch - d.y, -_s.maxPitch, _s.maxPitch);
            head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move()
        {
            // Au 1er déplacement, on synchronise la physique avec la position d'apparition
            // (sinon le CharacterController peut ramener le joueur à l'origine).
            if (!_physicsSynced)
            {
                Physics.SyncTransforms();
                _physicsSynced = true;
            }

            Vector2 input = Vector2.ClampMagnitude(Controls.Move.ReadValue<Vector2>(), 1f);
            float speed = Controls.Sprint.IsPressed() ? _s.sprintSpeed : _s.walkSpeed;
            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * speed;

            _verticalSpeed = _cc.isGrounded ? -1f : _verticalSpeed - _s.gravity * Time.deltaTime;
            velocity.y = _verticalSpeed;
            _cc.Move(velocity * Time.deltaTime);
        }
    }
}
