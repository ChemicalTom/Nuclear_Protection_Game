using System;
using NuclearProtection.Core;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Valeurs qui changent selon la difficulté (envoyées aux clients pour le mode Perso).</summary>
    [Serializable]
    public struct DifficultyValues : INetworkSerializable, IEquatable<DifficultyValues>
    {
        [Tooltip("Délai minimum / maximum entre deux avions (s).")]
        public float spawnIntervalMin, spawnIntervalMax;
        [Tooltip("Proportion d'avions amis (0 = tous ennemis).")]
        [Range(0f, 1f)] public float friendlyRatio;
        [Tooltip("Temps pour traverser un rayon du radar, du bord jusqu'à la base (s).")]
        public float timeToReachBase;
        [Tooltip("Probabilité qu'un avion passe sur le côté au lieu de foncer sur la base.")]
        [Range(0f, 1f)] public float sidePassChance;
        [Tooltip("Probabilité qu'un avion ennemi tire un missile vers la base.")]
        [Range(0f, 1f)] public float enemyMissileChance;
        [Tooltip("Temps de vol d'un missile ennemi depuis le bord du radar (s).")]
        public float enemyMissileFlightTime;
        [Tooltip("Nombre max d'ennemis présents en même temps sur le radar.")]
        public int maxSimultaneousEnemies;
        [Tooltip("Blocage du pupitre après un mauvais code de tir (s).")]
        public float wrongCodeCooldown;
        [Tooltip("Probabilité qu'un ordre nucléaire soit vrai (le reste = faux ordres).")]
        [Range(0f, 1f)] public float trueOrderRatio;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref spawnIntervalMin);
            s.SerializeValue(ref spawnIntervalMax);
            s.SerializeValue(ref friendlyRatio);
            s.SerializeValue(ref timeToReachBase);
            s.SerializeValue(ref sidePassChance);
            s.SerializeValue(ref enemyMissileChance);
            s.SerializeValue(ref enemyMissileFlightTime);
            s.SerializeValue(ref maxSimultaneousEnemies);
            s.SerializeValue(ref wrongCodeCooldown);
            s.SerializeValue(ref trueOrderRatio);
        }

        public bool Equals(DifficultyValues o) =>
            spawnIntervalMin.Equals(o.spawnIntervalMin) && spawnIntervalMax.Equals(o.spawnIntervalMax)
            && friendlyRatio.Equals(o.friendlyRatio) && timeToReachBase.Equals(o.timeToReachBase)
            && sidePassChance.Equals(o.sidePassChance) && enemyMissileChance.Equals(o.enemyMissileChance)
            && enemyMissileFlightTime.Equals(o.enemyMissileFlightTime) && maxSimultaneousEnemies == o.maxSimultaneousEnemies
            && wrongCodeCooldown.Equals(o.wrongCodeCooldown) && trueOrderRatio.Equals(o.trueOrderRatio);

        /// <summary>Remet chaque valeur entre les bornes données (et max ≥ min).</summary>
        public DifficultyValues Clamped(DifficultyValues min, DifficultyValues max)
        {
            var v = this;
            v.spawnIntervalMin = Mathf.Clamp(v.spawnIntervalMin, min.spawnIntervalMin, max.spawnIntervalMin);
            v.spawnIntervalMax = Mathf.Clamp(v.spawnIntervalMax, min.spawnIntervalMax, max.spawnIntervalMax);
            v.spawnIntervalMax = Mathf.Max(v.spawnIntervalMax, v.spawnIntervalMin);
            v.friendlyRatio = Mathf.Clamp(v.friendlyRatio, min.friendlyRatio, max.friendlyRatio);
            v.timeToReachBase = Mathf.Clamp(v.timeToReachBase, min.timeToReachBase, max.timeToReachBase);
            v.sidePassChance = Mathf.Clamp(v.sidePassChance, min.sidePassChance, max.sidePassChance);
            v.enemyMissileChance = Mathf.Clamp(v.enemyMissileChance, min.enemyMissileChance, max.enemyMissileChance);
            v.enemyMissileFlightTime = Mathf.Clamp(v.enemyMissileFlightTime, min.enemyMissileFlightTime, max.enemyMissileFlightTime);
            v.maxSimultaneousEnemies = Mathf.Clamp(v.maxSimultaneousEnemies, min.maxSimultaneousEnemies, max.maxSimultaneousEnemies);
            v.wrongCodeCooldown = Mathf.Clamp(v.wrongCodeCooldown, min.wrongCodeCooldown, max.wrongCodeCooldown);
            v.trueOrderRatio = Mathf.Clamp(v.trueOrderRatio, min.trueOrderRatio, max.trueOrderRatio);
            return v;
        }
    }

    /// <summary>Un niveau prédéfini : nom affiché, explication et valeurs.</summary>
    [Serializable]
    public struct DifficultyPreset
    {
        public DifficultyLevel level;
        public string label;
        [TextArea] public string description;
        public DifficultyValues values;
    }

    /// <summary>Niveaux de difficulté (choisis par l'hôte dans le lobby) et bornes du mode Perso.</summary>
    [Serializable]
    public class DifficultySettings
    {
        public DifficultyLevel defaultLevel = DifficultyLevel.Normal;

        public DifficultyPreset[] presets =
        {
            new() { level = DifficultyLevel.Recrue, label = "RECRUE", description = "Peu d'avions, beaucoup de temps, presque pas de missiles. Pour apprendre.",
                values = new() { spawnIntervalMin = 22, spawnIntervalMax = 40, friendlyRatio = 0.55f, timeToReachBase = 100, sidePassChance = 0.35f,
                    enemyMissileChance = 0.1f, enemyMissileFlightTime = 40, maxSimultaneousEnemies = 2, wrongCodeCooldown = 3, trueOrderRatio = 0.6f } },
            new() { level = DifficultyLevel.Normal, label = "NORMAL", description = "Le rythme prévu : il faut bien se parler, sans panique.",
                values = new() { spawnIntervalMin = 16, spawnIntervalMax = 32, friendlyRatio = 0.5f, timeToReachBase = 80, sidePassChance = 0.35f,
                    enemyMissileChance = 0.2f, enemyMissileFlightTime = 32, maxSimultaneousEnemies = 3, wrongCodeCooldown = 5, trueOrderRatio = 0.5f } },
            new() { level = DifficultyLevel.Difficile, label = "DIFFICILE", description = "Plus d'ennemis, plus rapides, plus de missiles et de faux ordres.",
                values = new() { spawnIntervalMin = 12, spawnIntervalMax = 26, friendlyRatio = 0.45f, timeToReachBase = 65, sidePassChance = 0.3f,
                    enemyMissileChance = 0.3f, enemyMissileFlightTime = 26, maxSimultaneousEnemies = 4, wrongCodeCooldown = 7, trueOrderRatio = 0.45f } },
            new() { level = DifficultyLevel.Expert, label = "EXPERT", description = "Ciel saturé, missiles fréquents, aucune erreur tolérée.",
                values = new() { spawnIntervalMin = 9, spawnIntervalMax = 20, friendlyRatio = 0.4f, timeToReachBase = 50, sidePassChance = 0.25f,
                    enemyMissileChance = 0.4f, enemyMissileFlightTime = 20, maxSimultaneousEnemies = 5, wrongCodeCooldown = 10, trueOrderRatio = 0.4f } },
        };

        [Header("Mode Perso")]
        public string customLabel = "PERSO";
        [TextArea] public string customDescription = "L'hôte règle chaque valeur lui-même.";
        [Tooltip("Niveau dont le mode Perso part au début.")]
        public DifficultyLevel customStartsFrom = DifficultyLevel.Normal;
        [Tooltip("Bornes basses des curseurs du mode Perso.")]
        public DifficultyValues customMin = new() { spawnIntervalMin = 4, spawnIntervalMax = 6, friendlyRatio = 0f, timeToReachBase = 25, sidePassChance = 0f,
            enemyMissileChance = 0f, enemyMissileFlightTime = 10, maxSimultaneousEnemies = 1, wrongCodeCooldown = 0, trueOrderRatio = 0f };
        [Tooltip("Bornes hautes des curseurs du mode Perso.")]
        public DifficultyValues customMax = new() { spawnIntervalMin = 60, spawnIntervalMax = 90, friendlyRatio = 0.9f, timeToReachBase = 150, sidePassChance = 0.8f,
            enemyMissileChance = 1f, enemyMissileFlightTime = 60, maxSimultaneousEnemies = 8, wrongCodeCooldown = 20, trueOrderRatio = 1f };

        /// <summary>Valeurs d'un niveau prédéfini (Perso → niveau de départ du mode Perso).</summary>
        public DifficultyValues ValuesFor(DifficultyLevel level)
        {
            if (level == DifficultyLevel.Perso) level = customStartsFrom;
            foreach (var p in presets) if (p.level == level) return p.values;
            return presets.Length > 0 ? presets[0].values : default;
        }

        public string LabelFor(DifficultyLevel level)
        {
            if (level == DifficultyLevel.Perso) return customLabel;
            foreach (var p in presets) if (p.level == level && !string.IsNullOrEmpty(p.label)) return p.label;
            return level.ToString().ToUpperInvariant();
        }

        public string DescriptionFor(DifficultyLevel level)
        {
            if (level == DifficultyLevel.Perso) return customDescription;
            foreach (var p in presets) if (p.level == level) return p.description;
            return "";
        }

        /// <summary>Corrige les intervalles incohérents (max inférieur au min) des niveaux prédéfinis.</summary>
        public void Sanitize()
        {
            for (int i = 0; i < presets.Length; i++)
                presets[i].values.spawnIntervalMax = Mathf.Max(presets[i].values.spawnIntervalMax, presets[i].values.spawnIntervalMin);
        }
    }
}
