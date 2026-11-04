using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>An invisible volume (a trigger BoxCollider) that tells whether the player is currently inside it.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CommandZone : MonoBehaviour
    {
        const int MaxOverlaps = 16;

        readonly Collider[] overlaps = new Collider[MaxOverlaps];

        BoxCollider box;

        public bool ContainsPlayer
        {
            get
            {
                if (box == null) return false;

                Vector3 scale = transform.lossyScale;
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f,
                    new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

                int count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(box.center), halfExtents, overlaps,
                    transform.rotation, ~0, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                    if (overlaps[i].GetComponentInParent<IInteractor>() != null) return true;

                return false;
            }
        }

        void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }
    }
}
