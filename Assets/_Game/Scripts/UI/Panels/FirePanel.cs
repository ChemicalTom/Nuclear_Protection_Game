using NuclearProtection.Core;
using NuclearProtection.Fire;
using NuclearProtection.Rooms;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>
    /// Pupitre de tir : saisie du code de tir (dicté par le Radar), état des 2 clés,
    /// compte à rebours du blocage et dernier message du serveur.
    /// </summary>
    public class FirePanel : IStationPanel
    {
        public StationType Type => StationType.PupitreTir;
        public string Title => "PUPITRE DE TIR";

        private const string InputName = "FireCodeInput";
        private string _input = "";
        private bool _focusPending;

        public void OnOpen(Station station)
        {
            _input = "";
            _focusPending = true;
        }

        public void OnClose() { }

        public void Draw(Rect area)
        {
            var fire = FireControl.Instance;
            if (fire == null) return;
            var big = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 };
            double now = NetworkManager.Singleton.ServerTime.Time;

            GUILayout.BeginArea(area);

            float locked = (float)(fire.LockedUntil.Value - now);
            if (locked > 0f)
                GUILayout.Label($"<color=#ff5050><b>PUPITRE BLOQUÉ : {Mathf.CeilToInt(locked)} s</b></color>", big);
            else
                DrawCodeInput(fire);

            GUILayout.Space(12);
            GUILayout.Label(fire.IsArmed ? $"Code armé : <b>{fire.ArmedCode.Value}</b>" : "Aucun code armé.", big);
            GUILayout.Label($"Clé 1 : {KeyText(fire.Key1Turned.Value)}     Clé 2 : {KeyText(fire.Key2Turned.Value)}", big);

            GUILayout.Space(12);
            GUILayout.Label(fire.Status.Value.ToString(), new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true });
            GUILayout.EndArea();
        }

        private void DrawCodeInput(FireControl fire)
        {
            // Entrée = valider (à lire avant le champ texte, qui consomme l'événement).
            var e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Code de tir :", GUILayout.Width(100));
            GUI.SetNextControlName(InputName);
            _input = GUILayout.TextField(_input, 2, GUILayout.Width(60)).ToUpperInvariant();
            bool clicked = GUILayout.Button("Valider", GUILayout.Width(90));
            GUILayout.EndHorizontal();

            if (_focusPending) { GUI.FocusControl(InputName); _focusPending = false; }

            if ((clicked || enter) && _input.Length > 0)
            {
                fire.SubmitCodeRpc(new FixedString32Bytes(_input));
                _input = "";
            }
        }

        private static string KeyText(bool turned) => turned ? "<color=#ffcc20>TOURNÉE</color>" : "en attente";
    }
}
