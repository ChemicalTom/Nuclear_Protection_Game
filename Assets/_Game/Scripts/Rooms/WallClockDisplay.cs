using NuclearProtection.Core;
using UnityEngine;

namespace NuclearProtection.Rooms
{
    /// <summary>Affiche l'heure de la partie sur un TextMesh (horloge murale du bunker).</summary>
    [RequireComponent(typeof(TextMesh))]
    public class WallClockDisplay : MonoBehaviour
    {
        private TextMesh _text;

        private void Awake() => _text = GetComponent<TextMesh>();

        private void Update()
        {
            if (GameClock.Instance != null) _text.text = GameClock.Instance.Formatted;
        }
    }
}
