using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SequentialChoosers<TOption> : IChooseData<TOption>
    {
        [Title("Settings")]
        [SerializeField]
        private ChoosingOrder m_ChoosingOrder;

        [OdinSerialize, ShowInInspector, Tooltip("Will be clamped between 1 and Data Count")]
        private IntCachedScore m_NumberOfChoosersToChooseFrom;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private List<IChooseData<TOption>> m_Data;

        private List<TOption> m_Chosen = new();
        private GameObject m_GameObject;

        private int m_CurrentIndex;
        private int m_BoomerangDirection = 1;

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

            int amountToReturn = Mathf.Clamp(GetIntScore(m_NumberOfChoosersToChooseFrom, 1), 1, m_Data.Count);

            for (int i = 0; i < amountToReturn; i++)
            {
                IChooseData<TOption> selected = GetNext();
                m_Chosen.AddRange(selected.ChooseData());
            }

            return m_Chosen;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            SetGameObjectOnScore(m_NumberOfChoosersToChooseFrom, gameObj);

            for (int i = 0; i < m_Data.Count; i++)
            {
                m_Data[i].SetGameObject(gameObj);
            }
            return true;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public IChooseData<TOption> Clone()
        {
            SequentialChoosers<TOption> clone = new()
            {
                m_ChoosingOrder = m_ChoosingOrder,
                m_NumberOfChoosersToChooseFrom = m_NumberOfChoosersToChooseFrom?.Clone() as IntCachedScore,
                m_Data = new(),
                m_CurrentIndex = 0,
                m_BoomerangDirection = 1
            };

            if (m_Data != null)
            {
                foreach (var data in m_Data)
                {
                    clone.m_Data.Add(data.Clone());
                }
            }

            clone.SetGameObject(m_GameObject);
            return clone;
        }
        private IChooseData<TOption> GetNext()
        {
            switch (m_ChoosingOrder)
            {
                case ChoosingOrder.Random:
                    return m_Data[RandomController.Instance.Range(0, m_Data.Count)];

                case ChoosingOrder.BoomerangInOrder:
                    return GetNextBoomerang();

                case ChoosingOrder.LoopInOrder:
                default:
                    return GetNextLoop();
            }
        }

        private IChooseData<TOption> GetNextLoop()
        {
            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Data.Count - 1);

            IChooseData<TOption> selected = m_Data[m_CurrentIndex];
            m_CurrentIndex = (m_CurrentIndex + 1) % m_Data.Count;

            return selected;
        }

        private IChooseData<TOption> GetNextBoomerang()
        {
            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Data.Count - 1);

            IChooseData<TOption> selected = m_Data[m_CurrentIndex];

            if (m_Data.Count <= 1)
            {
                return selected;
            }

            m_CurrentIndex += m_BoomerangDirection;

            if (m_CurrentIndex >= m_Data.Count)
            {
                m_CurrentIndex = m_Data.Count - 2;
                m_BoomerangDirection = -1;
            }
            else if (m_CurrentIndex < 0)
            {
                m_CurrentIndex = 1;
                m_BoomerangDirection = 1;
            }

            return selected;
        }

        private static int GetIntScore(IntCachedScore score, int defaultValue)
        {
            if (score == null)
            {
                return defaultValue;
            }

            return score.CalculateScoreAsInt();
        }

        private static void SetGameObjectOnScore(IntCachedScore score, GameObject gameObj)
        {
            score?.SetGameObject(gameObj);
        }
    }
}
