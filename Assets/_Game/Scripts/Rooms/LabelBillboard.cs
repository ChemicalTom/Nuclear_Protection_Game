using UnityEngine;

namespace NuclearProtection.Rooms
{
    /// <summary>Oriente une étiquette vers la caméra pour qu'elle reste lisible.</summary>
    public class LabelBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
