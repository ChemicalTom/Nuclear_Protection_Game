using System;
using System.Collections.Generic;
using NuclearProtection.Calls;
using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Keys
{
    /// <summary>
    /// Mallettes et lancement nucléaire (autorité serveur).
    /// - Mallette : deux joueurs différents insèrent leur clé perso (E) à 2 s d'écart max → elle s'ouvre
    ///   pour le reste de la partie et les serrures de l'équipe deviennent actives.
    /// - Nucléaire : deux joueurs tournent les 2 serrures d'une équipe à 1 s d'écart max (réglable) = 1 vote,
    ///   valable 30 s. Quand les deux équipes ont un vote valable, le missile part.
    /// </summary>
    public class KeySystem : NetworkBehaviour
    {
        public static KeySystem Instance { get; private set; }

        /// <summary>Serveur : lancement nucléaire effectué (legitimate = un vrai ordre était en cours).</summary>
        public static event Action<bool> ServerNuclearLaunched;

        private readonly NetworkVariable<bool> _radarOpen = new();
        private readonly NetworkVariable<bool> _tirOpen = new();
        private readonly NetworkVariable<double> _radarVoteUntil = new(-1d);
        private readonly NetworkVariable<double> _tirVoteUntil = new(-1d);

        private struct Turn { public ulong Client; public double Time; }

        // ---- État serveur uniquement ----
        private readonly Dictionary<TeamId, List<Turn>> _personalKeys = new() { [TeamId.Radar] = new(), [TeamId.Tir] = new() };
        private readonly Dictionary<(TeamId, bool), Turn?> _lockTurns = new();

        private static KeySettings S => ConnectionManager.Instance.Config.keys;
        private double Now => NetworkManager.ServerTime.Time;

        public bool IsBriefcaseOpen(TeamId team) => (team == TeamId.Radar ? _radarOpen : _tirOpen).Value;
        private NetworkVariable<bool> OpenVar(TeamId team) => team == TeamId.Radar ? _radarOpen : _tirOpen;
        private NetworkVariable<double> VoteVar(TeamId team) => team == TeamId.Radar ? _radarVoteUntil : _tirVoteUntil;
        private bool HasValidVote(TeamId team) => VoteVar(team).Value >= Now;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (IsServer) StationRegistry.ServerInteracted += OnServerStationInteracted;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) StationRegistry.ServerInteracted -= OnServerStationInteracted;
            if (Instance == this) Instance = null;
        }

        private void OnServerStationInteracted(Station station, ulong clientId, bool pressed)
        {
            if (!pressed) return;
            if (station.Type == StationType.Mallette) InsertPersonalKey(station.Team, clientId);
            else if (station.Type == StationType.SerrureNucA) TurnNuclearLock(station.Team, true, clientId);
            else if (station.Type == StationType.SerrureNucB) TurnNuclearLock(station.Team, false, clientId);
        }

        // ---------- Mallette ----------

        private void InsertPersonalKey(TeamId team, ulong clientId)
        {
            if (IsBriefcaseOpen(team)) { Notify("La mallette est déjà ouverte.", clientId); return; }

            // On garde les clés perso récentes, une seule par joueur.
            var keys = _personalKeys[team];
            keys.RemoveAll(k => Now - k.Time > S.briefcaseOpenTolerance || k.Client == clientId);
            keys.Add(new Turn { Client = clientId, Time = Now });

            if (keys.Count >= S.personalKeysToOpen)
            {
                OpenVar(team).Value = true;
                Debug.Log($"[Clés] Mallette {team} ouverte.");
                Notify($"Mallette {team} ouverte : les clés de lancement sont disponibles.", ClientsOf(keys));
                keys.Clear();
            }
            else
            {
                int missing = S.personalKeysToOpen - keys.Count;
                Notify($"Clé perso insérée. Encore {missing} clé(s) d'un autre joueur dans les {S.briefcaseOpenTolerance:0.#} s.", clientId);
            }
        }

        // ---------- Serrures nucléaires ----------

        private void TurnNuclearLock(TeamId team, bool isA, ulong clientId)
        {
            if (Fuse.FuseSystem.Instance != null && Fuse.FuseSystem.Instance.IsBlackedOut(team)) return; // serrure électrique : sans courant, rien ne se passe
            if (!IsBriefcaseOpen(team)) { Notify($"Serrure inactive : la mallette {team} est fermée.", clientId); return; }
            if (HasValidVote(team)) { Notify($"Le vote du bunker {team} est déjà enregistré.", clientId); return; }

            var turn = new Turn { Client = clientId, Time = Now };
            _lockTurns[(team, isA)] = turn;
            _lockTurns.TryGetValue((team, !isA), out var other);

            bool together = other.HasValue && Math.Abs(turn.Time - other.Value.Time) <= S.nuclearSimultaneityTolerance;
            if (together && other.Value.Client == clientId)
            {
                Notify("Il faut deux personnes : une à chaque serrure.", clientId);
            }
            else if (together)
            {
                _lockTurns[(team, true)] = _lockTurns[(team, false)] = null;
                Vote(team, clientId, other.Value.Client);
            }
            else Notify("Serrure tournée. L'autre serrure doit être tournée EN MÊME TEMPS.", clientId);
        }

        private void Vote(TeamId team, params ulong[] voters)
        {
            VoteVar(team).Value = Now + S.bunkerVoteWindow;
            Debug.Log($"[Clés] Vote nucléaire du bunker {team}.");

            var otherTeam = team == TeamId.Radar ? TeamId.Tir : TeamId.Radar;
            if (HasValidVote(otherTeam)) Launch();
            else Notify($"Vote du bunker {team} enregistré. L'autre bunker a {S.bunkerVoteWindow:0} s pour voter.", voters);
        }

        private void Launch()
        {
            _radarVoteUntil.Value = _tirVoteUntil.Value = -1d;

            NuclearOrder order = null;
            bool legitimate = CallDirector.Instance != null && CallDirector.Instance.TryGetActiveTrueOrder(out order);
            if (legitimate) order.Launched = true;
            SpoilerLog.Log($"[Clés] LANCEMENT NUCLÉAIRE ({(legitimate ? "vrai ordre en cours" : "SANS vrai ordre")}).");
            NotifyAll("LANCEMENT NUCLÉAIRE EFFECTUÉ.");
            ServerNuclearLaunched?.Invoke(legitimate);
        }

        // ---------- Retour visuel (tous les clients) ----------

        private void Update()
        {
            if (!IsSpawned) return;

            // Serveur : un vote expiré redevient « pas de vote ».
            if (IsServer)
                foreach (var team in new[] { TeamId.Radar, TeamId.Tir })
                    if (VoteVar(team).Value >= 0d && VoteVar(team).Value < Now) VoteVar(team).Value = -1d;

            foreach (var s in StationRegistry.All)
            {
                if (s.Type == StationType.Mallette)
                    s.SetStateColor(IsBriefcaseOpen(s.Team) ? S.briefcaseOpenColor : null);
                else if (s.Type is StationType.SerrureNucA or StationType.SerrureNucB)
                    s.SetStateColor(VoteVar(s.Team).Value >= NetworkManager.ServerTime.Time ? S.nuclearVoteColor : null);
            }
        }

        // ---------- Messages courts ----------

        private static ulong[] ClientsOf(List<Turn> turns)
        {
            var ids = new ulong[turns.Count];
            for (int i = 0; i < turns.Count; i++) ids[i] = turns[i].Client;
            return ids;
        }

        private void Notify(string text, params ulong[] clients) =>
            ToastRpc(new FixedString512Bytes(text), RpcTarget.Group(clients, RpcTargetUse.Temp));

        private void NotifyAll(string text) => ToastRpc(new FixedString512Bytes(text), RpcTarget.Everyone);

        [Rpc(SendTo.SpecifiedInParams)]
        private void ToastRpc(FixedString512Bytes text, RpcParams rpc)
        {
            UI.HudUI.Toast(text.ToString(), ConnectionManager.Instance.Config.display.toastDuration);
        }
    }
}
