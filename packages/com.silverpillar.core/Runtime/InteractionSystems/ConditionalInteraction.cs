using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        public enum QueryOrder
        {
            TopToBottom,
            BottomToTop,
            AlternateDirection,
            Random
        }


        [Title("Settings")]
        [SerializeField]
        private QueryOrder m_QueryOrder;

        [SerializeField, ShowIf(nameof(m_QueryOrder), QueryOrder.Random)]
        [Range(0, 1)]
        [Tooltip("0 always starts from the top. 1 always starts from the bottom.")]
        private float m_StartTopToStartBottomPercentage;

        [SerializeField]
        private ExecutionType m_ExecutionType;


        [Title("Conditions")]
        [OdinSerialize, ShowInInspector]
        private List<Data> m_ConditionData;

        [OdinSerialize, ShowInInspector]
        [Tooltip("Will be executed if all else return false.")]
        private IInteraction m_ElseInteraction;

        private GameObject m_Self;

        private bool m_NextQueryStartsFromBottom = false;

        public ConditionalInteraction() { }

        public ConditionalInteraction(ConditionalInteraction other)
        {
            m_QueryOrder = other.m_QueryOrder;

            m_StartTopToStartBottomPercentage =
                other.m_StartTopToStartBottomPercentage;

            m_ExecutionType = other.m_ExecutionType;
            m_Self = other.m_Self;

            if (other.m_ConditionData != null)
            {
                m_ConditionData =
                    new List<Data>(other.m_ConditionData.Count);

                for (int i = 0; i < other.m_ConditionData.Count; i++)
                {
                    Data source = other.m_ConditionData[i];

                    m_ConditionData.Add(new Data
                    {
                        Condition = source.Condition?.Clone(),
                        Interaction = source.Interaction?.Clone()
                    });
                }
            }

            m_ElseInteraction = other.m_ElseInteraction?.Clone();
        }

        public IInteraction Clone()
        {
            return new ConditionalInteraction(this);
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        private bool ShouldStartFromBottom()
        {
            switch (m_QueryOrder)
            {
                case QueryOrder.TopToBottom:

                    return false;

                case QueryOrder.BottomToTop:

                    return true;

                case QueryOrder.AlternateDirection:

                    bool startFromBottom = m_NextQueryStartsFromBottom;

                    m_NextQueryStartsFromBottom =
                        !m_NextQueryStartsFromBottom;

                    return startFromBottom;

                case QueryOrder.Random:

                    if (m_StartTopToStartBottomPercentage <= 0f)
                    {
                        return false;
                    }

                    if (m_StartTopToStartBottomPercentage >= 1f)
                    {
                        return true;
                    }

                    return UnityEngine.Random.value <
                           m_StartTopToStartBottomPercentage;

                default:

                    return false;
            }
        }

        public void Interact(GameObject target)
        {
            bool anyConditionFulfilled = false;

            int count = m_ConditionData?.Count ?? 0;

            if (count > 0)
            {
                bool startFromBottom = ShouldStartFromBottom();

                int index = startFromBottom ? count - 1 : 0;
                int endIndex = startFromBottom ? -1 : count;
                int step = startFromBottom ? -1 : 1;

                bool executeOnlyFirst =
                    m_ExecutionType ==
                    ExecutionType.ExecuteOnlyFirstToReturnTrue;

                for (; index != endIndex; index += step)
                {
                    Data data = m_ConditionData[index];

                    if (data.Condition != null &&
                        !data.Condition.IsFulfilled(target))
                    {
                        continue;
                    }

                    anyConditionFulfilled = true;

                    data.Interaction?.Interact(target);

                    if (executeOnlyFirst)
                    {
                        return;
                    }
                }
            }

            if (!anyConditionFulfilled)
            {
                m_ElseInteraction?.Interact(target);
            }
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;

            if (m_Self == null)
            {
                Debug.LogError(
                    $"{nameof(ConditionalInteraction)} could not initialize " +
                    "because the provided self GameObject is null.");

                return false;
            }

            bool allGood = true;

            m_NextQueryStartsFromBottom = false;

            if (m_ConditionData != null)
            {
                for (int i = 0; i < m_ConditionData.Count; i++)
                {
                    Data data = m_ConditionData[i];

                    if (data.Condition != null)
                    {
                        allGood &= data.Condition.SetGameObject(self);
                    }

                    if (data.Interaction != null)
                    {
                        allGood &= data.Interaction.SetSelf(self);
                    }
                    else
                    {
                        Debug.LogError(
                            $"{nameof(ConditionalInteraction)} " +
                            $"on '{m_Self.name}' contains a null " +
                            $"interaction at index {i}.",
                            m_Self);

                        allGood = false;
                    }
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