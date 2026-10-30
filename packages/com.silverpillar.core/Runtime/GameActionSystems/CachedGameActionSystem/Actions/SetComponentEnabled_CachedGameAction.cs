
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Reflection;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetComponentEnabled_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_WhichComponent;

        [SerializeField, ShowIf(
            nameof(m_WhichComponent),
            SelfType.CustomGameObject)]
        private Component m_CustomComponent;

        [OdinSerialize, ShowInInspector]
        [ShowIf(nameof(UsesSelfComponent))]
        [TypeSelectorSettings(
            FilterTypesFunction = nameof(IsToggleableComponentType))]
        private Type m_ComponentType;

        [SerializeField]
        private bool m_Enable = true;

        private GameObject m_Self;
        private Component m_CachedComponent;
        private PropertyInfo m_CachedEnabledProperty;

        private bool UsesSelfComponent =>
            m_WhichComponent != SelfType.CustomGameObject;

        private bool IsToggleableComponentType(Type type)
        {
            if (type == null ||
                type.IsAbstract ||
                type.ContainsGenericParameters ||
                !typeof(Component).IsAssignableFrom(type))
            {
                return false;
            }

            PropertyInfo property = type.GetProperty(
                "enabled",
                BindingFlags.Instance | BindingFlags.Public);

            return property != null &&
                   property.PropertyType == typeof(bool) &&
                   property.GetSetMethod() != null;
        }

        public ICachedGameAction Clone()
        {
            return new SetComponentEnabled_CachedGameAction
            {
                m_WhichComponent = m_WhichComponent,
                m_CustomComponent = m_CustomComponent,
                m_ComponentType = m_ComponentType,
                m_Enable = m_Enable
            };
        }

        public void Execute()
        {
            if (m_CachedComponent == null)
            {
                Debug.LogError(
                    $"{nameof(SetComponentEnabled_CachedGameAction)}: " +
                    "No component is cached. " +
                    "Make sure SetGameObject was successful.");
                return;
            }

            // Fast paths for common Unity component types.

            if (m_CachedComponent is Behaviour behaviour)
            {
                behaviour.enabled = m_Enable;
                return;
            }

            if (m_CachedComponent is Renderer renderer)
            {
                renderer.enabled = m_Enable;
                return;
            }

            if (m_CachedComponent is Collider collider)
            {
                collider.enabled = m_Enable;
                return;
            }

            if (m_CachedComponent is Collider2D collider2D)
            {
                collider2D.enabled = m_Enable;
                return;
            }

            // Fallback for other component types exposing
            // a public writable bool enabled property.

            m_CachedEnabledProperty.SetValue(
                m_CachedComponent,
                m_Enable);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            m_CachedComponent = null;
            m_CachedEnabledProperty = null;

            if (m_WhichComponent == SelfType.CustomGameObject)
            {
                m_CachedComponent = m_CustomComponent;
            }
            else
            {
                if (m_Self == null || m_ComponentType == null)
                {
                    Debug.LogError(
                        $"{nameof(SetComponentEnabled_CachedGameAction)}: " +
                        "GameObject or Component Type is null.");
                    return false;
                }

                if (!typeof(Component).IsAssignableFrom(m_ComponentType))
                {
                    Debug.LogError(
                        $"{nameof(SetComponentEnabled_CachedGameAction)}: " +
                        $"{m_ComponentType.Name} is not a Unity Component.");
                    return false;
                }

                m_CachedComponent =
                    m_Self.GetComponent(m_ComponentType);
            }

            if (m_CachedComponent == null)
            {
                Debug.LogError(
                    $"{nameof(SetComponentEnabled_CachedGameAction)}: " +
                    "Could not find the specified component.");
                return false;
            }

            m_CachedEnabledProperty =
                m_CachedComponent.GetType().GetProperty(
                    "enabled",
                    BindingFlags.Instance | BindingFlags.Public);

            if (m_CachedEnabledProperty == null ||
                m_CachedEnabledProperty.PropertyType != typeof(bool) ||
                m_CachedEnabledProperty.GetSetMethod() == null)
            {
                Debug.LogError(
                    $"{nameof(SetComponentEnabled_CachedGameAction)}: " +
                    $"Component {m_CachedComponent.GetType().Name} " +
                    "does not have a writable enabled property.");

                m_CachedComponent = null;
                m_CachedEnabledProperty = null;
                return false;
            }

            return true;
        }
    }
}
