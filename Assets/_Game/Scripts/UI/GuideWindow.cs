using NuclearProtection.Config;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>Fenêtre du guide (menu principal) : un onglet par page, texte qui défile, bouton Fermer.</summary>
    public class GuideWindow
    {
        private const float MaxWidth = 760f, TabHeight = 30f;

        public bool IsOpen { get; set; }

        /// <summary>Page affichée (index dans GuideSettings.pages).</summary>
        public int Page { get => _page; set { _page = value; _scroll = Vector2.zero; } }

        private int _page;
        private Vector2 _scroll;
        private GUIStyle _body;

        public void Draw(GuideSettings guide)
        {
            if (!IsOpen || guide.pages.Length == 0) return;
            _body ??= UiTheme.NoHover(new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true });
            _page = Mathf.Clamp(_page, 0, guide.pages.Length - 1);

            var size = UiTheme.Size;
            float w = Mathf.Min(MaxWidth, size.x - 20f), h = size.y - 40f;
            var inner = UiTheme.Panel(new Rect((size.x - w) / 2f, 20f, w, h), guide.windowTitle);

            // Onglets sur 2 lignes si besoin.
            var titles = new string[guide.pages.Length];
            for (int i = 0; i < titles.Length; i++) titles[i] = guide.pages[i].title;
            int perRow = Mathf.CeilToInt(titles.Length / 2f);
            int tabRows = Mathf.CeilToInt((float)titles.Length / perRow);
            int pick = GUI.SelectionGrid(new Rect(inner.x, inner.y, inner.width, TabHeight * tabRows), _page, titles, perRow);
            if (pick != _page) { _page = pick; _scroll = Vector2.zero; }

            float top = inner.y + TabHeight * tabRows + 12f, bottom = inner.yMax - 40f;
            var view = new Rect(inner.x, top, inner.width, bottom - top);
            var content = new GUIContent(guide.pages[_page].body);
            float textHeight = _body.CalcHeight(content, view.width - 20f);
            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0, 0, view.width - 20f, textHeight));
            GUI.Label(new Rect(0, 0, view.width - 20f, textHeight), content, _body);
            GUI.EndScrollView();

            if (GUI.Button(new Rect(inner.xMax - 140f, inner.yMax - 32f, 140f, 32f), "Fermer [Échap]")
                || Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                IsOpen = false;
        }
    }
}
