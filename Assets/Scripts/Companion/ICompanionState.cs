using Espionage.Core;

namespace Espionage.Companion
{
    /// <summary>One behaviour of the companion's state machine.</summary>
    public interface ICompanionState
    {
        CompanionStateId Id { get; }

        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
