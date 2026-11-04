using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>
    /// Faces the target, and only while it can actually see it works for the target's interact duration,
    /// then applies the target's real effect. Gives up if the target stays out of sight.
    /// </summary>
    public sealed class InteractState : ICompanionState
    {
        const float TurnSpeed = 240f;

        readonly ICompanionContext context;
        readonly ICompanion companion;
        readonly CompanionNavigator navigator;
        readonly IPerception perception;
        readonly float sightTimeout;

        float remaining;
        float blindTime;

        public CompanionStateId Id => CompanionStateId.Interact;

        public InteractState(ICompanionContext context, ICompanion companion, CompanionNavigator navigator,
            IPerception perception, float sightTimeout)
        {
            this.context = context;
            this.companion = companion;
            this.navigator = navigator;
            this.perception = perception;
            this.sightTimeout = sightTimeout;
        }

        public void Enter()
        {
            navigator.Stop();
            blindTime = 0f;

            var target = context.Command.Target;
            if (target == null)
            {
                context.Finish(CommandResult.NoTarget);
                return;
            }

            remaining = Mathf.Max(0f, target.InteractDuration);
        }

        public void Tick(float deltaTime)
        {
            var target = context.Command.Target;
            if (target == null)
            {
                context.Finish(CommandResult.NoTarget);
                return;
            }

            if (!target.IsAvailable)
            {
                context.Finish(CommandResult.TargetUnavailable);
                return;
            }

            if (target.FocusPoint != null)
                navigator.FaceTowards(target.FocusPoint.position, TurnSpeed, deltaTime);

            if (!perception.HasLineOfSight(target.FocusPoint))
            {
                blindTime += deltaTime;
                if (blindTime >= sightTimeout) context.Finish(CommandResult.Unreachable);
                return;
            }

            blindTime = 0f;
            remaining -= deltaTime;
            if (remaining > 0f) return;

            context.Finish(target.TryPerform(companion)
                ? CommandResult.Succeeded
                : CommandResult.TargetUnavailable);
        }

        public void Exit() { }
    }
}
