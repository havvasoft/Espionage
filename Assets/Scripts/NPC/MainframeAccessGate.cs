using System;
using Espionage.Core;

namespace Espionage.NPC
{
    public sealed class MainframeAccessGate
    {
        private readonly IObjectiveState _objectives;

        public MainframeAccessGate(IObjectiveState objectives)
        {
            _objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
        }

        public bool Apply(NpcParseResult result)
        {
            if (result == null || !result.Success)
                return false;

            if (result.Reply.Decision != NpcDecision.Grant)
                return false;

            return _objectives.TryComplete(ObjectiveId.MainframeAccessGranted);
        }
    }
}