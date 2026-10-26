using System;

namespace Espionage.Core
{
    /// <summary>
    /// How guards tell the rest of the game the player was caught, without referencing the player.
    /// Main thread only; the event fires synchronously.
    /// </summary>
    public interface IGuardAlarm
    {
        /// <summary>Raised when a guard catches the player.</summary>
        event Action PlayerCaught;

        /// <summary>Called by a guard. Repeated reports in quick succession count once.</summary>
        void ReportCaught();
    }
}
