using UnityEngine;

namespace Espionage.Core
{
    /// <summary>The actor performing an interaction (implemented by the player).</summary>
    public interface IInteractor
    {
        Transform Origin { get; }
        IInventory Inventory { get; }
    }
}
