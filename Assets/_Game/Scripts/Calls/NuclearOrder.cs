using NuclearProtection.Core;

namespace NuclearProtection.Calls
{
    /// <summary>Suivi serveur d'un ordre nucléaire émis (utile pour le lancement, les défaites et le résumé).</summary>
    public class NuclearOrder
    {
        public int Id;
        public bool IsTrue;
        public TeamId FirstTeam;
        public float FirstCallTime;    // secondes depuis le début de la partie
        public float SecondCallTime;   // vrai ordre seulement
        public float LaunchDeadline;   // vrai ordre : au-delà, l'ordre est raté

        public bool? AnsweredRadar;    // null = pas (encore) appelée, false = manqué
        public bool? AnsweredTir;
        public bool Launched;          // étape 8
        public bool Missed;

        public void SetAnswered(TeamId team, bool answered)
        {
            if (team == TeamId.Radar) AnsweredRadar = answered;
            else AnsweredTir = answered;
        }

        public override string ToString() => $"ordre #{Id} {(IsTrue ? "VRAI" : "FAUX")} (1er appel {FirstTeam} à {FirstCallTime:0} s)";
    }
}
