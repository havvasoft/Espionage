using System;
using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>
    /// Turns the player's command button into a companion order and reports how it went.
    /// The order is only given while the player is looking at the companion.
    /// </summary>
    public sealed class PlayerCompanionCommander : MonoBehaviour
    {
        const int MaxHits = 16;

        static readonly IComparer<RaycastHit> ByDistance =
            Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        [Tooltip("Object with the command dispatcher (anything implementing ICommandDispatcher).")]
        [SerializeField] MonoBehaviour dispatcherComponent;
        [SerializeField] CompanionCommandType commandType = CompanionCommandType.DisableLaser;
        [Tooltip("How far away the player can still order the companion by looking at it.")]
        [SerializeField] float aimDistance = 6f;
        [SerializeField] LayerMask aimMask = ~0;

        readonly RaycastHit[] hits = new RaycastHit[MaxHits];

        IPlayerInput input;
        IPlayerControl control;
        IInteractor interactor;
        ICommandDispatcher dispatcher;

        /// <summary>True while the crosshair is on the companion (for the prompt UI).</summary>
        public bool IsLookingAtCompanion { get; private set; }

        /// <summary>Short text for the player, e.g. "Companion is busy".</summary>
        public event Action<string> MessageRaised;

        void Awake()
        {
            input = GetComponent<IPlayerInput>();
            control = GetComponent<IPlayerControl>();
            interactor = GetComponent<IInteractor>();

            if (dispatcherComponent != null)
                dispatcher = dispatcherComponent as ICommandDispatcher
                             ?? dispatcherComponent.GetComponent<ICommandDispatcher>();

            if (input == null || interactor == null || dispatcher == null)
            {
                Debug.LogError($"{nameof(PlayerCompanionCommander)}: needs IPlayerInput and IInteractor on this object " +
                               "and a dispatcher (ICommandDispatcher) assigned; the companion cannot be commanded.", this);
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
            IsLookingAtCompanion = false;
            if (input != null) input.CommandPerformed -= OnCommand;
            if (dispatcher != null) dispatcher.CommandFinished -= OnCommandFinished;
        }

        void Update()
        {
            IsLookingAtCompanion = !(control != null && control.IsLocked) && FindCompanion();
        }

        void OnCommand()
        {
            if (control != null && control.IsLocked) return;

            if (!IsLookingAtCompanion)
            {
                Raise("Look at your companion to give an order.");
                return;
            }

            Raise(Describe(dispatcher.TryDispatch(commandType)));
        }

        /// <summary>Walks the ray hits nearest-first; the first solid thing that is not the player decides.</summary>
        bool FindCompanion()
        {
            var origin = interactor.Origin;
            int count = Physics.RaycastNonAlloc(origin.position, origin.forward, hits, aimDistance, aimMask,
                QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            Array.Sort(hits, 0, count, ByDistance);

            for (int i = 0; i < count; i++)
            {
                var hit = hits[i].collider;
                if (hit.transform.IsChildOf(transform)) continue;
                return hit.GetComponentInParent<ICompanion>() != null;
            }

            return false;
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
