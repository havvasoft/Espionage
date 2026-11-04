using System;
using System.Collections.Generic;
using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>Scene-wide record of completed level objectives.</summary>
    public sealed class ObjectiveState : MonoBehaviour, IObjectiveState
    {
        readonly HashSet<ObjectiveId> completed = new HashSet<ObjectiveId>();

        public event Action<ObjectiveId> ObjectiveCompleted;

        public bool IsComplete(ObjectiveId id) => completed.Contains(id);

        public bool TryComplete(ObjectiveId id)
        {
            if (id == ObjectiveId.None || !Enum.IsDefined(typeof(ObjectiveId), id)) return false;
            if (!completed.Add(id)) return false;

            Debug.Log($"Objective completed: {id}");
            ObjectiveCompleted?.Invoke(id);
            return true;
        }
    }
}
