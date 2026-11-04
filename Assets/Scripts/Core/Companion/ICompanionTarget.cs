using UnityEngine;

namespace Espionage.Core
{
    /// <summary>
    /// Something the companion can walk to and operate. Implemented by an adapter next to the
    /// mechanism, so the companion never references the mechanism itself.
    /// </summary>
    public interface ICompanionTarget
    {
        CompanionCommandType Type { get; }

        /// <summary>False if already done or otherwise unusable.</summary>
        bool IsAvailable { get; }

        /// <summary>Where the companion should stand.</summary>
        Transform ApproachPoint { get; }

        /// <summary>What the companion should face and raycast toward.</summary>
        Transform FocusPoint { get; }

        /// <summary>How close to <see cref="ApproachPoint"/> counts as arrived.</summary>
        float ArrivalRadius { get; }

        /// <summary>Seconds the companion spends working on the target.</summary>
        float InteractDuration { get; }

        /// <summary>Applies the real effect. Returns false if the target became unavailable.</summary>
        bool TryPerform(ICompanion actor);
    }
}
