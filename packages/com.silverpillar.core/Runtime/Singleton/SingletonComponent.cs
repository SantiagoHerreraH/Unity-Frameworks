using UnityEngine;
using Sirenix.OdinInspector;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SilverPillar.Core
{
#if UNITY_EDITOR
    /// <summary>
    /// Internal non-generic class to handle Editor events safely.
    /// Unity attributes like [InitializeOnLoadMethod] do not support generic classes.
    /// </summary>
    internal static class SingletonEditorWarmup
    {
        public static event Action OnPostExitPlayMode;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    OnPostExitPlayMode?.Invoke();
                }
            };
        }
    }
#endif

    /// <summary>
    /// Generic singleton base class for MonoBehaviours using Odin's SerializedMonoBehaviour.
    /// Provides a thread-safe, scene-persistent singleton pattern with full lifecycle control.
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
        /// </summary>
        public static T Instance
        {
            get
            {
                if (m_ApplicationIsQuitting)
                {
                    Debug.LogWarning($"[SingletonComponent] Instance of '{typeof(T).Name}' requested after application quit.");
                    return null;
                }

                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        m_Instance = FindFirstObjectByType<T>();

                        if (m_Instance == null)
                        {
                            Debug.LogWarning($"[SingletonComponent] No instance of '{typeof(T).Name}' found in scene.");
                        }
                    }

                    return m_Instance;
                }
            }
        }

        public static bool IsInitialized => m_Instance != null;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            lock (m_Lock)
            {
                if (m_Instance != null && m_Instance != this)
                {
                    Debug.LogWarning($"[SingletonComponent] Duplicate instance of '{typeof(T).Name}' detected. Destroying duplicate.");
                    Destroy(gameObject);
                    return;
                }

                m_Instance = (T)this;
                m_ApplicationIsQuitting = false;

#if UNITY_EDITOR
                // Subscribe this specific generic instance to the editor cleanup event
                SingletonEditorWarmup.OnPostExitPlayMode -= ResetStatics;
                SingletonEditorWarmup.OnPostExitPlayMode += ResetStatics;
#endif

                if (m_Persistent)
                {
                    if (transform.parent != null)
                    {
                        Debug.LogWarning($"[SingletonComponent] '{typeof(T).Name}' is persistent but not root. Detaching.");
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
#if UNITY_EDITOR
                SingletonEditorWarmup.OnPostExitPlayMode -= ResetStatics;
#endif
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

        protected abstract void OnAwake();
        protected abstract void OnShutdown();

        /// <summary>
        /// Resets the static references. Called manually or via Editor event.
        /// </summary>
        private static void ResetStatics()
        {
            m_Instance = null;
            m_ApplicationIsQuitting = false;
        }

        // ─── Editor Utilities ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        [TitleGroup("Singleton Settings")]
        [ShowInInspector, ReadOnly, LabelText("Is Active Instance")]
        private bool m_IsActiveInstance => m_Instance == this;
#endif
    }
}
