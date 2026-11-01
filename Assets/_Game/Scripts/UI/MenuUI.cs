using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Menu principal (IMGUI, thème terminal) : identité, puis Héberger (durée, seed) ou Rejoindre (IP).
    /// Affiché uniquement quand on n'est pas connecté.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        private const float ColumnWidth = 330f, Gap = 16f;

        private string _name, _port, _ip = "127.0.0.1", _customMinutes = "10", _seed = "";
        private int _durationChoice; // index dans les préréglages ; = longueur => personnalisé
        private readonly GuideWindow _guide = new();

        /// <summary>Ouvre le guide sur une page (bouton du menu, tests).</summary>
        public void OpenGuide(int page = 0)
        {
            _guide.Page = page;
            _guide.IsOpen = true;
        }

        private ConnectionManager Cm => ConnectionManager.Instance;

        private void Start()
        {
            if (Cm == null || Cm.Config == null) return;
            _name = Cm.Config.network.defaultPlayerName;
            _port = Cm.Config.network.defaultPort.ToString();
        }

        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || Cm == null || Cm.Config == null || _name == null) return;
            if (nm.IsListening || nm.ShutdownInProgress) return;

            UiTheme.Begin();
            var size = UiTheme.Size;
            UiTheme.DrawBackdrop();
            if (_guide.IsOpen) { _guide.Draw(Cm.Config.guide); return; }

            float total = ColumnWidth * 2 + Gap;
            float x = (size.x - total) / 2f, y = Mathf.Max(10f, size.y * 0.06f);

            GUI.Label(new Rect(0, y, size.x, 44), "NUCLEAR PROTECTION", UiTheme.Title);
            GUI.Label(new Rect(0, y + 42, size.x, 22), UiTheme.Dim("POSTE DE COMMANDEMENT — DÉFENSE AÉRIENNE"), UiTheme.Center);
            y += 76;

            DrawIdentity(new Rect(x, y, total, 74));
            y += 84;
            float colHeight = Mathf.Max(220f, size.y - y - 60f);
            DrawHostSection(new Rect(x, y, ColumnWidth, colHeight));
            DrawJoinSection(new Rect(x + ColumnWidth + Gap, y, ColumnWidth, colHeight));

            float bottom = y + colHeight + 8;
            if (GUI.Button(new Rect(x, bottom, 190, 30), Cm.Config.guide.buttonLabel)) OpenGuide();
            if (!string.IsNullOrEmpty(Cm.LastError))
                GUI.Label(new Rect(x + 200, bottom, total - 400, 40), UiTheme.Bad(Cm.LastError), UiTheme.Center);
            if (GUI.Button(new Rect(x + total - 190, bottom, 190, 30), DisplayControl.IsFullscreen ? "Fenêtré (F11)" : "Plein écran (F11)"))
                DisplayControl.ToggleFullscreen();
        }

        private void DrawIdentity(Rect r)
        {
            var inner = UiTheme.Panel(r);
            float half = (inner.width - Gap) / 2f;
            GUI.Label(new Rect(inner.x, inner.y, half, 20), UiTheme.Dim("INDICATIF (ton nom)"));
            _name = GUI.TextField(new Rect(inner.x, inner.y + 22, half, 30), _name, 20);
            GUI.Label(new Rect(inner.x + half + Gap, inner.y, half, 20), UiTheme.Dim("PORT UDP"));
            _port = GUI.TextField(new Rect(inner.x + half + Gap, inner.y + 22, half, 30), _port, 5);
        }

        private void DrawHostSection(Rect r)
        {
            var inner = UiTheme.Panel(r, "» HÉBERGER");
            GUILayout.BeginArea(inner);
            GUILayout.Label(UiTheme.Dim("DURÉE DE LA PARTIE"));
            var presets = Cm.Config.duration.presetMinutes;
            var labels = new string[presets.Length + 1];
            for (int i = 0; i < presets.Length; i++) labels[i] = $"{presets[i]} min";
            labels[presets.Length] = "Perso";
            _durationChoice = GUILayout.Toolbar(_durationChoice, labels, GUILayout.Height(30));

            if (_durationChoice == presets.Length)
            {
                var d = Cm.Config.duration;
                GUILayout.Label(UiTheme.Dim($"MINUTES ({d.customMinMinutes}-{d.customMaxMinutes})"));
                _customMinutes = GUILayout.TextField(_customMinutes, 3);
            }

            GUILayout.Label(UiTheme.Dim("SEED (vide = aléatoire)"));
            _seed = GUILayout.TextField(_seed, 11);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("CRÉER LA PARTIE", GUILayout.Height(38)) && TryParsePort(out var port))
                Cm.Host(_name, port, SelectedMinutes(), int.TryParse(_seed, out var s) ? s : null);
            GUILayout.EndArea();
        }

        private void DrawJoinSection(Rect r)
        {
            var inner = UiTheme.Panel(r, "» REJOINDRE");
            GUILayout.BeginArea(inner);
            GUILayout.Label(UiTheme.Dim("IP DE L'HÔTE"));
            _ip = GUILayout.TextField(_ip, 15);
            GUILayout.Label("L'hôte voit ses adresses dans le lobby.", UiTheme.Small);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SE CONNECTER", GUILayout.Height(38)) && TryParsePort(out var port))
                Cm.Join(_name, _ip, port);
            GUILayout.EndArea();
        }

        private int SelectedMinutes()
        {
            var presets = Cm.Config.duration.presetMinutes;
            if (_durationChoice < presets.Length) return presets[_durationChoice];
            return Cm.Config.duration.ClampCustom(int.TryParse(_customMinutes, out var m) ? m : 0);
        }

        private bool TryParsePort(out ushort port)
        {
            if (ushort.TryParse(_port, out port) && port > 0) return true;
            port = Cm.Config.network.defaultPort;
            _port = port.ToString();
            return true; // Port invalide : on revient au port par défaut.
        }
    }
}
