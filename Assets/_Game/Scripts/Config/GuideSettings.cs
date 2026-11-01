using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Une page du guide : titre (onglet) et texte (balises <b> et <color> acceptées).</summary>
    [Serializable]
    public struct GuidePage
    {
        public string title;
        [TextArea(4, 20)] public string body;
    }

    /// <summary>Guide du jeu affiché depuis le menu principal.</summary>
    [Serializable]
    public class GuideSettings
    {
        public string buttonLabel = "GUIDE DU JEU";
        public string windowTitle = "MANUEL DE L'OPÉRATEUR";

        public GuidePage[] pages =
        {
            new() { title = "BUT", body =
                "Vous défendez la base contre les avions ennemis pendant toute la durée de la partie.\n\n" +
                "<b>Victoire</b> : l'horloge arrive au bout.\n" +
                "<b>Défaite</b> si :\n" +
                "• trop d'ennemis (avions ou missiles) frappent la base ;\n" +
                "• trop d'avions AMIS sont abattus ;\n" +
                "• un vrai ordre nucléaire n'est pas exécuté (le 1er raté est toléré) ;\n" +
                "• vous lancez le nucléaire sans vrai ordre (mort immédiate).\n\n" +
                "Les compteurs sont cachés : seul le rapport de fin donne le détail et la note (S à F)." },

            new() { title = "ÉQUIPES", body =
                "<b>2 joueurs</b> : un seul bunker avec tous les postes, un joueur Radar et un joueur Tir.\n" +
                "<b>4 joueurs</b> : deux bunkers scellés (2 Radar / 2 Tir). On ne se voit pas : on se parle au téléphone.\n\n" +
                "<b>Équipe RADAR</b> : surveille l'écran radar, identifie chaque avion au Registre, dicte les codes de tir.\n" +
                "<b>Équipe TIR</b> : tape le code de tir au pupitre et tourne les clés pour abattre la cible.\n\n" +
                "Dans le bunker, on s'entend à la voix ; entre bunkers, seulement au téléphone." },

            new() { title = "RADAR", body =
                "Clique sur un écho pour voir :\n" +
                "• son <b>code d'identification</b> (ex. K7M4) — à chercher au Registre ;\n" +
                "• son <b>code de tir</b> (1 lettre + 1 chiffre) — à dicter à l'équipe Tir ;\n" +
                "• sa distance et le temps restant.\n\n" +
                "Écho vert = avion, orange = missile ennemi (toujours à abattre).\n" +
                "Tous les avions ne foncent pas sur la base : certains <b>passent sur le côté</b> et ressortent du radar. " +
                "Un ennemi qui passe à côté ne frappe pas la base, mais il peut tirer un missile en passant.\n\n" +
                "<b>REGISTRE</b> : liste triée de tous les codes, avec le camp (AMI / ENNEMI). " +
                "Il y a plus de codes que d'avions. Le radar, lui, ne dit jamais le camp." },

            new() { title = "TIR", body =
                "1. Ouvrir la <b>mallette Tir</b> une fois pour la partie : deux joueurs différents insèrent leur clé perso (E) à 2 s d'écart.\n" +
                "2. Au <b>pupitre de tir</b>, taper le code de tir dicté par le Radar (ex. K7).\n" +
                "3. Tourner la <b>clé 1</b> puis la <b>clé 2</b> : le missile part et touche la cible quelques secondes plus tard.\n\n" +
                "Un code qui ne correspond à aucun écho bloque le pupitre quelques secondes (plus long en difficulté élevée).\n" +
                "Abattre un avion AMI déclenche un appel de sanction du commandement." },

            new() { title = "ORDRES NUCLÉAIRES", body =
                "Le commandement appelle parfois : « Ordre de lancement nucléaire ».\n\n" +
                "<b>4 joueurs</b> : un <b>vrai</b> ordre appelle les DEUX bunkers (le 2e au plus 15 s après le 1er). " +
                "Un <b>faux</b> ordre n'appelle qu'un seul bunker. Le texte est identique : appelez l'autre bunker pour comparer !\n" +
                "<b>2 joueurs</b> : il n'y a qu'un téléphone, donc <b>tous les ordres sont vrais</b>.\n\n" +
                "Pour lancer : ouvrir la mallette de l'équipe, puis les 2 joueurs d'un bunker tournent les serrures A et B " +
                "ensemble (1 s d'écart max) = 1 vote. Les deux équipes doivent voter à 30 s d'écart au plus. " +
                "Vous avez environ une minute après le 2e appel." },

            new() { title = "TÉLÉPHONE", body =
                "Décrocher (E sur le téléphone) appelle automatiquement l'autre bunker, ou répond si ça sonne.\n" +
                "Seul celui qui tient le combiné entend et parle. Raccrocher puis redécrocher pour rappeler.\n" +
                "Un appel du commandement pendant une conversation fait un double appel : raccrochez vite pour le prendre, " +
                "sinon il est manqué.\n" +
                "Bunker commun : un seul téléphone pour les deux lignes ; décrocher prend la ligne qui sonne depuis le plus longtemps." },

            new() { title = "FUSIBLES", body =
                "Quand il se passe trop ou trop peu de choses, un fusible peut sauter : lumière rouge, postes éteints, " +
                "téléphone hors service.\n" +
                "Allez au <b>boîtier à fusibles</b> (mur opposé au radar) et réussissez le petit jeu (aiguille ou fils) pour rétablir le courant.\n" +
                "Pendant une panne, appeler ce bunker ne donne qu'un bip discret." },

            new() { title = "COMMANDES", body =
                "<b>ZQSD / WASD</b> : se déplacer      <b>Maj</b> : courir\n" +
                "<b>Souris</b> : regarder      <b>E</b> : utiliser un poste\n" +
                "<b>Échap</b> : fermer l'écran d'un poste / libérer la souris\n" +
                "<b>F11</b> : plein écran\n\n" +
                "<b>Difficulté</b> (choisie par l'hôte dans le lobby) : Recrue, Normal, Difficile, Expert, ou Perso pour tout régler." },
        };
    }
}
