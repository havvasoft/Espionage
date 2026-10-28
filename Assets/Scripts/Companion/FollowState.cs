using Espionage.Core;
using UnityEngine;

namespace Espionage.Companion
{
    /// <summary>
    /// Keeps the companion near the leader. Distances are measured along the NavMesh route, so a wall between
    /// the two does not fool it, and it speeds up when it has fallen far behind.
    /// </summary>
    public sealed class FollowState : ICompanionState
    {
        readonly CompanionNavigator navigator;
        readonly Transform leader;
        readonly float followDistance;
        readonly float stopDistance;
        readonly float repathInterval;
        readonly float catchUpDistance;
        readonly float catchUpMultiplier;

        float repathTimer;
        bool moving;

        public CompanionStateId Id => CompanionStateId.Follow;

        public FollowState(CompanionNavigator navigator, Transform leader, float followDistance, float stopDistance,
            float repathInterval, float catchUpDistance, float catchUpMultiplier)
        {
            this.navigator = navigator;
            this.leader = leader;
            this.followDistance = followDistance;
            this.stopDistance = stopDistance;
            this.repathInterval = repathInterval;
            this.catchUpDistance = catchUpDistance;
            this.catchUpMultiplier = catchUpMultiplier;
        }

        public void Enter()
        {
            repathTimer = 0f;
            moving = false;
            navigator.SetStoppingDistance(stopDistance);
        }

        public void Exit()
        {
            navigator.Stop();
            navigator.ResetMovementSettings();
        }

        public void Tick(float deltaTime)
        {
            if (leader == null || !navigator.IsReady) return;

            repathTimer -= deltaTime;
            if (repathTimer > 0f) return;
            repathTimer = repathInterval;

            // No complete route to the leader (e.g. mid-jump): keep doing what we were doing.
            if (!navigator.TryGetWalkingDistance(leader.position, out float walking)) return;

            if (walking <= stopDistance)
            {
                if (moving) navigator.Stop();
                moving = false;
                return;
            }

            if (!moving && walking < followDistance) return;

            moving = navigator.TryMoveTo(leader.position);
            navigator.SetSpeedMultiplier(walking > catchUpDistance ? catchUpMultiplier : 1f);
        }
    }
}
