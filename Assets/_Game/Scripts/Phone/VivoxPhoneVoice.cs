namespace NuclearProtection.Phone
{
    /// <summary>
    /// Ligne téléphonique en vraie voix (Unity Vivox) : un canal rejoint seulement par ceux qui tiennent
    /// un combiné en communication. La connexion et les erreurs sont gérées par VivoxSession.
    /// </summary>
    public class VivoxPhoneVoice : IPhoneVoice
    {
        private readonly string _displayName;
        private string _line;

        /// <summary>Texte court pour l'écran du téléphone (null = tout va bien).</summary>
        public string Status => VivoxSession.Status;

        public bool IsLineOpen => _line != null;
        public bool IsMuted { get; private set; }

        public VivoxPhoneVoice(string displayName) => _displayName = displayName;

        public void Connect(string lineId)
        {
            _line = lineId;
            Unmute();
            VivoxSession.Want(lineId, true, _displayName);
        }

        public void Disconnect()
        {
            if (_line == null) return;
            VivoxSession.Want(_line, false, _displayName);
            _line = null;
            Unmute(); // le micro reste ouvert pour le canal du bunker
        }

        public void Mute()
        {
            IsMuted = true;
            VivoxSession.SetMicMuted(true);
        }

        public void Unmute()
        {
            IsMuted = false;
            VivoxSession.SetMicMuted(false);
        }
    }
}
