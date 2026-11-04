using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>CharacterController-based walking, sprinting, jumping and gravity.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 6.5f;
        [SerializeField] float jumpHeight = 1f;
        [SerializeField] float gravity = -20f;

        CharacterController controller;
        IPlayerInput input;
        IPlayerControl control;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<IPlayerInput>();
            control = GetComponent<IPlayerControl>();

            if (control == null)
                Debug.LogError($"{nameof(PlayerMovement)}: no IPlayerControl (add PlayerControlLock); player cannot be locked.", this);

            if (input == null)
            {
                Debug.LogError($"{nameof(PlayerMovement)}: no IPlayerInput on this object.", this);
                enabled = false;
            }
        }

        void Update()
        {
            bool locked = control != null && control.IsLocked;
            Vector2 move = locked ? Vector2.zero : input.Move;
            bool sprint = !locked && input.SprintHeld;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (!locked && controller.isGrounded && input.JumpPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 horizontal = transform.right * move.x + transform.forward * move.y;
            if (horizontal.sqrMagnitude > 1f) horizontal.Normalize();
            horizontal *= sprint ? sprintSpeed : walkSpeed;

            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}
