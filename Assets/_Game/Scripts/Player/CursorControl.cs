using UnityEngine;

namespace NuclearProtection.Player
{
    /// <summary>Verrouille (jeu) ou libère (menus, panneaux) le curseur de la souris.</summary>
    public static class CursorControl
    {
        public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        public static void SetLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
