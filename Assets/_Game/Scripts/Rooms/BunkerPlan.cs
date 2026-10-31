using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Rooms
{
    /// <summary>Un poste calculé : identifiant stable (identique sur tous les clients), type, équipe, position monde.</summary>
    public struct StationInfo
    {
        public int Id;
        public StationType Type;
        public TeamId Team;
        public int BunkerIndex;
        public Vector3 Position;
        public float YRotation;
    }

    /// <summary>Un bunker calculé : centre et taille au sol.</summary>
    public struct BunkerInfo
    {
        public int Index;
        public Vector3 Center;
        public Vector2 Size;
    }

    /// <summary>
    /// Calcul pur et déterministe de la disposition (aucun GameObject) :
    /// mêmes entrées => mêmes bunkers, postes et identifiants sur toutes les machines.
    /// </summary>
    public class BunkerPlan
    {
        public readonly List<BunkerInfo> Bunkers = new();
        public readonly List<StationInfo> Stations = new();

        // Centre de l'espace de chaque équipe (dans un bunker commun, chaque équipe a sa moitié).
        private readonly Dictionary<TeamId, Vector3> _teamOrigin = new();
        private readonly Dictionary<TeamId, int> _teamBunker = new();
        private readonly GameConfig _config;
        private readonly bool _shared;

        public BunkerPlan(GameConfig config, BunkerLayout layout)
        {
            _config = config;
            var r = config.rooms;

            _shared = layout == BunkerLayout.UnBunkerCommun;
            if (_shared)
            {
                AddBunker(0, Vector3.zero, r.sharedBunkerSize);
                float quarter = r.sharedBunkerSize.x / 4f;
                SetTeamSpace(TeamId.Radar, 0, new Vector3(-quarter, 0f, 0f));
                SetTeamSpace(TeamId.Tir, 0, new Vector3(quarter, 0f, 0f));
            }
            else
            {
                AddBunker(0, Vector3.zero, r.singleTeamBunkerSize);
                AddBunker(1, new Vector3(r.distanceBetweenBunkers, 0f, 0f), r.singleTeamBunkerSize);
                SetTeamSpace(TeamId.Radar, 0, Bunkers[0].Center);
                SetTeamSpace(TeamId.Tir, 1, Bunkers[1].Center);
            }

            AddTeamStations(TeamId.Radar, r.radarStations);
            AddTeamStations(TeamId.Tir, r.tirStations);
        }

        public int BunkerOf(TeamId team) => _teamBunker[team];

        /// <summary>Centre (au sol) de l'espace d'une équipe.</summary>
        public Vector3 TeamOrigin(TeamId team) => _teamOrigin[team];

        /// <summary>Taille au sol de l'espace d'une équipe (la moitié du bunker commun à 2 joueurs).</summary>
        public Vector2 TeamSpaceSize(TeamId team)
        {
            var size = Bunkers[BunkerOf(team)].Size;
            bool shared = Bunkers.Count == 1;
            return shared ? new Vector2(size.x / 2f, size.y) : size;
        }

        /// <summary>Point d'apparition du n-ième joueur d'une équipe.</summary>
        public Vector3 SpawnPoint(TeamId team, int indexInTeam)
        {
            var r = _config.rooms;
            float side = (indexInTeam - 0.5f) * r.spawnSpacing; // 2 joueurs : -0.6 et +0.6
            return _teamOrigin[team] + r.spawnLocalPosition + new Vector3(side, 0f, 0f);
        }

        private void AddBunker(int index, Vector3 center, Vector2 size) =>
            Bunkers.Add(new BunkerInfo { Index = index, Center = center, Size = size });

        private void SetTeamSpace(TeamId team, int bunker, Vector3 origin)
        {
            _teamOrigin[team] = origin;
            _teamBunker[team] = bunker;
        }

        private void AddTeamStations(TeamId team, StationPlacement[] placements)
        {
            foreach (var p in placements)
            {
                // Bunker commun (2 joueurs) : un seul téléphone, celui de l'espace Radar, pour les deux équipes.
                if (_shared && team == TeamId.Tir && p.type == StationType.Telephone) continue;
                AddStation(team, p.type, p.localPosition, p.yRotation);
            }

            // Serrures nucléaires : écartées exactement de la distance configurée.
            float half = _config.keys.nuclearLockDistance / 2f;
            var c = _config.rooms.nuclearLocksCenter;
            AddStation(team, StationType.SerrureNucA, c + new Vector3(-half, 0f, 0f), 180f);
            AddStation(team, StationType.SerrureNucB, c + new Vector3(half, 0f, 0f), 180f);
        }

        private void AddStation(TeamId team, StationType type, Vector3 local, float yRot)
        {
            Stations.Add(new StationInfo
            {
                Id = Stations.Count, // ordre d'ajout fixe => identifiant stable
                Type = type,
                Team = team,
                BunkerIndex = _teamBunker[team],
                Position = _teamOrigin[team] + local,
                YRotation = yRot
            });
        }
    }
}
