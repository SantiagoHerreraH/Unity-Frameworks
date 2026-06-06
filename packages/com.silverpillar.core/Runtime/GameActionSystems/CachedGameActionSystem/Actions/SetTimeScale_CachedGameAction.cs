using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetTimeScale_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private float m_TimeScale = 1f;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetTimeScale_CachedGameAction
            {
                m_TimeScale = m_TimeScale,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            Time.timeScale = Mathf.Max(0f, m_TimeScale);
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            return true;
        }
    }
}