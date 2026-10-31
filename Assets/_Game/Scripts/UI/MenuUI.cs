using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Menu principal graybox (IMGUI) : Héberger (port, durée, seed optionnelle) ou Rejoindre (IP + port).
    /// Affiché uniquement quand on n'est pas connecté.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {

        private string _name, _port, _ip = "127.0.0.1", _customMinutes = "10", _seed = "";
        private int _durationChoice; // index dans les préréglages ; = longueur => personnalisé

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

            GUI.matrix = Matrix4x4.Scale(Vector3.one * DisplayControl.UiScale);
            GUILayout.BeginArea(new Rect(20, 20, 420, 660), GUI.skin.box);
            GUILayout.Label("<b>NUCLEAR PROTECTION</b>", RichLabel());

            GUILayout.Label("Ton nom :");
            _name = GUILayout.TextField(_name, 20);
            GUILayout.Label("Port UDP :");
            _port = GUILayout.TextField(_port, 5);

            GUILayout.Space(10);
            DrawHostSection();
            GUILayout.Space(10);
            DrawJoinSection();

            if (!string.IsNullOrEmpty(Cm.LastError))
                GUILayout.Label($"<color=#ff6060>{Cm.LastError}</color>", RichLabel());

            GUILayout.Space(10);
            if (GUILayout.Button(DisplayControl.IsFullscreen ? "Passer en fenêtre (F11)" : "Plein écran (F11)"))
                DisplayControl.ToggleFullscreen();
            GUILayout.EndArea();
        }

        private void DrawHostSection()
        {
            GUILayout.Label("<b>Héberger</b>", RichLabel());
            var presets = Cm.Config.duration.presetMinutes;
            var labels = new string[presets.Length + 1];
            for (int i = 0; i < presets.Length; i++) labels[i] = $"{presets[i]} min";
            labels[presets.Length] = "Perso";
            _durationChoice = GUILayout.Toolbar(_durationChoice, labels);

            if (_durationChoice == presets.Length)
            {
                var d = Cm.Config.duration;
                GUILayout.Label($"Durée perso ({d.customMinMinutes}-{d.customMaxMinutes} min) :");
                _customMinutes = GUILayout.TextField(_customMinutes, 3);
            }

            GUILayout.Label("Seed (vide = aléatoire) :");
            _seed = GUILayout.TextField(_seed, 11);

            if (GUILayout.Button("Héberger") && TryParsePort(out var port))
                Cm.Host(_name, port, SelectedMinutes(), int.TryParse(_seed, out var s) ? s : null);
        }

        private void DrawJoinSection()
        {
            GUILayout.Label("<b>Rejoindre</b>", RichLabel());
            GUILayout.Label("IP de l'hôte :");
            _ip = GUILayout.TextField(_ip, 15);
            if (GUILayout.Button("Rejoindre") && TryParsePort(out var port))
                Cm.Join(_name, _ip, port);
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

        private static GUIStyle RichLabel() => new(GUI.skin.label) { richText = true };
    }
}
