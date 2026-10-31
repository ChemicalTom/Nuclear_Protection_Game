using System;
using NuclearProtection.Config;
using UnityEngine.InputSystem;

namespace NuclearProtection.Player
{
    /// <summary>Actions d'entrée construites à partir des touches de la config.</summary>
    public sealed class PlayerControls : IDisposable
    {
        public readonly InputAction Move, Look, Sprint, Interact, ToggleCursor;

        public PlayerControls(PlayerControlSettings s)
        {
            Move = new InputAction("Move");
            Move.AddCompositeBinding("2DVector")
                .With("Up", s.moveUp).With("Down", s.moveDown)
                .With("Left", s.moveLeft).With("Right", s.moveRight);

            Look = new InputAction("Look", binding: s.look);
            Sprint = new InputAction("Sprint", binding: s.sprint);
            Interact = new InputAction("Interact", binding: s.interact);
            ToggleCursor = new InputAction("ToggleCursor", binding: s.toggleCursor);

            foreach (var a in All()) a.Enable();
        }

        public void Dispose()
        {
            foreach (var a in All()) { a.Disable(); a.Dispose(); }
        }

        private InputAction[] All() => new[] { Move, Look, Sprint, Interact, ToggleCursor };
    }
}
