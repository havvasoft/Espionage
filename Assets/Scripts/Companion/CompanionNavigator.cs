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
        NavMeshPath scratchPath;
        float baseSpeed;
        float baseStoppingDistance;

        public bool IsReady => agent != null && agent.isOnNavMesh;
        public Vector3 Position => transform.position;
        public bool HasPath => IsReady && agent.hasPath;
        public bool IsPathPending => IsReady && agent.pathPending;
        public float RemainingDistance => IsReady && !agent.pathPending ? agent.remainingDistance : float.PositiveInfinity;
        public float Speed => agent != null ? agent.velocity.magnitude : 0f;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            scratchPath = new NavMeshPath();
            baseSpeed = agent.speed;
            baseStoppingDistance = agent.stoppingDistance;
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

        /// <summary>
        /// Length of the walkable route to <paramref name="point"/> (not the straight-line distance, which a wall
        /// can make very misleading). False if the agent is not ready or there is no complete route.
        /// </summary>
        public bool TryGetWalkingDistance(Vector3 point, out float length)
        {
            length = float.PositiveInfinity;
            if (!IsReady || !TrySamplePosition(point, out var target)) return false;

            if (!NavMesh.CalculatePath(agent.nextPosition, target, NavMesh.AllAreas, scratchPath)
                || scratchPath.status != NavMeshPathStatus.PathComplete)
                return false;

            length = 0f;
            var corners = scratchPath.corners;
            for (int i = 1; i < corners.Length; i++)
                length += Vector3.Distance(corners[i - 1], corners[i]);
            return true;
        }

        /// <summary>Scales the walking speed (1 = the agent's normal speed).</summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            if (agent != null) agent.speed = baseSpeed * multiplier;
        }

        /// <summary>How far from the destination the agent stops on its own.</summary>
        public void SetStoppingDistance(float distance)
        {
            if (agent != null) agent.stoppingDistance = distance;
        }

        /// <summary>Back to the agent's original speed and stopping distance.</summary>
        public void ResetMovementSettings()
        {
            if (agent == null) return;
            agent.speed = baseSpeed;
            agent.stoppingDistance = baseStoppingDistance;
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
