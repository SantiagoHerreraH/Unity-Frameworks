using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SilverPillar.Core
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours using Odin's SerializedMonoBehaviour.
    /// Provides a thread-safe, scene-persistent singleton pattern with full lifecycle control.
    ///
    /// Usage:
    ///   public class MyManager : SingletonComponent&lt;MyManager&gt; { ... }
    ///   MyManager.Instance.DoSomething();
    /// </summary>
    public abstract class SingletonComponent<T> : SerializedMonoBehaviour
        where T : SingletonComponent<T>
    {
        // ─── Inspector ───────────────────────────────────────────────────────────

        [TitleGroup("Singleton Settings")]
        [Tooltip("If true, this GameObject will persist across scene loads.")]
        [SerializeField] private bool m_Persistent = true;


        // ─── Static State ────────────────────────────────────────────────────────

        private static T m_Instance;
        private static readonly object m_Lock = new object();
        private static bool m_ApplicationIsQuitting = false;

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the singleton instance. Returns null during application quit.
        /// Will log a warning if accessed before Awake has run.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_ApplicationIsQuitting)
                {
                    Debug.LogWarning(
                        $"[SingletonComponent] Instance of '{typeof(T).Name}' requested after application quit. Returning null.");
                    return null;
                }

                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        m_Instance = FindFirstObjectByType<T>();

                        if (m_Instance == null)
                        {
                            Debug.LogWarning(
                                $"[SingletonComponent] No instance of '{typeof(T).Name}' found in scene. " +
                                "Make sure it is present in the scene before accessing it.");
                        }
                    }

                    return m_Instance;
                }
            }
        }

        /// <summary>
        /// True once Awake has run and the instance is ready.
        /// </summary>
        public static bool IsInitialized => m_Instance != null;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            lock (m_Lock)
            {
                if (m_Instance != null && m_Instance != this)
                {
                    Debug.LogWarning(
                             $"[SingletonComponent] Duplicate instance of '{typeof(T).Name}' detected on " +
                             $"'{gameObject.name}'. Destroying duplicate.");
                    Destroy(gameObject);
                    return;
                }

                m_Instance = (T)this;
                m_ApplicationIsQuitting = false;

                if (m_Persistent)
                {
                    // Only call DontDestroyOnLoad on root GameObjects.
                    if (transform.parent != null)
                    {
                        Debug.LogWarning(
                            $"[SingletonComponent] '{typeof(T).Name}' is marked persistent but is not a root " +
                            "GameObject. DontDestroyOnLoad requires a root object. Detaching from parent.");
                        transform.SetParent(null);
                    }
                    DontDestroyOnLoad(gameObject);
                }
            }

            OnAwake();
        }

        private void OnDestroy()
        {
            if (m_Instance == this)
            {
                OnShutdown();
                m_Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            m_ApplicationIsQuitting = true;
            OnShutdown();
        }

        // ─── Extension Points ────────────────────────────────────────────────────

        /// <summary>
        /// Called once after this instance is confirmed as the singleton.
        /// Override instead of Awake for initialization logic.
        /// </summary>
        protected abstract void OnAwake();

        /// <summary>
        /// Called when the singleton instance is destroyed or the application quits.
        /// Override for cleanup logic (unsubscribing events, releasing resources, etc.).
        /// </summary>
        protected abstract void OnShutdown();

        // ─── Editor Utilities ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        [TitleGroup("Singleton Settings")]
        [ShowInInspector, ReadOnly]
        [LabelText("Is Active Instance")]
        private bool m_IsActiveInstance => m_Instance == this;

        /// <summary>
        /// Resets the static instance reference when exiting play mode in the editor,
        /// preventing stale references between play sessions.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ResetOnExitPlayMode()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    m_Instance = null;
                    m_ApplicationIsQuitting = false;
                }
            };
        }
#endif
    }
}