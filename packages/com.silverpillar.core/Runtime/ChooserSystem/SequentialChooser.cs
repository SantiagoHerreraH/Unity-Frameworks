using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum ChoosingOrder
    {
        LoopInOrder,
        BoomerangInOrder,
        Random
    }

    [Serializable]
    public class SequentialChooser<TOption> : IChooseData<TOption> 
    {
        [Title("Settings")]
        [SerializeField]
        private ChoosingOrder m_ChoosingOrder;

        [OdinSerialize, ShowInInspector, Tooltip("Will be clamped between 1 and Data Count")]
        private IntCachedScore m_NumberOfInstancesToReturn;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private List<ChoosingOption<TOption>> m_Data;

        private GameObject m_GameObject;

        private int m_CurrentIndex;
        private int m_BoomerangDirection = 1;

        public List<TOption> ChooseData()
        {
            List<TOption> generated = new();

            if (m_Data == null || m_Data.Count == 0)
            {
                return generated;
            }

            int amountToReturn = Mathf.Clamp(GetIntScore(m_NumberOfInstancesToReturn, 1), 1, m_Data.Count);

            for (int i = 0; i < amountToReturn; i++)
            {
                ChoosingOption<TOption> selected = GetNext();
                generated.Add(selected.Value);
            }

            return generated;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            SetGameObjectOnScore(m_NumberOfInstancesToReturn, gameObj);

            for (int i = 0; i < m_Data.Count; i++)
            {
                m_Data[i].Initialize(gameObj);
            }
            return true;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public IChooseData<TOption> Clone()
        {
            SequentialChooser<TOption> clone = new()
            {
                m_ChoosingOrder = m_ChoosingOrder,
                m_NumberOfInstancesToReturn = m_NumberOfInstancesToReturn?.Clone() as IntCachedScore,
                m_Data = new(),
                m_CurrentIndex = 0,
                m_BoomerangDirection = 1
            };

            if (m_Data != null)
            {
                foreach (ChoosingOption<TOption> data in m_Data)
                {
                    clone.m_Data.Add(data.Clone());
                }
            }

            clone.SetGameObject(m_GameObject);
            return clone;
        }
        private ChoosingOption<TOption> GetNext()
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

        private ChoosingOption<TOption> GetNextLoop()
        {
            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Data.Count - 1);

            ChoosingOption<TOption> selected = m_Data[m_CurrentIndex];
            m_CurrentIndex = (m_CurrentIndex + 1) % m_Data.Count;

            return selected;
        }

        private ChoosingOption<TOption> GetNextBoomerang()
        {
            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Data.Count - 1);

            ChoosingOption<TOption> selected = m_Data[m_CurrentIndex];

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