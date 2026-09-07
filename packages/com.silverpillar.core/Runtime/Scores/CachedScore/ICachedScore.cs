using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable
    public interface ICachedScore
    {
        public ICachedScore Clone();
        public GameObject? GetGameObject();
        public bool SetGameObject(GameObject self);
        public float CalculateScore();
    }

    public struct CachedScoreData
    {
        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Score;
        [SerializeField]
        private SelfType m_WhereToGetScoreGameObjectFrom;
        [SerializeField, ShowIf(nameof(m_WhereToGetScoreGameObjectFrom), SelfType.CustomGameObject)]
        private GameObject m_ScoreGameObject;

        public bool IsValid()
        {
            return m_Score != null && m_ScoreGameObject != null;
        }

        public CachedScoreData CloneData()
        {
            return new CachedScoreData { 
                m_Score = m_Score.Clone(), 
                m_ScoreGameObject = m_ScoreGameObject,
                m_WhereToGetScoreGameObjectFrom = m_WhereToGetScoreGameObjectFrom};
        }

        public ICachedScore Clone()
        {
            return m_Score.Clone();
        }
        public GameObject? GetGameObject()
        {
            return m_Score.GetGameObject();
        }
        public bool SetGameObject(GameObject self)
        {
            switch (m_WhereToGetScoreGameObjectFrom)
            {
                case SelfType.ThisGameObject:
                    m_ScoreGameObject = self;
                    return m_Score.SetGameObject(self);
                case SelfType.CustomGameObject:
                    return m_Score.SetGameObject(m_ScoreGameObject);
                default:
                    break;
            }

            return m_Score.SetGameObject(self);
        }
        public float CalculateScore()
        {
            return m_Score.CalculateScore();
        }
    }

    [Serializable]
    public class Constant_CachedScore : ICachedScore
    {
        [SerializeField]
        private float m_Value;

        public Constant_CachedScore() { }
        public Constant_CachedScore(float value) 
        {
            m_Value = value;
        }

        public Constant_CachedScore(Constant_CachedScore value)
        {
            m_Value = value.m_Value;
        }

        public float CalculateScore()
        {
            return m_Value;
        }

        public ICachedScore Clone()
        {
            return new Constant_CachedScore { m_Value = this.m_Value };
        }

        public GameObject? GetGameObject()
        {
            return null;
        }

        public bool SetGameObject(GameObject self)
        {
            return true;
        }
    }

}

