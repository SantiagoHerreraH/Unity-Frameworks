using SilverPillar.Core;
using Sirenix.OdinInspector;
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

        [Header("Side Cases")]
        [SerializeField]
        private bool m_WhatToReturnIfTargetDoesntHaveStatController = false;
        [SerializeField]
        private bool m_WhatToReturnIfTargetDoesntHaveStatType = false;
        [InfoBox("If Self doesnt have stat controller or stat type will return false")]

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

            bool statsFromOther = (m_CompareThis.From == TargetType.Other || m_AgainstThis.From == TargetType.Other);

            if (target.TryGetComponent(out targetStatController) && statsFromOther)
            {
                return m_WhatToReturnIfTargetDoesntHaveStatController;
            }

            if (m_CompareThis.CanGetValue(m_CachedStatController, targetStatController) && 
                m_AgainstThis.CanGetValue(m_CachedStatController, targetStatController))
            {
                float statValue = m_CompareThis.GetValue(m_CachedStatController, targetStatController);
                float statValueToCompare = m_AgainstThis.GetValue(m_CachedStatController, targetStatController);

                return FloatComparison.Compare(statValueToCompare, m_ConditionOperation, statValue);
            }

            if ((!m_CompareThis.CanGetValue(m_CachedStatController, targetStatController) && m_CompareThis.From == TargetType.Other) ||
                (!m_AgainstThis.CanGetValue(m_CachedStatController, targetStatController) && m_AgainstThis.From == TargetType.Other))
            {
                return m_WhatToReturnIfTargetDoesntHaveStatType;
            }

            //If self doesnt have stat type return false
            return false;
        }

    }
}
