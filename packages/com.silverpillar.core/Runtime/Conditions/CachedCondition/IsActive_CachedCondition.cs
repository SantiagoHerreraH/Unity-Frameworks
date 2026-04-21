using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class IsActive_CachedCondition : ICachedCondition
    {
        public enum Type
        {
            IsActiveInSceneHierarchy,
            IsSelfActive
        }
        [SerializeField]
        private Type m_Type;

        private GameObject m_GameObj;

        public IsActive_CachedCondition() { }
        public IsActive_CachedCondition(IsActive_CachedCondition other)
        {
            m_Type = other.m_Type;
            m_GameObj = other.m_GameObj;
        }

        public ICachedCondition Clone()
        {
            return new IsActive_CachedCondition(this);
        }

#nullable enable
        public GameObject? GetGameObject()
        {
            return m_GameObj;
        }

        public bool IsFulfilled()
        {
            if (m_GameObj != null)
            {
                switch (m_Type)
                {
                    case Type.IsActiveInSceneHierarchy:
                        return m_GameObj.activeInHierarchy;
                    case Type.IsSelfActive:
                        return m_GameObj.activeSelf;
                    default:
                        break;
                }


            }

            return false;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                m_GameObj = gameObj;
                return true;
            }

            return false;
        }
    }
}
