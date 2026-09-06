using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Choosers<TOption> : IChooseData<TOption>
    {
        [OdinSerialize, ShowInInspector]
        private List<IChooseData<TOption>> m_Choosers;
        private DataFromChoosing<TOption> m_DataFromChoosing;
        private GameObject m_Self;
        private List<TOption> m_AllData;

        public Choosers() { }

        public Choosers(Choosers<TOption> other)
        {
            m_Self = other.m_Self;

            if (other.m_Choosers != null)
            {
                if (m_Choosers == null)
                {
                    m_Choosers = new();
                }

                foreach (var item in other.m_Choosers)
                {
                    m_Choosers.Add(item.Clone());
                }
            }
        }
        public DataFromChoosing<TOption> ChooseData()
        {
            m_DataFromChoosing.Clear();

            for (int i = 0; i < m_Choosers.Count; i++)
            {
                m_DataFromChoosing.Append(m_Choosers[i].ChooseData());
            }
            return m_DataFromChoosing;
        }
        public List<TOption> AllData()
        {
            m_AllData ??= new();
            m_AllData.Clear();

            for (int i = 0; i < m_Choosers.Count; i++)
            {
                m_AllData.AddRange(m_Choosers[i].AllData());
            }

            return m_AllData;
        }
        public List<TOption> GetChosenData()
        {
            return m_DataFromChoosing.ChosenData;
        }

        public List<TOption> GetNotChosenData()
        {
            return m_DataFromChoosing.NotChosenData;
        }

        public IChooseData<TOption> Clone()
        {
            return new Choosers<TOption>(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            bool allGood = m_Self != null;

            foreach (var item in m_Choosers)
            {
                allGood &= item.SetGameObject(gameObj);
            }

            return allGood;
        }
    }
}
