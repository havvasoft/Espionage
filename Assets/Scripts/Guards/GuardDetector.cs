using Espionage.Core;
using UnityEngine;

namespace Espionage.Guards
{
    /// <summary>
    /// Turns "sees the target" into a suspicion meter. Seeing fills it (and the guard stops to watch),
    /// losing sight drains it, and a full meter reports the catch to the alarm.
    /// </summary>
    [RequireComponent(typeof(GuardVision), typeof(GuardPatrol))]
    public sealed class GuardDetector : MonoBehaviour, IGuardSuspicion
    {
        [Tooltip("Object with the scene's guard alarm (anything implementing IGuardAlarm).")]
        [SerializeField] MonoBehaviour alarmComponent;
        [Tooltip("Seconds of continuous sight before the guard catches the target.")]
        [SerializeField] float secondsToCatch = 1.5f;
        [Tooltip("Seconds without sight for a full meter to drain.")]
        [SerializeField] float secondsToCalm = 3f;
        [Tooltip("Seconds without sight before the guard resumes patrolling.")]
        [SerializeField] float resumeDelay = 0.75f;
        [SerializeField] float turnSpeed = 200f;

        IGuardVision vision;
        GuardPatrol patrol;
        IGuardAlarm alarm;
        float unseenTime;

        public float Suspicion { get; private set; }

        void Awake()
        {
            vision = GetComponent<IGuardVision>();
            patrol = GetComponent<GuardPatrol>();

            if (alarmComponent != null)
                alarm = alarmComponent as IGuardAlarm ?? alarmComponent.GetComponent<IGuardAlarm>();

            if (alarm == null)
                Debug.LogError($"{name}: no IGuardAlarm assigned; this guard can see but never catch the player.", this);
        }

        void OnEnable()
        {
            if (alarm != null) alarm.PlayerCaught += OnPlayerCaught;
        }

        void OnDisable()
        {
            if (alarm != null) alarm.PlayerCaught -= OnPlayerCaught;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (vision.CanSeeTarget)
            {
                unseenTime = 0f;
                if (patrol.IsPatrolling) patrol.Stop();
                FaceTowards(vision.LastSeenPosition, dt);

                Suspicion = Mathf.Min(1f, Suspicion + dt / Mathf.Max(0.01f, secondsToCatch));
                if (Suspicion >= 1f) alarm?.ReportCaught();
                return;
            }

            unseenTime += dt;
            Suspicion = Mathf.Max(0f, Suspicion - dt / Mathf.Max(0.01f, secondsToCalm));
            if (!patrol.IsPatrolling && unseenTime >= resumeDelay) patrol.Resume();
        }

        void OnPlayerCaught()
        {
            Suspicion = 0f;
            unseenTime = 0f;
            patrol.ResetToStart();
        }

        void FaceTowards(Vector3 point, float deltaTime)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), turnSpeed * deltaTime);
        }
    }
}
