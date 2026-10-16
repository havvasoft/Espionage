using UnityEngine;
using UnityEngine.AI;

namespace Espionage.Companion
{
    /// <summary>Thin wrapper over <see cref="NavMeshAgent"/>: real NavMesh path-finding, no scripted movement.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CompanionNavigator : MonoBehaviour
    {
        [Tooltip("How far from a requested point the nearest NavMesh position may be.")]
        [SerializeField] float sampleDistance = 2f;

        NavMeshAgent agent;

        public bool IsReady => agent != null && agent.isOnNavMesh;
        public Vector3 Position => transform.position;
        public bool HasPath => IsReady && agent.hasPath;
        public bool IsPathPending => IsReady && agent.pathPending;
        public float RemainingDistance => IsReady && !agent.pathPending ? agent.remainingDistance : float.PositiveInfinity;
        public float Speed => agent != null ? agent.velocity.magnitude : 0f;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            if (!agent.isOnNavMesh)
                Debug.LogError($"{name}: companion is not on a NavMesh. Bake the NavMesh and place the companion on it.", this);
        }

        /// <summary>Snaps <paramref name="point"/> to the NavMesh. False if none is within range.</summary>
        public bool TrySamplePosition(Vector3 point, out Vector3 onMesh)
        {
            if (NavMesh.SamplePosition(point, out var hit, sampleDistance, NavMesh.AllAreas))
            {
                onMesh = hit.position;
                return true;
            }

            onMesh = point;
            return false;
        }

        /// <summary>Requests a path to <paramref name="point"/>. False if the agent is not ready or no path exists.</summary>
        public bool TryMoveTo(Vector3 point)
        {
            if (!IsReady) return false;
            if (!TrySamplePosition(point, out var target)) return false;

            agent.isStopped = false;
            return agent.SetDestination(target);
        }

        /// <summary>Turns toward <paramref name="point"/> on the horizontal plane. Returns true once facing it.</summary>
        public bool FaceTowards(Vector3 point, float degreesPerSecond, float deltaTime)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return true;

            Quaternion goal = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, degreesPerSecond * deltaTime);
            return Quaternion.Angle(transform.rotation, goal) < 2f;
        }

        public void Stop()
        {
            if (!IsReady) return;
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}
