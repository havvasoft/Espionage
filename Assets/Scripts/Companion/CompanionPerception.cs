using System;
using System.Collections.Generic;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>Raycast sensors: a line-of-sight check toward a point of interest and a forward obstacle probe.</summary>
    public sealed class CompanionPerception : MonoBehaviour, IPerception
    {
        const int MaxHits = 16;

        static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        [Tooltip("Where the sensors start, relative to the companion.")]
        [SerializeField] Vector3 eyeOffset = new Vector3(0f, 0.3f, 0f);
        [SerializeField] LayerMask sightMask = ~0;
        [SerializeField] bool drawDebugRays = true;

        readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        Vector3 Eye => transform.position + eyeOffset;

        public bool HasLineOfSight(Transform focus)
        {
            if (focus == null) return false;

            Vector3 eye = Eye;
            Vector3 toFocus = focus.position - eye;
            float distance = toFocus.magnitude;
            if (distance < 0.01f) return true;

            int count = Physics.RaycastNonAlloc(eye, toFocus / distance, hits, distance, sightMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, 0, count, ByDistance);

            bool visible = true;
            for (int i = 0; i < count; i++)
            {
                var hitTransform = hits[i].collider.transform;
                if (hitTransform.IsChildOf(transform)) continue;                 // the companion itself
                if (hitTransform == focus || hitTransform.IsChildOf(focus)) break; // reached the target
                visible = false;                                                  // something else is in the way
                break;
            }

            if (drawDebugRays) Debug.DrawLine(eye, focus.position, visible ? Color.green : Color.red);
            return visible;
        }

        public bool TryGetObstacleAhead(float distance, out RaycastHit hit)
        {
            Vector3 eye = Eye;
            bool found = Physics.Raycast(eye, transform.forward, out hit, distance, sightMask,
                QueryTriggerInteraction.Ignore);

            if (drawDebugRays) Debug.DrawRay(eye, transform.forward * distance, found ? Color.yellow : Color.cyan);
            return found;
        }
    }
}
