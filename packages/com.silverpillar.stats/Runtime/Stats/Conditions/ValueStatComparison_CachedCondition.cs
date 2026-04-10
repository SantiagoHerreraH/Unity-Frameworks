using SilverPillar.Core;
using UnityEngine;

namespace SilverPillar.Stats
{
    public class ValueStatComparison_CachedCondition : ICachedCondition
    {

        [Header("If")]
        [SerializeField] private StatType m_StatType;
        [SerializeField] private StatVariable m_StatVariable;

        [Header("Condition")]
        [SerializeField] private FloatComparison.OperationType m_ConditionOperation;

        [Header("Than")]
        [SerializeField, Range(0,100)] private float m_Value;

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

                return FloatComparison.Compare(m_Value, m_ConditionOperation, statValue);
            }

            return false;
        }

        public ICachedCondition Clone()
        {
            var clone = new ValueStatComparison_CachedCondition
            {
                m_StatType = this.m_StatType,
                m_StatVariable = this.m_StatVariable,
                m_ConditionOperation = this.m_ConditionOperation,
                m_Value = this.m_Value
            };
            clone.SetGameObject(_cachedGameObject);
            return clone;
        }
    }
}

