using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum WhereToStartArrayChoosing
    {
        First,
        Last,
        Random
    }

    [Serializable]
    public class ChoosingList<TOption>
    {
        [SerializeField]
        private List<TOption> m_List = new();

        public List<TOption> List => m_List;
        public int Count => m_List == null ? 0 : m_List.Count;

        [SerializeField]
        private WhereToStartArrayChoosing m_WhereToStartChoosing;

        [SerializeField]
        private ChoosingOrder m_ChoosingOrder;

        private int m_CurrentIndex;
        private int m_Direction = 1;
        private bool m_Initialized;

        public TOption ChooseNext()
        {
            if (m_List == null || m_List.Count == 0)
                return default;

            if (!m_Initialized)
            {
                m_CurrentIndex = GetStartIndex();
                m_Direction = 1;
                m_Initialized = true;
            }

            TOption result = m_List[m_CurrentIndex];

            MoveNext();

            return result;
        }

        public void EnsureDefault(TOption defaultValue)
        {
            m_List ??= new();

            if (m_List.Count == 0)
                m_List.Add(defaultValue);
        }

        public ChoosingList<TOption> Clone()
        {
            ChoosingList<TOption> clone = new ChoosingList<TOption>();

            clone.m_List = m_List != null ? new List<TOption>(m_List) : new();
            clone.m_WhereToStartChoosing = m_WhereToStartChoosing;
            clone.m_ChoosingOrder = m_ChoosingOrder;
            clone.m_CurrentIndex = m_CurrentIndex;
            clone.m_Direction = m_Direction;
            clone.m_Initialized = m_Initialized;

            return clone;
        }

        private int GetStartIndex()
        {
            if (m_List == null || m_List.Count == 0)
                return 0;

            switch (m_WhereToStartChoosing)
            {
                case WhereToStartArrayChoosing.First:
                    return 0;

                case WhereToStartArrayChoosing.Last:
                    return m_List.Count - 1;

                case WhereToStartArrayChoosing.Random:
                    return RandomController.Instance.Range(0, m_List.Count);

                default:
                    return 0;
            }
        }

        private void MoveNext()
        {
            if (m_List == null || m_List.Count <= 1)
                return;

            switch (m_ChoosingOrder)
            {
                case ChoosingOrder.LoopInOrder:
                    m_CurrentIndex = (m_CurrentIndex + 1) % m_List.Count;
                    break;

                case ChoosingOrder.BoomerangInOrder:
                    m_CurrentIndex += m_Direction;

                    if (m_CurrentIndex >= m_List.Count)
                    {
                        m_CurrentIndex = m_List.Count - 2;
                        m_Direction = -1;
                    }
                    else if (m_CurrentIndex < 0)
                    {
                        m_CurrentIndex = 1;
                        m_Direction = 1;
                    }

                    break;

                case ChoosingOrder.Random:
                    m_CurrentIndex = RandomController.Instance.Range(0, m_List.Count);
                    break;

                default:
                    m_CurrentIndex = (m_CurrentIndex + 1) % m_List.Count;
                    break;
            }
        }
    }
}
