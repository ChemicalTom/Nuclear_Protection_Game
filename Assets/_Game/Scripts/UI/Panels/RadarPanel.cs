using NuclearProtection.Aircraft;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>
    /// Écran radar : échos des avions (verts) et missiles (orange). Cliquer un écho affiche
    /// son code d'identification, son code de tir, sa distance et son temps d'arrivée.
    /// Le camp n'est PAS affiché.
    /// </summary>
    public class RadarPanel : IStationPanel
    {
        public StationType Type => StationType.Radar;
        public string Title => "RADAR";

        private static readonly Color PlaneColor = new(0.3f, 1f, 0.4f);
        private static readonly Color MissileColor = new(1f, 0.55f, 0.1f);
        private static readonly Color SelectedColor = Color.white;


        private int _selectedId = -1;

        public void OnOpen(Station station) => _selectedId = -1;

        public void OnClose() { }

        public void Draw(Rect area)
        {
            var mgr = AircraftManager.Instance;
            if (mgr == null) return;
            double now = NetworkManager.Singleton.ServerTime.Time;

            float size = Mathf.Min(area.height, area.width * 0.62f);
            var scope = new Rect(area.x, area.y, size, size);
            GUI.DrawTexture(scope, RadarTextures.Scope(ConnectionManager.Instance.Config.aircraft.radarRings));

            foreach (var c in mgr.Contacts) DrawBlip(c, scope, now);
            DrawInfo(new Rect(scope.xMax + 16, area.y, area.width - size - 16, area.height), mgr, now);
        }

        private void DrawBlip(RadarContact c, Rect scope, double now)
        {
            Vector2 p = c.ScreenPosAt(now);
            var center = scope.center + new Vector2(p.x, -p.y) * (scope.width / 2f);
            float blip = ConnectionManager.Instance.Config.aircraft.radarBlipSize;
            var r = new Rect(center.x - blip / 2, center.y - blip / 2, blip, blip);

            var old = GUI.color;
            GUI.color = c.Id == _selectedId ? SelectedColor : c.Kind == ContactKind.Missile ? MissileColor : PlaneColor;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;

            // Zone cliquable un peu plus large que l'écho.
            if (GUI.Button(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), GUIContent.none, GUIStyle.none))
                _selectedId = c.Id;
        }

        private void DrawInfo(Rect area, AircraftManager mgr, double now)
        {
            GUILayout.BeginArea(area);
            GUILayout.Label($"Échos : {mgr.Contacts.Count}");
            GUILayout.Space(8);

            if (_selectedId >= 0 && mgr.TryGetContact(_selectedId, out var c))
            {
                float km = c.DistanceAt(now) * ConnectionManager.Instance.Config.aircraft.radarRangeKm;
                GUILayout.Label(c.Kind == ContactKind.Missile ? "<b>MISSILE</b>" : $"<b>Code : {c.Code}</b>", Rich());
                GUILayout.Label($"<b>Code de tir : {c.FireCode}</b>", Rich());
                GUILayout.Label($"Cap d'origine : {Mathf.RoundToInt(c.AngleDeg):000}°");
                GUILayout.Label($"Distance : {km:0} km");
                GUILayout.Label(c.PassesBy ? $"Passe à côté, sort du radar dans : {c.EtaAt(now):0} s" : $"Impact base dans : {c.EtaAt(now):0} s");
            }
            else GUILayout.Label("Clique sur un écho pour l'identifier.");
            GUILayout.EndArea();
        }

        private static GUIStyle Rich() => new(GUI.skin.label) { richText = true, fontSize = 18 };
    }
}
