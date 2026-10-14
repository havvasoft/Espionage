using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>A computer the player uses to disable the security cameras.</summary>
    public sealed class SecurityTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] ObjectiveId objective = ObjectiveId.CamerasDisabled;
        [SerializeField] MonoBehaviour objectiveState; // must implement IObjectiveState

        IObjectiveState objectives;

        public string Prompt => "Disable security cameras";

        void Awake() => objectives = SceneDependency.Resolve<IObjectiveState>(objectiveState, this);

        public bool CanInteract(IInteractor interactor) =>
            objectives != null && !objectives.IsComplete(objective);

        public void Interact(IInteractor interactor) => objectives?.TryComplete(objective);
    }
}
