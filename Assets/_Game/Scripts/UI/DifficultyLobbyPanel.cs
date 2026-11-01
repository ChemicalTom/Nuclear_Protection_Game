using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Bloc « DIFFICULTÉ » du lobby : l'hôte choisit le niveau (et règle les curseurs en mode Perso),
    /// les autres voient le niveau et un résumé des valeurs.
    /// </summary>
    public static class DifficultyLobbyPanel
    {
        private const float RowHeight = 40f, Gap = 16f;
        private static readonly DifficultyLevel[] Levels =
            { DifficultyLevel.Recrue, DifficultyLevel.Normal, DifficultyLevel.Difficile, DifficultyLevel.Expert, DifficultyLevel.Perso };

        private static DifficultySettings S => ConnectionManager.Instance.Config.difficulty;

        /// <summary>Hauteur du panneau selon ce qui est affiché.</summary>
        public static float Height(bool isHost, DifficultyLevel level) =>
            (isHost ? 100f : 82f) + (isHost && level == DifficultyLevel.Perso ? RowHeight * 5 + 4 : 44f); // résumé sur 2 lignes

        public static void Draw(Rect r, MatchSession session, bool isHost)
        {
            var level = session.Difficulty.Value;
            var inner = UiTheme.Panel(r, $"DIFFICULTÉ : {S.LabelFor(level)}");
            GUILayout.BeginArea(inner);

            if (isHost)
            {
                var labels = new string[Levels.Length];
                for (int i = 0; i < Levels.Length; i++) labels[i] = S.LabelFor(Levels[i]);
                int pick = GUILayout.Toolbar(System.Array.IndexOf(Levels, level), labels, GUILayout.Height(30));
                if (pick >= 0 && Levels[pick] != level) session.ServerSetDifficulty(Levels[pick]);
            }
            GUILayout.Label(S.DescriptionFor(level), UiTheme.Small);

            _colWidth = (inner.width - Gap) / 2f - 8f;
            if (isHost && level == DifficultyLevel.Perso) DrawSliders(session);
            else GUILayout.Label(Summary(session.CurrentDifficulty), UiTheme.Small);
            GUILayout.EndArea();
        }

        // Résumé lisible des valeurs (pour tout le monde).
        private static string Summary(DifficultyValues v)
        {
            string orders = NoFalseOrders()
                ? "aucun faux ordre (un seul téléphone)"
                : $"{Pct(1f - v.trueOrderRatio)} de faux ordres";
            return $"Avion toutes les {v.spawnIntervalMin:0}–{v.spawnIntervalMax:0} s · {Pct(v.friendlyRatio)} d'amis · {Pct(v.sidePassChance)} passent à côté · " +
                   $"traversée {v.timeToReachBase:0} s · missiles {Pct(v.enemyMissileChance)} · {v.maxSimultaneousEnemies} ennemis max · {orders}";
        }

        // Dans le lobby, la disposition n'est pas encore figée : on la prévoit d'après le nombre de joueurs.
        private static bool NoFalseOrders()
        {
            var session = MatchSession.Instance;
            var cfg = ConnectionManager.Instance.Config;
            return session != null && !cfg.calls.falseOrdersWithSharedPhone
                && TeamRules.LayoutFor(session.Players.Count, cfg.network) == BunkerLayout.UnBunkerCommun;
        }

        private static void DrawSliders(MatchSession session)
        {
            var v = session.CustomDifficulty.Value;
            DifficultyValues lo = S.customMin, hi = S.customMax;
            var old = v;

            Row(() => v.spawnIntervalMin = Slider("Délai min entre avions", v.spawnIntervalMin, lo.spawnIntervalMin, hi.spawnIntervalMin, " s"),
                () => v.spawnIntervalMax = Slider("Délai max entre avions", v.spawnIntervalMax, lo.spawnIntervalMax, hi.spawnIntervalMax, " s"));
            Row(() => v.friendlyRatio = Slider("Part d'avions amis", v.friendlyRatio, lo.friendlyRatio, hi.friendlyRatio, "%"),
                () => v.timeToReachBase = Slider("Traversée du radar", v.timeToReachBase, lo.timeToReachBase, hi.timeToReachBase, " s"));
            Row(() => v.sidePassChance = Slider("Passent sur le côté", v.sidePassChance, lo.sidePassChance, hi.sidePassChance, "%"),
                () => v.enemyMissileChance = Slider("Ennemis qui tirent un missile", v.enemyMissileChance, lo.enemyMissileChance, hi.enemyMissileChance, "%"));
            Row(() => v.enemyMissileFlightTime = Slider("Vol d'un missile ennemi", v.enemyMissileFlightTime, lo.enemyMissileFlightTime, hi.enemyMissileFlightTime, " s"),
                () => v.maxSimultaneousEnemies = Mathf.RoundToInt(Slider("Ennemis max en même temps", v.maxSimultaneousEnemies, lo.maxSimultaneousEnemies, hi.maxSimultaneousEnemies, "")));
            Row(() => v.wrongCodeCooldown = Slider("Blocage si mauvais code", v.wrongCodeCooldown, lo.wrongCodeCooldown, hi.wrongCodeCooldown, " s"),
                () => v.trueOrderRatio = Slider("Ordres vrais (4 joueurs)", v.trueOrderRatio, lo.trueOrderRatio, hi.trueOrderRatio, "%"));

            if (!v.Equals(old)) session.ServerSetCustomDifficulty(v);
        }

        // Largeur d'une colonne de curseurs (fixée pour que les deux colonnes restent alignées).
        private static float _colWidth;

        private static void Row(System.Action left, System.Action right)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(RowHeight));
            GUILayout.BeginVertical(GUILayout.Width(_colWidth)); left(); GUILayout.EndVertical();
            GUILayout.Space(Gap);
            GUILayout.BeginVertical(GUILayout.Width(_colWidth)); right(); GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // Curseur avec son libellé et sa valeur (unit "%" = proportion 0-1) ; pourcentages arrondis à 5 %, le reste à l'unité.
        private static float Slider(string label, float value, float min, float max, string unit)
        {
            bool pct = unit == "%";
            string shown = pct ? Pct(value) : $"{value:0}{unit}";
            GUILayout.Label($"{UiTheme.Dim(label)}  {UiTheme.Ok(shown)}", UiTheme.Small);
            float v = GUILayout.HorizontalSlider(value, min, max);
            return pct ? Mathf.Round(v * 20f) / 20f : Mathf.Round(v);
        }

        private static string Pct(float ratio) => $"{Mathf.RoundToInt(ratio * 100f)} %";
    }
}
