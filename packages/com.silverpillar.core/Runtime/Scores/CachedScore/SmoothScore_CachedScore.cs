using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SmoothScore_CachedScore : ICachedScore
    {
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_CachedScore;

        [SerializeField, Min(0)]
        private float m_MinChangePerScoreCalculation = 0f;

        [SerializeField, Min(float.MinValue)] 
        private float m_MaxChangePerScoreCalculation = 0.1f;

        private float m_CurrentValue;
        private bool m_IsInitialized;

        public SmoothScore_CachedScore() { }

        public SmoothScore_CachedScore(SmoothScore_CachedScore other)
        {
            m_CachedScore = other.m_CachedScore?.Clone();
            m_MinChangePerScoreCalculation = other.m_MinChangePerScoreCalculation;
            m_MaxChangePerScoreCalculation = other.m_MaxChangePerScoreCalculation;
            m_CurrentValue = other.m_CurrentValue;
            m_IsInitialized = other.m_IsInitialized;
        }

        public float CalculateScore()
        {
            float targetValue = m_CachedScore != null ? m_CachedScore.CalculateScore() : 0f;

            // If it's the first execution, skip smoothing to avoid starting from 0
            if (!m_IsInitialized)
            {
                m_CurrentValue = targetValue;
                m_IsInitialized = true;
                return m_CurrentValue;
            }

            float diff = targetValue - m_CurrentValue;
            float absDiff = Mathf.Abs(diff);

            if (absDiff > 0)
            {
                // Apply change limits
                float clampedDiff = Mathf.Clamp(absDiff, m_MinChangePerScoreCalculation, m_MaxChangePerScoreCalculation);

                // Ensure we don't overshoot the target if MinChange is high
                float finalChange = Mathf.Min(absDiff, clampedDiff);

                m_CurrentValue += finalChange * Mathf.Sign(diff);
            }

            return m_CurrentValue;
        }


        public ICachedScore Clone()
        {
            return new SmoothScore_CachedScore(this);
        }

        public GameObject GetGameObject()
        {
            return m_CachedScore?.GetGameObject();
        }

        public bool SetGameObject(GameObject self)
        {
            return m_CachedScore != null && m_CachedScore.SetGameObject(self);
        }
    }
}
