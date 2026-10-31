using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Fuse;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using Unity.Netcode;

namespace NuclearProtection.Phone
{
    /// <summary>
    /// Téléphones des deux équipes (autorité serveur). On décroche au poste Téléphone (touche E) :
    /// s'il sonne, on répond ; sinon, ça appelle automatiquement l'autre bunker.
    /// L'écran du téléphone reste ouvert tant qu'on tient le combiné, le fermer = raccrocher.
    /// Cette partie gère les lignes et les appels entre bunkers ; les appels du jeu sont dans PhoneSystem.GameCalls.
    /// </summary>
    public partial class PhoneSystem : NetworkBehaviour
    {
        public static PhoneSystem Instance { get; private set; }

        private readonly NetworkVariable<PhoneLine> _radarLine = new(PhoneLine.Idle);
        private readonly NetworkVariable<PhoneLine> _tirLine = new(PhoneLine.Idle);

        private static PhoneSettings S => ConnectionManager.Instance.Config.phone;
        private double Now => NetworkManager.ServerTime.Time;

        public PhoneLine Line(TeamId team) => Var(team).Value;
        private NetworkVariable<PhoneLine> Var(TeamId team) => team == TeamId.Radar ? _radarLine : _tirLine;
        private void Set(TeamId team, PhoneLine line) => Var(team).Value = line;
        private static TeamId Other(TeamId team) => team == TeamId.Radar ? TeamId.Tir : TeamId.Radar;
        private static readonly TeamId[] Teams = { TeamId.Radar, TeamId.Tir };

        public override void OnNetworkSpawn()
        {
            Instance = this;
            if (!IsServer) return;
            StationRegistry.ServerInteracted += OnServerStationInteracted;
            NetworkManager.OnClientDisconnectCallback += OnClientLeft;
            FuseSystem.ServerBlackoutStarted += CutForBlackout;
            FuseSystem.ServerRepaired += RestoreAfterRepair;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                StationRegistry.ServerInteracted -= OnServerStationInteracted;
                if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientLeft;
                FuseSystem.ServerBlackoutStarted -= CutForBlackout;
                FuseSystem.ServerRepaired -= RestoreAfterRepair;
            }
            if (Instance == this) Instance = null;
        }

        private static bool IsBlackedOut(TeamId team) => FuseSystem.Instance != null && FuseSystem.Instance.IsBlackedOut(team);

        // ---------- Décrocher ----------

        private void OnServerStationInteracted(Station station, ulong clientId, bool pressed)
        {
            if (pressed && station.Type == StationType.Telephone) PickUp(station.Team, clientId);
        }

        private void PickUp(TeamId team, ulong clientId)
        {
            if (SharedPhone && !PickSharedLine(ref team, clientId)) return;
            var line = Line(team);
            if (line.IsOffHook) return; // un coéquipier tient déjà le combiné

            line.Holder = clientId;
            line.Info = default;
            if (IsBlackedOut(team))
            {
                line.Mode = PhoneMode.HorsService; // notre panne : pas de tonalité, on ne peut appeler personne
                Set(team, line);
                return;
            }
            if (line.Mode == PhoneMode.Sonne && line.RingKind == CallKind.Equipe)
            {
                line.Mode = PhoneMode.Communication;
                var other = Line(Other(team));
                other.Mode = PhoneMode.Communication;
                Set(Other(team), other);
            }
            else if (line.Mode == PhoneMode.Sonne)
            {
                line.Mode = PhoneMode.Message;
                Set(team, line);
                AnswerGameCall(team, clientId);
                return;
            }
            else
            {
                // Personne ne nous appelle : décrocher appelle directement l'autre bunker.
                Dial(team, ref line);
                return;
            }
            Set(team, line);
        }

        // ---------- Bunker commun (2 joueurs) : un seul téléphone ----------

        /// <summary>Bunker commun : un seul téléphone porte les lignes des deux équipes (pas d'autre bunker à appeler).</summary>
        public static bool SharedPhone =>
            MatchSession.Instance != null && MatchSession.Instance.Layout.Value == BunkerLayout.UnBunkerCommun;

        // Choisit la ligne à prendre : celle qui sonne (la plus ancienne). Sinon, on décroche dans le vide.
        // Renvoie false si tout est déjà traité ici (combiné déjà pris, ou rien ne sonne).
        private bool PickSharedLine(ref TeamId team, ulong clientId)
        {
            if (Line(TeamId.Radar).IsOffHook || Line(TeamId.Tir).IsOffHook) return false; // un seul combiné
            if (TryGetRingingLine(out var ringing)) { team = ringing; return true; }

            var idle = Line(team);
            idle.Holder = clientId;
            idle.Mode = IsBlackedOut(team) ? PhoneMode.HorsService : PhoneMode.Tonalite;
            idle.Info = idle.Mode == PhoneMode.Tonalite ? "Aucun appel en attente." : default;
            Set(team, idle);
            return false;
        }

        private bool TryGetRingingLine(out TeamId team)
        {
            team = TeamId.Radar;
            bool found = false;
            foreach (var t in Teams)
            {
                var l = Line(t);
                if (l.Mode != PhoneMode.Sonne || (found && l.RingStart >= Line(team).RingStart)) continue;
                team = t;
                found = true;
            }
            return found;
        }

        /// <summary>Ligne qui fait sonner le poste téléphone d'une équipe (bunker commun : n'importe quelle ligne).</summary>
        public PhoneLine RingingLineFor(TeamId stationTeam) =>
            SharedPhone && TryGetRingingLine(out var t) ? Line(t) : Line(stationTeam);

        /// <summary>La ligne tenue par ce joueur, s'il tient un combiné.</summary>
        public bool TryGetHeldLine(ulong clientId, out TeamId team)
        {
            foreach (var t in Teams)
                if (Line(t).Holder == clientId) { team = t; return true; }
            team = TeamId.Radar;
            return false;
        }

        /// <summary>Bunker commun : l'autre ligne sonne pendant qu'on tient le combiné (il faut raccrocher pour la prendre).</summary>
        public bool OtherLineRinging(TeamId held) => SharedPhone && Line(Other(held)).Mode == PhoneMode.Sonne;

        // ---------- Appeler l'autre bunker ----------

        private void Dial(TeamId team, ref PhoneLine line)
        {
            var target = Line(Other(team));
            if (IsBlackedOut(Other(team))) line.Mode = PhoneMode.LigneMorte;
            else if (target.IsOffHook || target.Mode == PhoneMode.Sonne) line.Mode = PhoneMode.Occupe;
            else
            {
                line.Mode = PhoneMode.Appel;
                line.RingStart = Now;
                target.Mode = PhoneMode.Sonne;
                target.RingKind = CallKind.Equipe;
                target.RingStart = Now;
                Set(Other(team), target);
            }
            Set(team, line);
        }

        // ---------- Raccrocher ----------

        /// <summary>Le joueur qui tient le combiné raccroche (fermeture de l'écran du téléphone).</summary>
        [Rpc(SendTo.Server)]
        public void HangUpRpc(TeamId team, RpcParams rpc = default)
        {
            if (Line(team).Holder == rpc.Receive.SenderClientId) HangUp(team);
        }

        private void HangUp(TeamId team)
        {
            var line = Line(team);
            var previous = line.Mode;
            line.Holder = PhoneLine.NoHolder;
            line.Mode = PhoneMode.Raccroche;
            line.Info = default;

            // Double appel en attente : le téléphone se met à sonner (le temps de sonnerie continue).
            if (line.Waiting != CallKind.Aucun)
            {
                line.Mode = PhoneMode.Sonne;
                line.RingKind = line.Waiting;
                line.RingStart = line.WaitingStart;
                line.Waiting = CallKind.Aucun;
            }
            Set(team, line);

            var other = Line(Other(team));
            if (previous == PhoneMode.Communication && other.Mode == PhoneMode.Communication)
            {
                other.Mode = PhoneMode.Tonalite;
                other.Info = "Ligne coupée.";
                Set(Other(team), other);
            }
            else if (previous == PhoneMode.Appel && other.Mode == PhoneMode.Sonne && other.RingKind == CallKind.Equipe)
            {
                other.Mode = PhoneMode.Raccroche;
                Set(Other(team), other);
            }
            if (previous == PhoneMode.Message) EndGameMessage(team);
        }

        // ---------- Panne de fusible ----------

        // Le fusible saute : sonnerie, double appel et conversation en cours sont perdus.
        // Celui qui tient le combiné le garde, mais n'entend plus rien.
        private void CutForBlackout(TeamId team)
        {
            var line = Line(team);
            var previous = line.Mode;
            var other = Line(Other(team));

            if (line.Waiting != CallKind.Aucun)
            {
                line.Waiting = CallKind.Aucun;
                MissGameCall(team);
            }
            if (previous == PhoneMode.Sonne)
            {
                line.Mode = PhoneMode.Raccroche;
                if (line.RingKind != CallKind.Equipe) MissGameCall(team);
                else if (other.Mode == PhoneMode.Appel)
                {
                    other.Mode = PhoneMode.LigneMorte; // l'appelant ne sait pas que nous sommes en panne
                    Set(Other(team), other);
                }
            }
            if (line.IsOffHook)
            {
                line.Mode = PhoneMode.HorsService;
                line.Info = default;
            }
            Set(team, line);

            if (previous == PhoneMode.Communication && other.Mode == PhoneMode.Communication)
            {
                other.Mode = PhoneMode.Tonalite;
                other.Info = "Ligne coupée.";
                Set(Other(team), other);
            }
            else if (previous == PhoneMode.Appel && other.Mode == PhoneMode.Sonne && other.RingKind == CallKind.Equipe)
            {
                other.Mode = PhoneMode.Raccroche;
                Set(Other(team), other);
            }
            if (previous == PhoneMode.Message) EndGameMessage(team);
        }

        private void RestoreAfterRepair(TeamId team)
        {
            var line = Line(team);
            if (line.Mode != PhoneMode.HorsService) return;
            line.Mode = PhoneMode.Tonalite;
            line.Info = "Le courant est revenu.";
            Set(team, line);
        }

        private void OnClientLeft(ulong clientId)
        {
            foreach (var team in Teams)
                if (Line(team).Holder == clientId) HangUp(team);
        }

        // ---------- Boucle serveur ----------

        private void Update()
        {
            if (!IsServer) return;
            foreach (var team in Teams)
            {
                CheckRingTimeout(team);
                UpdateGameCalls(team);
            }
        }

        private void CheckRingTimeout(TeamId team)
        {
            var line = Line(team);
            if (line.Mode == PhoneMode.Sonne && Now - line.RingStart > S.ringTimeout)
            {
                line.Mode = PhoneMode.Raccroche;
                Set(team, line);
                if (line.RingKind != CallKind.Equipe) { MissGameCall(team); return; }

                var caller = Line(Other(team));
                if (caller.Mode != PhoneMode.Appel) return;
                caller.Mode = PhoneMode.Tonalite;
                caller.Info = "Pas de réponse.";
                Set(Other(team), caller);
            }
            else if (line.Waiting != CallKind.Aucun && Now - line.WaitingStart > S.ringTimeout)
            {
                line.Waiting = CallKind.Aucun;
                Set(team, line);
                MissGameCall(team);
            }
        }
    }
}
