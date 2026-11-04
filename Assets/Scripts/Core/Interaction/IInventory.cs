namespace Espionage.Core
{
    /// <summary>Holds the items an interactor carries. Main thread only.</summary>
    public interface IInventory
    {
        bool Has(ItemId item);
        void Add(ItemId item);
    }
}
