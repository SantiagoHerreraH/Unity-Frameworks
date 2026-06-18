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
        [SerializeField]
        private SelfType m_BoolControllerFromWho;
        [SerializeField, ShowIf(nameof(m_BoolControllerFromWho), SelfType.CustomGameObject)]
        BoolModuleController m_SelfController = null;


        [OdinSerialize, ShowInInspector]
        private IBoolModifier m_BoolModifier;

        private GameObject m_Self;

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
            if (m_Self != null && m_BoolControllerFromWho == SelfType.ThisGameObject)
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