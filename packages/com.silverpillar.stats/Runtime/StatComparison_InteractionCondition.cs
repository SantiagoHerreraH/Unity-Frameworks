using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class StatComparison_InteractionCondition : IInteractionCondition
    {

        [Header("If")]
        [SerializeField]
        private EntityStatIdentity m_CompareThis;

        [Header("Condition")]
        [SerializeField] private FloatComparison.OperationType m_ConditionOperation;

        [Header("Than")]
        [SerializeField]
        private EntityStatIdentity m_AgainstThis;

        private GameObject m_CachedGameObject;
        private StatController m_CachedStatController;

        public bool SetGameObject(GameObject gameObj)
        {
            m_CachedGameObject = gameObj;
            m_CachedStatController = gameObj != null ? gameObj.GetComponent<StatController>() : null;
            return m_CachedStatController != null;
        }

        public GameObject GetGameObject() => m_CachedGameObject;


        public IInteractionCondition Clone()
        {
            var clone = new StatComparison_InteractionCondition
            {
                m_CompareThis = this.m_CompareThis,
                m_AgainstThis = this.m_AgainstThis,
                m_ConditionOperation = this.m_ConditionOperation,
            };
            clone.SetGameObject(m_CachedGameObject);
            return clone;
        }

        public bool IsFulfilled(GameObject target)
        {
            StatController targetStatController = null;
            if (m_CachedStatController == null || target == null) return false;
            if (target.TryGetComponent(out targetStatController) && (m_CompareThis.From == TargetType.Other || m_AgainstThis.From == TargetType.Other))
            {
                return false;
            }

            if (m_CompareThis.CanGetValue(m_CachedStatController, targetStatController) && 
                m_AgainstThis.CanGetValue(m_CachedStatController, targetStatController))
            {
                float statValue = m_CompareThis.GetValue(m_CachedStatController, targetStatController);
                float statValueToCompare = m_AgainstThis.GetValue(m_CachedStatController, targetStatController);

                return FloatComparison.Compare(statValueToCompare, m_ConditionOperation, statValue);
            }

            return false;
        }

    }
}
