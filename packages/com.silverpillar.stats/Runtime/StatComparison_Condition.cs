using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class StatComparison_Condition : ICondition
    {
        public enum ConditionOperation
        {
            IsLess,
            IsMore,
            IsEqual
        }

        [Header("If")]
        [SerializeField]
        private StatType m_StatType;
        [SerializeField]
        private StatVariable m_StatVariable;

        [Header("Condition")]
        [SerializeField]
        private FloatComparison.OperationType m_ConditionOperation;

        [Header("Than")]
        [SerializeField]
        private StatType m_OtherStatType;
        [SerializeField]
        private StatVariable m_OtherStatVariable;


        public bool IsFulfilled(GameObject gameObj)
        {
            var statRegistry = gameObj.GetComponent<StatController>();
            if (statRegistry)
            {
                if (statRegistry.HasStatType(m_StatType))
                {
                    float statValue = statRegistry.GetStat(m_StatType, m_StatVariable);
                    float statValueToCompare = statRegistry.GetStat(m_OtherStatType, m_OtherStatVariable);

                    return FloatComparison.Compare(statValueToCompare, m_ConditionOperation, statValue);
                }
            }

            return false;
        }
    }
}
