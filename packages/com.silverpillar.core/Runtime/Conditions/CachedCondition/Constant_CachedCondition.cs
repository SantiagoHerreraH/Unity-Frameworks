using System;
using UnityEngine;

namespace SilverPillar.Core
{
#nullable enable

    [Serializable]
    public class Constant_CachedCondition : ICachedCondition
    {
        [SerializeField]
        private bool m_ReturnValue;

        private GameObject? m_GameObj;

        public Constant_CachedCondition() { }
        public Constant_CachedCondition(Constant_CachedCondition other)
        {
            m_ReturnValue = other.m_ReturnValue;
        }

        public ICachedCondition Clone()
        {
            return new Constant_CachedCondition(this);
        }

        public GameObject? GetGameObject()
        {
            return m_GameObj;
        }

        public bool IsFulfilled()
        {
            return m_ReturnValue;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null)
            {
                return false;
            }

            m_GameObj = gameObj;

            return true;
        }
    }
}
