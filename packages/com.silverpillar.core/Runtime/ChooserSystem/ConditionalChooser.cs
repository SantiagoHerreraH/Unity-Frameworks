using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum ChooserConditionType
    {
        ChooseFirst,
        ChooseAny,
        ChooseUntilMeetMaxNumber
    }

    [Serializable]
    public class ConditionalChooser<TOption> : IChooseData<TOption> 
    {
        [Serializable]
        public struct ConditionAndData<ValueType>
        {
            [OdinSerialize, ShowInInspector]
            public ICachedCondition Condition;

            [OdinSerialize, ShowInInspector]
            public ChoosingOption<ValueType> Data;
        }

        [Title("Choosing Settings")]
        [SerializeField]
        private ChooserConditionType m_ConditionType;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_ConditionType), ChooserConditionType.ChooseUntilMeetMaxNumber)]
        private IntCachedScore m_MaxNumberOfInstancesToReturn;

        [Title("Data")]
        [OdinSerialize, ShowInInspector]
        private List<ConditionAndData<TOption>> m_Data;

        private DataFromChoosing<TOption> m_DataFromChoosing;
        private GameObject m_GameObject;

        List<TOption> m_RawData;

        public DataFromChoosing<TOption> ChooseData()
        {
            m_DataFromChoosing.Clear();
            if (m_Data == null || m_Data.Count == 0)
            {
                return m_DataFromChoosing;
            }

            switch (m_ConditionType)
            {
                case ChooserConditionType.ChooseFirst:
                    ChooseFirst(m_DataFromChoosing);
                    break;

                case ChooserConditionType.ChooseAny:
                    ChooseAny(m_DataFromChoosing);
                    break;

                case ChooserConditionType.ChooseUntilMeetMaxNumber:
                    ChooseUntilMax(m_DataFromChoosing);
                    break;
            }

            return m_DataFromChoosing;
        }

        public List<TOption> AllData()
        {
            m_RawData ??= new();
            m_RawData.Clear();
            for (int i = 0; i < m_Data.Count; i++)
            {
                m_RawData.Add(m_Data[i].Data.Value);
            }

            return m_RawData;
        }

        public List<TOption> GetChosenData()
        {
            return m_DataFromChoosing.ChosenData;
        }

        public List<TOption> GetNotChosenData()
        {
            return m_DataFromChoosing.NotChosenData;
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
                    m_Data[i].Data.Initialize(gameObj);
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
            ConditionalChooser<TOption> clone = new()
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

        private void ChooseFirst(DataFromChoosing<TOption> chosen)
        {
            bool alreadyChose = false;
            for (int i = 0; i < m_Data.Count; i++)
            {
                if (!alreadyChose && IsFulfilled(m_Data[i].Condition))
                {
                    chosen.AddChosen(m_Data[i].Data.Value);
                    alreadyChose = true;
                }
                else
                {
                    chosen.AddNotChosen(m_Data[i].Data.Value);
                }
            }
        }

        private void ChooseAny(DataFromChoosing<TOption> chosen)
        {
            for (int i = 0; i < m_Data.Count; i++)
            {
                if (IsFulfilled(m_Data[i].Condition))
                {
                    chosen.AddChosen(m_Data[i].Data.Value);
                }
                else
                {
                    chosen.AddNotChosen(m_Data[i].Data.Value);
                }
            }
        }

        private void ChooseUntilMax(DataFromChoosing<TOption> chosen)
        {
            int max = Mathf.Clamp(GetIntScore(m_MaxNumberOfInstancesToReturn, m_Data.Count), 0, m_Data.Count);

            for (int i = 0; i < m_Data.Count; i++)
            {
                if (IsFulfilled(m_Data[i].Condition) && chosen.ChosenData.Count < max)
                {
                    chosen.AddChosen(m_Data[i].Data.Value);
                }
                else
                {
                    chosen.AddNotChosen(m_Data[i].Data.Value);
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