using System;
using System.Collections.Generic;
using NuclearProtection.Config;

namespace NuclearProtection.Aircraft
{
    /// <summary>Codes de tir : 1 lettre puis 1 chiffre (ex. « K7 »).</summary>
    public static class FireCodeGenerator
    {
        /// <summary>Tire un code qui n'est pas déjà porté par un contact en vol.</summary>
        public static string NextFree(FireSettings s, Random rng, ICollection<string> inUse)
        {
            int combos = s.fireLetters.Length * s.fireDigits.Length;
            for (int guard = 0; guard < combos * 4; guard++)
            {
                string code = $"{s.fireLetters[rng.Next(s.fireLetters.Length)]}{s.fireDigits[rng.Next(s.fireDigits.Length)]}";
                if (!inUse.Contains(code)) return code;
            }
            return null; // alphabet trop petit pour le nombre de contacts en vol
        }

        /// <summary>Le texte saisi a-t-il le bon format ? (une erreur de format n'est pas pénalisée)</summary>
        public static bool IsWellFormed(string code, FireSettings s) =>
            code != null && code.Length == 2
            && s.fireLetters.IndexOf(code[0]) >= 0 && s.fireDigits.IndexOf(code[1]) >= 0;
    }
}
