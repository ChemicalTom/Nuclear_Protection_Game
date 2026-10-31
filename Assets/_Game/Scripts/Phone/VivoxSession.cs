using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Connexion Vivox partagée (local) : connexion anonyme au premier besoin, puis on rejoint/quitte
    /// les canaux voulus (canal du bunker + ligne téléphonique). Le micro parle dans tous les canaux rejoints.
    /// Les opérations réseau sont asynchrones : Want() donne l'état VOULU, une seule tâche le rattrape.
    /// En cas d'échec (pas d'Internet, Vivox non activé), la partie continue sans voix et Status l'indique.
    /// </summary>
    public static class VivoxSession
    {
        private static readonly HashSet<string> Wanted = new();
        private static readonly HashSet<string> Joined = new();
        private static bool _busy, _failed, _micMuted;
        private static string _displayName = "Joueur";

        /// <summary>Texte court à afficher (null = tout va bien).</summary>
        public static string Status { get; private set; }

        public static bool IsJoined(string channel) => Joined.Contains(channel);

        /// <summary>Rejoindre (on = true) ou quitter un canal.</summary>
        public static void Want(string channel, bool on, string displayName)
        {
            if (string.IsNullOrEmpty(channel)) return;
            if (!string.IsNullOrEmpty(displayName)) _displayName = displayName;
            bool changed = on ? Wanted.Add(channel) : Wanted.Remove(channel);
            if (changed) _ = Sync();
        }

        public static void SetMicMuted(bool muted)
        {
            _micMuted = muted;
            if (!IsReady) return;
            if (muted) VivoxService.Instance.MuteInputDevice();
            else VivoxService.Instance.UnmuteInputDevice();
        }

        private static bool IsReady => UnityServices.State == ServicesInitializationState.Initialized && VivoxService.Instance.IsLoggedIn;

        // Rattrape l'état voulu, un canal à la fois (quitter d'abord, puis rejoindre).
        private static async Task Sync()
        {
            if (_busy || _failed) return;
            _busy = true;
            try
            {
                while (TryNextChange(out string channel, out bool join))
                {
                    if (!join)
                    {
                        await VivoxService.Instance.LeaveChannelAsync(channel);
                        Joined.Remove(channel);
                        continue;
                    }
                    await EnsureLoggedIn();
                    Status = "Connexion de la voix…";
                    await VivoxService.Instance.JoinGroupChannelAsync(channel, ChatCapability.AudioOnly);
                    Joined.Add(channel);
                    await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All);
                    SetMicMuted(_micMuted);
                    Status = null;
                    Debug.Log($"[Voix Vivox] Canal rejoint ({channel}).");
                }
            }
            catch (Exception e)
            {
                _failed = true; // on n'insiste pas : la partie continue sans voix
                Status = "Voix indisponible (vérifiez Internet / Vivox).";
                Debug.LogWarning($"[Voix Vivox] Échec : {e.Message}");
            }
            finally { _busy = false; }
        }

        private static bool TryNextChange(out string channel, out bool join)
        {
            foreach (var c in Joined)
                if (!Wanted.Contains(c)) { channel = c; join = false; return true; }
            foreach (var c in Wanted)
                if (!Joined.Contains(c)) { channel = c; join = true; return true; }
            channel = null;
            join = false;
            return false;
        }

        private static async Task EnsureLoggedIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // Profil distinct par instance : plusieurs joueurs sur la même machine (tests) ne se confondent pas.
                var options = new InitializationOptions().SetProfile($"np{Guid.NewGuid():N}".Substring(0, 30));
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            if (!VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.InitializeAsync();
                await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = _displayName });
            }
        }
    }
}
