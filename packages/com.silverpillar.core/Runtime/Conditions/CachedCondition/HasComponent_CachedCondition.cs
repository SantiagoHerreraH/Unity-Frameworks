using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class HasComponent_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private SelfType m_WhichGameObject;

        [SerializeField, ShowIf(nameof(m_WhichGameObject), SelfType.CustomGameObject)]
        private GameObject m_TargetGameObject;

        [OdinSerialize, ShowInInspector]
        [TypeDrawerSettings(BaseType = typeof(Component))]
        private Type m_ComponentType;

        private GameObject m_Self;


        public ICachedCondition Clone()
        {
            return new HasComponent_CachedCondition
            {
                m_WhichGameObject = m_WhichGameObject,
                m_TargetGameObject = m_TargetGameObject,
                m_ComponentType = m_ComponentType
            };
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool IsFulfilled()
        {
            if (m_TargetGameObject == null)
            {
                Debug.LogError(
                    $"{nameof(HasComponent_CachedCondition)}: Target GameObject is null.");
                return false;
            }

            if (m_ComponentType == null)
            {
                Debug.LogError(
                    $"{nameof(HasComponent_CachedCondition)} on " +
                    $"{m_TargetGameObject.name}: Component type is null.");
                return false;
            }

            if (!typeof(Component).IsAssignableFrom(m_ComponentType))
            {
                Debug.LogError(
                    $"{nameof(HasComponent_CachedCondition)} on " +
                    $"{m_TargetGameObject.name}: Type {m_ComponentType.Name} " +
                    $"does not derive from {nameof(Component)}.");
                return false;
            }

            return m_TargetGameObject.GetComponent(m_ComponentType) != null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhichGameObject == SelfType.ThisGameObject)
            {
                m_TargetGameObject = gameObj;
            }

            return m_Self != null
                && m_TargetGameObject != null
                && m_ComponentType != null;
        }


        private IEnumerable<Type> GetComponentTypes()
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (type == null)
                {
                    continue;
                }

                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                yield return type;
            }
        }
    }
}