using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Rooms
{
    /// <summary>
    /// Poste interactif graybox (non réseau). Son Id est identique sur toutes les machines,
    /// ce qui permet au serveur et aux clients de parler du même poste sans NetworkObject.
    /// </summary>
    public class Station : MonoBehaviour
    {
        public StationInfo Info { get; private set; }
        public int Id => Info.Id;
        public StationType Type => Info.Type;
        public TeamId Team => Info.Team;

        /// <summary>Texte affiché dans l'invite d'interaction.</summary>
        public string DisplayName => $"{Type} ({Team})";

        private Renderer _renderer;
        private Color _baseColor;
        private float _flashUntil;
        private float _glow;

        /// <summary>
        /// indicator = pièce du modèle qui affiche l'état (écran, porte…), sinon le rendu du poste lui-même.
        /// glow > 0 : la pièce est lumineuse (écran CRT), son émission suit la couleur d'état.
        /// </summary>
        public void Init(StationInfo info, Color color, Renderer indicator = null, float glow = 0f)
        {
            Info = info;
            _renderer = indicator != null ? indicator : GetComponent<Renderer>();
            _glow = glow;
            _baseColor = color;
            SetColor(color);
        }

        /// <summary>Retour visuel bref (démo de l'interaction validée par le serveur).</summary>
        public void Flash(float duration)
        {
            _flashUntil = Time.time + duration;
            SetColor(Color.white);
        }

        /// <summary>Couleur d'état durable (ex. clé tournée) ; null = couleur normale du poste.</summary>
        public void SetStateColor(Color? color)
        {
            _stateColor = color;
            if (_flashUntil <= 0f) SetColor(CurrentColor);
        }

        /// <summary>Panne de fusible : un poste hors tension s'assombrit (sa couleur d'état revient au retour du courant).</summary>
        public void SetPowered(bool powered, Color unpoweredColor)
        {
            if (_powered == powered) return;
            _powered = powered;
            _unpoweredColor = unpoweredColor;
            if (_flashUntil <= 0f) SetColor(CurrentColor);
        }

        private Color? _stateColor;
        private bool _powered = true;
        private Color _unpoweredColor;
        private Color CurrentColor => !_powered ? _unpoweredColor : _stateColor ?? _baseColor;

        private void Update()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                SetColor(CurrentColor);
            }
        }

        private void SetColor(Color c)
        {
            if (_renderer == null) return;
            if (_glow <= 0f) { _renderer.material.color = c; return; }
            // Écran : verre sombre teinté, la couleur d'état vient surtout de la lueur.
            _renderer.material.color = c * 0.15f;
            Visuals.MaterialKit.SetGlow(_renderer.material, c, _glow);
        }
    }
}
