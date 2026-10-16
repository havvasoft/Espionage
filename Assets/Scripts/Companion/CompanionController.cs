using System;
using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>
    /// The companion's state machine host. Implements <see cref="ICompanion"/>.
    /// Transitions requested by states are applied after the current Tick, in order.
    /// </summary>
    [RequireComponent(typeof(CompanionNavigator))]
    public sealed class CompanionController : MonoBehaviour, ICompanion, ICompanionContext
    {
        const int MaxTransitionsPerFrame = 8;

        [Tooltip("What the companion follows (the player).")]
        [SerializeField] Transform leader;
        [SerializeField] float followDistance = 2.5f;
        [SerializeField] float stopDistance = 1.8f;
        [SerializeField] float repathInterval = 0.25f;
        [Tooltip("Pause after receiving a command, before walking off.")]
        [SerializeField] float acknowledgeDelay = 0.5f;
        [Tooltip("Seconds before a trip to the target is given up as unreachable.")]
        [SerializeField] float navigateTimeout = 30f;

        readonly Dictionary<CompanionStateId, ICompanionState> states =
            new Dictionary<CompanionStateId, ICompanionState>();

        ICompanionState current;
        CompanionStateId? pending;
        CompanionCommand command;
        CommandResult result;

        public CompanionStateId State => current != null ? current.Id : CompanionStateId.Follow;
        public bool IsBusy => State != CompanionStateId.Follow || pending.HasValue;

        public CompanionCommand Command => command;

        public event Action<CompanionStateId> StateChanged;
        public event Action<CompanionCommand, CommandResult> CommandFinished;

        void Awake()
        {
            var navigator = GetComponent<CompanionNavigator>();

            if (leader == null)
            {
                Debug.LogError($"{name}: 'leader' is not assigned. Assign the player.", this);
                enabled = false;
                return;
            }

            Register(new FollowState(navigator, leader, followDistance, stopDistance, repathInterval));
            Register(new ReceiveCommandState(this, navigator, acknowledgeDelay));
            Register(new NavigateToTargetState(this, navigator, navigateTimeout));
            Register(new CompleteState(this));
        }

        void Start() => RequestStateAndApply(CompanionStateId.Follow);

        void Update()
        {
            current?.Tick(Time.deltaTime);
            ApplyPending();
        }

        public bool TryIssue(in CompanionCommand newCommand)
        {
            if (IsBusy) return false;
            if (newCommand.Type == CompanionCommandType.None) return false;
            if (newCommand.Target == null || newCommand.Target.Type != newCommand.Type) return false;
            if (!newCommand.Target.IsAvailable) return false;

            command = newCommand;
            RequestStateAndApply(CompanionStateId.ReceiveCommand);
            return true;
        }

        public void RequestState(CompanionStateId id) => pending = id;

        public void Finish(CommandResult commandResult)
        {
            result = commandResult;
            pending = CompanionStateId.Complete;
        }

        public void ReportFinished()
        {
            var finished = command;
            command = default;
            CommandFinished?.Invoke(finished, result);
        }

        void Register(ICompanionState state) => states[state.Id] = state;

        void RequestStateAndApply(CompanionStateId id)
        {
            pending = id;
            ApplyPending();
        }

        void ApplyPending()
        {
            for (int i = 0; i < MaxTransitionsPerFrame && pending.HasValue; i++)
            {
                var id = pending.Value;
                pending = null;
                ChangeState(id);
            }
        }

        void ChangeState(CompanionStateId id)
        {
            if (!states.TryGetValue(id, out var next))
            {
                Debug.LogError($"{name}: no state registered for {id}.", this);
                return;
            }

            current?.Exit();
            current = next;
            StateChanged?.Invoke(id);
            current.Enter();
        }
    }
}
