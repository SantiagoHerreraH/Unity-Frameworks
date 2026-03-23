using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class StatComparison_CachedCondition : ICachedCondition
    {
        public enum ConditionOperation
        {
            IsLess,
            IsMore,
            IsEqual
        }

        [Header("If")]
        [SerializeField] private StatType m_StatType;
        [SerializeField] private StatVariable m_StatVariable;

        [Header("Condition")]
        [SerializeField] private ConditionOperation m_ConditionOperation;

        [Header("Than")]
        [SerializeField] private StatType m_OtherStatType;
        [SerializeField] private StatVariable m_OtherStatVariable;

        private GameObject _cachedGameObject;
        private StatController _cachedStatController;

        public bool SetGameObject(GameObject gameObj)
        {
            _cachedGameObject = gameObj;
            _cachedStatController = gameObj != null ? gameObj.GetComponent<StatController>() : null;
            return _cachedStatController != null;
        }

        public GameObject GetGameObject() => _cachedGameObject;

        public bool IsFulfilled()
        {
            if (_cachedStatController == null) return false;

            if (_cachedStatController.HasStatType(m_StatType))
            {
                float statValue = _cachedStatController.GetStat(m_StatType, m_StatVariable);
                float statValueToCompare = _cachedStatController.GetStat(m_OtherStatType, m_OtherStatVariable);

                return m_ConditionOperation switch
                {
                    ConditionOperation.IsLess => statValueToCompare < statValue,
                    ConditionOperation.IsMore => statValueToCompare > statValue,
                    ConditionOperation.IsEqual => Mathf.Approximately(statValueToCompare, statValue), // Más seguro para floats que (int)
                    _ => false
                };
            }

            return false;
        }

        public ICachedCondition Clone()
        {
            var clone = new StatComparison_CachedCondition
            {
                m_StatType = this.m_StatType,
                m_StatVariable = this.m_StatVariable,
                m_ConditionOperation = this.m_ConditionOperation,
                m_OtherStatType = this.m_OtherStatType,
                m_OtherStatVariable = this.m_OtherStatVariable
            };
            clone.SetGameObject(_cachedGameObject);
            return clone;
        }
    }
}
