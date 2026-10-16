using Espionage.Core;

namespace Espionage.Companion
{
    /// <summary>Checks the command is workable, pauses briefly so it reads as "understood", then heads out.</summary>
    public sealed class ReceiveCommandState : ICompanionState
    {
        readonly ICompanionContext context;
        readonly CompanionNavigator navigator;
        readonly float acknowledgeDelay;

        float timer;

        public CompanionStateId Id => CompanionStateId.ReceiveCommand;

        public ReceiveCommandState(ICompanionContext context, CompanionNavigator navigator, float acknowledgeDelay)
        {
            this.context = context;
            this.navigator = navigator;
            this.acknowledgeDelay = acknowledgeDelay;
        }

        public void Enter()
        {
            timer = acknowledgeDelay;
            navigator.Stop();

            var target = context.Command.Target;
            if (target == null || target.ApproachPoint == null)
                context.Finish(CommandResult.NoTarget);
            else if (!target.IsAvailable)
                context.Finish(CommandResult.TargetUnavailable);
            else if (!navigator.TrySamplePosition(target.ApproachPoint.position, out _))
                context.Finish(CommandResult.Unreachable);
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f) context.RequestState(CompanionStateId.NavigateToTarget);
        }

        public void Exit() { }
    }
}
