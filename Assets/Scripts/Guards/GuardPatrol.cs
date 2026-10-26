using UnityEngine;
using UnityEngine.AI;

namespace Espionage.Guards
{
    /// <summary>Walks a loop of waypoints over the NavMesh, pausing at each one.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardPatrol : MonoBehaviour
    {
        [Tooltip("Visited in order, then back to the first. Place them on the NavMesh.")]
        [SerializeField] Transform[] waypoints;
        [SerializeField] float waitSeconds = 1.5f;
        [SerializeField] float arriveDistance = 0.5f;
        [Tooltip("How far a waypoint may be from the NavMesh and still be used.")]
        [SerializeField] float sampleDistance = 2f;

        NavMeshAgent agent;
        int next;
        float waitTimer;
        bool waiting;
        bool patrolling;

        public bool IsPatrolling => patrolling;

        void Awake() => agent = GetComponent<NavMeshAgent>();

        void Start()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"{name}: no waypoints assigned; the guard will stand still.", this);
                return;
            }

            if (!agent.isOnNavMesh)
            {
                Debug.LogError($"{name}: guard is not on the NavMesh. Bake the NavMesh and place the guard on it.", this);
                return;
            }

            Resume();
        }

        void Update()
        {
            if (!patrolling || !agent.isOnNavMesh) return;

            if (waiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    waiting = false;
                    GoToNext();
                }

                return;
            }

            if (agent.pathPending) return;

            if (agent.remainingDistance <= arriveDistance)
            {
                waiting = true;
                waitTimer = waitSeconds;
                agent.ResetPath();
            }
        }

        /// <summary>Starts or continues the patrol.</summary>
        public void Resume()
        {
            if (waypoints == null || waypoints.Length == 0 || !agent.isOnNavMesh) return;

            patrolling = true;
            agent.isStopped = false;
            if (!waiting) GoToNext();
        }

        /// <summary>Halts the guard where it stands (e.g. while it is watching something).</summary>
        public void Stop()
        {
            patrolling = false;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        void GoToNext()
        {
            // Skip empty slots so one unassigned waypoint does not break the loop.
            for (int tries = 0; tries < waypoints.Length; tries++)
            {
                var point = waypoints[next];
                next = (next + 1) % waypoints.Length;
                if (point == null) continue;

                if (NavMesh.SamplePosition(point.position, out var hit, sampleDistance, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }
    }
}
