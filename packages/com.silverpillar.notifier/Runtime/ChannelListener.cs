using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using SilverPillar.Core;
using Sirenix.Serialization;

namespace SilverPillar.Notifier
{

    public class ChannelListener : SerializedMonoBehaviour
    {
        private enum WhenToRegister
        {
            OnEnable,
            OnAwake
        }

        private enum WhenToDeregister
        {
            OnDisable,
            OnDestroy
        }

        [Title("Registration Settings")]
        [SerializeField]
        private WhenToRegister m_WhenToRegister;
        [SerializeField]
        private WhenToDeregister m_WhenToDeregister;


        [Title("Channels")]
        [SerializeField]
        private bool m_ListenOnAllChannels;
        [SerializeField]
        private List<Channel> m_ChannelsToListenTo = new();


        [Title("Events")]
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

        [Title("Actions")]
        [SerializeField]
        private bool m_DebugWarningsIfActionsDidNotExecute;
        [SerializeField]
        private WhatToTriggerFirst m_WhatToTriggerFirst;
        [SerializeField]
        private WhatActionsToTriggerFirst m_WhatActionsToTriggerFirst;


        [Title("Actions On Notifier")]

        [OdinSerialize, ShowInInspector, Tooltip("These actions are executed on listen.")]
        private List<ICachedGameAction> m_ActionsToExecuteOnNotifier = new();


        [Title("Actions On Self")]

        [OdinSerialize, ShowInInspector, Tooltip("These actions are executed on listen")]
        private List<ICachedGameAction> m_ActionsToExecuteOnSelf = new();
        private bool m_InitializedActionsOnSelf = false;
        [Button("Check Actions On Self")]
        private void CheckActionsOnSelfButton()
        {
            CheckActionsOnSelf(true);
        }


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

                var allChannels = ScriptableObjectRegistry.Instance.GetAllOfType<Channel>();
                foreach (var channel in allChannels)
                {
                    m_Channel_To_ChannelListeners.Add(channel, new());
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

            foreach (ChannelNotifier item in ChannelNotifier.RegisteredChannelNotifiers)
            {
                item.AddListener(listener);
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

            foreach (ChannelNotifier item in ChannelNotifier.RegisteredChannelNotifiers)
            {
                item.RemoveListener(listener);
            }
        }

        private void Awake()
        {
            if (m_WhenToRegister == WhenToRegister.OnAwake)
            {
                Register(this);
            }
        }

        private void OnEnable()
        {
            if (m_WhenToRegister == WhenToRegister.OnEnable)
            {
                Register(this);
            }
        }

        private void OnDisable()
        {
            if (m_WhenToDeregister == WhenToDeregister.OnDisable)
            {
                Unregister(this);
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToDeregister == WhenToDeregister.OnDestroy)
            {
                Unregister(this);
            }
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
            return m_ListenOnAllChannels || NotifierChannelMatchesListenerChannel(notifier);
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
            if (m_ActionsToExecuteOnSelf == null)
            {
                return;
            }

            if (!m_InitializedActionsOnSelf)
            {
                for (int i = 0; i < m_ActionsToExecuteOnSelf.Count; ++i)
                {
                    if (!m_ActionsToExecuteOnSelf[i].SetGameObject(gameObject))
                    {
                        Debug.LogWarning(
                            $"Could not execute action on self of type {m_ActionsToExecuteOnSelf[i].GetType().Name}" +
                            $" in Channel Listener. Warning from {nameof(ChannelListener)}" +
                            $" component in gameObject {gameObject.name}");
                    }
                }
                m_InitializedActionsOnSelf = true;
            }

            for (int i = 0; i < m_ActionsToExecuteOnSelf.Count; i++)
            {
                m_ActionsToExecuteOnSelf[i].Execute();
            }
        }

        private void TriggerNotifierActions(ChannelNotifier notifier)
        {
            for (int i = 0; i < m_ActionsToExecuteOnNotifier.Count; i++)
            {
                if (m_ActionsToExecuteOnNotifier[i].SetGameObject(notifier.gameObject))
                {
                    m_ActionsToExecuteOnNotifier[i].Execute();
                }
                else if (m_DebugWarningsIfActionsDidNotExecute)
                {
                    Debug.LogWarning(
                        $"Could not execute action of type {m_ActionsToExecuteOnNotifier[i].GetType().Name}" +
                        $" in Channel Notifier. Warning from {nameof(ChannelListener)}" +
                        $" component in gameObject {gameObject.name}");
                }
            }
        }

        private void CheckActionsOnSelf(bool showDebugWarnings)
        {
            for (int i = 0; i < m_ActionsToExecuteOnSelf.Count; i++)
            {
                if (!m_ActionsToExecuteOnSelf[i].SetGameObject(gameObject) && showDebugWarnings)
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
