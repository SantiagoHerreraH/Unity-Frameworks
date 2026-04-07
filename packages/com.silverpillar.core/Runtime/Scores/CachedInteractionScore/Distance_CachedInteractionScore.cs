using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Distance_CachedInteractionScore : ICachedInteractionScore
    {
        [SerializeField]
        private float m_DistanceScoreMultiplier = 1f;

        private Transform m_SelfTransform = null;

        public Distance_CachedInteractionScore() { }
        public Distance_CachedInteractionScore(Distance_CachedInteractionScore other)
        {
            m_DistanceScoreMultiplier = other.m_DistanceScoreMultiplier;
            m_SelfTransform = other.m_SelfTransform;
        }

        public float CalculateScore(GameObject target)
        {
            float distance = (m_SelfTransform.position - target.transform.position).magnitude;

            return distance * m_DistanceScoreMultiplier;
        }

        public ICachedInteractionScore Clone()
        {
            return new Distance_CachedInteractionScore(this);
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_SelfTransform ? m_SelfTransform.gameObject : null;
        }

        public bool SetGameObject(GameObject self)
        {
            if (self != null)
            {
                m_SelfTransform = self.transform;
                return true;
            }

            return false;
        }
    }
}

