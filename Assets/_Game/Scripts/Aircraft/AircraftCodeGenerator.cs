using System;
using System.Collections.Generic;
using NuclearProtection.Config;

namespace NuclearProtection.Aircraft
{
    /// <summary>Génère des codes d'identification uniques (au moins 1 lettre et 1 chiffre).</summary>
    public static class AircraftCodeGenerator
    {
        public static List<string> GenerateUnique(int count, AircraftSettings s, Random rng)
        {
            var result = new List<string>(count);
            var used = new HashSet<string>();
            int guard = count * 100; // sécurité si l'alphabet est trop petit
            while (result.Count < count && guard-- > 0)
            {
                string code = One(s, rng);
                if (used.Add(code)) result.Add(code);
            }
            return result;
        }

        private static string One(AircraftSettings s, Random rng)
        {
            var chars = new char[s.idCodeLength];
            // On garantit une lettre et un chiffre à deux positions distinctes, le reste est libre.
            int letterPos = rng.Next(chars.Length);
            int digitPos = (letterPos + 1 + rng.Next(chars.Length - 1)) % chars.Length;
            string all = s.idLetters + s.idDigits;

            for (int i = 0; i < chars.Length; i++)
            {
                if (i == letterPos) chars[i] = s.idLetters[rng.Next(s.idLetters.Length)];
                else if (i == digitPos) chars[i] = s.idDigits[rng.Next(s.idDigits.Length)];
                else chars[i] = all[rng.Next(all.Length)];
            }
            return new string(chars);
        }

        /// <summary>Vérifie le format (utile pour les tests).</summary>
        public static bool IsValid(string code, AircraftSettings s)
        {
            if (code == null || code.Length != s.idCodeLength) return false;
            bool hasLetter = false, hasDigit = false;
            foreach (char c in code)
            {
                hasLetter |= s.idLetters.IndexOf(c) >= 0;
                hasDigit |= s.idDigits.IndexOf(c) >= 0;
                if (s.idLetters.IndexOf(c) < 0 && s.idDigits.IndexOf(c) < 0) return false;
            }
            return hasLetter && hasDigit;
        }
    }
}
