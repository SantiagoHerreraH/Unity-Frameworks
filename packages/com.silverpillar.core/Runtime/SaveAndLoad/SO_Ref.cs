using Sirenix.OdinInspector;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pillar
{
    /*
     System to easily load, save (using easy save 3) and reference scriptable objects
     */
    [Serializable]
    public struct SO_Ref<T> : IEquatable<SO_Ref<T>> where T : SaveableScriptableObject
    {
        [SerializeField, HideInInspector]
        private string m_Guid;

        [SerializeField, InfoBox("ScriptableObject is missing!", InfoMessageType.Warning, "@ScriptableObject == null")]
        private T ScriptableObject;

        public SO_Ref(T scriptableObject)
        {
            ScriptableObject = scriptableObject;
            m_Guid = scriptableObject != null ? scriptableObject.Guid : null;
        }

        public T Get()
        {
            if (ScriptableObject != null)
            {
                // Keep GUID in sync if someone assigned the object directly.
                if (string.IsNullOrEmpty(m_Guid))
                    m_Guid = ScriptableObject.Guid;

                return ScriptableObject;
            }

            if (string.IsNullOrEmpty(m_Guid))
                return null;

            ScriptableObject = ScriptableObjectRegistry.Instance.Get<T>(m_Guid);
            return ScriptableObject;
        }

        public bool Equals(SO_Ref<T> other) => m_Guid == other.m_Guid;
        public override bool Equals(object obj) => obj is SO_Ref<T> other && Equals(other);

        public override int GetHashCode() => string.IsNullOrEmpty(m_Guid) ? 0 : m_Guid.GetHashCode();

        public static bool operator ==(SO_Ref<T> left, SO_Ref<T> right) => left.Equals(right);
        public static bool operator !=(SO_Ref<T> left, SO_Ref<T> right) => !left.Equals(right);

        public override string ToString() => $"{typeof(T).Name}({m_Guid})";
    }

}


