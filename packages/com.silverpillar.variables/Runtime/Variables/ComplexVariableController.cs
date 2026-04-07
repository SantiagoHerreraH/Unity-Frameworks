using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using Sirenix.Serialization;

namespace SilverPillar.Variables
{
    public class ComplexVariableController : SerializedMonoBehaviour, IVariableController
    {
        [OdinSerialize, ShowInInspector]
        private Dictionary<Variable, ICachedScore> m_Variable_To_Value = new();

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
                return m_Variable_To_Value[variable].CalculateScore();
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
            foreach (var item in m_Variable_To_Value)
            {
                item.Value.SetGameObject(gameObject);
            }
        }

        public bool HasVariable(Variable variable)
        {
            return m_Variable_To_Value.ContainsKey(variable);
        }
    }
}
