using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace SilverPillar.Core
{
    [Serializable]
    public class ChooseValue<T> : IChoose
    {
        [OdinSerialize, ShowInInspector]
        private List<T> m_ActionsToAlwaysChoose;
        [OdinSerialize, ShowInInspector]
        private IChooseData<T> m_Chooser;
        public IChooseData<T> Chooser => m_Chooser;
        private DataFromChoosing<T> m_DataFromChoosing;

        public ChooseValue() { }
        public ChooseValue(ChooseValue<T> other)
        {
            m_Chooser = other.Chooser.Clone();

            m_DataFromChoosing.Clear();
            m_DataFromChoosing.Append(other.m_DataFromChoosing);

            if (other.m_ActionsToAlwaysChoose != null)
            {
                for (int i = 0; i < other.m_ActionsToAlwaysChoose.Count; i++)
                {
                    m_ActionsToAlwaysChoose.Add(other.m_ActionsToAlwaysChoose[i]);
                }
            }
        }

        public void Choose()
        {
            m_DataFromChoosing = m_Chooser.ChooseData();
        }

        public ChooseValue<T> Clone()
        {
            return new ChooseValue<T>(this);
        }

        public List<T> GetChosenValues()
        {
            return m_DataFromChoosing.ChosenData;
        }
        public List<T> GetNotChosenValues()
        {
            return m_DataFromChoosing.NotChosenData;
        }
        public List<T> AllValues()
        {
            return m_DataFromChoosing.AllData();
        }
    }
}
