using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>Turns this object (on the horizontal plane only) to face a target while it is near, e.g. an NPC greeting the player.</summary>
    public sealed class FaceTarget : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float range = 12f;
        [SerializeField] float turnSpeed = 150f;

        Quaternion home;

        void Awake() => home = transform.rotation;

        void Update()
        {
            if (target == null) return;

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            Quaternion goal = toTarget.magnitude <= range && toTarget.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(toTarget)
                : home;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * Time.deltaTime);
        }
    }
}
