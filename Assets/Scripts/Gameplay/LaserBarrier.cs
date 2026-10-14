using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>
    /// A laser the player cannot pass. Disabling it removes the beams and completes its objective.
    /// The companion will trigger <see cref="Disable"/> through a target adapter added later.
    /// </summary>
    public sealed class LaserBarrier : MonoBehaviour
    {
        [SerializeField] ObjectiveId objective = ObjectiveId.LaserDisabled;
        [SerializeField] MonoBehaviour objectiveState; // must implement IObjectiveState
        [Tooltip("Beams and blocking collider; deactivated when the laser is disabled.")]
        [SerializeField] GameObject beams;

        IObjectiveState objectives;

        public bool IsActive { get; private set; } = true;

        void Awake()
        {
            objectives = SceneDependency.Resolve<IObjectiveState>(objectiveState, this);
            if (beams == null)
                Debug.LogError($"{name}: 'beams' is not assigned.", this);
        }

        /// <summary>Returns false if the laser was already disabled.</summary>
        [ContextMenu("Disable (test)")]
        public bool Disable()
        {
            if (!IsActive) return false;

            IsActive = false;
            if (beams != null) beams.SetActive(false);
            objectives?.TryComplete(objective);
            return true;
        }
    }
}
