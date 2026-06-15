using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Modules
{
    [Serializable]
    public class BoolModuleModifier_Interaction : IInteraction
    {
        [OdinSerialize, ShowInInspector]
        private IBoolModifier m_BoolModifier;

        [SerializeField]
        private GameObject m_Self;
        BoolModuleController m_SelfController = null;

        public IInteraction Clone()
        {
            return new BoolModuleModifier_Interaction
            {
                m_Self = m_Self,
                m_BoolModifier = m_BoolModifier?.Clone()
            };
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;
            if (m_Self != null)
                m_Self.TryGetComponent(out m_SelfController);
            return m_Self != null && m_SelfController != null;
        }

        public void Interact(GameObject target)
        {
            if (m_BoolModifier == null)
                return;

            BoolModuleController targetController = null;

            if (target != null)
                target.TryGetComponent(out targetController);

            m_BoolModifier.Modify(m_SelfController, targetController);
        }
    }
}