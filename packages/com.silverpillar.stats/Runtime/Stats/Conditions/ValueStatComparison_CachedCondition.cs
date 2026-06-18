using SilverPillar.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SilverPillar.Stats
{
    public class ValueStatComparison_CachedCondition : ICachedCondition
    {
        [Header("Stat Controller")]
        [SerializeField]
        private SelfType m_StatControllerFromWho;
        [SerializeField, ShowIf(nameof(m_StatControllerFromWho), SelfType.CustomGameObject)]
        private StatController m_StatController;

        [Header("If")]
        [SerializeField] private StatType m_StatType;
        [SerializeField] private StatVariable m_StatVariable;

        [Header("Condition")]
        [SerializeField] private FloatComparison.OperationType m_ConditionOperation;

        [Header("Than")]
        [SerializeField, Range(0,100)] private float m_Value;


        [Header("Side Cases")]
        [SerializeField] private bool m_ReturnValueIfNoStatType;

        private GameObject m_Self;

        public bool SetGameObject(GameObject gameObj)
        {
            if (m_StatControllerFromWho == SelfType.ThisGameObject)
            {
                m_StatController = gameObj != null ? gameObj.GetComponent<StatController>() : null;
            }
            m_Self = gameObj;
            return m_StatController != null;
        }

        public GameObject GetGameObject() => m_Self;

        public bool IsFulfilled()
        {
            if (m_StatController == null) return false;

            if (m_StatController.HasStatType(m_StatType))
            {
                float statValue = m_StatController.GetStat(m_StatType, m_StatVariable);

                return FloatComparison.Compare(statValue, m_ConditionOperation, m_Value);
            }

            return m_ReturnValueIfNoStatType;
        }

        public ICachedCondition Clone()
        {
            var clone = new ValueStatComparison_CachedCondition
            {
                m_StatType = this.m_StatType,
                m_StatVariable = this.m_StatVariable,
                m_ConditionOperation = this.m_ConditionOperation,
                m_Value = this.m_Value,
                m_ReturnValueIfNoStatType = this.m_ReturnValueIfNoStatType
            };
            clone.SetGameObject(m_Self);
            return clone;
        }
    }
}

