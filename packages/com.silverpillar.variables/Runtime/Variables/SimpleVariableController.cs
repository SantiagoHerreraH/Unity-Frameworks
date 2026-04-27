using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Variables
{
    public class SimpleVariableController : SerializedMonoBehaviour, IVariableController
    {
        [Serializable]
        public struct Data
        {
            public Variable Variable;
            public float Value;
        }

        [Serializable]
        public struct ScoreOperation
        {
            public enum OperationOrders
            {
                FirstVariable,
                FirstScore
            }

            [OdinSerialize, ShowInInspector]
            public ICachedScore Score;
            public ScoreOperationType OperationType;
            public OperationOrders OperationOrder;

            public bool Initialize(GameObject gameObj)
            {
                return Score.SetGameObject(gameObj);
            }
            public float ModifyInput(float inputValueFromVariable)
            {
                float score = Score.CalculateScore();
                float first = inputValueFromVariable;
                float second = score;

                if (OperationOrder == OperationOrders.FirstScore)
                {
                    first = score;
                    second = inputValueFromVariable;
                }

                switch (OperationType)
                {
                    case ScoreOperationType.Add:
                        return first + second;

                    case ScoreOperationType.Subtract:
                        return first - second;

                    case ScoreOperationType.Multiply:
                        return first * second;

                    case ScoreOperationType.Divide:
                        return second != 0 ? first / second : 0;

                    case ScoreOperationType.Power:
                        return Mathf.Pow(first, second);

                    case ScoreOperationType.Root:
                        if (second == 0) return 0; 
                        return Mathf.Pow(first, 1f / second);

                    default:
                        return first;
                }
            }
        }

        [Title("Modification Operations")]
        [OdinSerialize, ShowInInspector]
        private List<ScoreOperation> m_ModificationOperationsInOrder = new();

        [Title("Variable Data")]
        [SerializeField]
        private List<Data> m_Data = new();

        [Button(ButtonSizes.Small)]
        private void PopulateWithAllVariableTypes()
        {
            if (m_Data == null)
            {
                m_Data = new();
            }

            m_Data = m_Data
            .Where(x => x.Variable != null)
            .GroupBy(x => x.Variable)
            .Select(g => g.First())
            .ToList();

            var currentStatTypes = m_Data.Select(x => x.Variable).ToHashSet();

            var allStatTypes = ScriptableObjectRegistry.Instance.GetAllOfType<Variable>();

            foreach (var statType in allStatTypes)
            {
                if (statType != null && !currentStatTypes.Contains(statType))
                {
                    m_Data.Add(new Data { Variable = statType });
                }
            }
        }

        private Dictionary<Variable, float> m_Variable_To_Value = new();

        private bool m_Initialized = false;

        private void Awake()
        {
            CheckInitialized();
        }

        public float GetValue(Variable variable)
        {
            CheckInitialized();

            if (m_Variable_To_Value.ContainsKey(variable))
            {
                float value = m_Variable_To_Value[variable];

                for (int i = 0; i < m_ModificationOperationsInOrder.Count; i++)
                {
                    value = m_ModificationOperationsInOrder[i].ModifyInput(value);
                }

                return value;
            }

            return 0;
        }

        private void CheckInitialized()
        {
            if (!m_Initialized)
            {
                Initialize();
                m_Initialized = true;
            }
        }

        private void Initialize()
        {
            m_Data = m_Data
                .Where(d => d.Variable != null) // filter null
                .GroupBy(d => d.Variable)       // group by category tag
                .Select(g => g.First())            // select first (Distinct)
                .ToList();

            m_Variable_To_Value = m_Data
                .ToDictionary(
                    d => d.Variable,
                    d => d.Value
                );

            for (int i = 0; i < m_ModificationOperationsInOrder.Count; i++)
            {
                m_ModificationOperationsInOrder[i].Initialize(gameObject);
            }
        }

        public bool HasVariable(Variable variable)
        {
            return m_Variable_To_Value.ContainsKey(variable);
        }
    }
}
