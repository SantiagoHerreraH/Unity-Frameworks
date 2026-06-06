using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetParent_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private GameObject m_Parent;

        [SerializeField]
        private SelfType m_Child;

        [SerializeField, ShowIf(nameof(m_Child), SelfType.CustomGameObject)]
        private GameObject m_ChildGameObject;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetParent_CachedGameAction
            {
                m_Parent = m_Parent,
                m_Child = m_Child,
                m_ChildGameObject = m_ChildGameObject,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            GameObject child = GetChildGameObject();

            if (child == null)
                return;

            child.transform.SetParent(m_Parent != null ? m_Parent.transform : null, true);
        }

        public GameObject GetGameObject()
        {
            GameObject child = GetChildGameObject();

            if (child != null)
                return child;

            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            return GetChildGameObject() != null;
        }

        private GameObject GetChildGameObject()
        {
            switch (m_Child)
            {
                case SelfType.ThisGameObject:
                    return m_GameObject;

                case SelfType.CustomGameObject:
                    return m_ChildGameObject;

                default:
                    return m_GameObject;
            }
        }
    }
}