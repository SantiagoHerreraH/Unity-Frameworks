using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class DoesCurrentTargetFulfillInteractionConditions_CachedCondition : ICachedCondition
    {
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_ConditionToFulfill = null;

        [SerializeField]
        private bool m_ReturnValueIfCurrentTargetIsNull;

        private TargetSystem m_TargetSystem;

        public DoesCurrentTargetFulfillInteractionConditions_CachedCondition() { }

        public DoesCurrentTargetFulfillInteractionConditions_CachedCondition(DoesCurrentTargetFulfillInteractionConditions_CachedCondition other)
        {
            m_ConditionToFulfill = other.m_ConditionToFulfill.Clone();
            m_TargetSystem = other.m_TargetSystem;
            m_ReturnValueIfCurrentTargetIsNull = other.m_ReturnValueIfCurrentTargetIsNull;
        }

        public ICachedCondition Clone()
        {
            return new DoesCurrentTargetFulfillInteractionConditions_CachedCondition(this);
        }

        public GameObject GetGameObject()
        {
            return m_TargetSystem ? m_TargetSystem.gameObject : null;
        }

        public bool IsFulfilled()
        {
            if (m_TargetSystem != null)
            {
                if (m_TargetSystem.CurrentTarget != null)
                {
                    return m_ConditionToFulfill.IsFulfilled(m_TargetSystem.CurrentTarget);
                }

                return m_ReturnValueIfCurrentTargetIsNull;
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null && gameObj.TryGetComponent(out m_TargetSystem))
            {
                return m_ConditionToFulfill.SetGameObject(gameObj);
            }

            return false;
        }
    }
}

