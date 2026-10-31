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
            UiTheme.Begin();
            var size = UiTheme.Size;
            // Sous la plaque de l'horloge du HUD.
            float top = 56f;
            var frame = new Rect(size.x * 0.08f, top, size.x * 0.84f, size.y - top - 12f);

            // Panneau opaque (sinon les étiquettes 3D de la pièce se voient au travers).
            var content = UiTheme.Panel(frame, $"{_current.Title.ToUpperInvariant()}  —  {UiTheme.Dim("ÉQUIPE " + _station.Team.ToString().ToUpperInvariant())}");
            if (GUI.Button(new Rect(frame.xMax - 136, frame.y + 3, 132, 26), "Fermer [Échap]")) { Close(); return; }
            if (IsUnpowered(_station))
                GUI.Label(content, UiTheme.Bad("ÉCRAN ÉTEINT — PLUS DE COURANT"), new GUIStyle(UiTheme.Big) { alignment = TextAnchor.MiddleCenter });
            else _current.Draw(content);
        }
    }
}
