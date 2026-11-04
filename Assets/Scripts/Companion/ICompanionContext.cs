using Espionage.Core;

namespace Espionage.Companion
{
    /// <summary>What a state may ask of the companion that hosts it. Keeps states independent of the controller class.</summary>
    public interface ICompanionContext
    {
        /// <summary>The command being carried out. Only meaningful outside Follow.</summary>
        CompanionCommand Command { get; }

        /// <summary>Switches state after the current Tick/Enter returns.</summary>
        void RequestState(CompanionStateId id);

        /// <summary>Ends the current command with <paramref name="result"/> and moves to Complete.</summary>
        void Finish(CommandResult result);

        /// <summary>Raises the finished event for the current command and clears it. Called by Complete.</summary>
        void ReportFinished();
    }
}
