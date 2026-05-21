using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilverPillar.Core
{
    public class SingletonWrapper<T> where T : MonoBehaviour
    {
        private static T m_Instance;
        private static bool m_SubscribedToSceneLoaded;

        public static T Instance
        {
            get
            {
                EnsureInitialized();
                return m_Instance;
            }
        }

        public static bool HasInstance
        {
            get
            {
                EnsureInitialized();
                return m_Instance != null;
            }
        }

        private static void EnsureInitialized()
        {
            SubscribeToSceneLoaded();

            if (m_Instance != null)
                return;

            FindInstanceInScene();
        }

        private static void SubscribeToSceneLoaded()
        {
            if (m_SubscribedToSceneLoaded)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            m_SubscribedToSceneLoaded = true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindInstanceInScene();
        }

        private static void FindInstanceInScene()
        {
            m_Instance = Object.FindFirstObjectByType<T>();

            if (m_Instance == null)
            {
                Debug.LogWarning(
                    $"{nameof(SingletonWrapper<T>)}<{typeof(T).Name}>: No instance found in the active scene."
                );
            }
        }

        public static void Clear()
        {
            m_Instance = null;
        }
    }
}