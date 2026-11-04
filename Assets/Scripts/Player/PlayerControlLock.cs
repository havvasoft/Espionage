using System;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Reference-counted player lock. Locked while any returned handle is undisposed.</summary>
    public sealed class PlayerControlLock : MonoBehaviour, IPlayerControl
    {
        int lockCount;

        public bool IsLocked => lockCount > 0;

        public event Action<bool> LockStateChanged;

        public IDisposable RequestLock(string reason)
        {
            var handle = new LockHandle(this, reason);
            lockCount++;
            if (lockCount == 1) LockStateChanged?.Invoke(true);
            return handle;
        }

        void Release()
        {
            if (lockCount <= 0) return;
            lockCount--;
            if (lockCount == 0) LockStateChanged?.Invoke(false);
        }

        sealed class LockHandle : IDisposable
        {
            PlayerControlLock owner;
            readonly string reason;

            public LockHandle(PlayerControlLock owner, string reason)
            {
                this.owner = owner;
                this.reason = reason;
            }

            public void Dispose()
            {
                var o = owner;
                if (o == null) return; // already disposed
                owner = null;
                // Unity null check: the owner may have been destroyed before the handle is released.
                if (o) o.Release();
            }

            public override string ToString() => $"PlayerLock({reason})";
        }
    }
}
