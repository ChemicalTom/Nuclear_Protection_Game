using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Vraie voix via Unity Vivox (Unity Gaming Services). Connexion anonyme au premier appel,
    /// puis un canal audio par ligne. Les opérations réseau sont asynchrones : Connect/Disconnect
    /// donnent l'état VOULU, une seule tâche à la fois le rattrape. En cas d'échec (pas d'Internet,
    /// Vivox non activé sur le projet), la partie continue sans voix et Status l'indique.
    /// </summary>
    public class VivoxPhoneVoice : IPhoneVoice
    {
        private readonly string _displayName;
        private string _wanted;   // canal voulu (null = aucun)
        private string _joined;   // canal réellement rejoint
        private bool _busy, _failed;

        /// <summary>Texte court pour l'écran du téléphone (null = tout va bien).</summary>
        public string Status { get; private set; }

        public bool IsLineOpen => _wanted != null;
        public bool IsMuted { get; private set; }

        public VivoxPhoneVoice(string displayName) => _displayName = displayName;

        public void Connect(string lineId)
        {
            _wanted = lineId;
            IsMuted = false;
            _ = Sync();
        }

        public void Disconnect()
        {
            _wanted = null;
            _ = Sync();
        }

        public void Mute()
        {
            IsMuted = true;
            if (IsReady) VivoxService.Instance.MuteInputDevice();
        }

        public void Unmute()
        {
            IsMuted = false;
            if (IsReady) VivoxService.Instance.UnmuteInputDevice();
        }

        private static bool IsReady => UnityServices.State == ServicesInitializationState.Initialized && VivoxService.Instance.IsLoggedIn;

        // Rattrape l'état voulu (rejoindre / quitter / changer de canal), une étape à la fois.
        private async Task Sync()
        {
            if (_busy || _failed) return;
            _busy = true;
            try
            {
                while (_joined != _wanted)
                {
                    if (_joined != null)
                    {
                        await VivoxService.Instance.LeaveChannelAsync(_joined);
                        _joined = null;
                        continue;
                    }
                    await EnsureLoggedIn();
                    string target = _wanted;
                    Status = "Connexion de la ligne…";
                    await VivoxService.Instance.JoinGroupChannelAsync(target, ChatCapability.AudioOnly);
                    _joined = target;
                    Status = null;
                    if (IsMuted) VivoxService.Instance.MuteInputDevice();
                    else VivoxService.Instance.UnmuteInputDevice();
                    Debug.Log($"[Voix Vivox] Ligne ouverte ({target}).");
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

        private async Task EnsureLoggedIn()
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
