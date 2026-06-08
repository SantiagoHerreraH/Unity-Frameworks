using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.Stats
{
    [Serializable]
    public class StatComparison_CachedCondition : ICachedCondition
    {

        [Header("If")]
        [SerializeField] private StatType m_StatType;
        [SerializeField] private StatVariable m_StatVariable;

        [Header("Condition")]
        [SerializeField] private FloatComparison.OperationType m_ConditionOperation;

        [Header("Than")]
        [SerializeField] private StatType m_OtherStatType;
        [SerializeField] private StatVariable m_OtherStatVariable;

        public StatType StatType { get { return m_StatType; }  set { m_StatType = value; } }
        public StatVariable StatVariable { get { return m_StatVariable; } set { m_StatVariable = value; } }
        public StatType OtherStatType { get { return m_OtherStatType; } set { m_OtherStatType = value; } }
        public StatVariable OtherStatVariable { get { return m_OtherStatVariable; } set { m_OtherStatVariable = value; } }
        public FloatComparison.OperationType ConditionOperation { get { return m_ConditionOperation; } set { m_ConditionOperation = value; } }


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

            if (_cachedStatController.HasStatType(m_StatType) && 
                _cachedStatController.HasStatType(m_OtherStatType))
            {
                float statValue = _cachedStatController.GetStat(m_StatType, m_StatVariable);
                float statValueToCompare = _cachedStatController.GetStat(m_OtherStatType, m_OtherStatVariable);

                return FloatComparison.Compare(statValue, m_ConditionOperation, statValueToCompare);
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
