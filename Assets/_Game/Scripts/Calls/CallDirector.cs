using System;
using System.Collections.Generic;
using NuclearProtection.Aircraft;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Fire;
using NuclearProtection.Fuse;
using NuclearProtection.Network;
using NuclearProtection.Phone;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Calls
{
    /// <summary>
    /// Serveur : déclenche les appels du commandement.
    /// - Vrai ordre : les deux équipes sont appelées, la 2e au plus 15 s après la 1re.
    /// - Faux ordre : une seule équipe est appelée (même texte : seule la comparaison les distingue).
    /// - Sanction : quand un avion ami est abattu, les deux équipes sont appelées, de façon échelonnée.
    /// </summary>
    public class CallDirector : NetworkBehaviour
    {
        public static CallDirector Instance { get; private set; }

        /// <summary>Serveur : un ordre vient d'être émis (1re sonnerie).</summary>
        public static event Action<NuclearOrder> ServerOrderIssued;

        /// <summary>Serveur : un vrai ordre n'a pas été exécuté à temps (défaite à la 2e fois, étape 10).</summary>
        public static event Action<NuclearOrder> ServerTrueOrderMissed;

        private struct ScheduledCall { public float Time; public TeamId Team; public CallKind Kind; public string Message; public NuclearOrder Order; }

        private readonly List<NuclearOrder> _orders = new();
        private readonly List<ScheduledCall> _scheduled = new();
        private List<PlannedOrder> _plan;
        private int _nextPlanned;
        private System.Random _sanctionRng, _blackoutRng;

        public IReadOnlyList<NuclearOrder> Orders => _orders;

        /// <summary>Résumé de fin : appels de sanction passés (un par équipe) et appels du jeu manqués (tous types).</summary>
        public int SanctionCalls { get; private set; }
        public int MissedCalls { get; private set; }
        private static CallSettings S => ConnectionManager.Instance.Config.calls;
        private static GameClock Clock => GameClock.Instance;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer) FireControl.ServerShotDown += OnServerShotDown;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) FireControl.ServerShotDown -= OnServerShotDown;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsServer || Clock == null || !Clock.IsActive) return;
            if (_plan == null) BuildPlan();
            float now = Clock.Elapsed;

            if (_nextPlanned < _plan.Count && !ApplyBlackoutRule(now) && now >= _plan[_nextPlanned].Time)
                IssueOrder(_plan[_nextPlanned++], now);

            PlaceDueCalls(now);
            CheckDeadlines(now);
        }

        private void BuildPlan()
        {
            int seed = MatchSession.Instance.Seed.Value;
            // Un seul téléphone (bunker commun) : impossible de comparer les appels, donc pas de faux ordre.
            bool allowFalse = !PhoneSystem.SharedPhone || S.falseOrdersWithSharedPhone;
            _plan = OrderPlanner.Plan(S, ConnectionManager.Instance.Config.director, Clock.DurationSeconds, SeededRandom.Create(seed, SeededRandom.SaltOrders),
                MatchSession.Instance.CurrentDifficulty.trueOrderRatio, allowFalse);
            _sanctionRng = SeededRandom.Create(seed, SeededRandom.SaltSanctions);
            _blackoutRng = SeededRandom.Create(seed, SeededRandom.SaltBlackoutOrders);
            SpoilerLog.Log($"[Appels] {_plan.Count} ordres prévus pour cette partie.");
        }

        // ---------- Ordres nucléaires ----------

        private void IssueOrder(PlannedOrder plan, float now)
        {
            var order = new NuclearOrder { Id = _orders.Count + 1, IsTrue = plan.IsTrue, FirstTeam = plan.FirstTeam, FirstCallTime = now };
            _orders.Add(order);
            Schedule(now, plan.FirstTeam, CallKind.OrdreNucleaire, S.orderMessage, order);

            if (plan.IsTrue)
            {
                order.SecondCallTime = now + plan.SecondCallDelay;
                order.LaunchDeadline = order.SecondCallTime + S.trueOrderLaunchDeadline;
                Schedule(order.SecondCallTime, Other(plan.FirstTeam), CallKind.OrdreNucleaire, S.orderMessage, order);
            }
            SpoilerLog.Log($"[Appels] Émission de l'{order}.");
            ServerOrderIssued?.Invoke(order);
        }

        private void CheckDeadlines(float now)
        {
            foreach (var order in _orders)
            {
                if (!order.IsTrue || order.Launched || order.Missed || now <= order.LaunchDeadline) continue;
                order.Missed = true;
                SpoilerLog.Log($"[Appels] {order} : pas de lancement à temps, ordre RATÉ.");
                ServerTrueOrderMissed?.Invoke(order);
            }
        }

        /// <summary>
        /// Serveur (étape 8) : y a-t-il un vrai ordre en cours (1er appel passé, délai pas écoulé, pas encore exécuté) ?
        /// </summary>
        public bool TryGetActiveTrueOrder(out NuclearOrder active)
        {
            float now = Clock != null ? Clock.Elapsed : 0f;
            foreach (var order in _orders)
            {
                if (!order.IsTrue || order.Launched || order.Missed || now > order.LaunchDeadline) continue;
                active = order;
                return true;
            }
            active = null;
            return false;
        }

        // ---------- Pannes de fusible (règle des 20 s) ----------

        /// <summary>
        /// Vrai = le prochain ordre doit attendre (une panne a commencé il y a moins de 20 s).
        /// Si une panne dure au-delà, le prochain ordre peut être avancé à maintenant (probable, pas garanti) ;
        /// il est alors adressé en premier à l'équipe valide, qui ne peut pas vérifier auprès de l'équipe en panne.
        /// </summary>
        private bool ApplyBlackoutRule(float now)
        {
            var fuse = FuseSystem.Instance;
            if (fuse == null) return false;
            var f = ConnectionManager.Instance.Config.fuse;

            TeamId? lasting = null;
            foreach (var team in new[] { TeamId.Radar, TeamId.Tir })
            {
                float age = fuse.BlackoutAge(team);
                if (age < 0f) continue;
                if (age < f.noNuclearOrderAfterBlackout) return true;
                lasting = team;
            }
            if (lasting is not TeamId dark || OrderInProgress()) return false;

            double chance = 1d - Math.Exp(-f.blackoutOrderRatePerMinute / 60d * Time.deltaTime);
            if (_blackoutRng.NextDouble() >= chance) return false;

            var next = _plan[_nextPlanned];
            next.Time = now;
            next.FirstTeam = Other(dark);
            _plan[_nextPlanned] = next;
            Debug.Log($"[Appels] Panne {dark} qui dure : ordre avancé.");
            return false;
        }

        private bool OrderInProgress() =>
            TryGetActiveTrueOrder(out _) || _scheduled.Exists(c => c.Kind == CallKind.OrdreNucleaire);

        // ---------- Sanctions ----------

        private void OnServerShotDown(RadarContact contact, bool wasEnemy)
        {
            if (wasEnemy || Clock == null) return;
            float first = Clock.Elapsed + S.sanctionDelayAfterKill;
            float second = first + _sanctionRng.Range(S.sanctionStaggerMin, S.sanctionStaggerMax);
            var firstTeam = _sanctionRng.Next(2) == 0 ? TeamId.Radar : TeamId.Tir;
            string message = string.Format(S.sanctionMessage, contact.Code);

            Schedule(first, firstTeam, CallKind.Sanction, message, null);
            Schedule(second, Other(firstTeam), CallKind.Sanction, message, null);
            Debug.Log($"[Appels] Sanction prévue : ami {contact.Code} abattu.");
        }

        // ---------- File des appels ----------

        private void Schedule(float time, TeamId team, CallKind kind, string message, NuclearOrder order) =>
            _scheduled.Add(new ScheduledCall { Time = time, Team = team, Kind = kind, Message = message, Order = order });

        private void PlaceDueCalls(float now)
        {
            for (int i = 0; i < _scheduled.Count; i++)
            {
                var call = _scheduled[i];
                if (now < call.Time) continue;
                _scheduled.RemoveAt(i--);

                var order = call.Order;
                order?.SetAnswered(call.Team, false); // « appelée mais pas encore décroché »
                if (call.Kind == CallKind.Sanction) SanctionCalls++;
                PhoneSystem.Instance.ServerPlaceGameCall(call.Team, call.Kind, call.Message, answered =>
                {
                    order?.SetAnswered(call.Team, answered);
                    if (!answered) MissedCalls++;
                    Debug.Log($"[Appels] {call.Kind} vers {call.Team} : {(answered ? "décroché" : "manqué")}.");
                });
            }
        }

        private static TeamId Other(TeamId team) => team == TeamId.Radar ? TeamId.Tir : TeamId.Radar;

        // ---------- Tests depuis l'inspecteur (Play Mode, hôte) ----------

        [ContextMenu("Test : vrai ordre maintenant")]
        private void TestTrueOrder() => IssueOrder(new PlannedOrder { IsTrue = true, FirstTeam = TeamId.Radar, SecondCallDelay = 8f }, Clock.Elapsed);

        [ContextMenu("Test : faux ordre maintenant")]
        private void TestFalseOrder() => IssueOrder(new PlannedOrder { IsTrue = false, FirstTeam = TeamId.Tir }, Clock.Elapsed);
    }
}
