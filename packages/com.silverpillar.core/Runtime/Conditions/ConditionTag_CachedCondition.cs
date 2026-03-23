using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ConditionTag_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private ConditionTag m_ConditionTag = null;

        private ConditionMachine m_ConditionMachine = null;

        public ConditionTag_CachedCondition() { }

        public ConditionTag_CachedCondition(ConditionTag_CachedCondition other)
        {
            m_ConditionTag = other.m_ConditionTag;
            m_ConditionMachine = other.m_ConditionMachine;
        }

        public ICachedCondition Clone()
        {
            return new ConditionTag_CachedCondition(this);
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_ConditionMachine ? m_ConditionMachine.gameObject : null;
        }

        public bool IsFulfilled()
        {
            if (m_ConditionMachine != null)
            {
                return m_ConditionMachine.IsFulfilled(m_ConditionTag);
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            return gameObj.TryGetComponent<ConditionMachine>(out m_ConditionMachine);
        }
    }
}
