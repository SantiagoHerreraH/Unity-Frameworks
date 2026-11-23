using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class InteractionConditionOnGameObjectCalculator_CachedCondition : ICachedCondition
    {
        private enum WhoIsSelf
        {
            Self,
            Custom,
            GameObjectCalculator
        }
        [Title("Self Game Object")]
        [SerializeField]
        private WhoIsSelf m_WhoIsInteractionSelf;
        [SerializeField, ShowIf(nameof(m_WhoIsInteractionSelf), WhoIsSelf.Custom)]
        private GameObject m_InteractionSelf;
        [OdinSerialize, ShowIf(nameof(m_WhoIsInteractionSelf), WhoIsSelf.GameObjectCalculator)]
        private IGameObjectCalculator m_InteractionSelfCalculator;


        [Title("Target Game Object")]

        [OdinSerialize]
        private IGameObjectCalculator m_TargetGameObject;

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
                m_TargetGameObject = m_TargetGameObject.Clone(),
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
            var gameObj = m_TargetGameObject.CalculateGameObject();

            if (gameObj == null)
            {
                return m_WhatToReturnIfGameObjectIsNull;
            }

            return m_InteractionCondition.IsFulfilled(gameObj);
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                Debug.LogError($"NULL input gameobject in class {nameof(InteractionConditionOnGameObjectCalculator_CachedCondition)}");
                return false;
            }
            m_Self = gameObj;

            switch (m_WhoIsInteractionSelf)
            {
                case WhoIsSelf.Self:
                    m_InteractionSelf = m_Self;
                    break;
                case WhoIsSelf.Custom:
                    break;
                case WhoIsSelf.GameObjectCalculator:
                    m_InteractionSelfCalculator?.SetGameObject(m_Self);
                    m_InteractionSelf = m_InteractionSelfCalculator?.CalculateGameObject();
                    break;
                default:
                    break;
            }

            if (m_InteractionSelf == null)
            {
                Debug.LogError($"NULL Interaction Self in gameobject {m_Self.name} in class {nameof(InteractionConditionOnGameObjectCalculator_CachedCondition)}");
                return false;
            }

            bool result = m_TargetGameObject.SetGameObject(m_InteractionSelf);
            result &= m_InteractionCondition.SetGameObject(m_InteractionSelf);

            return result;
        }
    }
}
