namespace NuclearProtection.Phone
{
    /// <summary>
    /// Voix du téléphone entre les deux bunkers : VivoxPhoneVoice (vraie voix) ou FakePhoneVoice
    /// (aucun son, tests hors ligne), au choix dans PhoneSettings.voiceBackend.
    /// </summary>
    public interface IPhoneVoice
    {
        /// <summary>Rejoint la ligne vocale (micro ouvert).</summary>
        void Connect(string lineId);

        /// <summary>Quitte la ligne (raccroché, ligne coupée).</summary>
        void Disconnect();

        void Mute();
        void Unmute();

        bool IsLineOpen { get; }
        bool IsMuted { get; }
    }
}
