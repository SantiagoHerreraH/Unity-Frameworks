using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class StatModify_Interaction : IInteraction
    {
        public enum WhichStatModifiersToUse
        {
            FromStatController,
            FromCustom
        }

        [SerializeField]
        private WhichStatModifiersToUse m_WhichStatModifiersToUse;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_WhichStatModifiersToUse), WhichStatModifiersToUse.FromCustom)]
        private List<IStatModifier> m_Modifiers;
        private StatController m_SelfController;

        public StatModify_Interaction() { }
        public StatModify_Interaction(StatModify_Interaction other)
        {
            m_WhichStatModifiersToUse = other.m_WhichStatModifiersToUse;

            if (other.m_Modifiers != null)
            {
                m_Modifiers = new();

                m_Modifiers.AddRange(other.m_Modifiers);
            }

            m_SelfController = other.m_SelfController;

            m_Modifiers = other.m_Modifiers;
        }

        public void Interact(GameObject target)
        {
            StatController targetController = null;
            if (m_SelfController == null || !target.TryGetComponent(out targetController))
            {
                return;
            }

            switch (m_WhichStatModifiersToUse)
            {
                case WhichStatModifiersToUse.FromStatController:

                    m_SelfController.ModifyTarget(targetController);

                    break;
                case WhichStatModifiersToUse.FromCustom:

                    if (m_Modifiers == null)
                    {
                        return;
                    }

                    foreach (var modifier in m_Modifiers)
                    {
                        modifier.Modify(m_SelfController, targetController);
                    }
                    break;
                default:
                    break;
            }
        }

        public bool SetSelf(GameObject self)
        {
            return self.TryGetComponent(out m_SelfController);
        }

#nullable enable
        public GameObject? GetSelf()
        {
            return m_SelfController != null ? m_SelfController.gameObject : null;
        }

        public IInteraction Clone()
        {
            return new StatModify_Interaction(this);
        }
    }
}
