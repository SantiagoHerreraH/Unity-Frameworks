using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class CachedCondition_InteractionCondition : IInteractionCondition
    {
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_CachedCondition = null;
        [SerializeField]
        private TargetType m_OnWhoToApplyCachedCondition;

        public CachedCondition_InteractionCondition() { }
        public CachedCondition_InteractionCondition(CachedCondition_InteractionCondition other)
        {
            m_CachedCondition = other.m_CachedCondition.Clone();
            m_OnWhoToApplyCachedCondition = other.m_OnWhoToApplyCachedCondition;
        }

        public IInteractionCondition Clone()
        {
            return new CachedCondition_InteractionCondition(this);
        }

        public GameObject GetGameObject()
        {
            return m_CachedCondition.GetGameObject();
        }

        public bool IsFulfilled(GameObject target)
        {
            switch (m_OnWhoToApplyCachedCondition)
            {
                case TargetType.Self:

                    return m_CachedCondition.IsFulfilled();

                case TargetType.Other:

                    if (m_CachedCondition.SetGameObject(target))
                    {
                        return m_CachedCondition.IsFulfilled();
                    }
                    break;

                default:
                    break;
            }

            return false;
        }

        public bool SetGameObject(GameObject self)
        {
            if (self != null)
            {

                if (m_OnWhoToApplyCachedCondition == TargetType.Self)
                {
                    m_CachedCondition.SetGameObject(self);
                }

                return true;
            }

            return false;
        }
    }
}
