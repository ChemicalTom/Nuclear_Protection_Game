using System.Collections.Generic;
using NuclearProtection.Calls;
using NuclearProtection.Core;
using NuclearProtection.Fuse;
using NuclearProtection.Network;
using Unity.Netcode;

namespace NuclearProtection.Outcome
{
    /// <summary>Résumé de fin : assemblé par le serveur à la fin, envoyé à tous les joueurs.</summary>
    public partial class MatchOutcome
    {
        /// <summary>Clients : résumé reçu (null tant que la partie n'est pas finie).</summary>
        public static MatchSummary? LocalSummary { get; private set; }

        private readonly List<float> _falseLaunchTimes = new(); // secondes de partie

        private MatchSummary BuildSummary()
        {
            var config = ConnectionManager.Instance.Config;
            var session = MatchSession.Instance;
            var calls = CallDirector.Instance;

            var m = new MatchSummary
            {
                Result = Result.Value,
                Reason = Reason.Value,
                Seed = session.Seed.Value,
                DurationMinutes = session.DurationMinutes.Value,
                PlayedSeconds = GameClock.Instance.Elapsed,
                EnemiesDowned = EnemiesDowned,
                EnemiesPassed = EnemiesPassed,
                FriendliesDowned = FriendliesDowned,
                EnemiesPassedLimit = S.enemiesPassedLimit,
                FriendliesDownedLimit = S.friendliesDownedLimit,
                MissedTrueOrders = MissedTrueOrders,
                MissedTrueOrdersLimit = S.missedTrueOrdersLimit,
                FalseLaunches = FalseLaunches,
                SanctionCalls = calls != null ? calls.SanctionCalls : 0,
                MissedCalls = calls != null ? calls.MissedCalls : 0,
                Orders = BuildOrderLines(calls)
            };

            if (FuseSystem.Instance != null)
            {
                FuseSystem.Instance.GetStats(out m.Blackouts, out m.AverageRepairSeconds);
            }
            else m.AverageRepairSeconds = -1f;

            ScoreCalculator.Compute(ref m, config.score);
            SpoilerLog.Log($"[Fin] Score {m.Score}/{m.PerfectScore} ({m.Percent:0}%) → {m.Grade}.");
            return m;
        }

        // Un faux ordre est « lancé sur faux » si un lancement sans vrai ordre a eu lieu entre son appel et l'ordre suivant.
        private OrderLine[] BuildOrderLines(CallDirector calls)
        {
            if (calls == null) return new OrderLine[0];
            var orders = calls.Orders;
            var lines = new OrderLine[orders.Count];
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                float until = i + 1 < orders.Count ? orders[i + 1].FirstCallTime : float.MaxValue;
                OrderVerdict verdict;
                if (o.IsTrue) verdict = o.Launched ? OrderVerdict.BienLance : o.Missed ? OrderVerdict.Rate : OrderVerdict.EnCours;
                else verdict = _falseLaunchTimes.Exists(t => t >= o.FirstCallTime && t < until) ? OrderVerdict.LanceSurFaux : OrderVerdict.BienIgnore;
                lines[i] = new OrderLine { Id = o.Id, IsTrue = o.IsTrue, Time = o.FirstCallTime, Verdict = verdict };
            }
            return lines;
        }

        [Rpc(SendTo.Everyone)]
        private void SummaryRpc(MatchSummary summary) => LocalSummary = summary;

        public override void OnDestroy()
        {
            if (Instance == this) LocalSummary = null; // nouvelle partie : on oublie l'ancien résumé
            base.OnDestroy();
        }
    }
}
