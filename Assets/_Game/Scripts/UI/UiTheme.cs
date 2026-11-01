using NuclearProtection.Config;
using NuclearProtection.Network;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Thème commun de l'interface IMGUI (terminal militaire guerre froide), construit en code depuis UiSettings.
    /// Chaque OnGUI commence par UiTheme.Begin() : skin, échelle et taille virtuelle de l'écran.
    /// </summary>
    public static class UiTheme
    {
        private static GUISkin _skin;
        private static UiSettings _fallback;

        public static UiSettings S =>
            ConnectionManager.Instance != null && ConnectionManager.Instance.Config != null
                ? ConnectionManager.Instance.Config.ui
                : _fallback ??= new UiSettings();

        public static GUIStyle Title, Header, Small, Center, Big, Badge, PanelStyle, TitleBarStyle, Backdrop;

        /// <summary>Taille de l'écran en unités d'interface (après mise à l'échelle).</summary>
        public static Vector2 Size => new(Screen.width / DisplayControl.UiScale, Screen.height / DisplayControl.UiScale);

        /// <summary>À appeler au début de chaque OnGUI.</summary>
        public static void Begin()
        {
            if (_skin == null) Build();
            GUI.skin = _skin;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * DisplayControl.UiScale);
        }

        /// <summary>Panneau à bordure, avec barre de titre si title n'est pas null. Renvoie la zone intérieure.</summary>
        public static Rect Panel(Rect r, string title = null)
        {
            GUI.Box(r, GUIContent.none, PanelStyle);
            if (title == null) return new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20);
            GUI.Box(new Rect(r.x, r.y, r.width, 32), GUIContent.none, TitleBarStyle);
            GUI.Label(new Rect(r.x + 12, r.y, r.width - 24, 32), title, Header);
            return new Rect(r.x + 12, r.y + 42, r.width - 24, r.height - 52);
        }

        /// <summary>Voile sombre sur tout l'écran (menus, fin de partie).</summary>
        public static void DrawBackdrop() => GUI.Box(new Rect(0, 0, Size.x, Size.y), GUIContent.none, Backdrop);

        // Texte coloré pour les labels riches.
        public static string Ok(string t) => Paint(t, S.accent);
        public static string Warn(string t) => Paint(t, S.warning);
        public static string Bad(string t) => Paint(t, S.danger);
        public static string Dim(string t) => Paint(t, S.dimText);
        public static string Paint(string t, Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{t}</color>";

        // Construit le skin à partir du skin par défaut (doit être appelé pendant OnGUI).
        private static void Build()
        {
            var s = S;
            _skin = Object.Instantiate(GUI.skin);
            var font = FindFont(s);
            if (font != null) _skin.font = font;

            Style(_skin.label, s.text, null);
            _skin.label.richText = true;
            _skin.label.fontSize = s.fontSize;

            Style(_skin.box, s.text, Framed(s.panel, s.border));
            Style(_skin.button, s.text, Framed(s.button, s.border));
            _skin.button.hover.background = Framed(s.buttonHover, s.accent);
            _skin.button.active.background = Framed(s.buttonActive, s.accent);
            _skin.button.onNormal.background = Framed(s.buttonActive, s.accent); // onglet sélectionné (Toolbar)
            _skin.button.onHover.background = Framed(s.buttonActive, s.accent);
            _skin.button.hover.textColor = _skin.button.active.textColor = _skin.button.onNormal.textColor = _skin.button.onHover.textColor = Color.white;
            _skin.button.fontSize = s.fontSize;
            _skin.button.padding = new RectOffset(10, 10, 6, 6);
            _skin.button.richText = true;

            Style(_skin.textField, s.accent, Framed(s.field, s.border));
            _skin.textField.focused.background = Framed(s.field, s.accent);
            _skin.textField.focused.textColor = s.accent;
            _skin.textField.fontSize = s.fontSize + 1;
            _skin.textField.padding = new RectOffset(8, 8, 5, 5);

            // Barres de défilement fines, sans flèches.
            foreach (var (bar, thumb) in new[] { (_skin.verticalScrollbar, _skin.verticalScrollbarThumb), (_skin.horizontalScrollbar, _skin.horizontalScrollbarThumb) })
            {
                Style(bar, s.text, Framed(s.field, s.border));
                Style(thumb, s.text, Framed(s.buttonActive, s.accent));
            }
            _skin.verticalScrollbarUpButton = _skin.verticalScrollbarDownButton = GUIStyle.none;
            _skin.horizontalScrollbarLeftButton = _skin.horizontalScrollbarRightButton = GUIStyle.none;

            // Curseurs (mode de difficulté Perso).
            Style(_skin.horizontalSlider, s.text, Framed(s.field, s.border));
            _skin.horizontalSlider.fixedHeight = 8;
            _skin.horizontalSlider.margin = new RectOffset(4, 4, 8, 4);
            Style(_skin.horizontalSliderThumb, s.text, Framed(s.buttonActive, s.accent));
            _skin.horizontalSliderThumb.hover.background = _skin.horizontalSliderThumb.active.background = Framed(s.accent, s.accent);
            _skin.horizontalSliderThumb.fixedHeight = 16;
            _skin.horizontalSliderThumb.fixedWidth = 12;

            PanelStyle = new GUIStyle(_skin.box) { normal = { background = Framed(s.panel, s.border, 2) }, border = new RectOffset(3, 3, 3, 3) };
            TitleBarStyle = new GUIStyle(_skin.box) { normal = { background = Framed(s.titleBar, s.border, 2) }, border = new RectOffset(3, 3, 3, 3) };
            Backdrop = new GUIStyle { normal = { background = Flat(s.backdrop) } };
            Title = new GUIStyle(_skin.label) { fontSize = s.fontSize * 2 + 6, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = s.accent } };
            Header = new GUIStyle(_skin.label) { fontSize = s.fontSize + 2, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = s.accent } };
            Small = new GUIStyle(_skin.label) { fontSize = s.fontSize - 2, wordWrap = true, normal = { textColor = s.dimText } };
            Center = new GUIStyle(_skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            Big = new GUIStyle(_skin.label) { fontSize = s.fontSize + 5, wordWrap = true };
            Badge = new GUIStyle(_skin.box) { fontSize = s.fontSize, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true, padding = new RectOffset(10, 10, 6, 6), normal = { background = Framed(s.titleBar, s.warning), textColor = s.text } };

            // Les textes ne changent pas de couleur au survol de la souris.
            foreach (var st in new[] { _skin.label, Title, Header, Small, Center, Big }) NoHover(st);
        }

        /// <summary>Garde la couleur normale au survol (à appliquer aux styles de texte dérivés).</summary>
        public static GUIStyle NoHover(GUIStyle st)
        {
            st.hover.textColor = st.normal.textColor;
            return st;
        }

        private static Font FindFont(UiSettings s)
        {
            var installed = Font.GetOSInstalledFontNames();
            foreach (var name in s.fontNames)
                if (System.Array.IndexOf(installed, name) >= 0) return Font.CreateDynamicFontFromOSFont(name, s.fontSize);
            return null; // police par défaut de Unity
        }

        private static void Style(GUIStyle st, Color text, Texture2D bg)
        {
            st.normal.textColor = text;
            if (bg != null)
            {
                st.normal.background = bg;
                st.border = new RectOffset(2, 2, 2, 2);
            }
        }

        // Petite texture unie avec un liseré (étirée en 9 parties par Unity).
        private static Texture2D Framed(Color fill, Color edge, int edgePx = 1)
        {
            const int n = 8;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, x < edgePx || y < edgePx || x >= n - edgePx || y >= n - edgePx ? edge : fill);
            tex.Apply();
            return tex;
        }

        private static Texture2D Flat(Color c) => Framed(c, c);
    }
}
