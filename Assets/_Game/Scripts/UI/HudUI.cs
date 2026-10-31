using NuclearProtection.Core;
using NuclearProtection.Player;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>HUD graybox : horloge (visible par tous), viseur, invite d'interaction, aide curseur.</summary>
    public class HudUI : MonoBehaviour
    {
        private GUIStyle _clockStyle, _centerStyle, _toastStyle;

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
            _clockStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
            _centerStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            GUI.matrix = Matrix4x4.Scale(Vector3.one * DisplayControl.UiScale);
            float w = Screen.width / DisplayControl.UiScale, h = Screen.height / DisplayControl.UiScale;

            if (GameClock.Instance != null)
                GUI.Label(new Rect(0, 8, w, 40), GameClock.Instance.Formatted, _clockStyle);

            // Indication visuelle de la sonnerie (utile sans casque, et pour les malentendants).
            if (Phone.PhoneAudio.RingingHint != null)
                GUI.Label(new Rect(0, 44, w, 24), Phone.PhoneAudio.RingingHint, _centerStyle);

            if (_toast != null && Time.time < _toastUntil)
            {
                _toastStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleCenter };
                GUI.Box(new Rect(w / 2 - 260, h - 110, 520, 44), _toast, _toastStyle);
            }

            if (Panels.StationPanelHost.IsOpen || Outcome.MatchOutcome.IsOver) return; // l'écran du poste (ou de fin) prend la place

            GUI.Label(new Rect(w / 2 - 10, h / 2 - 10, 20, 20), "+", _centerStyle);

            if (PlayerInteractor.LocalPrompt != null)
                GUI.Label(new Rect(0, h / 2 + 14, w, 24), PlayerInteractor.LocalPrompt, _centerStyle);

            if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Label(new Rect(0, h - 30, w, 24), "Curseur libre — Échap pour reprendre le contrôle", _centerStyle);
        }
    }
}
