using NuclearProtection.Core;
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
        private const string LineId = "ligne-inter-bunkers";

        public static PhoneVoiceDriver Instance { get; private set; }

        /// <summary>Implémentation utilisée (factice tant qu'aucun service vocal n'est choisi).</summary>
        public IPhoneVoice Voice { get; } = new FakePhoneVoice();

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            Voice.Disconnect();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool shouldBeOpen = LocalIsInCall();
            if (shouldBeOpen && !Voice.IsLineOpen) Voice.Connect(LineId);
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
