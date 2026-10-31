namespace NuclearProtection.Phone
{
    /// <summary>
    /// Voix du téléphone entre les deux bunkers. Le service vocal réel (Vivox, Discord…)
    /// n'est pas encore choisi : seule l'implémentation factice existe.
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
