using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Integrations.MMTopDown
{
    [Serializable]
    public class CombatRange_CachedCondition : ICachedCondition
    {
        private enum RangeConditionType
        {
            CanAttackShortRange,
            CanAttackLongRange,
            PreferShortRange,
            PreferLongRange
        }

        public enum SelfType
        {
            ThisGameObject,
            CustomGameObject
        }

        [SerializeField]
        private SelfType m_WhichCombatRangeType;

        [SerializeField, ShowIf(nameof(m_WhichCombatRangeType), SelfType.CustomGameObject)]
        private CombatRangeType m_CombatRangeType;

        [SerializeField]
        private RangeConditionType m_RangeConditionType;

        private GameObject m_SelfGameObject;

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null) return false;

            m_SelfGameObject = gameObj;

            // Pre-cache component if tracking the host GameObject
            if (m_WhichCombatRangeType == SelfType.ThisGameObject)
            {
                m_CombatRangeType = m_SelfGameObject.GetComponent<CombatRangeType>();
                if (m_CombatRangeType == null)
                {
                    Debug.LogError($"[{nameof(CombatRange_CachedCondition)}] CombatRangeType component missing on {gameObj.name}");
                    return false;
                }
            }

            return true;
        }

        public GameObject GetGameObject()
        {
            return m_SelfGameObject;
        }

        public bool IsFulfilled()
        {
            CombatRangeType targetComponent = GetCombatRangeComponent();
            if (targetComponent == null)
            {
                return false;
            }

            switch (m_RangeConditionType)
            {
                case RangeConditionType.CanAttackShortRange:
                    return targetComponent.CanAttackShortRange();
                case RangeConditionType.CanAttackLongRange:
                    return targetComponent.CanAttackLongRange();
                case RangeConditionType.PreferShortRange:
                    return targetComponent.PreferShortRange();
                case RangeConditionType.PreferLongRange:
                    return targetComponent.PreferLongRange();
                default:
                    return false;
            }
        }

        public ICachedCondition Clone()
        {
            return new CombatRange_CachedCondition()
            {
                m_WhichCombatRangeType = this.m_WhichCombatRangeType,
                m_CombatRangeType = this.m_CombatRangeType,
                m_RangeConditionType = this.m_RangeConditionType,
                m_SelfGameObject = this.m_SelfGameObject
            };
        }

        private CombatRangeType GetCombatRangeComponent()
        {
            if (m_WhichCombatRangeType == SelfType.CustomGameObject)
            {
                return m_CombatRangeType;
            }

            if (m_CombatRangeType == null && m_SelfGameObject != null)
            {
                m_CombatRangeType = m_SelfGameObject.GetComponent<CombatRangeType>();
            }

            return m_CombatRangeType;
        }
    }
}
