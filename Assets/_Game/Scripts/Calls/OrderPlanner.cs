using System;
using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Calls
{
    /// <summary>Un ordre nucléaire prévu à l'avance (tiré de la seed, indépendant des actions des joueurs).</summary>
    public struct PlannedOrder
    {
        public float Time;            // secondes depuis le début de la partie
        public bool IsTrue;
        public TeamId FirstTeam;
        public float SecondCallDelay; // vrai ordre seulement
    }

    /// <summary>
    /// Planifie tous les ordres de la partie : rythme adapté à la durée, plancher « forcer un appel »,
    /// et nombre minimum d'ordres (vrais + faux) garanti.
    /// </summary>
    public static class OrderPlanner
    {
        /// <param name="trueOrderRatio">Probabilité qu'un ordre soit vrai (selon la difficulté).</param>
        /// <param name="allowFalseOrders">Faux : tous les ordres sont vrais (un seul téléphone, impossible de comparer).</param>
        public static List<PlannedOrder> Plan(CallSettings s, DirectorSettings director, float durationSec, System.Random rng,
            float trueOrderRatio, bool allowFalseOrders)
        {
            var times = PlanTimes(s, durationSec, rng);
            var orders = new List<PlannedOrder>(times.Count);
            foreach (float t in times)
            {
                orders.Add(new PlannedOrder
                {
                    Time = t,
                    IsTrue = rng.NextDouble() < trueOrderRatio || !allowFalseOrders, // on tire toujours : même suite de hasard pour une seed
                    FirstTeam = rng.Next(2) == 0 ? TeamId.Radar : TeamId.Tir,
                    SecondCallDelay = rng.Range(s.trueOrderSecondCallMin, s.trueOrderSecondCallMax)
                });
            }
            ApplyStreakLimits(orders, director, allowFalseOrders);
            return orders;
        }

        /// <summary>
        /// Directeur : le hasard décide, mais on casse les séries trop longues
        /// (trop d'ordres vrais ou faux d'affilée, toujours la même équipe appelée en premier).
        /// </summary>
        private static void ApplyStreakLimits(List<PlannedOrder> orders, DirectorSettings d, bool allowFalseOrders)
        {
            int sameTruth = 0, sameTeam = 0;
            for (int i = 0; i < orders.Count; i++)
            {
                var o = orders[i];
                if (i > 0)
                {
                    var prev = orders[i - 1];
                    int truthLimit = prev.IsTrue ? d.maxConsecutiveTrueOrders : d.maxConsecutiveFalseOrders;
                    if (allowFalseOrders && o.IsTrue == prev.IsTrue && sameTruth >= truthLimit) o.IsTrue = !o.IsTrue;
                    if (o.FirstTeam == prev.FirstTeam && sameTeam >= d.maxSameTeamCalledFirst)
                        o.FirstTeam = o.FirstTeam == TeamId.Radar ? TeamId.Tir : TeamId.Radar;

                    sameTruth = o.IsTrue == prev.IsTrue ? sameTruth + 1 : 1;
                    sameTeam = o.FirstTeam == prev.FirstTeam ? sameTeam + 1 : 1;
                }
                else sameTruth = sameTeam = 1;
                orders[i] = o;
            }
        }

        private static List<float> PlanTimes(CallSettings s, float durationSec, System.Random rng)
        {
            float scale = s.intervalScaleByDurationRatio.Evaluate(durationSec / 60f / s.referenceDurationMinutes);
            float iMin = s.intervalMin * scale;
            float iMax = Mathf.Max(iMin, s.intervalMax * scale);
            float force = s.forceCallAfter * scale;

            // Un ordre doit pouvoir être mené à terme avant la fin (2e appel + délai de lancement).
            float start = s.startGracePeriod;
            float end = durationSec - (s.trueOrderSecondCallMax + s.trueOrderLaunchDeadline);
            var times = new List<float>();
            if (end <= start) return times; // partie trop courte pour un ordre

            float t = start + rng.Range(0f, Mathf.Min(iMin, force));
            while (t <= end)
            {
                times.Add(t);
                t += Mathf.Min(rng.Range(iMin, iMax), force);
            }

            // Minimum garanti : on répartit les ordres régulièrement (avec un peu de hasard) dans la fenêtre.
            int min = Mathf.RoundToInt(s.minNuclearOrdersByMinutes.Evaluate(durationSec / 60f));
            if (times.Count < min)
            {
                times.Clear();
                float slot = (end - start) / min;
                for (int i = 0; i < min; i++)
                    times.Add(start + slot * (i + 0.5f + rng.Range(-0.25f, 0.25f)));
            }
            return times;
        }
    }
}
