using System;
using System.Collections.Generic;
using Espionage.Core;
using NUnit.Framework;

namespace Espionage.NPC.Tests
{
    public class MainframeAccessGateTests
    {
        private sealed class FakeObjectiveState : IObjectiveState
        {
            private readonly HashSet<ObjectiveId> _completed = new HashSet<ObjectiveId>();

            public event Action<ObjectiveId> ObjectiveCompleted;

            public bool IsComplete(ObjectiveId id) => _completed.Contains(id);

            public bool TryComplete(ObjectiveId id)
            {
                if (id == ObjectiveId.None || !_completed.Add(id))
                    return false;

                ObjectiveCompleted?.Invoke(id);
                return true;
            }
        }

        private static NpcParseResult ReplyWith(NpcDecision decision)
            => NpcParseResult.Ok(new NpcReply(decision, "test"));

        [Test]
        public void Grant_CompletesMainframeAccess()
        {
            var state = new FakeObjectiveState();
            var gate = new MainframeAccessGate(state);

            bool granted = gate.Apply(ReplyWith(NpcDecision.Grant));

            Assert.IsTrue(granted);
            Assert.IsTrue(state.IsComplete(ObjectiveId.MainframeAccessGranted));
        }

        [Test]
        public void Deny_DoesNotCompleteAnything()
        {
            var state = new FakeObjectiveState();
            var gate = new MainframeAccessGate(state);

            bool granted = gate.Apply(ReplyWith(NpcDecision.Deny));

            Assert.IsFalse(granted);
            Assert.IsFalse(state.IsComplete(ObjectiveId.MainframeAccessGranted));
        }

        [Test]
        public void Undecided_DoesNotCompleteAnything()
        {
            var state = new FakeObjectiveState();
            var gate = new MainframeAccessGate(state);

            bool granted = gate.Apply(ReplyWith(NpcDecision.Undecided));

            Assert.IsFalse(granted);
            Assert.IsFalse(state.IsComplete(ObjectiveId.MainframeAccessGranted));
        }

        [Test]
        public void FailedParse_DoesNotCompleteAnything()
        {
            var state = new FakeObjectiveState();
            var gate = new MainframeAccessGate(state);

            bool granted = gate.Apply(NpcParseResult.Fail("bozuk cevap"));

            Assert.IsFalse(granted);
            Assert.IsFalse(state.IsComplete(ObjectiveId.MainframeAccessGranted));
        }

        [Test]
        public void GrantTwice_SecondCallReturnsFalse()
        {
            var state = new FakeObjectiveState();
            var gate = new MainframeAccessGate(state);

            gate.Apply(ReplyWith(NpcDecision.Grant));
            bool secondTime = gate.Apply(ReplyWith(NpcDecision.Grant));

            Assert.IsFalse(secondTime);
        }
    }
}