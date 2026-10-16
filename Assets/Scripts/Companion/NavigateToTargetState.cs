using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>Walks to the target's approach point over the NavMesh and reports whether it arrived.</summary>
    public sealed class NavigateToTargetState : ICompanionState
    {
        readonly ICompanionContext context;
        readonly CompanionNavigator navigator;
        readonly float timeout;

        float elapsed;

        public CompanionStateId Id => CompanionStateId.NavigateToTarget;

        public NavigateToTargetState(ICompanionContext context, CompanionNavigator navigator, float timeout)
        {
            this.context = context;
            this.navigator = navigator;
            this.timeout = timeout;
        }

        public void Enter()
        {
            elapsed = 0f;

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
                context.RequestState(CompanionStateId.Interact);
        }

        public void Exit() => navigator.Stop();
    }
}
