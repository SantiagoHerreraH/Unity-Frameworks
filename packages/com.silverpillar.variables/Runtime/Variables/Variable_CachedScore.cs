using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Variables
{
    [Serializable]
    public class Variable_CachedScore : ICachedScore
    {
        [Header("Variable")]
        [SerializeField]
        private Variable m_Variable;

        [Header("Value To Return If No Variable")]
        [SerializeField]
        private float m_DefaultValue = 0f;

        private GameObject m_GameObject;
        private IVariableController m_Controller;

        public float CalculateScore()
        {
            if (m_Controller != null && m_Controller.HasVariable(m_Variable))
            {
                return m_Controller.GetValue(m_Variable);
            }

            return m_DefaultValue;
        }

        public bool SetGameObject(GameObject self)
        {
            m_GameObject = self;
            if (self != null)
            {
                self.TryGetComponent(out m_Controller);
            }
            else
            {
                m_Controller = null;
            }

            return m_Controller != null;
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public ICachedScore Clone()
        {
            return new Variable_CachedScore
            {
                m_Variable = this.m_Variable,
                m_DefaultValue = this.m_DefaultValue,
                m_GameObject = this.m_GameObject,
                m_Controller = this.m_Controller
            };
        }
    }
}
