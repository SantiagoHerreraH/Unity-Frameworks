using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Custom_GameObject : IGameObjectCalculator
    {
        [SerializeField]
        private GameObject m_CustomGameObject;
        private GameObject m_Self;

        public Custom_GameObject() { }
        public Custom_GameObject(Custom_GameObject other)
        {
            m_CustomGameObject = other.m_CustomGameObject;
            m_Self = other.m_Self;
        }

        public GameObject CalculateGameObject()
        {
            return m_CustomGameObject;
        }

        public IGameObjectCalculator Clone()
        {
            return new Custom_GameObject(this);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObject)
        {
            m_Self = gameObject;
            return m_Self != null;
        }
    }
}
