using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Variables
{
    public class SimpleVariableController : MonoBehaviour, IVariableController
    {
        [Serializable]
        public struct Data
        {
            public Variable Variable;
            public float Value;
        }

        [SerializeField]
        private List<Data> m_Data = new();
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
                return m_Variable_To_Value[variable];
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
        }

        public bool HasVariable(Variable variable)
        {
            return m_Variable_To_Value.ContainsKey(variable);
        }
    }
}
