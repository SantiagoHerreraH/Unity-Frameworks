using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetActive_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private bool m_SetActive;
        [SerializeField]
        private SelfType m_Who;
        [SerializeField, ShowIf(nameof(m_Who), SelfType.CustomGameObject)]
        private GameObject m_WhoToSetActive;
        private GameObject m_Self;

        public SetActive_CachedGameAction() { }
        public SetActive_CachedGameAction(SetActive_CachedGameAction other)
        {
            m_SetActive = other.m_SetActive;
            m_Who = other.m_Who;
            m_WhoToSetActive = other.m_WhoToSetActive;
            m_Self = other.m_Self;
        }

        public ICachedGameAction Clone()
        {
            return new SetActive_CachedGameAction(this);
        }

        public void Execute()
        {
            m_WhoToSetActive.SetActive(m_SetActive);
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;
            if (m_Who == SelfType.ThisGameObject)
            {
                m_WhoToSetActive = gameObj;
            }
            return m_Self != null && m_WhoToSetActive != null;
        }
    }
}
