using System;
using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Raycasts from the camera and interacts with the <see cref="IInteractable"/> it hits.</summary>
    public sealed class PlayerInteractor : MonoBehaviour, IInteractor
    {
        const int MaxHits = 32;

        static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        [Tooltip("Required. Normally the camera (or camera pivot); the interaction ray starts here.")]
        [SerializeField] Transform rayOrigin;
        [SerializeField] float maxDistance = 3f;
        [SerializeField] LayerMask interactMask = ~0;

        readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        IPlayerInput input;
        IPlayerControl control;
        IInventory inventory;

        // Only reachable with a null rayOrigin if the component is disabled (see Awake), so no ray uses this fallback.
        public Transform Origin => rayOrigin != null ? rayOrigin : transform;
        public IInventory Inventory => inventory;

        /// <summary>Usable interactable under the crosshair, or null (for prompt UI).</summary>
        public IInteractable Current { get; private set; }

        void Awake()
        {
            input = GetComponent<IPlayerInput>();
            control = GetComponent<IPlayerControl>();
            inventory = GetComponent<IInventory>();

            if (control == null)
                Debug.LogError($"{nameof(PlayerInteractor)}: no IPlayerControl (add PlayerControlLock); player cannot be locked.", this);

            if (rayOrigin == null)
            {
                Debug.LogError($"{nameof(PlayerInteractor)}: 'rayOrigin' is not assigned. Assign the player camera; interaction is disabled.", this);
                enabled = false;
                return;
            }

            if (input == null || inventory == null)
            {
                Debug.LogError($"{nameof(PlayerInteractor)}: needs IPlayerInput and IInventory on this object.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (input != null) input.InteractPerformed += OnInteract;
        }

        void OnDisable()
        {
            if (input != null) input.InteractPerformed -= OnInteract;
            Current = null;
        }

        void Update()
        {
            Current = control != null && control.IsLocked ? null : FindInteractable();
        }

        void OnInteract()
        {
            if (control != null && control.IsLocked) return;
            FindInteractable()?.Interact(this);
        }

        /// <summary>
        /// Walks the ray hits nearest-first. Own colliders and triggers without an interactable are skipped;
        /// a solid collider without an interactable blocks; the first interactable found is the target.
        /// </summary>
        IInteractable FindInteractable()
        {
            var origin = rayOrigin;
            int count = Physics.RaycastNonAlloc(origin.position, origin.forward, hits, maxDistance,
                interactMask, QueryTriggerInteraction.Collide);
            if (count == 0) return null;

            Array.Sort(hits, 0, count, ByDistance);

            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(transform)) continue;

                var target = col.GetComponentInParent<IInteractable>();
                if (target != null)
                    return target.CanInteract(this) ? target : null;

                if (!col.isTrigger) return null; // solid, non-interactable: blocks the ray
            }

            return null;
        }
    }
}
