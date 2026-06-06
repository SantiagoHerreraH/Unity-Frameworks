using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class ApplicationQuit_CachedGameAction : ICachedGameAction
    {
        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new ApplicationQuit_CachedGameAction { m_GameObject = m_GameObject};
        }

        public void Execute()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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