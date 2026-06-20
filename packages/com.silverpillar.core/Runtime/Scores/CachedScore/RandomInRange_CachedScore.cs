using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class RandomInRange_CachedScore : ICachedScore
    {
        [SerializeField]
        private float m_MinInclusive;
        [SerializeField]
        private float m_MaxInclusive;

        private GameObject m_Self;

        public RandomInRange_CachedScore() { }
        public RandomInRange_CachedScore(RandomInRange_CachedScore other)
        {
            m_MaxInclusive = other.m_MaxInclusive;
            m_MinInclusive = other.m_MinInclusive;
            m_Self = other.m_Self;
        }

        public float CalculateScore()
        {
            return RandomController.Instance.Range(m_MinInclusive, m_MaxInclusive);
        }

        public ICachedScore Clone()
        {
            return new RandomInRange_CachedScore(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject self)
        {
            m_Self = self;
            return m_Self != null;
        }
    }
}
