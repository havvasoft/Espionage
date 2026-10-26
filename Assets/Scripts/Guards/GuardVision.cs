using System;
using System.Collections.Generic;
using UnityEngine;

namespace Espionage.Guards
{
    /// <summary>
    /// A view cone plus Raycasts. The target is seen when part of its body is inside the cone, in range,
    /// and nothing solid blocks the ray, so tall cover hides the player.
    /// </summary>
    public sealed class GuardVision : MonoBehaviour, IGuardVision
    {
        const int MaxHits = 16;

        static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        [Tooltip("What the guard looks for (the player). Needs a collider, e.g. the CharacterController.")]
        [SerializeField] Transform target;
        [Tooltip("Full angle of the cone, in degrees.")]
        [SerializeField, Range(10f, 180f)] float viewAngle = 90f;
        [SerializeField] float viewDistance = 9f;
        [Tooltip("Where the guard's eyes are, relative to the guard.")]
        [SerializeField] Vector3 eyeOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] LayerMask sightMask = ~0;

        readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        Collider targetCollider;

        public bool CanSeeTarget { get; private set; }

        Vector3 Eye => transform.position + eyeOffset;

        void Awake()
        {
            if (target == null)
            {
                Debug.LogError($"{name}: 'target' is not assigned. Assign the player; the guard cannot see.", this);
                enabled = false;
                return;
            }

            targetCollider = target.GetComponentInChildren<Collider>();
            if (targetCollider == null)
            {
                Debug.LogError($"{name}: the target has no collider to aim at; the guard cannot see.", this);
                enabled = false;
            }
        }

        void Update() => CanSeeTarget = Evaluate();

        void OnDisable() => CanSeeTarget = false;

        bool Evaluate()
        {
            var bounds = targetCollider.bounds;
            Vector3 head = new Vector3(bounds.center.x, bounds.max.y - 0.1f, bounds.center.z);
            Vector3 feet = new Vector3(bounds.center.x, bounds.min.y + 0.2f, bounds.center.z);

            return CanSeePoint(bounds.center) || CanSeePoint(head) || CanSeePoint(feet);
        }

        bool CanSeePoint(Vector3 point)
        {
            Vector3 eye = Eye;
            Vector3 toPoint = point - eye;
            float distance = toPoint.magnitude;
            if (distance > viewDistance || distance < 0.01f) return false;

            Vector3 flat = toPoint;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return true;
            if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;

            int count = Physics.RaycastNonAlloc(eye, toPoint / distance, hits, distance, sightMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, ByDistance);

            for (int i = 0; i < count; i++)
            {
                var hit = hits[i].collider.transform;
                if (hit.IsChildOf(transform)) continue;                    // the guard itself
                return hit == target || hit.IsChildOf(target);             // first thing in the way
            }

            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = CanSeeTarget ? Color.red : Color.yellow;

            Vector3 eye = transform.position + eyeOffset;
            float half = viewAngle * 0.5f;
            Vector3 left = Quaternion.Euler(0f, -half, 0f) * transform.forward * viewDistance;
            Vector3 right = Quaternion.Euler(0f, half, 0f) * transform.forward * viewDistance;

            Gizmos.DrawLine(eye, eye + left);
            Gizmos.DrawLine(eye, eye + right);
            Gizmos.DrawLine(eye + left, eye + right);
        }
    }
}
