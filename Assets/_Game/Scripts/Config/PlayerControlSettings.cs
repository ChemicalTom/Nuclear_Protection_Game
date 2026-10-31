using System;
using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>Déplacement à la première personne, interaction et touches (chemins Input System).</summary>
    [Serializable]
    public class PlayerControlSettings
    {
        [Header("Déplacement")]
        [Min(0.5f)] public float walkSpeed = 3.5f;
        [Min(0.5f)] public float sprintSpeed = 5.5f;
        [Min(1f)] public float gravity = 20f;
        [Min(1f)] public float eyeHeight = 1.65f;

        [Header("Regard")]
        [Min(0.01f)] public float mouseSensitivity = 0.12f;
        [Range(30f, 90f)] public float maxPitch = 85f;

        [Header("Interaction")]
        [Tooltip("Portée de visée depuis les yeux (m).")]
        [Min(0.5f)] public float interactRange = 2.2f;
        [Tooltip("Marge acceptée par le serveur (latence, arrondis).")]
        [Min(0f)] public float serverRangeTolerance = 1f;
        [Tooltip("Durée du flash visuel quand le serveur valide une interaction (s).")]
        [Min(0f)] public float interactFeedbackDuration = 0.25f;

        [Header("Touches (chemins Input System)")]
        public string moveUp = "<Keyboard>/w";
        public string moveDown = "<Keyboard>/s";
        public string moveLeft = "<Keyboard>/a";
        public string moveRight = "<Keyboard>/d";
        public string look = "<Mouse>/delta";
        public string sprint = "<Keyboard>/leftShift";
        public string interact = "<Keyboard>/e";
        public string toggleCursor = "<Keyboard>/escape";
    }
}
