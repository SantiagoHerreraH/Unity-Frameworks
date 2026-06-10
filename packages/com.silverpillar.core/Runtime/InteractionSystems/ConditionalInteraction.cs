using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

namespace SilverPillar.Core
{
    [Serializable]
    public class ConditionalInteraction : IInteraction
    {
        [Serializable]
        public struct Data
        {
            [OdinSerialize, ShowInInspector, Tooltip("If null will return true")]
            public IInteractionCondition Condition;
            [OdinSerialize, ShowInInspector]
            public IInteraction Interaction;
        }

        public enum ExecutionType
        {
            ExecuteOnlyFirstToReturnTrue,
            ExecuteAllIfTrue
        }

        [SerializeField]
        private ExecutionType m_ExecutionType;
        [OdinSerialize, ShowInInspector]
        private List<Data> m_ConditionData;
        [OdinSerialize, ShowInInspector, Tooltip("Will be executed if all else return false.")]
        private IInteraction m_ElseInteraction;
        private GameObject m_Self;

        public ConditionalInteraction() { }
        public ConditionalInteraction(ConditionalInteraction other)
        {
            if (other.m_ConditionData != null)
            {
                m_ConditionData ??= new List<Data>();

                for (int i = 0; i < other.m_ConditionData.Count; i++)
                {
                    Data data = new();
                    data.Condition = other.m_ConditionData[i].Condition.Clone();
                    data.Interaction = other.m_ConditionData[i].Interaction.Clone();
                    m_ConditionData.Add(data);
                }
            }

            m_ElseInteraction = other.m_ElseInteraction.Clone();
        }

        public IInteraction Clone()
        {
            return new ConditionalInteraction(this);
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public void Interact(GameObject target)
        {
            if (m_ConditionData != null)
            {
                for (int i = 0; i < m_ConditionData.Count; i++)
                {
                    if (m_ConditionData[i].Condition.IsFulfilled(target))
                    {
                        m_ConditionData[i].Interaction.Interact(target);

                        if (m_ExecutionType == ExecutionType.ExecuteOnlyFirstToReturnTrue)
                        {
                            return;
                        }
                    }
                }
            }

            if (m_ElseInteraction != null)
            {
                m_ElseInteraction.Interact(target);
            }
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;

            bool allGood = m_Self != null;

            if (m_ConditionData != null)
            {
                for (int i = 0; i < m_ConditionData.Count; i++)
                {
                    allGood &= m_ConditionData[i].Condition.SetGameObject(self);
                    allGood &= m_ConditionData[i].Interaction.SetSelf(self);
                }
            }

            if (m_ElseInteraction != null)
            {
                allGood &= m_ElseInteraction.SetSelf(self);
            }

            return allGood;
        }

    }
}
