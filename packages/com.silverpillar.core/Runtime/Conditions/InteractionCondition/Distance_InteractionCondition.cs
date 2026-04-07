using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Distance_InteractionCondition : IInteractionCondition
    {
        [SerializeField]
        private FloatComparison.OperationType m_IfDistanceIs;
        [OdinSerialize, ShowInInspector] private ICachedScore m_ThanDistanceToCompare;
        [SerializeField]
        private float m_ValueIfNullScore = 0f;
        
        private GameObject m_Self;

        public IInteractionCondition Clone()
        {
            return new Distance_InteractionCondition
            {
                m_ThanDistanceToCompare = this.m_ThanDistanceToCompare.Clone(),
                m_Self = this.m_Self
            };
        }

        public GameObject GetGameObject() => m_Self;

        public bool SetGameObject(GameObject self)
        {
            m_ThanDistanceToCompare?.SetGameObject(self);
            m_Self = self;
            return m_Self != null;
        }

        public bool IsFulfilled(GameObject target)
        {
            if (m_Self == null || target == null) return false;

            float distanceSqr = (m_Self.transform.position - target.transform.position).sqrMagnitude;
            float score = m_ThanDistanceToCompare == null ? m_ValueIfNullScore : m_ThanDistanceToCompare.CalculateScore();
            return FloatComparison.Compare(distanceSqr, m_IfDistanceIs, score * score);
        }
    }
}
