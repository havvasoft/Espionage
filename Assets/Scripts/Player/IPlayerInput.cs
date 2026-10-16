using System;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Player input abstraction so movement/look/interaction do not depend on the Input System.</summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }

        /// <summary>Raw look delta for this frame.</summary>
        Vector2 Look { get; }

        bool SprintHeld { get; }

        /// <summary>True only on the frame the jump button was pressed.</summary>
        bool JumpPressedThisFrame { get; }

        event Action InteractPerformed;

        /// <summary>Raised once when the player presses the companion command button.</summary>
        event Action CommandPerformed;
    }
}
