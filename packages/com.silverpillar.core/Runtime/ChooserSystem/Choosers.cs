using Sirenix.OdinInspector;
using Sirenix.Serialization;
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
        private List<TOption> m_Chosen;
        private GameObject m_Self;

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
        public List<TOption> ChooseData()
        {
            if (m_Chosen == null) m_Chosen = new();
            m_Chosen.Clear();

            for (int i = 0; i < m_Choosers.Count; i++)
            {
                m_Chosen.AddRange(m_Choosers[i].ChooseData());
            }
            return m_Chosen;
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
