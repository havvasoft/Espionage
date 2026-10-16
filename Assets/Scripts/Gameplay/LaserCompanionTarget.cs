using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>
    /// Lets the companion walk to a <see cref="LaserBarrier"/>'s control panel and switch the laser off.
    /// The player can only give the order while standing inside the command zone.
    /// </summary>
    public sealed class LaserCompanionTarget : MonoBehaviour, ICompanionTarget, IGatedCompanionTarget
    {
        [SerializeField] LaserBarrier barrier;
        [Tooltip("Where the companion stands to work on the laser.")]
        [SerializeField] Transform approachPoint;
        [Tooltip("What the companion faces; usually the control panel.")]
        [SerializeField] Transform focusPoint;
        [Tooltip("The player must be inside this zone to give the order. Leave empty to allow it from anywhere.")]
        [SerializeField] CommandZone commandZone;
        [SerializeField] float arrivalRadius = 0.6f;
        [SerializeField] float interactDuration = 2f;

        public CompanionCommandType Type => CompanionCommandType.DisableLaser;
        public bool IsAvailable => barrier != null && barrier.IsActive;
        public Transform ApproachPoint => approachPoint;
        public Transform FocusPoint => focusPoint != null ? focusPoint : approachPoint;
        public float ArrivalRadius => arrivalRadius;
        public float InteractDuration => interactDuration;
        public bool IsCommandAllowed => commandZone == null || commandZone.ContainsPlayer;

        void Awake()
        {
            if (barrier == null)
                Debug.LogError($"{name}: 'barrier' is not assigned.", this);
            if (approachPoint == null)
                Debug.LogError($"{name}: 'approachPoint' is not assigned.", this);
        }

        public bool TryPerform(ICompanion actor) => barrier != null && barrier.Disable();
    }
}
