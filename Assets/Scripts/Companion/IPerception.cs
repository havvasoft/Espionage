using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>What the companion can sense about the world around it.</summary>
    public interface IPerception
    {
        /// <summary>True if nothing solid lies between the companion and <paramref name="focus"/>.</summary>
        bool HasLineOfSight(Transform focus);

        /// <summary>True if something solid is within <paramref name="distance"/> straight ahead.</summary>
        bool TryGetObstacleAhead(float distance, out RaycastHit hit);
    }
}
