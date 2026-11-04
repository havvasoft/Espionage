namespace Espionage.Core
{
    /// <summary>
    /// Optional extra for an <see cref="ICompanionTarget"/> that may only be commanded under some condition
    /// (e.g. the player is close to it). Checked once, when the command is issued.
    /// </summary>
    public interface IGatedCompanionTarget
    {
        bool IsCommandAllowed { get; }
    }
}
