using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>Unity cannot serialize interface fields; this resolves an Inspector-assigned component to an interface.</summary>
    static class SceneDependency
    {
        /// <summary>Returns the interface, or null after logging a clear error naming <paramref name="owner"/>.</summary>
        public static T Resolve<T>(MonoBehaviour assigned, Object owner) where T : class
        {
            if (assigned == null)
            {
                Debug.LogError($"{owner.name}: required {typeof(T).Name} reference is not assigned.", owner);
                return null;
            }

            if (assigned is T typed) return typed;

            Debug.LogError($"{owner.name}: '{assigned.name}' does not implement {typeof(T).Name}.", owner);
            return null;
        }
    }
}
