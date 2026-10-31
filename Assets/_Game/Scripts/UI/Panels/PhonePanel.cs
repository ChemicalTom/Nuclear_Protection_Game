using NuclearProtection.Core;
using NuclearProtection.Phone;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>
    /// Écran du téléphone, ouvert tant que le joueur tient le combiné. Le fermer (Échap, Raccrocher,
    /// ou s'éloigner) raccroche. Les messages du jeu ne s'affichent que chez celui qui a décroché.
    /// </summary>
    public class PhonePanel : IStationPanel
    {
        public StationType Type => StationType.Telephone;
        public string Title => "TÉLÉPHONE";

        private Station _station;
        private static string _message;
        private static CallKind _messageKind;
        private static float _messageUntil;

        static PhonePanel()
        {
            PhoneSystem.LocalMessageReceived += (kind, text, duration) =>
            {
                _messageKind = kind;
                _message = text;
                _messageUntil = Time.time + duration;
            };
        }

        public void OnOpen(Station station) => _station = station;

        public void OnClose()
        {
            var phones = PhoneSystem.Instance;
            if (phones != null && phones.TryGetHeldLine(NetworkManager.Singleton.LocalClientId, out var held)) phones.HangUpRpc(held);
        }

        // Ligne affichée : celle qu'on tient ; bunker commun : celle que tient le coéquipier, sinon celle du poste.
        private TeamId ShownTeam(PhoneSystem phones)
        {
            if (phones.TryGetHeldLine(NetworkManager.Singleton.LocalClientId, out var held)) return held;
            if (PhoneSystem.SharedPhone)
                foreach (var t in new[] { TeamId.Radar, TeamId.Tir })
                    if (phones.Line(t).IsOffHook) return t;
            return _station.Team;
        }

        private static bool IsMine(PhoneLine line) => line.Holder == NetworkManager.Singleton.LocalClientId;

        public void Draw(Rect area)
        {
            var phones = PhoneSystem.Instance;
            if (phones == null) return;
            var team = ShownTeam(phones);
            var line = phones.Line(team);
            var big = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18, wordWrap = true };

            GUILayout.BeginArea(area);
            if (!line.IsOffHook) GUILayout.Label("Décrochage…", big);
            else if (!IsMine(line)) GUILayout.Label("Un coéquipier tient déjà le combiné.", big);
            else DrawHolderView(phones, team, line, big);
            GUILayout.EndArea();
        }

        private void DrawHolderView(PhoneSystem phones, TeamId team, PhoneLine line, GUIStyle big)
        {
            GUILayout.Label(ModeText(line.Mode), big);

            if (line.Mode == PhoneMode.Message && _message != null && Time.time < _messageUntil)
            {
                string who = _messageKind == CallKind.Sanction ? "Commandement (sanction)" : "Commandement";
                GUILayout.Space(8);
                GUILayout.Label($"<b>{who} :</b>\n« {_message} »", big);
            }

            if (line.Waiting != CallKind.Aucun)
                GUILayout.Label("<color=#ffcc20><b>Double appel ! Raccrochez pour le prendre.</b></color>", big);

            if (phones.OtherLineRinging(team))
                GUILayout.Label("<color=#ffcc20><b>L'autre ligne sonne ! Raccrochez pour la prendre.</b></color>", big);

            if (line.Info.Length > 0) GUILayout.Label(line.Info.ToString(), big);
            if (line.Mode is PhoneMode.Tonalite or PhoneMode.Occupe && !PhoneSystem.SharedPhone)
                GUILayout.Label("Raccrochez puis redécrochez pour rappeler l'autre bunker.");

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (line.Mode == PhoneMode.Communication && PhoneVoiceDriver.Instance != null)
            {
                var voice = PhoneVoiceDriver.Instance;
                if (voice.Status != null) GUILayout.Label($"<i>{voice.Status}</i>");
                if (GUILayout.Button(voice.Voice.IsMuted ? "Rouvrir le micro" : "Couper le micro", GUILayout.Width(150), GUILayout.Height(30)))
                    voice.ToggleMute();
            }

            if (GUILayout.Button("Raccrocher", GUILayout.Width(120), GUILayout.Height(30)))
                StationPanelHost.Instance.Close();
            GUILayout.EndHorizontal();
        }

        // Volontairement neutre pour la ligne morte : l'appelant ne doit pas savoir que l'autre bunker est en panne.
        private static string ModeText(PhoneMode mode) => mode switch
        {
            PhoneMode.Tonalite => "Tonalité.",
            PhoneMode.Appel => "Ça sonne chez l'autre bunker…",
            PhoneMode.Occupe => "Occupé.",
            PhoneMode.LigneMorte => "…",
            PhoneMode.Communication => "En ligne avec l'autre bunker.",
            PhoneMode.Message => "Appel du commandement.",
            PhoneMode.HorsService => "Aucune tonalité.",
            _ => ""
        };
    }
}
