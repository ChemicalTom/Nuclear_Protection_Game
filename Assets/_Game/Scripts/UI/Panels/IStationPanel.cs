using NuclearProtection.Core;
using NuclearProtection.Rooms;
using UnityEngine;

namespace NuclearProtection.UI.Panels
{
    /// <summary>Écran d'un poste (radar, registre…), dessiné en IMGUI dans la zone fournie.</summary>
    public interface IStationPanel
    {
        StationType Type { get; }
        string Title { get; }
        void OnOpen(Station station);
        void Draw(Rect area);

        /// <summary>Appelé à la fermeture de l'écran (Échap, bouton, éloignement).</summary>
        void OnClose();
    }
}
