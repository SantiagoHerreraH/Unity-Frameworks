using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    public enum WhenToSetGameObjectOnCondition
    {
        OnSetGameObject,
        OnIsFulfilled
    }

    [Serializable]
    public class ConditionOnGameObjectCalculator_CachedCondition : ICachedCondition
    {
        [Title("Game Object")]

        [OdinSerialize]
        private IGameObjectCalculator m_GameObjectCalculator;

        [Title("Condition")]

        [SerializeField]
        private WhenToSetGameObjectOnCondition m_WhenToSetGameObjectOnCondition;
        [OdinSerialize]
        private ICachedCondition m_Condition;

        [Title("Side cases")]

        [SerializeField]
        private bool m_WhatToReturnIfGameObjectIsNull;
        private GameObject m_Self;

        public ICachedCondition Clone()
        {
            return new ConditionOnGameObjectCalculator_CachedCondition
            {
                m_GameObjectCalculator = m_GameObjectCalculator.Clone(),
                m_WhenToSetGameObjectOnCondition = m_WhenToSetGameObjectOnCondition,
                m_Condition = m_Condition.Clone(),
                m_WhatToReturnIfGameObjectIsNull = m_WhatToReturnIfGameObjectIsNull,
                m_Self = m_Self
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled()
        {
            if (m_WhenToSetGameObjectOnCondition == WhenToSetGameObjectOnCondition.OnSetGameObject &&
                m_GameObjectCalculator != null)
            {
                var gameObj = m_GameObjectCalculator.CalculateGameObject();

                if (gameObj != null)
                {
                    m_Condition.SetGameObject(gameObj);
                }
                else
                {
                    return m_WhatToReturnIfGameObjectIsNull;
                }
            }

            return m_Condition.IsFulfilled();
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            bool result = m_GameObjectCalculator.SetGameObject(gameObj);
            if (m_WhenToSetGameObjectOnCondition == WhenToSetGameObjectOnCondition.OnSetGameObject &&
                result)
            {
                result &= m_Condition.SetGameObject(m_GameObjectCalculator.CalculateGameObject());
            }
            return m_Self != null && result;
        }
    }
}
