using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Fuse;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>
    /// Boîtier à fusible. Fusible sauté : mini-jeu tiré au hasard à chaque panne.
    /// - Aiguille : appuyer (Espace ou bouton) quand l'aiguille passe dans la zone verte, plusieurs fois d'affilée.
    /// - Fils : cliquer un fil à gauche puis la prise de la même couleur à droite.
    /// La progression est tenue par le serveur : les coéquipiers la voient aussi.
    /// </summary>
    public class FusePanel : IStationPanel
    {
        public StationType Type => StationType.Fusible;
        public string Title => "BOÎTIER À FUSIBLE";

        private const float PressCooldown = 0.25f; // anti double appui
        private Station _station;
        private int _selectedWire = -1;
        private float _nextPress;

        private static FuseSettings S => ConnectionManager.Instance.Config.fuse;

        public void OnOpen(Station station)
        {
            _station = station;
            _selectedWire = -1;
        }

        public void OnClose() { }

        public void Draw(Rect area)
        {
            var fuse = FuseSystem.Instance;
            if (fuse == null) return;
            var state = fuse.State(_station.Team);
            var big = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18, wordWrap = true };

            if (!state.Blown)
            {
                GUI.Label(new Rect(area.x, area.y, area.width, 30), "<color=#60ff70>Fusible en place. Tension normale.</color>", big);
                _selectedWire = -1;
                return;
            }

            GUI.Label(new Rect(area.x, area.y, area.width, 30), "<color=#ff5040><b>FUSIBLE SAUTÉ</b></color> — remettez le courant.", big);
            var game = new Rect(area.x, area.y + 40, area.width, area.height - 40);
            if (state.Game == FuseMiniGame.Aiguille) DrawNeedle(fuse, state, game, big);
            else DrawWires(fuse, state, game, big);
        }

        // ---------- Aiguille ----------

        private void DrawNeedle(FuseSystem fuse, FuseState state, Rect area, GUIStyle big)
        {
            float angle = FuseMiniGames.NeedleAngle(NetworkManager.Singleton.ServerTime.Time, S.needleSpeed);
            float zone = FuseMiniGames.NeedleZoneCenter(state.Seed, state.Progress);

            GUI.Label(new Rect(area.x, area.y, area.width, 26),
                $"Appuyez quand l'aiguille est dans le VERT. Réussites : {state.Progress} / {S.needleSuccessesRequired}", big);

            float radius = Mathf.Min(area.width, area.height - 110) / 2f;
            var center = new Vector2(area.center.x, area.y + 40 + radius);

            // Cadran : graduations grises, zone verte, aiguille blanche.
            for (int deg = 0; deg < 360; deg += 10) DrawRay(center, deg, radius * 0.85f, radius, 2f, Color.gray);
            for (float deg = zone - S.needleGreenZone / 2f; deg <= zone + S.needleGreenZone / 2f; deg += 1.5f)
                DrawRay(center, deg, radius * 0.6f, radius, 4f, new Color(0.2f, 0.9f, 0.3f));
            DrawRay(center, angle, 0f, radius * 0.95f, 5f, Color.white);

            var e = Event.current;
            bool space = e.type == EventType.KeyDown && e.keyCode == KeyCode.Space;
            bool clicked = GUI.Button(new Rect(center.x - 90, center.y + radius + 12, 180, 36), "APPUYER (Espace)");
            if (space) e.Use();
            if ((space || clicked) && Time.time >= _nextPress)
            {
                _nextPress = Time.time + PressCooldown;
                fuse.NeedlePressRpc(_station.Team, FuseMiniGames.IsInZone(angle, zone, S.needleGreenZone));
            }
        }

        // Trait radial de « from » à « to » pixels du centre, à l'angle donné (0 = haut, sens horaire).
        private static void DrawRay(Vector2 center, float deg, float from, float to, float thickness, Color color)
        {
            // Rotation dans le repère local (avant l'échelle de l'UI), sinon le pivot se décale.
            var matrix = GUI.matrix;
            var old = GUI.color;
            var pivot = new Vector3(center.x, center.y, 0f);
            GUI.matrix = matrix * Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, deg)) * Matrix4x4.Translate(-pivot);
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - thickness / 2f, center.y - to, thickness, to - from), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.matrix = matrix;
        }

        // ---------- Fils ----------

        private void DrawWires(FuseSystem fuse, FuseState state, Rect area, GUIStyle big)
        {
            int count = S.EffectiveWireCount;
            var sockets = FuseMiniGames.WireSocketOrder(state.Seed, count);
            if (_selectedWire >= 0 && FuseMiniGames.IsWireConnected(state.Progress, _selectedWire)) _selectedWire = -1;

            GUI.Label(new Rect(area.x, area.y, area.width, 26),
                _selectedWire < 0 ? "Cliquez un fil à gauche, puis la prise de la même couleur à droite." : "Choisissez la prise de la même couleur.", big);

            float rowH = Mathf.Min(48f, (area.height - 40) / count);
            float colW = Mathf.Min(220f, area.width / 3f);
            for (int i = 0; i < count; i++)
            {
                float y = area.y + 40 + i * rowH;

                // Fil i (gauche).
                bool connected = FuseMiniGames.IsWireConnected(state.Progress, i);
                string label = connected ? "branché ✔" : i == _selectedWire ? "> SÉLECTIONNÉ <" : "fil";
                var wireRect = new Rect(area.x + 20, y, colW, rowH - 8);
                if (ColorButton(wireRect, S.wireColors[i], label, !connected, !connected, i == _selectedWire)) _selectedWire = i;

                // Prise (droite) : sa couleur est celle du fil attendu.
                int expected = sockets[i];
                bool used = FuseMiniGames.IsWireConnected(state.Progress, expected);
                var socketRect = new Rect(area.xMax - 20 - colW, y, colW, rowH - 8);
                if (ColorButton(socketRect, S.wireColors[expected], used ? "branché ✔" : "prise", !used && _selectedWire >= 0, !used, false))
                {
                    fuse.ConnectWireRpc(_station.Team, _selectedWire, i);
                    _selectedWire = -1;
                }
            }

            if (S.wireMistakeResets)
                GUI.Label(new Rect(area.x, area.y + 48 + count * rowH, area.width, 26), "Attention : un mauvais branchement fait tout sauter.", big);
        }

        private GUIStyle _swatchText;

        // Bouton en couleur pleine (la teinte du skin IMGUI sombre rendait les couleurs illisibles).
        // bright = couleur pleine (fil/prise libre) ; sinon assombri (déjà branché).
        private bool ColorButton(Rect r, Color color, string label, bool enabled, bool bright, bool selected)
        {
            _swatchText ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 };
            var old = GUI.color;
            if (selected) { GUI.color = Color.white; GUI.DrawTexture(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Texture2D.whiteTexture); }
            GUI.color = bright ? color : color * 0.35f;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;

            // Texte noir sur les couleurs claires, blanc sur les sombres (le style label ne grise pas le texte désactivé).
            float luma = 0.3f * color.r + 0.59f * color.g + 0.11f * color.b;
            _swatchText.normal.textColor = luma > 0.5f && bright ? Color.black : Color.white;
            GUI.enabled = enabled;
            bool clicked = GUI.Button(r, label, _swatchText);
            GUI.enabled = true;
            return clicked;
        }
    }
}
