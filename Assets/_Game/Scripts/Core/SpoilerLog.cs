using System.Diagnostics;

namespace NuclearProtection.Core
{
    /// <summary>
    /// Journal des informations cachées aux joueurs (camp d'un avion, ordre vrai/faux, compteurs).
    /// Visible dans l'éditeur pour le développement, supprimé des builds : rien à lire dans le Player.log de l'hôte.
    /// </summary>
    public static class SpoilerLog
    {
        [Conditional("UNITY_EDITOR")]
        public static void Log(string message) => UnityEngine.Debug.Log(message);
    }
}
