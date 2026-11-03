using Espionage.Core;
using UnityEngine;

namespace Espionage.UI
{
    /// <summary>Lets the player start the mainframe dialogue through the shared interaction system.</summary>
    public sealed class DialogueTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueController controller;
        [SerializeField] private MonoBehaviour objectiveState; // must implement IObjectiveState
        [SerializeField] private MonoBehaviour playerControl;  // must implement IPlayerControl

        private IObjectiveState _objectives;

        public string Prompt => "Talk to the mainframe guard";

        private void Awake()
        {
            _objectives = Resolve<IObjectiveState>(objectiveState);
            controller.Bind(_objectives, Resolve<IPlayerControl>(playerControl));
        }

        public bool CanInteract(IInteractor interactor) =>
            _objectives != null && !_objectives.IsComplete(ObjectiveId.MainframeAccessGranted);

        public void Interact(IInteractor interactor) => controller.Open();

        private T Resolve<T>(MonoBehaviour assigned) where T : class
        {
            if (assigned == null)
            {
                Debug.LogError($"{name}: required {typeof(T).Name} reference is not assigned.", this);
                return null;
            }

            if (assigned is T typed)
                return typed;

            Debug.LogError($"{name}: '{assigned.name}' does not implement {typeof(T).Name}.", this);
            return null;
        }
    }
}