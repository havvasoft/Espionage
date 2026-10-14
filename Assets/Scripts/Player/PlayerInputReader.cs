using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Espionage.Player
{
    /// <summary>Reads the "Player" action map of the project's InputActionAsset.</summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        const string MapName = "Player";

        [SerializeField] InputActionAsset actions;

        InputActionMap map;
        InputAction move;
        InputAction look;
        InputAction jump;
        InputAction sprint;
        InputAction interact;

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => look != null ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool SprintHeld => sprint != null && sprint.IsPressed();
        public bool JumpPressedThisFrame => jump != null && jump.WasPressedThisFrame();

        public event Action InteractPerformed;

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)}: no InputActionAsset assigned.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(MapName, false);
            if (map == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)}: action map '{MapName}' not found.", this);
                enabled = false;
                return;
            }

            move = map.FindAction("Move", false);
            look = map.FindAction("Look", false);
            jump = map.FindAction("Jump", false);
            sprint = map.FindAction("Sprint", false);
            interact = map.FindAction("Interact", false);
        }

        void OnEnable()
        {
            if (map == null) return;
            map.Enable();
            // 'started' fires once on press, even though the asset gives Interact a Hold interaction
            // (whose 'performed' would only fire after the hold time).
            if (interact != null) interact.started += OnInteract;
        }

        void OnDisable()
        {
            if (map == null) return;
            if (interact != null) interact.started -= OnInteract;
            map.Disable();
        }

        void OnInteract(InputAction.CallbackContext _) => InteractPerformed?.Invoke();
    }
}
