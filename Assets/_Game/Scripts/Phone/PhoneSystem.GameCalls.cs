using System;
using System.Collections.Generic;
using NuclearProtection.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Appels du jeu (ordres nucléaires, sanctions) : file par équipe, sonnerie, double appel, message.
    /// Le contenu et le moment des appels seront décidés par le système d'appels (étape 7).
    /// </summary>
    public partial class PhoneSystem
    {
        private class GameCall
        {
            public CallKind Kind;
            public string Message;
            public Action<bool> OnResult; // true = décroché à temps
            public bool Presented;        // déjà en train de sonner (ou en double appel)
        }

        private readonly Dictionary<TeamId, Queue<GameCall>> _calls = new()
        {
            [TeamId.Radar] = new Queue<GameCall>(),
            [TeamId.Tir] = new Queue<GameCall>()
        };
        private readonly Dictionary<TeamId, double> _messageEnd = new() { [TeamId.Radar] = -1d, [TeamId.Tir] = -1d };

        /// <summary>Message reçu par le joueur qui a décroché (lui seul « entend » l'appel).</summary>
        public static event Action<CallKind, string, float> LocalMessageReceived;

        /// <summary>
        /// Serveur : fait sonner le téléphone d'une équipe pour un appel du jeu.
        /// onResult(true) si quelqu'un décroche à temps, (false) si l'appel est manqué ou si l'équipe est en panne.
        /// </summary>
        public void ServerPlaceGameCall(TeamId team, CallKind kind, string message, Action<bool> onResult = null)
        {
            _calls[team].Enqueue(new GameCall { Kind = kind, Message = message, OnResult = onResult });
        }

        /// <summary>Serveur : appels du jeu qui sonnent ou attendent pour l'équipe (charge du fusible).</summary>
        public int PendingGameCalls(TeamId team) => _calls[team].Count;

        private void UpdateGameCalls(TeamId team)
        {
            var line = Line(team);

            // Fin du message : le correspondant raccroche.
            if (_messageEnd[team] > 0d && Now >= _messageEnd[team])
            {
                _messageEnd[team] = -1d;
                if (line.Mode == PhoneMode.Message)
                {
                    line.Mode = PhoneMode.Tonalite;
                    line.Info = "Le correspondant a raccroché.";
                    Set(team, line);
                }
            }

            if (_calls[team].Count == 0) return;
            var call = _calls[team].Peek();
            if (call.Presented) return;

            if (IsBlackedOut(team)) { MissGameCall(team); return; } // l'équipe en panne ne reçoit pas l'appel

            if (line.Mode == PhoneMode.Raccroche)
            {
                line.Mode = PhoneMode.Sonne;
                line.RingKind = call.Kind;
                line.RingStart = Now;
            }
            else if (line.IsOffHook && line.Mode != PhoneMode.Message && line.Waiting == CallKind.Aucun)
            {
                line.Waiting = call.Kind; // double appel : il faut raccrocher pour le prendre
                line.WaitingStart = Now;
            }
            else return; // le téléphone sonne déjà ou un message est en cours : on attend
            call.Presented = true;
            Set(team, line);
        }

        private void AnswerGameCall(TeamId team, ulong clientId)
        {
            if (_calls[team].Count == 0) return;
            var call = _calls[team].Dequeue();
            _messageEnd[team] = Now + S.gameMessageDuration;
            Debug.Log($"[Téléphone] {team} a décroché : {call.Kind}.");
            DeliverMessageRpc(call.Kind, call.Message, S.gameMessageDuration, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            call.OnResult?.Invoke(true);
        }

        private void MissGameCall(TeamId team)
        {
            if (_calls[team].Count == 0) return;
            var call = _calls[team].Dequeue();
            Debug.Log($"[Téléphone] Appel {call.Kind} manqué par {team}.");
            call.OnResult?.Invoke(false);
        }

        private void EndGameMessage(TeamId team) => _messageEnd[team] = -1d;

        [Rpc(SendTo.SpecifiedInParams)]
        private void DeliverMessageRpc(CallKind kind, FixedString512Bytes message, float duration, RpcParams rpc)
        {
            LocalMessageReceived?.Invoke(kind, message.ToString(), duration);
        }

        // ---------- Tests depuis l'inspecteur (Play Mode, hôte) ----------

        [ContextMenu("Test : ordre nucléaire → Radar")]
        private void TestOrderRadar() => ServerPlaceGameCall(TeamId.Radar, CallKind.OrdreNucleaire, "[TEST] Ordre nucléaire de test.");

        [ContextMenu("Test : sanction → Tir")]
        private void TestSanctionTir() => ServerPlaceGameCall(TeamId.Tir, CallKind.Sanction, "[TEST] Un avion ami a été abattu.");
    }
}
