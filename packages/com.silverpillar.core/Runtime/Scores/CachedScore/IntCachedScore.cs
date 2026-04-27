using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class IntCachedScore : ICachedScore
    {
        public enum HowToRoundVariable
        {
            Floor,
            Ceil,
            StandardMathRounding
        }

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Score;

        [SerializeField]
        private HowToRoundVariable m_HowToRoundVariable;

        public bool SetGameObject(GameObject gameObj)
        {
            return m_Score.SetGameObject(gameObj);
        }

        public GameObject GetGameObject()
        {
            return m_Score.GetGameObject();
        }

        public float CalculateScore()
        {
            return CalculateScoreAsInt();   
        }

        public int CalculateScoreAsInt()
        {
            if (m_Score == null) return 0;

            float rawScore = m_Score.CalculateScore();

            switch (m_HowToRoundVariable)
            {
                case HowToRoundVariable.Floor:
                    return Mathf.FloorToInt(rawScore);

                case HowToRoundVariable.Ceil:
                    return Mathf.CeilToInt(rawScore);

                case HowToRoundVariable.StandardMathRounding:
                    return Mathf.RoundToInt(rawScore);

                default:
                    return (int)rawScore;
            }
        }

        public ICachedScore Clone()
        {
            return new IntCachedScore
            {
                m_Score = m_Score?.Clone(),
                m_HowToRoundVariable = m_HowToRoundVariable
            };
        }
    }
}
