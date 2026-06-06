using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChooseCachedScore : ICachedScore, IChoose
    {
        [SerializeField]
        private HowToCalculateScore m_HowToCalculateScore = HowToCalculateScore.ChooseFirst;
        [OdinSerialize, ShowInInspector]
        private IChooseData<ICachedScore> m_Chooser;
        public IChooseData<ICachedScore> Chooser => m_Chooser;
        private List<ICachedScore> m_ChosenScores;
        private List<float> m_Scores;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseCachedScore() { }
        public ChooseCachedScore(ChooseCachedScore other)
        {
            m_Chooser = other.Chooser.Clone();

            if (other.m_ChosenScores != null)
            {
                if (m_ChosenScores == null)
                {
                    m_ChosenScores = new();
                }

                for (int i = 0; i < other.m_ChosenScores.Count; i++)
                {
                    m_ChosenScores.Add(other.m_ChosenScores[i].Clone());
                }
            }
        }

        public void Choose()
        {
            m_ChosenScores = m_Chooser.ChooseData();

            if (m_Scores == null)
            {
                m_Scores = new();
            }
            m_Scores.Capacity = m_Scores.Capacity < m_ChosenScores.Count ? m_ChosenScores.Count : m_Scores.Capacity;
        }

        public ICachedScore Clone()
        {
            return new ChooseCachedScore(this);
        }

        public float CalculateScore()
        {
            if (m_ChosenScores == null || m_ChosenScores.Count == 0)
            {
                return 0;
            }

            m_Scores.Clear();

            for (int i = 0; i < m_ChosenScores.Count; i++)
            {
                if (m_ChosenScores[i] != null)
                {
                    m_Scores.Add(m_ChosenScores[i].CalculateScore());
                }
            }

            return ScoreTools.CalculateScore(m_HowToCalculateScore, m_Scores);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_InitializedCorrectly = true;
            m_Self = gameObj;
            m_InitializedCorrectly &= m_Self != null;

            m_InitializedCorrectly &= m_Chooser == null ? false : m_Chooser.SetGameObject(gameObj);

            if (m_ChosenScores != null)
            {
                for (int i = 0; i < m_ChosenScores.Count; i++)
                {
                    m_InitializedCorrectly &= m_ChosenScores[i] == null ? false : m_ChosenScores[i].SetGameObject(gameObj);
                }
            }

            return m_InitializedCorrectly;
        }
    }
}
