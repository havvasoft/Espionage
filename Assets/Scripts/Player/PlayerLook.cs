using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Mouse/stick look: yaw rotates the body, pitch rotates the camera pivot.</summary>
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] float sensitivity = 0.1f;
        [SerializeField] float maxPitch = 85f;
        [SerializeField] bool manageCursor = true;

        IPlayerInput input;
        IPlayerControl control;
        float pitch;

        void Awake()
        {
            input = GetComponent<IPlayerInput>();
            control = GetComponent<IPlayerControl>();

            if (control == null)
                Debug.LogError($"{nameof(PlayerLook)}: no IPlayerControl (add PlayerControlLock); player cannot be locked.", this);

            if (input == null || cameraPivot == null)
            {
                Debug.LogError($"{nameof(PlayerLook)}: needs an IPlayerInput and a camera pivot.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (control != null) control.LockStateChanged += OnLockStateChanged;
            ApplyCursor(control != null && control.IsLocked);
        }

        void OnDisable()
        {
            if (control != null) control.LockStateChanged -= OnLockStateChanged;
        }

        void Update()
        {
            if (control != null && control.IsLocked) return;

            Vector2 delta = input.Look * sensitivity;
            transform.Rotate(Vector3.up, delta.x, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void OnLockStateChanged(bool locked) => ApplyCursor(locked);

        void ApplyCursor(bool uiMode)
        {
            if (!manageCursor) return;
            Cursor.lockState = uiMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = uiMode;
        }
    }
}
