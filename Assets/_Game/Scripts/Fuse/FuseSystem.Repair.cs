using NuclearProtection.Core;
using NuclearProtection.Player;
using NuclearProtection.Rooms;
using Unity.Netcode;

namespace NuclearProtection.Fuse
{
    /// <summary>
    /// Réparation du fusible au boîtier de l'équipe. Les clients jouent le mini-jeu et envoient
    /// chaque tentative ; le serveur tient la progression (visible par les coéquipiers).
    /// </summary>
    public partial class FuseSystem
    {
        /// <summary>Aiguille : le joueur a appuyé (hit = l'aiguille était dans la zone verte de son écran).</summary>
        [Rpc(SendTo.Server)]
        public void NeedlePressRpc(TeamId team, bool hit, RpcParams rpc = default)
        {
            if (!CanRepair(team, FuseMiniGame.Aiguille, rpc.Receive.SenderClientId, out var st)) return;
            st.Progress = hit ? st.Progress + 1 : 0;
            if (st.Progress >= S.needleSuccessesRequired) { Repair(team); return; }
            Var(team).Value = st;
        }

        /// <summary>Fils : le joueur branche le fil « wire » sur la prise « socket ».</summary>
        [Rpc(SendTo.Server)]
        public void ConnectWireRpc(TeamId team, int wire, int socket, RpcParams rpc = default)
        {
            if (!CanRepair(team, FuseMiniGame.Fils, rpc.Receive.SenderClientId, out var st)) return;
            int count = S.EffectiveWireCount;
            if (wire < 0 || wire >= count || socket < 0 || socket >= count || FuseMiniGames.IsWireConnected(st.Progress, wire)) return;

            if (FuseMiniGames.WireSocketOrder(st.Seed, count)[socket] == wire) st.Progress |= 1 << wire;
            else if (S.wireMistakeResets) st.Progress = 0;

            if (st.Progress == FuseMiniGames.AllWiresMask(count)) { Repair(team); return; }
            Var(team).Value = st;
        }

        // Il faut que le fusible ait sauté, que ce soit le bon mini-jeu et que le joueur soit au boîtier de l'équipe.
        private bool CanRepair(TeamId team, FuseMiniGame game, ulong clientId, out FuseState state)
        {
            state = State(team);
            return state.Blown && state.Game == game && IsAtFuseBox(clientId, team);
        }

        private bool IsAtFuseBox(ulong clientId, TeamId team)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null) return false;
            var interactor = client.PlayerObject.GetComponent<PlayerInteractor>();
            foreach (var s in StationRegistry.All)
                if (s.Type == StationType.Fusible && s.Team == team && interactor.IsInRangeOf(s)) return true;
            return false;
        }
    }
}
