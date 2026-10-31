using System;
using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Téléphone : délais, sonneries et tonalités du combiné.</summary>
    [Serializable]
    public class PhoneSettings
    {
        [Tooltip("Durée de sonnerie avant que l'appel soit compté comme manqué (s). Vaut aussi pour le double appel.")]
        [Min(1f)] public float ringTimeout = 12f;

        [Tooltip("Durée d'affichage d'un message du jeu (ordre, sanction) avant que le correspondant raccroche (s).")]
        [Min(1f)] public float gameMessageDuration = 10f;

        [Tooltip("Ce qu'entend l'équipe valide en appelant l'équipe en panne.")]
        public DeadLineSound deadLineSound = DeadLineSound.BipDiscret;

        [Header("Voix (communication entre bunkers)")]
        [Tooltip("Vivox = vraie voix (Unity Gaming Services, Internet). Factice = aucun son (tests hors ligne).")]
        public PhoneVoiceBackend voiceBackend = PhoneVoiceBackend.Vivox;
        [Tooltip("Voix dans le bunker : les joueurs d'un même bunker s'entendent (jamais ceux de l'autre bunker).")]
        public bool bunkerVoiceChat = true;
        [Tooltip("Préfixe du canal vocal ; l'identifiant unique de la partie est ajouté derrière.")]
        public string voiceChannelPrefix = "np";

        [Header("Sonneries (entendues dans le bunker, autour du téléphone)")]
        public PhoneTone teamCallRing = new() { freqA = 440f, freqB = 480f, onTime = 0.4f, gapTime = 0.2f, bursts = 2, offTime = 2f, volume = 0.5f };
        public PhoneTone nuclearOrderRing = new() { freqA = 880f, freqB = 0f, onTime = 0.15f, gapTime = 0.1f, bursts = 4, offTime = 1f, volume = 0.6f };
        public PhoneTone sanctionRing = new() { freqA = 330f, freqB = 0f, onTime = 1f, gapTime = 0f, bursts = 1, offTime = 1.5f, volume = 0.5f };

        [Tooltip("Distance max à laquelle on entend sonner (m). Doit rester < distance entre bunkers.")]
        [Min(1f)] public float ringMaxDistance = 15f;

        [Header("Tonalités du combiné (seul celui qui le tient les entend)")]
        public PhoneTone dialTone = new() { freqA = 350f, freqB = 440f, onTime = 1f, gapTime = 0f, bursts = 1, offTime = 0f, volume = 0.25f };
        public PhoneTone ringbackTone = new() { freqA = 440f, freqB = 480f, onTime = 1f, gapTime = 0f, bursts = 1, offTime = 3f, volume = 0.25f };
        public PhoneTone busyTone = new() { freqA = 480f, freqB = 620f, onTime = 0.5f, gapTime = 0f, bursts = 1, offTime = 0.5f, volume = 0.25f };
        public PhoneTone deadLineBeep = new() { freqA = 1000f, freqB = 0f, onTime = 0.08f, gapTime = 0f, bursts = 1, offTime = 4f, volume = 0.12f };
        public PhoneTone callWaitingBeep = new() { freqA = 440f, freqB = 0f, onTime = 0.2f, gapTime = 0.2f, bursts = 2, offTime = 3f, volume = 0.3f };
    }

    /// <summary>
    /// Motif sonore généré par le jeu (en attendant de vrais sons) : « bursts » bips de « onTime »
    /// séparés par « gapTime », puis un silence « offTime », en boucle. Un clip assigné remplace le motif.
    /// </summary>
    [Serializable]
    public class PhoneTone
    {
        public AudioClip clipOverride;
        [Min(0f)] public float freqA = 440f;
        [Tooltip("Seconde fréquence superposée (0 = aucune).")]
        [Min(0f)] public float freqB;
        [Min(0.01f)] public float onTime = 0.5f;
        [Min(0f)] public float gapTime;
        [Min(1)] public int bursts = 1;
        [Min(0f)] public float offTime = 1f;
        [Range(0f, 1f)] public float volume = 0.5f;
    }
}
