using System;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Turns the player's command button into a companion order and reports how it went.</summary>
    public sealed class PlayerCompanionCommander : MonoBehaviour
    {
        [Tooltip("Object with the command dispatcher (anything implementing ICommandDispatcher).")]
        [SerializeField] MonoBehaviour dispatcherComponent;
        [SerializeField] CompanionCommandType commandType = CompanionCommandType.DisableLaser;

        IPlayerInput input;
        IPlayerControl control;
        ICommandDispatcher dispatcher;

        /// <summary>Short text for the player, e.g. "Companion is busy".</summary>
        public event Action<string> MessageRaised;

        void Awake()
        {
            input = GetComponent<IPlayerInput>();
            control = GetComponent<IPlayerControl>();

            if (dispatcherComponent != null)
                dispatcher = dispatcherComponent as ICommandDispatcher
                             ?? dispatcherComponent.GetComponent<ICommandDispatcher>();

            if (input == null || dispatcher == null)
            {
                Debug.LogError($"{nameof(PlayerCompanionCommander)}: needs IPlayerInput on this object and a " +
                               "dispatcher (ICommandDispatcher) assigned; the companion cannot be commanded.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (input == null || dispatcher == null) return;
            input.CommandPerformed += OnCommand;
            dispatcher.CommandFinished += OnCommandFinished;
        }

        void OnDisable()
        {
            if (input != null) input.CommandPerformed -= OnCommand;
            if (dispatcher != null) dispatcher.CommandFinished -= OnCommandFinished;
        }

        void OnCommand()
        {
            if (control != null && control.IsLocked) return;
            Raise(Describe(dispatcher.TryDispatch(commandType)));
        }

        void OnCommandFinished(CompanionCommandType type, CommandResult result) => Raise(Describe(result));

        void Raise(string message)
        {
            if (!string.IsNullOrEmpty(message)) MessageRaised?.Invoke(message);
        }

        static string Describe(DispatchResult result)
        {
            switch (result)
            {
                case DispatchResult.Accepted: return "Companion: on it.";
                case DispatchResult.NoTarget: return "There is nothing for the companion to do.";
                case DispatchResult.TargetUnavailable: return "That is already taken care of.";
                case DispatchResult.CompanionBusy: return "Companion is busy.";
                case DispatchResult.OutOfRange: return "Get closer before giving that order.";
                default: return "No companion available.";
            }
        }

        static string Describe(CommandResult result)
        {
            switch (result)
            {
                case CommandResult.Succeeded: return "Companion: done.";
                case CommandResult.Unreachable: return "Companion could not get it done.";
                case CommandResult.TargetUnavailable: return "Companion: it is no longer possible.";
                default: return "Companion: cannot do that.";
            }
        }
    }
}
