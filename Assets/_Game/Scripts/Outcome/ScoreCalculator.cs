using NuclearProtection.Config;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Outcome
{
    /// <summary>
    /// Calcul pur du score et de la note (formule et seuils dans ScoreSettings).
    /// Score = points (ennemis abattus, vrais ordres exécutés, faux ordres ignorés) − malus.
    /// Pourcentage = score / score parfait possible (tous les ennemis apparus abattus, tous les ordres bien traités).
    /// </summary>
    public static class ScoreCalculator
    {
        public static void Compute(ref MatchSummary m, ScoreSettings s)
        {
            int trueLaunched = 0, falseIgnored = 0, trueJudged = 0, falseJudged = 0;
            foreach (var o in m.Orders)
            {
                if (o.Verdict == OrderVerdict.EnCours) continue;
                if (o.IsTrue) { trueJudged++; if (o.Verdict == OrderVerdict.BienLance) trueLaunched++; }
                else { falseJudged++; if (o.Verdict == OrderVerdict.BienIgnore) falseIgnored++; }
            }

            int gains = m.EnemiesDowned * s.pointsPerEnemyDowned
                        + trueLaunched * s.pointsPerTrueOrderLaunched
                        + falseIgnored * s.pointsPerFalseOrderIgnored;
            int penalties = m.EnemiesPassed * s.penaltyPerEnemyPassed
                            + m.FriendliesDowned * s.penaltyPerFriendlyDowned
                            + m.MissedCalls * s.penaltyPerMissedCall
                            + m.MissedTrueOrders * s.penaltyPerMissedTrueOrder
                            + m.FalseLaunches * s.penaltyPerFalseLaunch;

            m.Score = gains - penalties;
            m.PerfectScore = (m.EnemiesDowned + m.EnemiesPassed) * s.pointsPerEnemyDowned
                             + trueJudged * s.pointsPerTrueOrderLaunched
                             + falseJudged * s.pointsPerFalseOrderIgnored;
            m.Percent = m.PerfectScore > 0
                ? Mathf.Clamp(100f * m.Score / m.PerfectScore, 0f, 100f)
                : (m.Score >= 0 ? 100f : 0f); // rien à faire de toute la partie

            bool mistake = m.EnemiesPassed > 0 || m.FriendliesDowned > 0 || m.MissedTrueOrders > 0 || m.FalseLaunches > 0;
            if (m.Result == MatchResult.Defaite && s.defeatAlwaysF) m.Grade = Grade.F;
            else
            {
                m.Grade = s.GradeFromPercent(m.Percent);
                if (m.Grade == Grade.S && s.sRequiresNoMistake && mistake) m.Grade = Grade.A;
            }
        }
    }
}
