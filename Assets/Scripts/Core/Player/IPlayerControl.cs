using System;

namespace Espionage.Core
{
    /// <summary>
    /// Reference-counted lock on player input. The player is locked while at least one
    /// handle returned by <see cref="RequestLock"/> is undisposed. Main thread only.
    /// </summary>
    public interface IPlayerControl
    {
        bool IsLocked { get; }

        /// <summary>
        /// Requests a lock. Dispose the returned handle to release it; disposing more than once is safe.
        /// <paramref name="reason"/> is for debugging only.
        /// </summary>
        IDisposable RequestLock(string reason);

        /// <summary>Raised with true when becoming locked and false when becoming unlocked.</summary>
        event Action<bool> LockStateChanged;
    }
}
