using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>Walks to the target's approach point over the NavMesh and reports whether it arrived.</summary>
    public sealed class NavigateToTargetState : ICompanionState
    {
        const float ProbeDistance = 1.5f;
        const float ProgressCheckInterval = 1f;
        const float MinProgress = 0.15f;
        const float StuckTimeout = 4f;

        readonly ICompanionContext context;
        readonly CompanionNavigator navigator;
        readonly IPerception perception;
        readonly float timeout;

        float elapsed;
        float checkTimer;
        float stuckTime;
        Vector3 lastCheckedPosition;

        public CompanionStateId Id => CompanionStateId.NavigateToTarget;

        public NavigateToTargetState(ICompanionContext context, CompanionNavigator navigator,
            IPerception perception, float timeout)
        {
            this.context = context;
            this.navigator = navigator;
            this.perception = perception;
            this.timeout = timeout;
        }

        public void Enter()
        {
            elapsed = 0f;
            checkTimer = 0f;
            stuckTime = 0f;
            lastCheckedPosition = navigator.Position;

            var target = context.Command.Target;
            if (target == null || target.ApproachPoint == null)
                context.Finish(CommandResult.NoTarget);
            else if (!navigator.TryMoveTo(target.ApproachPoint.position))
                context.Finish(CommandResult.Unreachable);
        }

        public void Tick(float deltaTime)
        {
            var target = context.Command.Target;
            if (target == null || target.ApproachPoint == null)
            {
                context.Finish(CommandResult.NoTarget);
                return;
            }

            if (!target.IsAvailable)
            {
                context.Finish(CommandResult.TargetUnavailable);
                return;
            }

            elapsed += deltaTime;
            if (elapsed >= timeout)
            {
                context.Finish(CommandResult.Unreachable);
                return;
            }

            if (navigator.IsPathPending) return;

            Vector3 offset = target.ApproachPoint.position - navigator.Position;
            offset.y = 0f;
            if (offset.magnitude <= target.ArrivalRadius)
            {
                context.RequestState(CompanionStateId.Interact);
                return;
            }

            bool blockedAhead = perception.TryGetObstacleAhead(ProbeDistance, out var obstacle);
            if (IsStuck(deltaTime))
            {
                string cause = blockedAhead ? $"blocked by '{obstacle.collider.name}'" : "no obstacle detected ahead";
                Debug.LogWarning($"Companion stuck on the way to the target ({cause}); giving up.");
                context.Finish(CommandResult.Unreachable);
            }
        }

        /// <summary>True once the companion has made almost no progress for <see cref="StuckTimeout"/> seconds.</summary>
        bool IsStuck(float deltaTime)
        {
            checkTimer += deltaTime;
            if (checkTimer < ProgressCheckInterval) return false;
            checkTimer = 0f;

            if ((navigator.Position - lastCheckedPosition).magnitude >= MinProgress)
                stuckTime = 0f;
            else
                stuckTime += ProgressCheckInterval;

            lastCheckedPosition = navigator.Position;
            return stuckTime >= StuckTimeout;
        }

        public void Exit() => navigator.Stop();
    }
}
