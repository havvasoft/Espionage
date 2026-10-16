using System;
using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>
    /// The companion's state machine host. Implements <see cref="ICompanion"/>.
    /// Only Follow is registered so far; command handling states are added in the next step.
    /// </summary>
    [RequireComponent(typeof(CompanionNavigator))]
    public sealed class CompanionController : MonoBehaviour, ICompanion
    {
        [Tooltip("What the companion follows (the player).")]
        [SerializeField] Transform leader;
        [SerializeField] float followDistance = 2.5f;
        [SerializeField] float stopDistance = 1.8f;
        [SerializeField] float repathInterval = 0.25f;

        readonly Dictionary<CompanionStateId, ICompanionState> states =
            new Dictionary<CompanionStateId, ICompanionState>();

        ICompanionState current;

        public CompanionStateId State => current != null ? current.Id : CompanionStateId.Follow;
        public bool IsBusy => State != CompanionStateId.Follow;

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
        }

        void Start() => ChangeState(CompanionStateId.Follow);

        void Update() => current?.Tick(Time.deltaTime);

        /// <summary>Commands are not handled yet (added with the command states); always returns false.</summary>
        public bool TryIssue(in CompanionCommand command) => false;

        void Register(ICompanionState state) => states[state.Id] = state;

        void ChangeState(CompanionStateId id)
        {
            if (!states.TryGetValue(id, out var next))
            {
                Debug.LogError($"{name}: no state registered for {id}.", this);
                return;
            }

            current?.Exit();
            current = next;
            current.Enter();
            StateChanged?.Invoke(id);
        }
    }
}
