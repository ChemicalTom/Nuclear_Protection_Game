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
            GUI.matrix = Matrix4x4.Scale(Vector3.one * DisplayControl.UiScale);
            float w = Screen.width / DisplayControl.UiScale, h = Screen.height / DisplayControl.UiScale;

            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;

            InitStyles();
            var d = ConnectionManager.Instance.Config.defeat;
            var result = MatchOutcome.Instance != null ? MatchOutcome.Instance.Result.Value : MatchResult.Defaite;
            bool victory = result == MatchResult.Victoire;
            string color = victory ? "#60ff70" : "#ff4030";

            var area = new Rect(w * 0.08f, 12, w * 0.84f, h - 24);
            GUILayout.BeginArea(area);
            GUILayout.Label($"<color={color}>{(victory ? d.victoryTitle : d.defeatTitle)}</color>", _title);
            var reason = MatchOutcome.Instance != null ? MatchOutcome.Instance.Reason.Value : DefeatReason.Aucune;
            GUILayout.Label(victory ? d.victoryText : ReasonText(d, reason), _text);

            if (MatchOutcome.LocalSummary is MatchSummary m) DrawSummary(m);
            else GUILayout.Label("Calcul du résumé…", _text);

            GUILayout.Space(8);
            if (GUILayout.Button("Retour au menu", GUILayout.Height(34))) ConnectionManager.Instance.Leave();
            GUILayout.EndArea();
        }

        private void DrawSummary(MatchSummary m)
        {
            GUILayout.Label($"<color={GradeColor(m.Grade)}>Note : {m.Grade}</color>", _grade);
            GUILayout.Label($"Score {m.Score} / {m.PerfectScore} possibles ({m.Percent:0} %)", _text);

            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(Screen.width / DisplayControl.UiScale * 0.4f));
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
            if (m.FalseLaunches > 0) Line($"<color=#ff4030>Lancements sans ordre valide : {m.FalseLaunches}</color>");
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
            _title ??= new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            _grade ??= new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            _text ??= new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = true, alignment = TextAnchor.UpperLeft };
            _section ??= new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.8f, 0.3f) } };
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        private static string VerdictText(OrderVerdict v) => v switch
        {
            OrderVerdict.BienLance => "<color=#60ff70>lancé ✔</color>",
            OrderVerdict.Rate => "<color=#ff4030>raté ✘</color>",
            OrderVerdict.BienIgnore => "<color=#60ff70>ignoré ✔</color>",
            OrderVerdict.LanceSurFaux => "<color=#ff4030>lancé sur un faux ✘</color>",
            _ => "<color=#a0a0a0>en cours à la fin (non noté)</color>"
        };

        private static string GradeColor(Grade g) => g switch
        {
            Grade.S => "#ffd700",
            Grade.A => "#60ff70",
            Grade.B => "#a0e060",
            Grade.C => "#e0e060",
            Grade.D => "#ff9040",
            _ => "#ff4030"
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
