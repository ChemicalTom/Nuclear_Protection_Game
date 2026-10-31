using System;
using System.Collections.Generic;

namespace NuclearProtection.Rooms
{
    /// <summary>
    /// Annuaire local des postes (par Id) + événement serveur des interactions validées.
    /// Les systèmes de jeu (radar, tir, téléphone…) s'abonneront à ServerInteracted.
    /// </summary>
    public static class StationRegistry
    {
        private static readonly Dictionary<int, Station> Stations = new();

        /// <summary>Serveur : un joueur a appuyé (pressed=true) ou relâché (false) sur un poste.</summary>
        public static event Action<Station, ulong, bool> ServerInteracted;

        public static void Register(Station s) => Stations[s.Id] = s;
        public static void Clear() => Stations.Clear();
        public static bool TryGet(int id, out Station s) => Stations.TryGetValue(id, out s);
        public static IEnumerable<Station> All => Stations.Values;

        internal static void RaiseServerInteracted(Station s, ulong clientId, bool pressed) =>
            ServerInteracted?.Invoke(s, clientId, pressed);

#if UNITY_EDITOR
        /// <summary>Tests dans l'éditeur uniquement : simule l'appui d'un joueur (ex. 2e joueur) côté serveur.</summary>
        public static void EditorSimulateInteraction(Station s, ulong clientId) => RaiseServerInteracted(s, clientId, true);
#endif
    }
}
