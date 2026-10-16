using System;
using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>
    /// Turns "do this kind of task" into a concrete <see cref="CompanionCommand"/> on a target assigned in the
    /// Inspector, and hands it to the companion. The player side only sees <see cref="ICommandDispatcher"/>.
    /// </summary>
    public sealed class CompanionCommandDispatcher : MonoBehaviour, ICommandDispatcher
    {
        [Tooltip("The companion. Must implement ICompanion.")]
        [SerializeField] MonoBehaviour companionComponent;
        [Tooltip("Everything the companion can be sent to. Each must implement ICompanionTarget.")]
        [SerializeField] List<MonoBehaviour> targetComponents = new List<MonoBehaviour>();

        ICompanion companion;
        readonly List<ICompanionTarget> targets = new List<ICompanionTarget>();

        public event Action<CompanionCommandType, CommandResult> CommandFinished;

        void Awake()
        {
            companion = Resolve<ICompanion>(companionComponent);
            if (companion == null)
                Debug.LogError($"{name}: 'companionComponent' is missing or its object has no ICompanion.", this);

            foreach (var component in targetComponents)
            {
                var target = Resolve<ICompanionTarget>(component);
                if (target != null) targets.Add(target);
                else Debug.LogError($"{name}: '{(component != null ? component.name : "null")}' has no ICompanionTarget.", this);
            }
        }

        /// <summary>
        /// Dragging an object into an Inspector field assigns its first component, which may not be the one
        /// wanted; fall back to the assigned object's own component that implements <typeparamref name="T"/>.
        /// </summary>
        static T Resolve<T>(MonoBehaviour assigned) where T : class
        {
            if (assigned == null) return null;
            return assigned as T ?? assigned.GetComponent<T>();
        }

        void OnEnable()
        {
            if (companion != null) companion.CommandFinished += OnCompanionFinished;
        }

        void OnDisable()
        {
            if (companion != null) companion.CommandFinished -= OnCompanionFinished;
        }

        public DispatchResult TryDispatch(CompanionCommandType type)
        {
            if (companion == null) return DispatchResult.CompanionUnavailable;
            if (companion.IsBusy) return DispatchResult.CompanionBusy;

            ICompanionTarget match = null;
            bool foundType = false;
            foreach (var target in targets)
            {
                if (target.Type != type) continue;
                foundType = true;
                if (!target.IsAvailable) continue;
                match = target;
                break;
            }

            if (match == null) return foundType ? DispatchResult.TargetUnavailable : DispatchResult.NoTarget;

            return companion.TryIssue(new CompanionCommand(type, match))
                ? DispatchResult.Accepted
                : DispatchResult.CompanionBusy;
        }

        void OnCompanionFinished(CompanionCommand command, CommandResult result) =>
            CommandFinished?.Invoke(command.Type, result);

        [ContextMenu("Dispatch DisableLaser (test)")]
        void DispatchDisableLaserForTest() =>
            Debug.Log($"Dispatch DisableLaser: {TryDispatch(CompanionCommandType.DisableLaser)}");
    }
}
