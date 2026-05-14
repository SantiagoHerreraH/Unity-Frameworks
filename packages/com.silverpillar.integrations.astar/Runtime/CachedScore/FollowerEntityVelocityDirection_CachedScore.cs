using Pathfinding;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class FollowerEntityVelocityDirection_CachedScore : ICachedScore
    {
#nullable enable
        [InfoBox(
            "Calculates the scalar projection of the current velocity onto the " +
            "custom axis relative to the object's forward orientation.")]
        [SerializeField] private Vector3 speedAxisVector = Vector3.forward;
        [SerializeField] private SelfType m_WhoToUseAsForwardVectorReference;
        [SerializeField, ShowIf(nameof(m_WhoToUseAsForwardVectorReference), SelfType.CustomGameObject)]
        private FollowerEntity? m_FollowerEntity;
        [SerializeField]
        private VelocityType m_VelocityType;

        public ICachedScore Clone()
        {
            return new FollowerEntityVelocityDirection_CachedScore
            {
                m_WhoToUseAsForwardVectorReference = this.m_WhoToUseAsForwardVectorReference,
                speedAxisVector = this.speedAxisVector,
                m_FollowerEntity = this.m_FollowerEntity
            };
        }

        public GameObject? GetGameObject() => m_FollowerEntity ? m_FollowerEntity.gameObject : null;

        public bool SetGameObject(GameObject self)
        {
            if (self == null) return false;

            if (m_WhoToUseAsForwardVectorReference != SelfType.CustomGameObject)
            {
                return self.TryGetComponent(out m_FollowerEntity);
            }

            return m_FollowerEntity != null;
        }

        public float CalculateScore()
        {
            if (m_FollowerEntity == null) return 0f; 
            
            if (!m_FollowerEntity.gameObject.activeInHierarchy)
            {
                return 0f;
            }

            Vector3 worldAxis = m_FollowerEntity.transform.TransformDirection(speedAxisVector.normalized);

            Vector3 velocity = m_FollowerEntity.velocity;

            switch (m_VelocityType)
            {
                case VelocityType.Velocity:
                    break;
                case VelocityType.DesiredVelocity:
                    velocity = m_FollowerEntity.desiredVelocity;
                    break;
                case VelocityType.DesiredVelocityWithoutLocalAvoidance:
                    velocity = m_FollowerEntity.desiredVelocityWithoutLocalAvoidance;
                    break;
                default:
                    break;
            }


            float projection = Vector3.Dot(velocity, worldAxis);

            return projection;
        }
    }
}
