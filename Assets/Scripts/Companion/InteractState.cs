using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>Faces the target, works on it for its interact duration, then applies its real effect.</summary>
    public sealed class InteractState : ICompanionState
    {
        const float TurnSpeed = 240f;

        readonly ICompanionContext context;
        readonly ICompanion companion;
        readonly CompanionNavigator navigator;

        float remaining;

        public CompanionStateId Id => CompanionStateId.Interact;

        public InteractState(ICompanionContext context, ICompanion companion, CompanionNavigator navigator)
        {
            this.context = context;
            this.companion = companion;
            this.navigator = navigator;
        }

        public void Enter()
        {
            navigator.Stop();

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

            remaining -= deltaTime;
            if (remaining > 0f) return;

            context.Finish(target.TryPerform(companion)
                ? CommandResult.Succeeded
                : CommandResult.TargetUnavailable);
        }

        public void Exit() { }
    }
}
