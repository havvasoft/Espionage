using System;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Sends the player back to a checkpoint when the guards catch them.</summary>
    public sealed class PlayerRespawner : MonoBehaviour
    {
        [Tooltip("Object with the scene's guard alarm (anything implementing IGuardAlarm).")]
        [SerializeField] MonoBehaviour alarmComponent;
        [Tooltip("Where the player reappears after being caught.")]
        [SerializeField] Transform checkpoint;

        IGuardAlarm alarm;
        CharacterController controller;

        /// <summary>Short text for the player, e.g. "Caught!".</summary>
        public event Action<string> MessageRaised;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (alarmComponent != null)
                alarm = alarmComponent as IGuardAlarm ?? alarmComponent.GetComponent<IGuardAlarm>();

            if (alarm == null || checkpoint == null)
            {
                Debug.LogError($"{nameof(PlayerRespawner)}: needs an IGuardAlarm and a checkpoint assigned; " +
                               "the player will not be sent back when caught.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (alarm != null) alarm.PlayerCaught += OnCaught;
        }

        void OnDisable()
        {
            if (alarm != null) alarm.PlayerCaught -= OnCaught;
        }

        void OnCaught()
        {
            // A CharacterController overrides direct position changes unless it is switched off first.
            if (controller != null) controller.enabled = false;
            transform.SetPositionAndRotation(checkpoint.position, checkpoint.rotation);
            if (controller != null) controller.enabled = true;

            MessageRaised?.Invoke("Caught! Back to the checkpoint.");
        }
    }
}
