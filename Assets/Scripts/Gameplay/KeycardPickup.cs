using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>A card lying in the world. Taking it adds it to the inventory and completes its objective.</summary>
    public sealed class KeycardPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemId item = ItemId.EmployeeCard;
        [SerializeField] ObjectiveId objective = ObjectiveId.EmployeeCardFound;
        [SerializeField] MonoBehaviour objectiveState; // must implement IObjectiveState

        IObjectiveState objectives;

        public string Prompt => "Take employee card";

        void Awake() => objectives = SceneDependency.Resolve<IObjectiveState>(objectiveState, this);

        public bool CanInteract(IInteractor interactor) => !interactor.Inventory.Has(item);

        public void Interact(IInteractor interactor)
        {
            interactor.Inventory.Add(item);
            objectives?.TryComplete(objective);
            gameObject.SetActive(false);
        }
    }
}
