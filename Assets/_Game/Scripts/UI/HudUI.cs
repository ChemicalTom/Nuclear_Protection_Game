using NuclearProtection.Core;
using NuclearProtection.Player;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>HUD : horloge (visible par tous), viseur, invite d'interaction, sonnerie, messages courts.</summary>
    public class HudUI : MonoBehaviour
    {
        private GUIStyle _clockStyle, _crosshair;

        private static string _toast;
        private static float _toastUntil;

        /// <summary>Affiche un message court en bas de l'écran du joueur local.</summary>
        public static void Toast(string text, float duration)
        {
            _toast = text;
            _toastUntil = Time.time + duration;
        }

        private void OnGUI()
        {
            UiTheme.Begin();
            _clockStyle ??= new GUIStyle(UiTheme.Title) { fontSize = UiTheme.S.fontSize + 11 };
            _crosshair ??= new GUIStyle(UiTheme.Center) { fontSize = UiTheme.S.fontSize + 3, normal = { textColor = UiTheme.S.accent } };
            var size = UiTheme.Size;
            float w = size.x, h = size.y;

            if (GameClock.Instance != null)
            {
                var plate = new Rect(w / 2 - 80, 6, 160, 42);
                GUI.Box(plate, GUIContent.none, UiTheme.PanelStyle);
                GUI.Label(plate, GameClock.Instance.Formatted, _clockStyle);
            }

            // Indication visuelle de la sonnerie (utile sans casque, et pour les malentendants).
            if (Phone.PhoneAudio.RingingHint != null)
                GUI.Box(new Rect(w / 2 - 170, 54, 340, 30), UiTheme.Warn(Phone.PhoneAudio.RingingHint), UiTheme.Badge);

            if (_toast != null && Time.time < _toastUntil)
                GUI.Box(new Rect(w / 2 - 260, h - 110, 520, 44), _toast, UiTheme.Badge);

            if (Panels.StationPanelHost.IsOpen || Outcome.MatchOutcome.IsOver) return; // l'écran du poste (ou de fin) prend la place

            GUI.Label(new Rect(w / 2 - 10, h / 2 - 11, 20, 20), "+", _crosshair);

            if (PlayerInteractor.LocalPrompt != null)
                GUI.Label(new Rect(0, h / 2 + 14, w, 24), UiTheme.Ok(PlayerInteractor.LocalPrompt), UiTheme.Center);

            if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Label(new Rect(0, h - 30, w, 24), UiTheme.Dim("Curseur libre — Échap pour reprendre le contrôle"), UiTheme.Center);
        }
    }
}
