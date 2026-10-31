using System.Collections.Generic;
using NuclearProtection.Player;
using NuclearProtection.Rooms;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>
    /// Affiche l'écran du poste ouvert par le joueur local (ouverture décidée par le serveur).
    /// Se ferme avec Échap, le bouton Fermer, ou si le joueur s'éloigne du poste.
    /// </summary>
    public class StationPanelHost : MonoBehaviour
    {
        [Tooltip("Fond opaque de l'écran (sinon les étiquettes 3D de la pièce se voient au travers).")]
        [SerializeField] private Color backgroundColor = new(0.1f, 0.11f, 0.13f, 1f);

        public static StationPanelHost Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._current != null;

        private readonly Dictionary<Core.StationType, IStationPanel> _panels = new();
        private IStationPanel _current;
        private Station _station;

        private void Awake()
        {
            Instance = this;
            Register(new RadarPanel());
            Register(new RegistryPanel());
            Register(new FirePanel());
            Register(new PhonePanel());
            Register(new FusePanel());
        }

        // Pendant une panne, les écrans de l'équipe sont éteints (sauf le boîtier à fusible et le téléphone, qui gère sa ligne).
        private static bool IsUnpowered(Station s) =>
            s.Type is not (Core.StationType.Fusible or Core.StationType.Telephone)
            && Fuse.FuseSystem.Instance != null && Fuse.FuseSystem.Instance.IsBlackedOut(s.Team);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Register(IStationPanel p) => _panels[p.Type] = p;

        /// <summary>Ouvre l'écran du poste, s'il en a un.</summary>
        public void Open(Station station)
        {
            if (!_panels.TryGetValue(station.Type, out var panel)) return;
            _current = panel;
            _station = station;
            panel.OnOpen(station);
            CursorControl.SetLocked(false);
        }

        public void Close()
        {
            if (_current == null) return;
            _current.OnClose();
            _current = null;
            _station = null;
            CursorControl.SetLocked(true);
        }

        private void Update()
        {
            // Fermeture automatique si on s'éloigne du poste.
            if (_current != null && !PlayerInteractor.LocalIsInRangeOf(_station)) Close();
        }

        private void OnGUI()
        {
            if (_current == null) return;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * DisplayControl.UiScale);
            float w = Screen.width / DisplayControl.UiScale, h = Screen.height / DisplayControl.UiScale;
            var frame = new Rect(w * 0.1f, h * 0.08f, w * 0.8f, h * 0.84f);

            var old = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(frame, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Box(frame, $"{_current.Title} — {_station.Team}");
            if (GUI.Button(new Rect(frame.xMax - 126, frame.y + 4, 120, 24), "Fermer (Échap)")) { Close(); return; }
            var content = new Rect(frame.x + 10, frame.y + 34, frame.width - 20, frame.height - 44);
            if (IsUnpowered(_station))
                GUI.Label(content, "<color=#803030><size=22>Écran éteint. Plus de courant.</size></color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
            else _current.Draw(content);
        }
    }
}
