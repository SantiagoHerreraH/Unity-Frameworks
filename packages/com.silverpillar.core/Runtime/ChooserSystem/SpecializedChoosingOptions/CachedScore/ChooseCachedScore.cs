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
        private bool m_AlwaysCallChooseWhenCalculatingScore = false;
        [SerializeField]
        private HowToCalculateScore m_HowToCalculateScore = HowToCalculateScore.ChooseFirst;
        [OdinSerialize, ShowInInspector]
        private IChooseData<ICachedScore> m_Chooser;
        public IChooseData<ICachedScore> Chooser => m_Chooser;
        private DataFromChoosing<ICachedScore> m_DataFromChoosing;
        private List<float> m_Scores;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseCachedScore() { }
        public ChooseCachedScore(ChooseCachedScore other)
        {
            m_Chooser = other.Chooser.Clone();

            m_DataFromChoosing.Clear();

            for (int i = 0; i < other.m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_DataFromChoosing.AddChosen(other.m_DataFromChoosing.ChosenData[i].Clone());
            }

            for (int i = 0; i < other.m_DataFromChoosing.NotChosenData.Count; i++)
            {
                m_DataFromChoosing.AddNotChosen(other.m_DataFromChoosing.ChosenData[i].Clone());

            }
        }

        public void Choose()
        {
            m_DataFromChoosing = m_Chooser.ChooseData();

            if (m_Scores == null)
            {
                m_Scores = new();
            }
            m_Scores.Capacity = m_Scores.Capacity < m_DataFromChoosing.ChosenData.Count ? m_DataFromChoosing.ChosenData.Count : m_Scores.Capacity;
        }

        public ICachedScore Clone()
        {
            return new ChooseCachedScore(this);
        }

        public float CalculateScore()
        {
            if (m_AlwaysCallChooseWhenCalculatingScore)
            {
                Choose();
            }

            if (m_DataFromChoosing.ChosenData.Count == 0)
            {
                return 0;
            }

            m_Scores.Clear();

            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                if (m_DataFromChoosing.ChosenData[i] != null)
                {
                    m_Scores.Add(m_DataFromChoosing.ChosenData[i].CalculateScore());
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

            for (int i = 0; i < m_DataFromChoosing.ChosenData.Count; i++)
            {
                m_InitializedCorrectly &= m_DataFromChoosing.ChosenData[i] == null ? false : m_DataFromChoosing.ChosenData[i].SetGameObject(gameObj);
            }

            return m_InitializedCorrectly;
        }
    }
}
