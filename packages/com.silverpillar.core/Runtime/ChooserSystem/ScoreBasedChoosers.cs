
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ScoreBasedChoosers<TOption> : IChooseData<TOption>
    {
        [Serializable]
        public struct ScoreData<ValueType>
        {
            [OdinSerialize, ShowInInspector, Tooltip("If null, 0")]
            public ICachedScore Score;

            [OdinSerialize]
            public IChooseData<ValueType> Data;
        }

        public enum HowToChoose
        {
            ChooseFromHighestToLowest,
            ChooseFromLowestToHighest
        }

        [Title("Choosing Settings")]
        [SerializeField]
        private HowToChoose m_HowToChoose;

        [OdinSerialize, ShowInInspector]
        private IntCachedScore m_MaxNumberOfInstancesToChoose;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private List<ScoreData<TOption>> m_Data;

        private GameObject m_GameObject;

        List<TOption> m_Chosen = new();

        public List<TOption> ChooseData()
        {
            if (m_Chosen == null)
            {
                m_Chosen = new();
            }

            m_Chosen.Clear();

            if (m_Data == null || m_Data.Count == 0)
            {
                return m_Chosen;
            }


            int max = Mathf.Clamp(GetIntScore(m_MaxNumberOfInstancesToChoose, m_Data.Count), 0, m_Data.Count);

            List<ScoreData<TOption>> sortedTypes = new(m_Data);

            sortedTypes.Sort((a, b) =>
            {
                float scoreA = GetScore(a.Score);
                float scoreB = GetScore(b.Score);

                if (m_HowToChoose == HowToChoose.ChooseFromHighestToLowest)
                {
                    return scoreB.CompareTo(scoreA);
                }

                return scoreA.CompareTo(scoreB);
            });

            for (int i = 0; i < sortedTypes.Count && m_Chosen.Count < max; i++)
            {
                m_Chosen.AddRange(sortedTypes[i].Data.ChooseData());
            }

            return m_Chosen;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            m_MaxNumberOfInstancesToChoose?.SetGameObject(gameObj);

            if (m_Data != null)
            {
                for (int i = 0; i < m_Data.Count; i++)
                {
                    m_Data[i].Score?.SetGameObject(gameObj);
                    m_Data[i].Data?.SetGameObject(gameObj);
                }
            }

            return true;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public IChooseData<TOption> Clone()
        {
            ScoreBasedChoosers<TOption> clone = new()
            {
                m_HowToChoose = m_HowToChoose,
                m_MaxNumberOfInstancesToChoose = m_MaxNumberOfInstancesToChoose?.Clone() as IntCachedScore,
                m_Data = new List<ScoreData<TOption>>()
            };

            if (m_Data != null)
            {
                foreach (ScoreData<TOption> type in m_Data)
                {
                    clone.m_Data.Add(new ScoreData<TOption>
                    {
                        Score = type.Score?.Clone(),
                        Data = type.Data.Clone()
                    });
                }
            }

            clone.SetGameObject(m_GameObject);
            return clone;
        }

        private static float GetScore(ICachedScore score)
        {
            if (score == null)
            {
                return 0f;
            }

            return score.CalculateScore();
        }

        private static int GetIntScore(IntCachedScore score, int defaultValue)
        {
            if (score == null)
            {
                return defaultValue;
            }

            return score.CalculateScoreAsInt();
        }
    }
}