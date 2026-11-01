using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Appels rares : ordres nucléaires (vrais/faux) et appels de sanction.</summary>
    [Serializable]
    public class CallSettings
    {
        [Header("Rythme (pour une partie de référence)")]
        [Tooltip("Délai minimum entre deux appels (s).")]
        [Min(10f)] public float intervalMin = 120f;

        [Tooltip("Délai maximum entre deux appels (s).")]
        [Min(10f)] public float intervalMax = 240f;

        [Tooltip("Plancher : si aucun appel depuis ce temps, le jeu en force un (s).")]
        [Min(10f)] public float forceCallAfter = 300f;

        [Tooltip("Aucun ordre avant ce temps en début de partie (s).")]
        [Min(0f)] public float startGracePeriod = 30f;

        [Header("Adaptation à la durée")]
        [Tooltip("Durée de référence pour laquelle les intervalles ci-dessus sont réglés (minutes).")]
        [Min(1)] public int referenceDurationMinutes = 15;

        [Tooltip("Facteur appliqué aux intervalles selon (durée choisie / référence). X = ratio, Y = facteur.")]
        public AnimationCurve intervalScaleByDurationRatio = AnimationCurve.Linear(0.3f, 0.5f, 2f, 1.3f);

        [Tooltip("Nombre minimum d'ordres nucléaires garantis selon la durée (X = minutes, Y = nombre).")]
        public AnimationCurve minNuclearOrdersByMinutes = AnimationCurve.Linear(7f, 2f, 15f, 4f);

        [Header("Ordres nucléaires")]
        // La probabilité qu'un ordre soit vrai dépend de la difficulté (DifficultyValues.trueOrderRatio).
        [Tooltip("Faux ordres même avec un seul téléphone (bunker commun à 2). Décoché : impossible de comparer, tous les ordres sont vrais.")]
        public bool falseOrdersWithSharedPhone = false;

        [Tooltip("Écart min entre les 2 appels d'un vrai ordre (s).")]
        [Min(0f)] public float trueOrderSecondCallMin = 3f;

        [Tooltip("Écart max entre les 2 appels d'un vrai ordre (s). Règle : 15 s ou moins.")]
        [Range(1f, 15f)] public float trueOrderSecondCallMax = 15f;

        [Tooltip("Temps laissé pour lancer après le 2e appel d'un vrai ordre, avant de le compter raté (s).")]
        [Min(5f)] public float trueOrderLaunchDeadline = 60f;

        [Header("Appels de sanction (ami abattu)")]
        [Tooltip("Écart entre l'appel à la 1re équipe et à la 2e (s).")]
        [Min(0f)] public float sanctionStaggerMin = 2f;
        [Min(0f)] public float sanctionStaggerMax = 8f;
        [Tooltip("Délai entre l'abattage de l'ami et le 1er appel de sanction (s).")]
        [Min(0f)] public float sanctionDelayAfterKill = 3f;

        [Header("Textes du commandement")]
        [Tooltip("Identique pour un vrai et un faux ordre : seule la comparaison entre bunkers les distingue.")]
        [TextArea] public string orderMessage = "Ici le commandement. Ordre de lancement nucléaire. Je répète : ordre de lancement nucléaire.";

        [Tooltip("{0} = code de l'avion ami abattu.")]
        [TextArea] public string sanctionMessage = "Ici le commandement. Vous avez abattu l'avion ami {0}. Ce sera noté dans votre dossier.";
    }
}
