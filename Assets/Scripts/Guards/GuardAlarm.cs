using System;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Guards
{
    /// <summary>Scene-wide channel that guards report catches to and the rest of the game listens on.</summary>
    public sealed class GuardAlarm : MonoBehaviour, IGuardAlarm
    {
        [Tooltip("Reports closer together than this count as one catch.")]
        [SerializeField] float cooldownSeconds = 1f;

        float lastReportTime = float.NegativeInfinity;

        public event Action PlayerCaught;

        public void ReportCaught()
        {
            if (Time.time - lastReportTime < cooldownSeconds) return;
            lastReportTime = Time.time;

            Debug.Log("Player caught by a guard.");
            PlayerCaught?.Invoke();
        }
    }
}
