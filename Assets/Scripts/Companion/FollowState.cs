using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>Keeps the companion near the player by re-planning a NavMesh path at a fixed interval.</summary>
    public sealed class FollowState : ICompanionState
    {
        readonly CompanionNavigator navigator;
        readonly Transform leader;
        readonly float followDistance;
        readonly float stopDistance;
        readonly float repathInterval;

        float repathTimer;

        public CompanionStateId Id => CompanionStateId.Follow;

        public FollowState(CompanionNavigator navigator, Transform leader,
            float followDistance, float stopDistance, float repathInterval)
        {
            this.navigator = navigator;
            this.leader = leader;
            this.followDistance = followDistance;
            this.stopDistance = stopDistance;
            this.repathInterval = repathInterval;
        }

        public void Enter() => repathTimer = 0f;

        public void Exit() => navigator.Stop();

        public void Tick(float deltaTime)
        {
            if (leader == null || !navigator.IsReady) return;

            Vector3 toLeader = leader.position - navigator.Position;
            toLeader.y = 0f;
            float distance = toLeader.magnitude;

            if (distance <= stopDistance)
            {
                navigator.Stop();
                return;
            }

            if (distance < followDistance && navigator.HasPath) return;

            repathTimer -= deltaTime;
            if (repathTimer > 0f) return;
            repathTimer = repathInterval;

            // Aim for a spot followDistance away from the leader, on the companion's side.
            Vector3 standOff = leader.position - toLeader.normalized * followDistance;
            navigator.TryMoveTo(standOff);
        }
    }
}
