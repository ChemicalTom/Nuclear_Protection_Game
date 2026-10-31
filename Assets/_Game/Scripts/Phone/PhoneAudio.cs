using System.Collections.Generic;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Player;
using NuclearProtection.Rooms;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Client : fait sonner les téléphones (son 3D, entendu seulement près du poste) et joue
    /// dans le combiné du joueur local les tonalités de sa ligne (tonalité, occupé, double appel…).
    /// </summary>
    public class PhoneAudio : MonoBehaviour
    {
        /// <summary>Texte d'aide pour le HUD quand un téléphone sonne à portée d'oreille (null sinon).</summary>
        public static string RingingHint { get; private set; }

        private readonly Dictionary<Station, AudioSource> _ringers = new();
        private AudioSource _earpiece, _waiting;

        private static PhoneSettings S => ConnectionManager.Instance.Config.phone;

        private void Awake()
        {
            _earpiece = Create2DSource("Combiné");
            _waiting = Create2DSource("DoubleAppel");
        }

        private AudioSource Create2DSource(string sourceName)
        {
            var src = new GameObject(sourceName).AddComponent<AudioSource>();
            src.transform.SetParent(transform, false);
            src.loop = true;
            src.spatialBlend = 0f;
            return src;
        }

        private void Update()
        {
            var phones = PhoneSystem.Instance;
            if (phones == null || !phones.IsSpawned) { RingingHint = null; return; }

            RingingHint = null;
            foreach (var station in StationRegistry.All)
                if (station.Type == StationType.Telephone) UpdateRinger(station, phones.RingingLineFor(station.Team));

            UpdateEarpiece(phones);
        }

        // ---- Sonnerie dans la pièce ----

        private void UpdateRinger(Station station, PhoneLine line)
        {
            if (!_ringers.TryGetValue(station, out var src) || src == null)
            {
                src = station.gameObject.AddComponent<AudioSource>();
                src.loop = true;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.maxDistance = S.ringMaxDistance;
                _ringers[station] = src;
            }

            bool ringing = line.Mode == PhoneMode.Sonne;
            Play(src, ringing ? RingTone(line.RingKind) : null);

            if (ringing && IsLocalPlayerNear(station))
                RingingHint = $"Le téléphone ({station.Team}) sonne !";
        }

        private static PhoneTone RingTone(CallKind kind) => kind switch
        {
            CallKind.OrdreNucleaire => S.nuclearOrderRing,
            CallKind.Sanction => S.sanctionRing,
            _ => S.teamCallRing
        };

        private static bool IsLocalPlayerNear(Station station) =>
            PlayerInteractor.Local != null
            && Vector3.Distance(PlayerInteractor.Local.transform.position, station.transform.position) <= S.ringMaxDistance;

        // ---- Combiné du joueur local ----

        private void UpdateEarpiece(PhoneSystem phones)
        {
            ulong me = NetworkManager.Singleton.LocalClientId;
            PhoneLine? mine = null;
            foreach (var team in new[] { TeamId.Radar, TeamId.Tir })
                if (phones.Line(team).Holder == me) mine = phones.Line(team);

            Play(_earpiece, mine.HasValue ? EarpieceTone(mine.Value.Mode) : null);
            Play(_waiting, mine.HasValue && mine.Value.Waiting != CallKind.Aucun ? S.callWaitingBeep : null);
        }

        private static PhoneTone EarpieceTone(PhoneMode mode) => mode switch
        {
            PhoneMode.Tonalite => S.dialTone,
            PhoneMode.Appel => S.ringbackTone,
            PhoneMode.Occupe => S.busyTone,
            PhoneMode.LigneMorte => S.deadLineSound == DeadLineSound.BipDiscret ? S.deadLineBeep : null,
            _ => null // en communication ou message : la voix (IPhoneVoice / message du jeu)
        };

        private static void Play(AudioSource src, PhoneTone tone)
        {
            var clip = PhoneToneLibrary.Get(tone);
            if (clip == null) { if (src.isPlaying) src.Stop(); src.clip = null; return; }
            if (src.clip == clip && src.isPlaying) return;
            src.clip = clip;
            src.Play();
        }
    }
}
