using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>
    /// A door that opens once for an interactor carrying the required item.
    /// Put this on a parent of the door leaf; the leaf holds the collider the player aims at.
    /// </summary>
    public sealed class LockedDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemId requiredItem = ItemId.EmployeeCard;
        [SerializeField] ObjectiveId objective = ObjectiveId.DoorOpened;
        [SerializeField] MonoBehaviour objectiveState; // must implement IObjectiveState
        [Tooltip("The part that physically blocks the doorway; deactivated when the door opens.")]
        [SerializeField] GameObject leaf;

        IObjectiveState objectives;
        bool isOpen;

        public string Prompt => isOpen ? string.Empty : "Open door";

        void Awake()
        {
            objectives = SceneDependency.Resolve<IObjectiveState>(objectiveState, this);
            if (leaf == null)
                Debug.LogError($"{name}: 'leaf' is not assigned; the door cannot open.", this);
        }

        public bool CanInteract(IInteractor interactor) => !isOpen && leaf != null;

        public void Interact(IInteractor interactor)
        {
            if (!interactor.Inventory.Has(requiredItem))
            {
                Debug.Log($"{name}: locked. Requires {requiredItem}.");
                return;
            }

            isOpen = true;
            leaf.SetActive(false);
            objectives?.TryComplete(objective);
        }
    }
}
