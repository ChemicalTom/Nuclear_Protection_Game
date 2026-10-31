using NuclearProtection.Core;
using NuclearProtection.Network;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Client : ouvre la ligne vocale quand le joueur local tient un combiné en communication
    /// avec l'autre bunker, et la ferme sinon. Seul celui qui tient le combiné parle et entend.
    /// </summary>
    public class PhoneVoiceDriver : MonoBehaviour
    {
        public static PhoneVoiceDriver Instance { get; private set; }

        /// <summary>Implémentation utilisée (choisie dans la config : Vivox ou factice).</summary>
        public IPhoneVoice Voice { get; private set; }

        /// <summary>Message à afficher sur l'écran du téléphone (connexion en cours, voix indisponible…).</summary>
        public string Status => Voice is VivoxPhoneVoice vivox ? vivox.Status : null;

        private void Awake()
        {
            Instance = this;
            var phone = ConnectionManager.Instance.Config.phone;
            Voice = phone.voiceBackend == PhoneVoiceBackend.Vivox ? new VivoxPhoneVoice(LocalPlayerName()) : new FakePhoneVoice();
        }

        // Canal propre à la partie : préfixe + identifiant unique tiré par l'hôte.
        private static string LineId() =>
            $"{ConnectionManager.Instance.Config.phone.voiceChannelPrefix}_{MatchSession.Instance.SessionId.Value}";

        private static string LocalPlayerName()
        {
            var session = MatchSession.Instance;
            if (session == null) return "Joueur";
            ulong me = NetworkManager.Singleton.LocalClientId;
            foreach (var p in session.Players)
                if (p.ClientId == me) return p.Name.ToString();
            return "Joueur";
        }

        private void OnDestroy()
        {
            Voice.Disconnect();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool shouldBeOpen = LocalIsInCall();
            if (shouldBeOpen && !Voice.IsLineOpen) Voice.Connect(LineId());
            else if (!shouldBeOpen && Voice.IsLineOpen) Voice.Disconnect();
        }

        public void ToggleMute()
        {
            if (Voice.IsMuted) Voice.Unmute();
            else Voice.Mute();
        }

        private static bool LocalIsInCall()
        {
            var phones = PhoneSystem.Instance;
            if (phones == null || !phones.IsSpawned) return false;
            ulong me = NetworkManager.Singleton.LocalClientId;
            foreach (var team in new[] { TeamId.Radar, TeamId.Tir })
            {
                var line = phones.Line(team);
                if (line.Holder == me && line.Mode == PhoneMode.Communication) return true;
            }
            return false;
        }
    }
}
