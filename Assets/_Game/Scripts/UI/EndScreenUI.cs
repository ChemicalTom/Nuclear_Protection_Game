using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Outcome;
using NuclearProtection.Player;
using NuclearProtection.UI.Panels;
using UnityEngine;

namespace NuclearProtection.UI
{
    /// <summary>
    /// Écran de fin chez tous les joueurs : victoire/défaite, note S à F, et résumé complet
    /// (compteurs cachés révélés, ordres et leur traitement, appels, pannes, seed et durée).
    /// </summary>
    public class EndScreenUI : MonoBehaviour
    {
        private bool _shown;
        private Vector2 _scroll;
        private GUIStyle _title, _grade, _text, _section;

        private void Update()
        {
            if (_shown || !MatchOutcome.IsOver) return;
            _shown = true;
            if (StationPanelHost.IsOpen) StationPanelHost.Instance.Close();
            CursorControl.SetLocked(false);
        }

        private void OnGUI()
        {
            if (!_shown) return;
            GUI.depth = -100; // au-dessus du HUD et des écrans de poste
            UiTheme.Begin();
            var size = UiTheme.Size;
            float w = size.x, h = size.y;
            UiTheme.DrawBackdrop();

            InitStyles();
            var d = ConnectionManager.Instance.Config.defeat;
            var result = MatchOutcome.Instance != null ? MatchOutcome.Instance.Result.Value : MatchResult.Defaite;
            bool victory = result == MatchResult.Victoire;

            var area = UiTheme.Panel(new Rect(w * 0.06f, 10, w * 0.88f, h - 20), "RAPPORT DE MISSION");
            GUILayout.BeginArea(area);
            string title = victory ? d.victoryTitle : d.defeatTitle;
            GUILayout.Label(victory ? UiTheme.Ok(title) : UiTheme.Bad(title), _title);
            var reason = MatchOutcome.Instance != null ? MatchOutcome.Instance.Reason.Value : DefeatReason.Aucune;
            GUILayout.Label(victory ? d.victoryText : ReasonText(d, reason), UiTheme.Center);

            if (MatchOutcome.LocalSummary is MatchSummary m) DrawSummary(m, area.width);
            else GUILayout.Label(UiTheme.Dim("Calcul du résumé…"), UiTheme.Center);

            GUILayout.Space(8);
            if (GUILayout.Button("RETOUR AU MENU", GUILayout.Height(36))) ConnectionManager.Instance.Leave();
            GUILayout.EndArea();
        }

        private void DrawSummary(MatchSummary m, float width)
        {
            GUILayout.Label(UiTheme.Paint($"NOTE : {m.Grade}", GradeColor(m.Grade)), _grade);
            GUILayout.Label(UiTheme.Dim($"Score {m.Score} / {m.PerfectScore} possibles ({m.Percent:0} %)"), UiTheme.Center);

            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(width * 0.45f));
            Section("AVIONS");
            Line($"Ennemis abattus : {m.EnemiesDowned}");
            Line($"Ennemis passés : {m.EnemiesPassed} sur {m.EnemiesPassedLimit} max");
            Line($"Amis abattus : {m.FriendliesDowned} sur {m.FriendliesDownedLimit} max");

            Section("APPELS");
            Line($"Appels de sanction : {m.SanctionCalls}");
            Line($"Appels manqués : {m.MissedCalls}");

            Section("PANNES DE FUSIBLE");
            Line($"Pannes : {m.Blackouts}");
            Line(m.AverageRepairSeconds >= 0f ? $"Temps moyen de réparation : {m.AverageRepairSeconds:0} s" : "Temps moyen de réparation : —");

            Section("PARTIE");
            Line($"Durée : {m.DurationMinutes} min (jouée : {Clock(m.PlayedSeconds)})");
            Line($"Seed : {m.Seed}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Section("ORDRES NUCLÉAIRES");
            int trueCount = 0;
            foreach (var o in m.Orders) if (o.IsTrue) trueCount++;
            Line($"Reçus : {m.Orders.Length} ({trueCount} vrais, {m.Orders.Length - trueCount} faux)");
            Line($"Vrais ordres ratés : {m.MissedTrueOrders} sur {m.MissedTrueOrdersLimit} max");
            if (m.FalseLaunches > 0) Line(UiTheme.Bad($"Lancements sans ordre valide : {m.FalseLaunches}"));
            foreach (var o in m.Orders)
                Line($"#{o.Id} à {Clock(o.Time)} — {(o.IsTrue ? "VRAI" : "FAUX")} : {VerdictText(o.Verdict)}");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void Section(string title)
        {
            GUILayout.Space(8);
            GUILayout.Label(title, _section);
        }

        private void Line(string text) => GUILayout.Label(text, _text);

        private void InitStyles()
        {
            _title ??= new GUIStyle(UiTheme.Title) { fontSize = UiTheme.S.fontSize * 2 + 14 };
            _grade ??= new GUIStyle(UiTheme.Title) { fontSize = UiTheme.S.fontSize + 13 };
            _text ??= new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            _section ??= UiTheme.NoHover(new GUIStyle(UiTheme.Header) { normal = { textColor = UiTheme.S.warning } });
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        private static string VerdictText(OrderVerdict v) => v switch
        {
            OrderVerdict.BienLance => UiTheme.Ok("lancé ✔"),
            OrderVerdict.Rate => UiTheme.Bad("raté ✘"),
            OrderVerdict.BienIgnore => UiTheme.Ok("ignoré ✔"),
            OrderVerdict.LanceSurFaux => UiTheme.Bad("lancé sur un faux ✘"),
            _ => UiTheme.Dim("en cours à la fin (non noté)")
        };

        // S doré, puis du vert (A) vers le rouge (F) en passant par l'ambre.
        private static Color GradeColor(Grade g) => g switch
        {
            Grade.S => new Color(1f, 0.84f, 0f),
            Grade.A => UiTheme.S.accent,
            Grade.B => Color.Lerp(UiTheme.S.accent, UiTheme.S.warning, 0.35f),
            Grade.C => Color.Lerp(UiTheme.S.accent, UiTheme.S.warning, 0.7f),
            Grade.D => UiTheme.S.warning,
            _ => UiTheme.S.danger
        };

        private static string ReasonText(DefeatSettings d, DefeatReason reason) => reason switch
        {
            DefeatReason.EnnemisPasses => d.reasonEnemies,
            DefeatReason.AmisAbattus => d.reasonFriendlies,
            DefeatReason.OrdresRates => d.reasonMissedOrders,
            DefeatReason.FauxLancement => d.reasonFalseLaunch,
            _ => ""
        };
    }
}
