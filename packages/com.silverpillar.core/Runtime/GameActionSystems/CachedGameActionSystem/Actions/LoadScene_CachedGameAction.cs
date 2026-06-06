using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilverPillar.Core
{
    [Serializable]
    public class LoadScene_CachedGameAction : ICachedGameAction
    {
        [OdinSerialize, ShowInInspector]
        private IString m_SceneName;

        [SerializeField]
        private LoadSceneMode m_Mode = LoadSceneMode.Single;

        private GameObject m_TargetGameObject;

        public ICachedGameAction Clone()
        {
            return new LoadScene_CachedGameAction
            {
                m_SceneName = this.m_SceneName,
                m_Mode = this.m_Mode,
                m_TargetGameObject = this.m_TargetGameObject
            };
        }

        public void Execute()
        {
            var sceneName = m_SceneName.CalculateString();
            if (m_SceneName != null && !string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName, m_Mode);
            }
            else
            {
                Debug.LogWarning($"LoadScene_CachedGameAction in game object {m_TargetGameObject} with scene name {sceneName} : No valid scene name.");
            }
        }

        public GameObject GetGameObject()
        {
            return m_TargetGameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_TargetGameObject = gameObj;
            bool allGood = m_SceneName != null ? m_SceneName.SetGameObject(gameObj) : false;
            return allGood && gameObj != null;
        }
    }
}
