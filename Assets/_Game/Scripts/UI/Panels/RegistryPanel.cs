using NuclearProtection.Aircraft;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>Registre des avions : codes triés avec leur camp, recherche optionnelle (config).</summary>
    public class RegistryPanel : IStationPanel
    {
        public StationType Type => StationType.Registre;
        public string Title => "REGISTRE DES AVIONS";

        private string _search = "";
        private Vector2 _scroll;

        public void OnOpen(Station station)
        {
            _search = "";
            _scroll = Vector2.zero;
        }

        public void OnClose() { }

        public void Draw(Rect area)
        {
            var mgr = AircraftManager.Instance;
            if (mgr == null) return;
            var rich = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 };

            GUILayout.BeginArea(area);
            if (ConnectionManager.Instance.Config.aircraft.registryAllowSearch)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Rechercher :", GUILayout.Width(100));
                _search = GUILayout.TextField(_search, 8).ToUpperInvariant();
                GUILayout.EndHorizontal();
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var e in mgr.Registry)
            {
                string code = e.Code.ToString();
                if (_search.Length > 0 && !code.Contains(_search)) continue;
                string side = e.IsEnemy ? UiTheme.Bad("ENNEMI") : UiTheme.Ok("AMI");
                GUILayout.Label($"{code}    {side}", rich);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
