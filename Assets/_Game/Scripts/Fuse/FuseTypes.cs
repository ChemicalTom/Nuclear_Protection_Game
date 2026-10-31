using System;
using Unity.Netcode;

namespace NuclearProtection.Fuse
{
    /// <summary>Mini-jeu à réussir pour remettre le fusible.</summary>
    public enum FuseMiniGame : byte
    {
        Aiguille = 0, // appuyer quand l'aiguille est dans la zone verte
        Fils = 1      // brancher chaque fil sur la prise de sa couleur
    }

    /// <summary>Pourquoi le fusible a sauté (journal serveur et résumé de fin).</summary>
    public enum BlowCause : byte
    {
        Surcharge = 0,
        SousCharge = 1,
        Test = 2
    }

    /// <summary>État synchronisé du fusible d'une équipe.</summary>
    public struct FuseState : INetworkSerializable, IEquatable<FuseState>
    {
        public bool Blown;
        public double Since;        // temps serveur du début de la panne
        public FuseMiniGame Game;
        public int Seed;            // détermine la zone verte / l'ordre des prises
        public int Progress;        // aiguille : réussites d'affilée ; fils : masque des fils branchés

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Blown);
            s.SerializeValue(ref Since);
            s.SerializeValue(ref Game);
            s.SerializeValue(ref Seed);
            s.SerializeValue(ref Progress);
        }

        public bool Equals(FuseState o) => Blown == o.Blown && Since.Equals(o.Since) && Game == o.Game && Seed == o.Seed && Progress == o.Progress;
    }

    /// <summary>Une panne terminée ou en cours (pour le résumé de fin, étape 11).</summary>
    public class BlackoutRecord
    {
        public Core.TeamId Team;
        public BlowCause Cause;
        public float StartTime;      // secondes de partie
        public float? RepairTime;    // null = jamais réparée
        public float? Duration => RepairTime - StartTime;
    }
}
