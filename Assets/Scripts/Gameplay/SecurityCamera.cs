using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>Switches off its active visual when the cameras objective completes.</summary>
    public sealed class SecurityCamera : MonoBehaviour
    {
        [SerializeField] ObjectiveId disabledBy = ObjectiveId.CamerasDisabled;
        [SerializeField] MonoBehaviour objectiveState; // must implement IObjectiveState
        [Tooltip("Visual shown while the camera is active (e.g. a red light or view cone).")]
        [SerializeField] GameObject activeVisual;

        IObjectiveState objectives;

        void Awake() => objectives = SceneDependency.Resolve<IObjectiveState>(objectiveState, this);

        void OnEnable()
        {
            if (objectives == null) return;
            objectives.ObjectiveCompleted += OnObjectiveCompleted;
            if (objectives.IsComplete(disabledBy)) Deactivate();
        }

        void OnDisable()
        {
            if (objectives != null) objectives.ObjectiveCompleted -= OnObjectiveCompleted;
        }

        void OnObjectiveCompleted(ObjectiveId id)
        {
            if (id == disabledBy) Deactivate();
        }

        void Deactivate()
        {
            if (activeVisual != null) activeVisual.SetActive(false);
        }
    }
}
