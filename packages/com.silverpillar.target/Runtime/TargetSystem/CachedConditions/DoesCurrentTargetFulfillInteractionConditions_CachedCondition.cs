using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Target
{
    [Serializable]
    public class DoesCurrentTargetFulfillInteractionConditions_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private SelfType m_WhichTargetSystem;
        [SerializeField, ShowIf(nameof(m_WhichTargetSystem), SelfType.CustomGameObject)]
        private TargetSystem m_TargetSystem;

        [OdinSerialize, ShowInInspector, Tooltip("Self gameobject in interaction condition will be target system's gameobject")]
        private IInteractionCondition m_ConditionToFulfill = null;

        [SerializeField]
        private bool m_ReturnValueIfCurrentTargetIsNull;


        public DoesCurrentTargetFulfillInteractionConditions_CachedCondition() { }

        public DoesCurrentTargetFulfillInteractionConditions_CachedCondition(DoesCurrentTargetFulfillInteractionConditions_CachedCondition other)
        {
            m_ConditionToFulfill = other.m_ConditionToFulfill.Clone();
            m_TargetSystem = other.m_TargetSystem;
            m_ReturnValueIfCurrentTargetIsNull = other.m_ReturnValueIfCurrentTargetIsNull;
        }

        public ICachedCondition Clone()
        {
            return new DoesCurrentTargetFulfillInteractionConditions_CachedCondition(this);
        }

        public GameObject GetGameObject()
        {
            return m_TargetSystem ? m_TargetSystem.gameObject : null;
        }

        public bool IsFulfilled()
        {
            if (m_TargetSystem != null)
            {
                if (m_TargetSystem.CurrentTarget != null)
                {
                    return m_ConditionToFulfill.IsFulfilled(m_TargetSystem.CurrentTarget);
                }

                return m_ReturnValueIfCurrentTargetIsNull;
            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError($"Null gameobject trying to be set in {(nameof(DoesCurrentTargetFulfillInteractionConditions_CachedCondition))}");
                return false;
            }
            if (m_WhichTargetSystem == SelfType.ThisGameObject)
            {
                if (!gameObj.TryGetComponent(out m_TargetSystem))
                {
                    Debug.LogError($"No target system in input gameobject {gameObj.name} in class {nameof(DoesCurrentTargetFulfillInteractionConditions_CachedCondition)}");
                    return false;
                }
            }

            return m_ConditionToFulfill.SetGameObject(m_TargetSystem.gameObject);
        }
    }
}

