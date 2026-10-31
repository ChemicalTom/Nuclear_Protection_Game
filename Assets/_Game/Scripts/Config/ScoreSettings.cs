using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Formule du score et seuils des notes S à F.</summary>
    [Serializable]
    public class ScoreSettings
    {
        [Header("Points")]
        public int pointsPerEnemyDowned = 100;
        public int pointsPerTrueOrderLaunched = 300;
        public int pointsPerFalseOrderIgnored = 200;

        [Header("Malus")]
        public int penaltyPerEnemyPassed = 150;
        public int penaltyPerFriendlyDowned = 200;
        public int penaltyPerMissedCall = 25;
        public int penaltyPerMissedTrueOrder = 300;
        public int penaltyPerFalseLaunch = 1000;

        [Header("Notes")]
        [Tooltip("Score ramené en % du score parfait possible ; seuils minimum pour S, A, B, C, D (sinon F).")]
        public float minPercentS = 90f;
        public float minPercentA = 75f;
        public float minPercentB = 55f;
        public float minPercentC = 40f;
        public float minPercentD = 20f;

        [Tooltip("S exige zéro erreur (aucun ennemi passé, ami abattu, ordre raté ou faux lancement).")]
        public bool sRequiresNoMistake = true;

        [Tooltip("Toute défaite donne F.")]
        public bool defeatAlwaysF = true;

        /// <summary>Convertit un pourcentage de score en note (hors règles spéciales).</summary>
        public Grade GradeFromPercent(float percent)
        {
            if (percent >= minPercentS) return Grade.S;
            if (percent >= minPercentA) return Grade.A;
            if (percent >= minPercentB) return Grade.B;
            if (percent >= minPercentC) return Grade.C;
            if (percent >= minPercentD) return Grade.D;
            return Grade.F;
        }
    }
}
