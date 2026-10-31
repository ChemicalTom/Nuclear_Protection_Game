using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Affichage : plein écran et taille de l'interface.</summary>
    [Serializable]
    public class DisplaySettings
    {
        [Tooltip("Touche pour basculer plein écran / fenêtre (chemin Input System).")]
        public string toggleFullscreen = "<Keyboard>/f11";

        [Tooltip("Hauteur d'écran (px) pour laquelle l'interface a la taille « uiScaleAtReference ».")]
        [Min(240)] public int referenceHeight = 720;

        [Tooltip("Taille de l'interface à la hauteur de référence. Elle grandit avec la résolution.")]
        [Range(0.5f, 4f)] public float uiScaleAtReference = 1.5f;

        [Tooltip("Taille minimale de l'interface (petites fenêtres).")]
        [Range(0.5f, 4f)] public float minUiScale = 1f;

        [Tooltip("Durée d'affichage des messages courts à l'écran (« Mallette ouverte »…) (s).")]
        [Min(0.5f)] public float toastDuration = 4f;
    }
}
