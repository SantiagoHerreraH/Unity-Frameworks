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

        public float CalculateScore()
        {
            if (m_FollowerEntity == null) return 0f; 
            
            if (!m_FollowerEntity.gameObject.activeInHierarchy)
            {
                return 0f;
            }

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

        public ICachedScore Clone()
        {
            return new FollowerEntitySpeed_CachedScore
            {
                m_SpeedFromWho = this.m_SpeedFromWho,
                m_FollowerEntity = this.m_FollowerEntity
            };
        }

        public GameObject? GetGameObject() => m_FollowerEntity != null ? m_FollowerEntity.gameObject : null;

        public bool SetGameObject(GameObject self)
        {
            if (self == null) return false;

            if (m_SpeedFromWho == SelfType.CustomGameObject)
            {
                return m_FollowerEntity != null;
            }

            return self.TryGetComponent<FollowerEntity>(out m_FollowerEntity);
        }
    }
}
