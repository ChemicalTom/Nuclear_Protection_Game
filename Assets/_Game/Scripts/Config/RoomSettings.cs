using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Emplacement d'un poste dans l'espace d'une équipe (repère local du bunker).</summary>
    [Serializable]
    public struct StationPlacement
    {
        public StationType type;
        public Vector3 localPosition;
        [Tooltip("Rotation autour de Y (degrés). 0 = face vers +Z.")]
        public float yRotation;
    }

    /// <summary>Apparence graybox d'un type de poste.</summary>
    [Serializable]
    public struct StationLook
    {
        public StationType type;
        public Vector3 size;
        public Color color;
    }

    /// <summary>
    /// Bunkers graybox : dimensions et disposition des postes.
    /// Les 2 serrures nucléaires sont placées automatiquement à KeySettings.nuclearLockDistance l'une de l'autre.
    /// </summary>
    [Serializable]
    public class RoomSettings
    {
        [Header("Dimensions (m)")]
        [Tooltip("Bunker d'une seule équipe (mode 4 joueurs).")]
        public Vector2 singleTeamBunkerSize = new(10f, 8f);
        [Tooltip("Bunker commun aux 2 équipes (mode 2 joueurs).")]
        public Vector2 sharedBunkerSize = new(18f, 10f);
        [Min(2f)] public float wallHeight = 3f;
        [Min(0.05f)] public float wallThickness = 0.3f;

        [Tooltip("Distance entre les 2 bunkers (assez grande pour qu'aucun son ni lumière ne passe).")]
        [Min(20f)] public float distanceBetweenBunkers = 200f;

        [Header("Postes de l'équipe Radar (repère local de son espace)")]
        public StationPlacement[] radarStations =
        {
            new() { type = StationType.Radar,     localPosition = new Vector3(0f, 0f, 3f),    yRotation = 0f },
            new() { type = StationType.Registre,  localPosition = new Vector3(2.5f, 0f, 3f),  yRotation = 0f },
            new() { type = StationType.Telephone, localPosition = new Vector3(-3.5f, 0f, 1f), yRotation = -90f },
            new() { type = StationType.Fusible,   localPosition = new Vector3(3.8f, 0f, -3.7f), yRotation = 180f }, // mur opposé au radar
            new() { type = StationType.Mallette,  localPosition = new Vector3(-2f, 0f, -1f),  yRotation = 180f },
        };

        [Header("Postes de l'équipe Tir (repère local de son espace)")]
        public StationPlacement[] tirStations =
        {
            new() { type = StationType.PupitreTir,   localPosition = new Vector3(0f, 0f, 3f),    yRotation = 0f },
            new() { type = StationType.CleConv1,     localPosition = new Vector3(-1.5f, 0f, 2f), yRotation = 0f },
            new() { type = StationType.CleConv2,     localPosition = new Vector3(1.5f, 0f, 2f),  yRotation = 0f },
            new() { type = StationType.Telephone,    localPosition = new Vector3(-3.5f, 0f, 1f), yRotation = -90f },
            new() { type = StationType.Fusible,      localPosition = new Vector3(3.8f, 0f, -3.7f), yRotation = 180f }, // mur opposé au pupitre
            new() { type = StationType.Mallette,     localPosition = new Vector3(-2f, 0f, -1f),  yRotation = 180f },
        };

        [Header("Serrures nucléaires (communes aux 2 équipes)")]
        [Tooltip("Centre du segment entre les 2 serrures, dans l'espace de l'équipe.")]
        public Vector3 nuclearLocksCenter = new(0f, 0f, -3.2f);

        [Header("Apparence graybox des postes")]
        public Vector3 defaultStationSize = new(0.6f, 1.1f, 0.4f);
        public Color defaultStationColor = new(0.6f, 0.6f, 0.6f);
        public StationLook[] stationLooks =
        {
            new() { type = StationType.Radar,       size = new Vector3(1.6f, 1.0f, 0.8f), color = new Color(0.2f, 0.7f, 0.3f) },
            new() { type = StationType.Registre,    size = new Vector3(0.8f, 0.9f, 0.5f), color = new Color(0.8f, 0.75f, 0.5f) },
            new() { type = StationType.PupitreTir,  size = new Vector3(1.6f, 1.0f, 0.8f), color = new Color(0.75f, 0.3f, 0.2f) },
            new() { type = StationType.CleConv1,    size = new Vector3(0.4f, 1.1f, 0.4f), color = new Color(0.9f, 0.7f, 0.1f) },
            new() { type = StationType.CleConv2,    size = new Vector3(0.4f, 1.1f, 0.4f), color = new Color(0.9f, 0.7f, 0.1f) },
            new() { type = StationType.Telephone,   size = new Vector3(0.4f, 1.2f, 0.4f), color = new Color(0.15f, 0.15f, 0.15f) },
            new() { type = StationType.Fusible,     size = new Vector3(0.6f, 1.6f, 0.3f), color = new Color(0.4f, 0.45f, 0.6f) },
            new() { type = StationType.Mallette,    size = new Vector3(0.7f, 0.9f, 0.5f), color = new Color(0.35f, 0.25f, 0.15f) },
            new() { type = StationType.SerrureNucA, size = new Vector3(0.4f, 1.2f, 0.4f), color = new Color(0.9f, 0.1f, 0.1f) },
            new() { type = StationType.SerrureNucB, size = new Vector3(0.4f, 1.2f, 0.4f), color = new Color(0.9f, 0.1f, 0.1f) },
        };

        [Header("Couleurs des murs")]
        public Color floorColor = new(0.25f, 0.25f, 0.27f);
        public Color wallColor = new(0.45f, 0.47f, 0.45f);
        public Color ceilingColor = new(0.3f, 0.3f, 0.3f);

        [Header("Éclairage normal")]
        public Color roomLightColor = new(1f, 0.95f, 0.85f);
        [Min(0f)] public float roomLightIntensity = 3f;

        [Header("Apparition des joueurs")]
        public Vector3 spawnLocalPosition = new(0f, 0f, 0f);
        [Tooltip("Écart latéral entre deux joueurs qui apparaissent dans le même espace.")]
        public float spawnSpacing = 1.2f;

        /// <summary>Taille et couleur graybox d'un type de poste.</summary>
        public void GetLook(StationType type, out Vector3 size, out Color color)
        {
            foreach (var l in stationLooks)
                if (l.type == type) { size = l.size; color = l.color; return; }
            size = defaultStationSize;
            color = defaultStationColor;
        }
    }
}
