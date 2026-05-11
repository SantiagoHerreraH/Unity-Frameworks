using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Pathfinding;

namespace SilverPillar.Core
{
    [Serializable]
    public class FollowerEntityAngularSpeed_CachedScore: ICachedScore
    {
#nullable enable
        [SerializeField]
        private SelfType m_SpeedFromWho;

        [SerializeField, ShowIf(nameof(m_SpeedFromWho), SelfType.CustomGameObject)]
        private FollowerEntity? m_FollowerEntity;

        public float CalculateScore()
        {
            if (m_FollowerEntity == null) return 0f;
            if (!m_FollowerEntity.gameObject.activeInHierarchy)
            {
                return 0f;
            }

            return m_FollowerEntity.rotationSpeed;
        }

        public ICachedScore Clone()
        {
            return new FollowerEntityAngularSpeed_CachedScore
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
