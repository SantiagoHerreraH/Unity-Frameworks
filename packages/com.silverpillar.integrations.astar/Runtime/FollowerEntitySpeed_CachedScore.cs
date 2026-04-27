using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Pathfinding;

namespace SilverPillar.Core
{
    public enum VelocityType
    {
        Velocity,
        DesiredVelocity,
        DesiredVelocityWithoutLocalAvoidance
    }


    [Serializable]
    public class FollowerEntitySpeed_CachedScore : ICachedScore
    {
        

#nullable enable
        [SerializeField]
        private SelfType m_SpeedFromWho;
        [SerializeField]
        private VelocityType m_VelocityType;

        [SerializeField, ShowIf(nameof(m_SpeedFromWho), SelfType.CustomGameObject)]
        private FollowerEntity? m_FollowerEntity;

        /// <summary>
        /// Returns the magnitude of the FollowerEntity's current velocity.
        /// </summary>
        public float CalculateScore()
        {
            // If no entity is assigned, speed is zero
            if (m_FollowerEntity == null) return 0f;

            // FollowerEntity (A*) uses .velocity to report its current movement vector

            switch (m_VelocityType)
            {
                case VelocityType.Velocity:
                    return m_FollowerEntity.velocity.magnitude;

                case VelocityType.DesiredVelocity:
                    return m_FollowerEntity.desiredVelocity.magnitude;

                case VelocityType.DesiredVelocityWithoutLocalAvoidance:
                    return m_FollowerEntity.desiredVelocityWithoutLocalAvoidance.magnitude;
                default:
                    return m_FollowerEntity.velocity.magnitude;
            }

        }

        /// <summary>
        /// Creates a copy of this score provider.
        /// </summary>
        public ICachedScore Clone()
        {
            return new FollowerEntitySpeed_CachedScore
            {
                m_SpeedFromWho = this.m_SpeedFromWho,
                m_FollowerEntity = this.m_FollowerEntity
            };
        }

        public GameObject? GetGameObject() => m_FollowerEntity != null ? m_FollowerEntity.gameObject : null;

        /// <summary>
        /// Sets the reference. If set to Self, it looks for the FollowerEntity on the provided GameObject.
        /// </summary>
        public bool SetGameObject(GameObject self)
        {
            if (self == null) return false;

            // If configured to use a specific manual reference, check if it's assigned
            if (m_SpeedFromWho == SelfType.CustomGameObject)
            {
                return m_FollowerEntity != null;
            }

            // Otherwise, try to find the component on the target GameObject
            return self.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }
    }
}
