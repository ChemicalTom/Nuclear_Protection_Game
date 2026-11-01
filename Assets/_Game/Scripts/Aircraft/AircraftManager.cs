using System;
using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Aircraft
{
    /// <summary>
    /// Serveur : génère le registre, fait apparaître avions et missiles, détecte leur arrivée sur la base.
    /// Clients : lisent Contacts (écran radar) et Registry (poste Registre).
    /// Le camp de chaque contact reste côté serveur (_sides).
    /// </summary>
    public class AircraftManager : NetworkBehaviour
    {
        public static AircraftManager Instance { get; private set; }

        /// <summary>Serveur : un contact a atteint la base (isEnemy = un ennemi est passé).</summary>
        public static event Action<RadarContact, bool> ServerContactReachedBase;

        [SerializeField] private GameConfig config;
        [SerializeField] private GameClock clock;

        public NetworkList<RadarContact> Contacts { get; private set; }
        public NetworkList<RegistryEntry> Registry { get; private set; }

        // ---- État serveur uniquement ----
        private System.Random _rng;
        private readonly List<RegistryEntry> _unflown = new();          // codes pas encore apparus
        private readonly Dictionary<int, bool> _sides = new();           // id contact -> ennemi ?
        private readonly Dictionary<int, double> _missileFireTimes = new(); // id avion -> heure du tir
        private double _nextSpawnTime = -1d;
        private int _nextId;
        private int _sameSideStreak;
        private bool _lastWasEnemy;

        private AircraftSettings S => config.aircraft;
        private static DifficultyValues D => MatchSession.Instance.CurrentDifficulty;

        private void Awake()
        {
            Contacts = new NetworkList<RadarContact>();
            Registry = new NetworkList<RegistryEntry>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer) BuildRegistry();
            else if (Debug.isDebugBuild) LogClientSync();
        }

        // Builds de développement : trace côté client que le registre et les échos arrivent bien.
        private void LogClientSync()
        {
            Debug.Log($"[Avions] Registre reçu : {Registry.Count} codes.");
            Contacts.OnListChanged += e =>
            {
                if (e.Type == NetworkListEvent<RadarContact>.EventType.Add)
                    Debug.Log($"[Avions] Nouvel écho {e.Value.Kind} {e.Value.Code} (cap {e.Value.AngleDeg:0}°).");
            };
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
        }

        // Registre trié : un code par avion prévu + des leurres, avec la bonne proportion d'amis.
        private void BuildRegistry()
        {
            _rng = SeededRandom.Create(MatchSession.Instance.Seed.Value, SeededRandom.SaltRegistry);
            float durationSec = MatchSession.Instance.DurationMinutes.Value * 60f;
            int planned = Mathf.CeilToInt(durationSec / D.spawnIntervalMin) + 1;
            var codes = AircraftCodeGenerator.GenerateUnique(planned + S.registryExtraCodes, S, _rng);

            int friendlies = Mathf.RoundToInt(codes.Count * D.friendlyRatio);
            var entries = new List<RegistryEntry>();
            for (int i = 0; i < codes.Count; i++)
                entries.Add(new RegistryEntry { Code = new FixedString32Bytes(codes[i]), IsEnemy = i >= friendlies });

            _unflown.AddRange(entries); // l'ordre de génération est déjà aléatoire
            entries.Sort((a, b) => string.CompareOrdinal(a.Code.ToString(), b.Code.ToString()));
            foreach (var e in entries) Registry.Add(e);

            // Le rythme des avions a son propre générateur (indépendant du registre).
            _rng = SeededRandom.Create(MatchSession.Instance.Seed.Value, SeededRandom.SaltAircraft);
        }

        private void Update()
        {
            if (!IsServer || clock == null || !clock.IsActive) return;
            double now = NetworkManager.ServerTime.Time;

            if (_nextSpawnTime < 0d) _nextSpawnTime = now + S.firstAircraftDelay;
            if (now >= _nextSpawnTime && _unflown.Count > 0)
            {
                // Directeur : si aucun avion acceptable maintenant, on retente un peu plus tard.
                _nextSpawnTime = SpawnAircraft(now)
                    ? now + _rng.Range(D.spawnIntervalMin, D.spawnIntervalMax)
                    : now + config.director.spawnRetryDelay;
            }

            FireDueMissiles(now);
            ProcessArrivals(now);
        }

        private bool SpawnAircraft(double now)
        {
            int pick = PickNextAircraft();
            if (pick < 0) return false;
            var entry = _unflown[pick];
            _unflown.RemoveAt(pick);
            _sameSideStreak = entry.IsEnemy == _lastWasEnemy ? _sameSideStreak + 1 : 1;
            _lastWasEnemy = entry.IsEnemy;

            float angle = _rng.Range(S.approachAngleMin, S.approachAngleMax);
            Vector2 end = Vector2.zero;
            float flight = D.timeToReachBase;
            if (_rng.NextDouble() < D.sidePassChance) end = SidePassExit(angle, ref flight);

            var c = NewContact(ContactKind.Avion, entry.Code, angle, 1f, now, flight, end);
            _sides[c.Id] = entry.IsEnemy;

            // Un ennemi peut tirer, même s'il passe sur le côté.
            if (entry.IsEnemy && _rng.NextDouble() < D.enemyMissileChance)
                _missileFireTimes[c.Id] = now + flight * _rng.Range(S.missileFireAtMin, S.missileFireAtMax);
            return true;
        }

        /// <summary>
        /// Trajectoire qui frôle la base : corde du cercle radar passant à une distance tirée au hasard du centre.
        /// Renvoie le point de sortie ; la durée du vol est allongée selon la longueur de la corde (vitesse constante).
        /// </summary>
        private Vector2 SidePassExit(float entryAngle, ref float flight)
        {
            float miss = _rng.Range(S.sidePassMissMin, S.sidePassMissMax);
            float side = _rng.Next(2) == 0 ? -1f : 1f;
            // Le point le plus proche de la base est décalé de acos(miss) par rapport à l'entrée ; la sortie lui est symétrique.
            Vector2 closest = RadarContact.Direction(entryAngle + side * Mathf.Acos(miss) * Mathf.Rad2Deg) * miss;
            flight *= 2f * Mathf.Sqrt(1f - miss * miss);
            return 2f * closest - RadarContact.Direction(entryAngle);
        }

        /// <summary>
        /// Directeur : tirage au hasard, sauf si cela crée une situation injuste
        /// (trop d'ennemis en même temps sur le radar, ou trop d'avions du même camp d'affilée).
        /// Renvoie -1 s'il faut attendre (trop d'ennemis et plus aucun ami disponible).
        /// </summary>
        private int PickNextAircraft()
        {
            var d = config.director;
            int pick = _rng.Next(_unflown.Count);

            bool? forceEnemy = null;
            if (EnemiesInFlight() >= D.maxSimultaneousEnemies) forceEnemy = false;
            else if (_sameSideStreak >= d.maxConsecutiveSameSide) forceEnemy = !_lastWasEnemy;
            if (forceEnemy == null || _unflown[pick].IsEnemy == forceEnemy) return pick;

            // Premier code du camp voulu, en partant du tirage (reste déterministe pour une même seed).
            for (int k = 1; k < _unflown.Count; k++)
            {
                int i = (pick + k) % _unflown.Count;
                if (_unflown[i].IsEnemy == forceEnemy) return i;
            }
            return forceEnemy == false && EnemiesInFlight() >= D.maxSimultaneousEnemies ? -1 : pick;
        }

        private int EnemiesInFlight()
        {
            int n = 0;
            foreach (bool enemy in _sides.Values) if (enemy) n++;
            return n;
        }

        private void FireDueMissiles(double now)
        {
            for (int i = 0; i < Contacts.Count; i++)
            {
                var plane = Contacts[i];
                if (!_missileFireTimes.TryGetValue(plane.Id, out double t) || now < t) continue;
                _missileFireTimes.Remove(plane.Id);

                // Le missile part de la position actuelle de l'avion, droit sur la base.
                Vector2 p = plane.ScreenPosAt(now);
                float d = p.magnitude, bearing = Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg;
                var m = NewContact(ContactKind.Missile, default, (bearing + 360f) % 360f, d, now, D.enemyMissileFlightTime * d, Vector2.zero);
                _sides[m.Id] = true;
            }
        }

        private void ProcessArrivals(double now)
        {
            for (int i = Contacts.Count - 1; i >= 0; i--)
            {
                var c = Contacts[i];
                if (c.EtaAt(now) > 0f) continue;
                bool enemy = _sides[c.Id];
                RemoveAt(i);
                if (c.PassesBy)
                {
                    // Passé sur le côté : il sort du radar sans rien toucher.
                    SpoilerLog.Log($"[Avions] {c.Code} sorti du radar sur le côté ({(enemy ? "ennemi" : "ami")}).");
                    continue;
                }
                SpoilerLog.Log($"[Avions] {c.Kind} {c.Code} arrivé sur la base ({(enemy ? "ENNEMI PASSÉ" : "ami")}).");
                ServerContactReachedBase?.Invoke(c, enemy);
            }
        }

        private RadarContact NewContact(ContactKind kind, FixedString32Bytes code, float angle, float startDist, double now, float flight, Vector2 end)
        {
            // Code de tir unique parmi les contacts en vol (le pupitre vise avec ce seul code).
            var inUse = new HashSet<string>();
            foreach (var other in Contacts) inUse.Add(other.FireCode.ToString());
            string fireCode = FireCodeGenerator.NextFree(config.fire, _rng, inUse) ?? "";

            var c = new RadarContact
            {
                Id = _nextId++, Kind = kind, Code = code, FireCode = new FixedString32Bytes(fireCode), AngleDeg = angle % 360f,
                StartDistance = startDist, EndPos = end, StartTime = now, FlightTime = Mathf.Max(0.1f, flight)
            };
            Contacts.Add(c);
            return c;
        }

        private void RemoveAt(int index)
        {
            int id = Contacts[index].Id;
            Contacts.RemoveAt(index);
            _sides.Remove(id);
            _missileFireTimes.Remove(id);
        }

        /// <summary>Serveur (étape 5) : retire un contact abattu ; renvoie son camp.</summary>
        public bool ServerTryDestroy(int contactId, out RadarContact contact, out bool wasEnemy)
        {
            for (int i = 0; i < Contacts.Count; i++)
            {
                if (Contacts[i].Id != contactId) continue;
                contact = Contacts[i];
                wasEnemy = _sides[contactId];
                RemoveAt(i);
                return true;
            }
            contact = default;
            wasEnemy = false;
            return false;
        }

        /// <summary>Serveur : contact en vol portant ce code de tir.</summary>
        public bool TryFindByFireCode(string fireCode, out RadarContact contact)
        {
            foreach (var c in Contacts)
                if (c.FireCode.ToString() == fireCode) { contact = c; return true; }
            contact = default;
            return false;
        }

        /// <summary>Clients : recherche d'un contact par id.</summary>
        public bool TryGetContact(int id, out RadarContact contact)
        {
            foreach (var c in Contacts)
                if (c.Id == id) { contact = c; return true; }
            contact = default;
            return false;
        }
    }
}
