namespace Espionage.Guards
{
    /// <summary>What a guard can see right now.</summary>
    public interface IGuardVision
    {
        /// <summary>True while the target is inside the view cone with a clear line of sight.</summary>
        bool CanSeeTarget { get; }
    }
}
