using System;

namespace Espionage.Core
{
    /// <summary>The autonomous companion. Commands should be issued through <see cref="ICommandDispatcher"/>.</summary>
    public interface ICompanion
    {
        CompanionStateId State { get; }

        /// <summary>True whenever the companion is not in <see cref="CompanionStateId.Follow"/>.</summary>
        bool IsBusy { get; }

        /// <summary>
        /// Returns false (never throws) if the companion is busy, the command type is None,
        /// or the target is null or does not match the command type.
        /// </summary>
        bool TryIssue(in CompanionCommand command);

        event Action<CompanionStateId> StateChanged;
        event Action<CompanionCommand, CommandResult> CommandFinished;
    }
}
