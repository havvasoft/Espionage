using System;

namespace Espionage.Core
{
    /// <summary>
    /// Lets the player side request a companion task without referencing Gameplay or Companion.
    /// </summary>
    public interface ICommandDispatcher
    {
        DispatchResult TryDispatch(CompanionCommandType type);

        /// <summary>Relayed from <see cref="ICompanion.CommandFinished"/>.</summary>
        event Action<CompanionCommandType, CommandResult> CommandFinished;
    }
}
