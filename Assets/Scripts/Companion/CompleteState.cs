using Espionage.Core;

namespace Espionage.Companion
{
    /// <summary>Announces the outcome of the command and sends the companion back to following.</summary>
    public sealed class CompleteState : ICompanionState
    {
        readonly ICompanionContext context;

        public CompanionStateId Id => CompanionStateId.Complete;

        public CompleteState(ICompanionContext context) => this.context = context;

        public void Enter()
        {
            context.ReportFinished();
            context.RequestState(CompanionStateId.Follow);
        }

        public void Tick(float deltaTime) { }

        public void Exit() { }
    }
}
