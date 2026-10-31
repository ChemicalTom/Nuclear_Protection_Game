using System;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Fuse;
using NuclearProtection.Network;
using NuclearProtection.Phone;
using NuclearProtection.Rooms.Visuals;
using UnityEngine;

namespace NuclearProtection.Rooms
{
    /// <summary>
    /// Construit localement (sur chaque machine) les bunkers graybox et leurs postes
    /// à partir du BunkerPlan. Aucune géométrie n'est synchronisée : elle est identique partout.
    /// </summary>
    public class BunkerBuilder : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Matériau de base (URP Lit) ; la couleur est appliquée par objet.")]
        [SerializeField] private Material baseMaterial;

        public static BunkerPlan CurrentPlan { get; private set; }
        public static event Action<BunkerPlan> Built;

        private void Start()
        {
            // Hors réseau (test direct de la scène), on prend la disposition 4 joueurs.
            var layout = MatchSession.Instance != null ? MatchSession.Instance.Layout.Value : config.network.fourPlayerLayout;
            CurrentPlan = new BunkerPlan(config, layout);
            MaterialKit.Init(baseMaterial, config.visuals);
            PostFxSetup.Create(transform, config.visuals);

            foreach (var b in CurrentPlan.Bunkers) BuildShell(b, CurrentPlan.Bunkers.Count == 1);
            foreach (var s in CurrentPlan.Stations) BuildStation(s);

            // Lumière, bourdonnement et panne : par équipe (dans le bunker commun, chaque moitié a les siens).
            foreach (var team in new[] { TeamId.Radar, TeamId.Tir })
                TeamAmbience.Create(transform, team, CurrentPlan.TeamOrigin(team), CurrentPlan.TeamSpaceSize(team), config);
            BunkerVoice.Create(transform, CurrentPlan, config);
            Built?.Invoke(CurrentPlan);
        }

        private void OnDestroy()
        {
            StationRegistry.Clear();
            CurrentPlan = null;
        }

        // Sol, plafond, 4 murs, décor et horloge murale d'un bunker.
        private void BuildShell(BunkerInfo b, bool shared)
        {
            var r = config.rooms;
            var root = new GameObject($"Bunker_{b.Index}").transform;
            root.SetParent(transform, false);
            root.position = b.Center;

            float h = r.wallHeight, t = r.wallThickness, w = b.Size.x, d = b.Size.y;
            float tile = config.visuals.floorTileSize;
            Box(root, "Sol", new Vector3(0, -t / 2, 0), new Vector3(w + 2 * t, t, d + 2 * t), MaterialKit.Tiled(Mat.Floor, new Vector2(w, d) / tile));
            Box(root, "Plafond", new Vector3(0, h + t / 2, 0), new Vector3(w + 2 * t, t, d + 2 * t), MaterialKit.Tiled(Mat.Ceiling, new Vector2(w, d) / 3f));
            Box(root, "Mur_Nord", new Vector3(0, h / 2, d / 2 + t / 2), new Vector3(w + 2 * t, h, t), MaterialKit.Tiled(Mat.Concrete, new Vector2(w, h) / 3f));
            Box(root, "Mur_Sud", new Vector3(0, h / 2, -d / 2 - t / 2), new Vector3(w + 2 * t, h, t), MaterialKit.Tiled(Mat.Concrete, new Vector2(w, h) / 3f));
            Box(root, "Mur_Est", new Vector3(w / 2 + t / 2, h / 2, 0), new Vector3(t, h, d), MaterialKit.Tiled(Mat.Concrete, new Vector2(d, h) / 3f));
            Box(root, "Mur_Ouest", new Vector3(-w / 2 - t / 2, h / 2, 0), new Vector3(t, h, d), MaterialKit.Tiled(Mat.Concrete, new Vector2(d, h) / 3f));
            if (config.visuals.buildDecor) RoomDecor.Build(root, b, r, config.visuals, shared);

            // Horloge murale sur le mur nord, lisible depuis l'intérieur.
            var clock = TextLabel.Create(root, "HorlogeMurale", "00:00", 0.12f);
            clock.color = config.ui.accent;
            clock.transform.localPosition = new Vector3(0, h - 0.6f, d / 2 - 0.02f);
            clock.gameObject.AddComponent<WallClockDisplay>();
        }

        private void BuildStation(StationInfo info)
        {
            config.rooms.GetLook(info.Type, out var size, out var color);
            // Racine au sol, non mise à l'échelle : la boîte de collision = zone d'interaction, le modèle est dessous.
            var go = new GameObject($"Poste_{info.Id}_{info.Type}_{info.Team}");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(info.Position, Quaternion.Euler(0, info.YRotation, 0));
            var box = go.AddComponent<BoxCollider>();
            box.center = Vector3.up * size.y / 2;
            box.size = size;

            var indicator = StationModels.Build(go.transform, info.Type, size);
            bool screen = info.Type is StationType.Radar or StationType.PupitreTir;
            var station = go.AddComponent<Station>();
            station.Init(info, color, indicator, screen ? config.visuals.screenGlow : 0f);
            StationRegistry.Register(station);

            var label = TextLabel.Create(transform, $"Etiquette_{info.Id}", config.ui.LabelFor(info.Type), config.ui.worldLabelSize);
            label.color = config.ui.worldLabelColor;
            label.transform.position = info.Position + Vector3.up * (size.y + 0.25f);
            label.gameObject.AddComponent<LabelBillboard>();
        }

        // Mur, sol ou plafond (avec collision, contrairement au décor).
        private static void Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
