using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Serialization.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChooseCachedCondition : ICachedCondition, IChoose
    {
        [SerializeField]
        private ConditionType m_ConditionType;
        [SerializeField]
        private bool m_ReturnValueOnNotCosen;
        [OdinSerialize, ShowInInspector]
        private IChooseData<ICachedCondition> m_Chooser;
        public IChooseData<ICachedCondition> Chooser => m_Chooser;
        private List<ICachedCondition> m_ChosenConditions;
        private GameObject m_Self;
        private bool m_InitializedCorrectly = false;

        public ChooseCachedCondition() { }
        public ChooseCachedCondition(ChooseCachedCondition other)
        {
            m_Chooser = other.Chooser.Clone();

            if (other.m_ChosenConditions != null)
            {
                if (m_ChosenConditions == null)
                {
                    m_ChosenConditions = new();
                }

                for (int i = 0; i < other.m_ChosenConditions.Count; i++)
                {
                    m_ChosenConditions.Add(other.m_ChosenConditions[i].Clone());
                }
            }
        }
        public ICachedCondition Clone()
        {
            return new ChooseCachedCondition(this);
        }

        public void Choose()
        {
            if (m_InitializedCorrectly)
            {
                m_ChosenConditions = m_Chooser.ChooseData();
            }
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

            if (m_ChosenConditions != null)
            {
                for (int i = 0; i < m_ChosenConditions.Count; i++)
                {
                    m_InitializedCorrectly &= m_ChosenConditions[i] == null ? false : m_ChosenConditions[i].SetGameObject(gameObj);
                }
            }

            return m_InitializedCorrectly;
        }


        public bool IsFulfilled()
        {
            if (m_ChosenConditions == null || m_ChosenConditions.Count == 0)
            {
                return m_ReturnValueOnNotCosen;
            }

            return CachedConditions.IsFulfilled(m_ConditionType, m_ChosenConditions);
        }
    }
}
