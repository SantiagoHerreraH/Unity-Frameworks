using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class DoesCurrentTargetFulfill_CachedCondition : ICachedCondition
    {
        [OdinSerialize, ShowInInspector]
        private ICachedCondition m_ConditionToFulfill;

        private TargetSystem m_TargetSystem;

        public DoesCurrentTargetFulfill_CachedCondition() { }

        public DoesCurrentTargetFulfill_CachedCondition(DoesCurrentTargetFulfill_CachedCondition other)
        {
            m_ConditionToFulfill = other.m_ConditionToFulfill.Clone();
            m_TargetSystem = other.m_TargetSystem;
        }

        public ICachedCondition Clone()
        {
            return new DoesCurrentTargetFulfill_CachedCondition(this);
        }

        public GameObject GetGameObject()
        {
            return m_TargetSystem ? m_TargetSystem.gameObject : null;
        }

        public bool IsFulfilled()
        {
            if (m_TargetSystem != null)
            {
                return m_ConditionToFulfill.IsFulfilled();
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj.TryGetComponent(out m_TargetSystem) && m_TargetSystem.CurrentTarget != null)
            {
                return m_ConditionToFulfill.SetGameObject(m_TargetSystem.CurrentTarget);
            }

            return false;
        }
    }
}
