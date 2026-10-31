using System;

namespace NuclearProtection.Core
{
    /// <summary>
    /// Générateurs aléatoires dérivés de la seed de la partie : un générateur indépendant par système
    /// (avions, appels…), pour qu'une même seed rejoue la même partie.
    /// </summary>
    public static class SeededRandom
    {
        // Sels fixes : changer un sel change le tirage du système concerné.
        public const int SaltRegistry = 0x1A2B;
        public const int SaltAircraft = 0x3C4D;
        public const int SaltOrders = 0x5E6F;
        public const int SaltSanctions = 0x7A8B;
        public const int SaltFuse = 0x9CAD;
        public const int SaltBlackoutOrders = 0xBE0F;

        public static Random Create(int matchSeed, int salt) => new(unchecked(matchSeed * 486187739 + salt));

        /// <summary>Flottant uniforme dans [min, max].</summary>
        public static float Range(this Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
