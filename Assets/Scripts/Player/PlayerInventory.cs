using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    public sealed class PlayerInventory : MonoBehaviour, IInventory
    {
        readonly HashSet<ItemId> items = new HashSet<ItemId>();

        public bool Has(ItemId item) => item != ItemId.None && items.Contains(item);

        public void Add(ItemId item)
        {
            if (item == ItemId.None) return;
            items.Add(item);
        }
    }
}
