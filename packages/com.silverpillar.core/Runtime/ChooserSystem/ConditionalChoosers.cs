using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ConditionalChoosers<TOption> : IChooseData<TOption>
    {
        [Serializable]
        public struct ConditionAndData<ValueType>
        {
            [OdinSerialize, ShowInInspector]
            public ICachedCondition Condition;

            [OdinSerialize, ShowInInspector]
            public IChooseData<ValueType> Data;
        }

        [Title("Choosing Settings")]
        [SerializeField]
        private ChooserConditionType m_ConditionType;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_ConditionType), ChooserConditionType.ChooseUntilMeetMaxNumber)]
        private IntCachedScore m_MaxNumberOfInstancesToReturn;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private List<ConditionAndData<TOption>> m_Data;

        private GameObject m_GameObject;

        public List<TOption> ChooseData()
        {
            List<TOption> generated = new();

            if (m_Data == null || m_Data.Count == 0)
            {
                return generated;
            }

            switch (m_ConditionType)
            {
                case ChooserConditionType.ChooseFirst:
                    ChooseFirst(generated);
                    break;

                case ChooserConditionType.ChooseAny:
                    ChooseAny(generated);
                    break;

                case ChooserConditionType.ChooseUntilMeetMaxNumber:
                    ChooseUntilMax(generated);
                    break;
            }

            return generated;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;

            m_MaxNumberOfInstancesToReturn?.SetGameObject(gameObj);

            if (m_Data != null)
            {
                for (int i = 0; i < m_Data.Count; i++)
                {
                    m_Data[i].Condition?.SetGameObject(gameObj);
                    m_Data[i].Data.SetGameObject(gameObj);
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
            ConditionalChoosers<TOption> clone = new()
            {
                m_ConditionType = m_ConditionType,
                m_MaxNumberOfInstancesToReturn = m_MaxNumberOfInstancesToReturn?.Clone() as IntCachedScore,
                m_Data = new List<ConditionAndData<TOption>>()
            };

            if (m_Data != null)
            {
                foreach (ConditionAndData<TOption> type in m_Data)
                {
                    clone.m_Data.Add(new ConditionAndData<TOption>
                    {
                        Condition = type.Condition?.Clone(),
                        Data = type.Data.Clone()
                    });
                }
            }

            clone.SetGameObject(m_GameObject);
            return clone;
        }

        private void ChooseFirst(List<TOption> chosen)
        {
            for (int i = 0; i < m_Data.Count; i++)
            {
                if (IsFulfilled(m_Data[i].Condition))
                {
                    chosen.AddRange(m_Data[i].Data.ChooseData());
                    return;
                }
            }
        }

        private void ChooseAny(List<TOption> chosen)
        {
            for (int i = 0; i < m_Data.Count; i++)
            {
                if (IsFulfilled(m_Data[i].Condition))
                {
                    chosen.AddRange(m_Data[i].Data.ChooseData());
                }
            }
        }

        private void ChooseUntilMax(List<TOption> chosen)
        {
            int max = Mathf.Clamp(GetIntScore(m_MaxNumberOfInstancesToReturn, m_Data.Count), 0, m_Data.Count);

            for (int i = 0; i < m_Data.Count && chosen.Count < max; i++)
            {
                if (IsFulfilled(m_Data[i].Condition))
                {
                    chosen.AddRange(m_Data[i].Data.ChooseData());
                }
            }
        }

        private static bool IsFulfilled(ICachedCondition condition)
        {
            return condition == null || condition.IsFulfilled();
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
