using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedScore_CachedInteractionScore : ICachedInteractionScore
    {
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Score = null;

        [SerializeField]
        private TargetType m_OnWhoToCalculateScore;

        public CachedScore_CachedInteractionScore() { }

        public CachedScore_CachedInteractionScore(CachedScore_CachedInteractionScore other)
        {
            m_Score = other.m_Score.Clone();
            m_OnWhoToCalculateScore = other.m_OnWhoToCalculateScore;
        }

        public float CalculateScore(GameObject target)
        {
            if (m_OnWhoToCalculateScore == TargetType.Other)
            {
                m_Score.SetGameObject(target);
            }

            return m_Score.CalculateScore();
        }

        public ICachedInteractionScore Clone()
        {
            return new CachedScore_CachedInteractionScore(this);
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_Score != null ? m_Score.GetGameObject() : null;
        }

        public bool SetGameObject(GameObject self)
        {
            if (self != null && m_Score != null && m_OnWhoToCalculateScore == TargetType.Self)
            {
                return m_Score.SetGameObject(self);
            }
            return false;
        }
    }
}
