namespace NuclearProtection.Core
{
    /// <summary>Les deux équipes (une par bunker en mode 4 joueurs).</summary>
    public enum TeamId
    {
        Radar = 0,
        Tir = 1
    }

    /// <summary>
    /// Disposition des bunkers selon le nombre de joueurs.
    /// 4 joueurs : 2 bunkers séparés. 2 joueurs : 1 bunker commun avec tous les outils.
    /// </summary>
    public enum BunkerLayout
    {
        DeuxBunkersSepares = 0,
        UnBunkerCommun = 1
    }

    /// <summary>Pénalité appliquée en cas d'erreur sur le code de tir.</summary>
    public enum FireCodePenaltyMode
    {
        Cooldown = 0,      // blocage court après chaque erreur
        LimiteEssais = 1,  // gros blocage après N erreurs consécutives
        LesDeux = 2
    }

    /// <summary>Ce qu'entend l'équipe valide en appelant l'équipe en panne.</summary>
    public enum DeadLineSound
    {
        Silence = 0,
        BipDiscret = 1
    }

    /// <summary>Service vocal utilisé pour le téléphone entre bunkers.</summary>
    public enum PhoneVoiceBackend
    {
        Factice = 0,
        Vivox = 1
    }

    /// <summary>Types de postes interactifs placés dans les bunkers.</summary>
    public enum StationType
    {
        Radar,
        Registre,
        PupitreTir,
        CleConv1,
        CleConv2,
        Telephone,
        Fusible,
        Mallette,
        SerrureNucA,
        SerrureNucB
    }

    /// <summary>Notes de fin de partie, de la meilleure à la pire.</summary>
    public enum Grade
    {
        S, A, B, C, D, F
    }
}
