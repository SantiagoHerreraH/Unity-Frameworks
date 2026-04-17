using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ScoreIfCondition_CachedScore : ICachedScore
    {
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_Condition;

        [SerializeField]
        private float m_ScoreIfFalse;

        [SerializeField]
        private float m_ScoreIfTrue;

        public ScoreIfCondition_CachedScore() { }
        public ScoreIfCondition_CachedScore(ScoreIfCondition_CachedScore other)
        {
            m_Condition = other.m_Condition.Clone();
            m_ScoreIfFalse = other.m_ScoreIfFalse;
            m_ScoreIfTrue = other.m_ScoreIfTrue;
        }

        public float CalculateScore()
        {
            if (m_Condition.IsFulfilled())
            {
                return m_ScoreIfTrue;
            }

            return m_ScoreIfFalse;
        }

        public ICachedScore Clone()
        {
            return new ScoreIfCondition_CachedScore(this);
        }

        public GameObject GetGameObject()
        {
            return m_Condition.GetGameObject();
        }

        public bool SetGameObject(GameObject self)
        {
            return m_Condition.SetGameObject(self);
        }
    }
}
