using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>Voix factice : ne transmet aucun son, trace seulement les changements d'état.</summary>
    public class FakePhoneVoice : IPhoneVoice
    {
        private string _line;

        public bool IsLineOpen => _line != null;
        public bool IsMuted { get; private set; }

        public void Connect(string lineId)
        {
            _line = lineId;
            IsMuted = false;
            Debug.Log($"[Voix factice] Connecté à la ligne « {lineId} ».");
        }

        public void Disconnect()
        {
            if (_line == null) return;
            Debug.Log($"[Voix factice] Déconnecté de « {_line} ».");
            _line = null;
        }

        public void Mute()
        {
            IsMuted = true;
            Debug.Log("[Voix factice] Micro coupé.");
        }

        public void Unmute()
        {
            IsMuted = false;
            Debug.Log("[Voix factice] Micro ouvert.");
        }
    }
}
