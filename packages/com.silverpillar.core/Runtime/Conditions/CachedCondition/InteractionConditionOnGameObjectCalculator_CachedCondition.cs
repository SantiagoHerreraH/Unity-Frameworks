using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class InteractionConditionOnGameObjectCalculator_CachedCondition : ICachedCondition
    {
        [Title("Game Object")]

        [OdinSerialize]
        private IGameObjectCalculator m_GameObjectCalculatorData;

        [Title("Condition")]

        [OdinSerialize, Tooltip("Self is self, target is calculated game object from game object calculator data.")]
        private IInteractionCondition m_InteractionCondition;

        [Title("Side cases")]

        [SerializeField]
        private bool m_WhatToReturnIfGameObjectIsNull;
        private GameObject m_Self;

        public ICachedCondition Clone()
        {
            return new InteractionConditionOnGameObjectCalculator_CachedCondition
            {
                m_GameObjectCalculatorData = m_GameObjectCalculatorData.Clone(),
                m_InteractionCondition = m_InteractionCondition.Clone(),
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
            var gameObj = m_GameObjectCalculatorData.CalculateGameObject();

            if (gameObj != null)
            {
                m_InteractionCondition.SetGameObject(gameObj);
            }
            else
            {
                return m_WhatToReturnIfGameObjectIsNull;
            }

            return m_InteractionCondition.IsFulfilled(gameObj);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            bool result = m_GameObjectCalculatorData.SetGameObject(gameObj);
            result &= m_InteractionCondition.SetGameObject(gameObj);

            return m_Self != null && result;
        }
    }
}
