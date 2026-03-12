using CrashKonijn.Agent.Runtime;
using Pillar;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Pillar
{
    public enum WhenToCheckActions
    {
        DontCheckAtRuntime,
        OnAwake,
        OnStart,
        BeforeExecutingAction
    }

    public class ChannelListener : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("The gameObject has to be active at least once for it to listen to anything")]
        private bool m_ListenEvenIfDeactivated;


        [Header("Channels")]
        [SerializeField]
        private bool m_ListenOnAllChannels;
        [SerializeField]
        private List<Channel> m_ChannelsToListenTo = new();


        [Header("Events")]
        [SerializeField, Tooltip("Triggered on static and non static calls. Triggered before everything.")]
        private UnityEvent m_OnListen = new();
        [SerializeField, Tooltip("Triggered only on non static calls")]
        private UnityEvent<GameObject> m_OnListenTo = new();

        public enum WhatToTriggerFirst
        {
            OnListenFromEvent,
            Actions
        }

        public enum WhatActionsToTriggerFirst
        {
            ExecuteOnSelf,
            ExecuteOnNotifier
        }

        [Header("Actions")]
        [SerializeField]
        private bool m_DebugWarningsIfActionsDidNotExecute;
        [SerializeField]
        private WhatToTriggerFirst m_WhatToTriggerFirst;
        [SerializeField]
        private WhatActionsToTriggerFirst m_WhatActionsToTriggerFirst;

        

        [Header("Actions On Self")]

        [SerializeField, Tooltip("Debug warnings have to be turned on if you want to see any debug warnings")]
        private WhenToCheckActions m_WhenToCheckActionsOnSelf;
        [Button("Check Actions On Self")]
        private void CheckActionsOnSelfButton()
        {
            CheckActionsOnSelf(true);
        }

        [SerializeField, Tooltip("These actions are executed on listen")]
        private List<IGameAction> m_ActionsToExecuteOnSelf = new();

        [Header("Actions On Notifier")]

        [SerializeField]
        private bool m_CheckIfActionsOnNotifierCanExecuteBeforeExecutingThem;
        [SerializeField, Tooltip("These actions are executed on listen.")]
        private List<IGameAction> m_ActionsToExecuteOnNotifier = new();

        private static HashSet<ChannelListener> m_AllChannelListeners = new();
        private static Dictionary<Channel, HashSet<ChannelListener>> m_Channel_To_ChannelListeners = new();
        private static bool m_Initialized = false;

        public static HashSet<ChannelListener> AllChannelListeners
        {
            get
            {
                InitializeIfNeeded();
                return m_AllChannelListeners;
            }
        }

        public static List<ChannelListener> GetChannelListeners(Channel channel)
        {
            InitializeIfNeeded();
            return m_Channel_To_ChannelListeners[channel].ToList();
        }

        private static void InitializeIfNeeded()
        {
            if (!m_Initialized)
            {
                m_Initialized = true;
                m_AllChannelListeners = Object.FindObjectsByType<ChannelListener>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToHashSet();

                var allChannels = ScriptableObjectRegistry.Instance.GetAllOfType<Channel>();
                foreach (var channel in allChannels)
                {
                    m_Channel_To_ChannelListeners.Add(channel, new());
                }

                foreach (var listener in m_AllChannelListeners)
                {
                    Register(listener);
                }

            }
        }

        private static void Register(ChannelListener listener)
        {
            InitializeIfNeeded();
            m_AllChannelListeners.Add(listener);

            if (listener.m_ListenOnAllChannels)
            {
                foreach (var pair in m_Channel_To_ChannelListeners)
                {
                    pair.Value.Add(listener);
                }
            }
            else
            {
                foreach (var channel in listener.m_ChannelsToListenTo)
                {
                    m_Channel_To_ChannelListeners[channel].Add(listener);
                }
            }
        }

        private static void Unregister(ChannelListener listener)
        {
            InitializeIfNeeded();
            m_AllChannelListeners.Remove(listener);

            if (listener.m_ListenOnAllChannels)
            {
                foreach (var pair in m_Channel_To_ChannelListeners)
                {
                    pair.Value.Remove(listener);
                }
            }
            else
            {
                foreach (var channel in listener.m_ChannelsToListenTo)
                {
                    m_Channel_To_ChannelListeners[channel].Remove(listener);
                }
            }
        }

        private void Awake()
        {
            Register(this);

            if (m_WhenToCheckActionsOnSelf == WhenToCheckActions.OnAwake)
            {
                CheckActionsOnSelf(m_DebugWarningsIfActionsDidNotExecute);
            }
        }
        private void Start()
        {
            if (m_WhenToCheckActionsOnSelf == WhenToCheckActions.OnStart)
            {
                CheckActionsOnSelf(m_DebugWarningsIfActionsDidNotExecute);
            }
        }

        private void OnDestroy()
        {
            Unregister(this);
        }

        public void Listen(ChannelNotifier notifier)
        {
            InitializeIfNeeded();

            m_OnListen.Invoke();

            switch (m_WhatToTriggerFirst)
            {
                case WhatToTriggerFirst.OnListenFromEvent:

                    m_OnListenTo.Invoke(notifier.gameObject);
                    TriggerActions(notifier);

                    break;
                case WhatToTriggerFirst.Actions:

                    TriggerActions(notifier);
                    m_OnListenTo.Invoke(notifier.gameObject);

                    break;
                default:
                    break;
            }

        }

        public void TriggerListen()
        {
            m_OnListen.Invoke();
        }

        public bool CanListenTo(ChannelNotifier notifier)
        {
            return
                (m_ListenEvenIfDeactivated || (gameObject.activeInHierarchy)) &&
                (m_ListenOnAllChannels || NotifierChannelMatchesListenerChannel(notifier));
        }

        public bool HasChannel(Channel dataChannel)
        {
            return m_ChannelsToListenTo.Contains(dataChannel);
        }

        private bool NotifierChannelMatchesListenerChannel(ChannelNotifier notifier)
        {
            foreach (var listenerChannel in m_ChannelsToListenTo)
            {
                if (notifier.ChannelsToNotifyIn.Contains(listenerChannel))
                {
                    return true;
                }
            }

            return false;
        }

        private void TriggerActions(ChannelNotifier notifier)
        {
            switch (m_WhatActionsToTriggerFirst)
            {
                case WhatActionsToTriggerFirst.ExecuteOnSelf:

                    TriggerSelfActions();
                    TriggerNotifierActions(notifier);

                    break;
                case WhatActionsToTriggerFirst.ExecuteOnNotifier:

                    TriggerNotifierActions(notifier);
                    TriggerSelfActions();

                    break;
                default:
                    break;
            }
        }

        private void TriggerSelfActions()
        {
            bool canExecute = true;
            for (int i = 0; i < m_ActionsToExecuteOnSelf.Count; i++)
            {
                if (m_WhenToCheckActionsOnSelf == WhenToCheckActions.BeforeExecutingAction)
                {
                    canExecute = m_ActionsToExecuteOnSelf[i].CanExecute(gameObject);
                    if (!canExecute && m_DebugWarningsIfActionsDidNotExecute)
                    {
                        Debug.LogWarning(
                            $"Could not execute action on self of type {m_ActionsToExecuteOnSelf[i].GetType().Name}" +
                            $" in Channel Listener. Warning from {nameof(ChannelListener)}" +
                            $" component in gameObject {gameObject.name}");
                    }
                }

                if (canExecute)
                {
                    m_ActionsToExecuteOnSelf[i].Execute(gameObject);
                }

                canExecute = true;
            }
        }

        private void TriggerNotifierActions(ChannelNotifier notifier)
        {
            for (int i = 0; i < m_ActionsToExecuteOnNotifier.Count; i++)
            {
                if (m_CheckIfActionsOnNotifierCanExecuteBeforeExecutingThem)
                {
                    if (m_ActionsToExecuteOnNotifier[i].CanExecute(notifier.gameObject))
                    {
                        m_ActionsToExecuteOnNotifier[i].Execute(notifier.gameObject);
                    }
                    else if (m_DebugWarningsIfActionsDidNotExecute)
                    {
                        Debug.LogWarning(
                            $"Could not execute action of type {m_ActionsToExecuteOnNotifier[i].GetType().Name}" +
                            $" in Channel Notifier. Warning from {nameof(ChannelListener)}" +
                            $" component in gameObject {gameObject.name}");
                    }
                }
                else
                {
                    m_ActionsToExecuteOnNotifier[i].Execute(notifier.gameObject);
                }
                
            }
        }

        private void CheckActionsOnSelf(bool showDebugWarnings)
        {
            for (int i = 0; i < m_ActionsToExecuteOnSelf.Count; i++)
            {
                if (!m_ActionsToExecuteOnSelf[i].CanExecute(gameObject) && showDebugWarnings)
                {
                    Debug.LogWarning(
                        $"Could not execute action on self of type {m_ActionsToExecuteOnNotifier[i].GetType().Name}" +
                        $" in Channel Listener. Warning from {nameof(ChannelListener)}" +
                        $" component in gameObject {gameObject.name}");
                }

            }
        }
    }
}
