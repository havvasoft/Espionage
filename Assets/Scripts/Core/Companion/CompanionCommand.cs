namespace Espionage.Core
{
    /// <summary>A task for the companion: what to do and the target to do it on.</summary>
    public readonly struct CompanionCommand
    {
        public CompanionCommandType Type { get; }
        public ICompanionTarget Target { get; }

        public CompanionCommand(CompanionCommandType type, ICompanionTarget target)
        {
            Type = type;
            Target = target;
        }
    }
}
