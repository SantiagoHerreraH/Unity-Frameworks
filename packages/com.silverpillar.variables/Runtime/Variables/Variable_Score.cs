using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Variables
{
    [Serializable]
    public class Variable_Score : IScore
    {
        [Header("Variable")]
        [SerializeField]
        private Variable m_Variable;

        [Header("Value To Return If No Variable")]
        [SerializeField]
        private float m_DefaultValue = 0f;

        public float CalculateScore(GameObject gameObject)
        {
            IVariableController controller = null;

            if (gameObject.TryGetComponent(out controller))
            {
                if (controller.HasVariable(m_Variable))
                {
                    return controller.GetValue(m_Variable);
                }
                
            }

            return m_DefaultValue;
        }
    }
}
