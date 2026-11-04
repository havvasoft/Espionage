namespace Espionage.Guards
{
    /// <summary>How close a guard is to catching its target, from 0 (calm) to 1 (caught).</summary>
    public interface IGuardSuspicion
    {
        float Suspicion { get; }
    }
}
