using NuclearProtection.Config;
using NuclearProtection.Network;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Plein écran (touche F11 par défaut, ou bouton du menu) et taille de l'interface,
    /// calculée d'après la hauteur de l'écran pour qu'elle ne soit jamais minuscule.
    /// </summary>
    public class DisplayControl : MonoBehaviour
    {
        private InputAction _toggle;

        private static DisplaySettings S =>
            ConnectionManager.Instance != null && ConnectionManager.Instance.Config != null
                ? ConnectionManager.Instance.Config.display
                : null;

        /// <summary>Échelle à appliquer à toute l'interface IMGUI.</summary>
        public static float UiScale
        {
            get
            {
                var s = S;
                if (s == null) return 1.5f;
                return Mathf.Max(s.minUiScale, s.uiScaleAtReference * Screen.height / s.referenceHeight);
            }
        }

        public static bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

        /// <summary>Bascule plein écran (fenêtré sans bordure, à la résolution native) / fenêtre.</summary>
        public static void ToggleFullscreen()
        {
            if (IsFullscreen)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
            else
            {
                var native = Screen.currentResolution;
                Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            }
        }

        private void Start()
        {
            if (S == null) return;
            _toggle = new InputAction("ToggleFullscreen", binding: S.toggleFullscreen);
            _toggle.Enable();
        }

        private void OnDestroy() => _toggle?.Dispose();

        private void Update()
        {
            if (_toggle != null && _toggle.WasPressedThisFrame()) ToggleFullscreen();
        }
    }
}
