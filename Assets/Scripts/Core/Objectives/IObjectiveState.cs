using System;

namespace Espionage.Core
{
    /// <summary>
    /// Tracks which objectives are complete. Main thread only; events fire synchronously.
    /// </summary>
    public interface IObjectiveState
    {
        bool IsComplete(ObjectiveId id);

        /// <summary>
        /// Marks the objective complete. Returns true only on the first transition;
        /// returns false if already complete or if <paramref name="id"/> is not a valid objective.
        /// </summary>
        bool TryComplete(ObjectiveId id);

        /// <summary>Raised once per objective, when it becomes complete.</summary>
        event Action<ObjectiveId> ObjectiveCompleted;
    }
}
