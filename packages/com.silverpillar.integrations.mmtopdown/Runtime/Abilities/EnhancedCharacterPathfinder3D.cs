using MoreMountains.TopDownEngine;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace SilverPillar.Integrations.MMTopDown
{
    public class EnhancedCharacterPathfinder3D : CharacterPathfinder3D
    {
        [Header("Refresh Path Upon Destination Arrival")]
        [SerializeField, Min(0.01f), Tooltip("Once arrived to destination, " +
            "will refresh path if target position is further from this distance.")]
        private float m_RefreshPathDistance = 1;

        protected override void Initialization()
        {
            if (_initialized)
            {
                return;
            }
            base.Initialization();
            AgentPath = new NavMeshPath();
            _lastValidTargetPosition = this.transform.position;
            Array.Resize(ref Waypoints, 5);
            _initialized = true;
        }

        /// <summary>
		/// Sets a new destination the character will pathfind to
		/// </summary>
		/// <param name="destinationTransform"></param>
		public void SetNewDestination(GameObject target)
        {
            if (target == null)
            {
                Target = null;
                return;
            }

            Initialization();

            Target = target.transform;
            TargetPositionSet = true;
            ShouldMoveToTarget = true;
            DeterminePath(this.transform.position, Target.position, true);
        }

        protected override void PerformRefresh()
        {
            if (Target != null)
            {
                TargetPosition = Target.position;
            }

            if (!TargetPositionSet)
            {
                return;
            }

            if (PathRefreshMode == PathRefreshModes.None)
            {
                return;
            }

            float currentSqrDistance = (TargetPosition - this.transform.position).sqrMagnitude;
            float desiredSqrDistance = m_RefreshPathDistance * m_RefreshPathDistance;

            if (currentSqrDistance <= desiredSqrDistance && NextWaypointIndex <= 0)
            {
                return;
            }

            bool refreshNeeded = false;

            if (Time.time - _lastRequestAt > RefreshInterval)
            {
                refreshNeeded = true;
                _lastRequestAt = Time.time;
            }

            if (PathRefreshMode == PathRefreshModes.SpeedThresholdBased)
            {
                if (_controller.Speed.magnitude > RefreshSpeedThreshold)
                {
                    refreshNeeded = false;
                }
            }

            if (refreshNeeded)
            {
                DeterminePath(this.transform.position, TargetPosition, true);
            }
        }
    }
}
