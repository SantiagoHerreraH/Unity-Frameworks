using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class SetScreenTimeoutSettings_CachedGameAction : ICachedGameAction
    {
        public enum SleepTimeoutSettings
        {
            NeverSleep,
            SystemSettings
        }

        [SerializeField]
        private SleepTimeoutSettings m_SleepTimeoutSettings;

        private GameObject m_GameObject;

        public ICachedGameAction Clone()
        {
            return new SetScreenTimeoutSettings_CachedGameAction
            {
                m_SleepTimeoutSettings = m_SleepTimeoutSettings,
                m_GameObject = m_GameObject
            };
        }

        public void Execute()
        {
            switch (m_SleepTimeoutSettings)
            {
                case SleepTimeoutSettings.NeverSleep:
                    Screen.sleepTimeout = SleepTimeout.NeverSleep;
                    break;

                case SleepTimeoutSettings.SystemSettings:
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    break;

                default:
                    Screen.sleepTimeout = SleepTimeout.SystemSetting;
                    break;
            }
        }

        public GameObject GetGameObject()
        {
            return m_GameObject;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_GameObject = gameObj;
            return m_GameObject != null;
        }
    }
}