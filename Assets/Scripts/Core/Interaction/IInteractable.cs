namespace Espionage.Core
{
    /// <summary>Something an <see cref="IInteractor"/> can use, e.g. a door, pickup or terminal.</summary>
    public interface IInteractable
    {
        /// <summary>Short text shown to the player, e.g. "Open door".</summary>
        string Prompt { get; }

        /// <summary>Must have no side effects.</summary>
        bool CanInteract(IInteractor interactor);

        /// <summary>Only called after <see cref="CanInteract"/> returned true.</summary>
        void Interact(IInteractor interactor);
    }
}
