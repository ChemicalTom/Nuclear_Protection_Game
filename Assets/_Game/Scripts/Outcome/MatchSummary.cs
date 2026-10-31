using NuclearProtection.Core;
using Unity.Netcode;

namespace NuclearProtection.Outcome
{
    /// <summary>Comment l'équipe a traité un ordre nucléaire.</summary>
    public enum OrderVerdict : byte
    {
        BienLance = 0,    // vrai ordre exécuté
        Rate = 1,         // vrai ordre non exécuté à temps
        BienIgnore = 2,   // faux ordre, aucun lancement
        LanceSurFaux = 3, // faux ordre, lancement quand même
        EnCours = 4       // la partie s'est terminée avant la fin de l'ordre (non noté)
    }

    /// <summary>Une ligne « ordre » du résumé.</summary>
    public struct OrderLine : INetworkSerializable
    {
        public int Id;
        public bool IsTrue;
        public float Time; // 1er appel, en secondes de partie
        public OrderVerdict Verdict;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Id);
            s.SerializeValue(ref IsTrue);
            s.SerializeValue(ref Time);
            s.SerializeValue(ref Verdict);
        }
    }

    /// <summary>
    /// Résumé de fin de partie, calculé par le serveur et envoyé à tous (compteurs cachés révélés).
    /// </summary>
    public struct MatchSummary : INetworkSerializable
    {
        public MatchResult Result;
        public DefeatReason Reason;

        // Partie
        public int Seed;
        public int DurationMinutes;
        public float PlayedSeconds;

        // Avions (+ limites pour révéler « x sur 4 »)
        public int EnemiesDowned, EnemiesPassed, FriendliesDowned;
        public int EnemiesPassedLimit, FriendliesDownedLimit;

        // Ordres
        public OrderLine[] Orders;
        public int MissedTrueOrders, MissedTrueOrdersLimit, FalseLaunches;

        // Appels
        public int SanctionCalls, MissedCalls;

        // Pannes
        public int Blackouts;
        public float AverageRepairSeconds; // -1 = aucune réparée

        // Note
        public int Score, PerfectScore;
        public float Percent;
        public Grade Grade;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Result);
            s.SerializeValue(ref Reason);
            s.SerializeValue(ref Seed);
            s.SerializeValue(ref DurationMinutes);
            s.SerializeValue(ref PlayedSeconds);
            s.SerializeValue(ref EnemiesDowned);
            s.SerializeValue(ref EnemiesPassed);
            s.SerializeValue(ref FriendliesDowned);
            s.SerializeValue(ref EnemiesPassedLimit);
            s.SerializeValue(ref FriendliesDownedLimit);
            s.SerializeValue(ref MissedTrueOrders);
            s.SerializeValue(ref MissedTrueOrdersLimit);
            s.SerializeValue(ref FalseLaunches);
            s.SerializeValue(ref SanctionCalls);
            s.SerializeValue(ref MissedCalls);
            s.SerializeValue(ref Blackouts);
            s.SerializeValue(ref AverageRepairSeconds);
            s.SerializeValue(ref Score);
            s.SerializeValue(ref PerfectScore);
            s.SerializeValue(ref Percent);
            s.SerializeValue(ref Grade);

            int count = Orders?.Length ?? 0;
            s.SerializeValue(ref count);
            if (s.IsReader) Orders = new OrderLine[count];
            for (int i = 0; i < count; i++) s.SerializeValue(ref Orders[i]);
        }
    }
}
