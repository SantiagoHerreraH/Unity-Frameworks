using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using SilverPillar.Core;

namespace SilverPillar.Notifier
{
    public class ChannelNotifier : MonoBehaviour
    {
        [Header("Locking")]
        [SerializeField, Tooltip("If it is locked, data sending won't work even if you call it")]
        private bool m_IsLocked = false;

        [Header("Channels")]
        [SerializeField]
        private bool m_NotifyOnAllChannels;
        [SerializeField]
        private List<Channel> m_ChannelsToNotifyIn;
        public List<Channel> ChannelsToNotifyIn { get { return m_ChannelsToNotifyIn; } }

        [Header("Events")]
        [SerializeField, Tooltip("Called once when you send to all of the possible receivers within your allowed channels.Called right before you notify them")]
        private UnityEvent m_OnNotifyAllPossibleListeners = new();
        [SerializeField, Tooltip("Called once per send")]
        private UnityEvent<GameObject> m_OnNotifyTo = new();

        public enum WhatToTriggerFirstOnNotify
        {
            OnNotifyToEvent,
            ActionsOnListener
        }

        public enum WhenToTriggerActionsOnSelf
        {
            BeforeNotify,
            AfterNotify
        }

        [Header("Actions")]
        [SerializeField]
        private bool m_DebugWarningsIfActionsDidNotExecute;
        [SerializeField]
        private WhenToTriggerActionsOnSelf m_WhenToTriggerActionsOnSelf;
        [SerializeField]
        private WhatToTriggerFirstOnNotify m_WhatToTriggerFirstOnNotify;

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

        [Header("Actions On Listener")]

        [SerializeField]
        private bool m_CheckIfActionsOnListenerCanExecuteBeforeExecutingThem;
        [SerializeField, Tooltip("These actions are executed on notify to.")]
        private List<IGameAction> m_ActionsToExecuteOnListener = new();

        private List<ChannelListener> m_PossibleChannelListeners = new();
        private List<ChannelNotifier> m_GameObjectChannelNotifiers = new();
        public static List<ChannelNotifier> RegisteredChannelNotifiers { get; private set; } = new();
        private void Awake()
        {
            Register(this);
            InitializeLocal();
        }

        private void OnDestroy()
        {
            Unregister(this);
        }

        private void Start()
        {
            RefreshPossibleListeners();
        }

        #region Static Calls

        private static void Register(ChannelNotifier dataSender)
        {
            RegisteredChannelNotifiers.Add(dataSender);
        }

        private static void Unregister(ChannelNotifier dataSender)
        {
            RegisteredChannelNotifiers.Remove(dataSender);
        }

        public static void LockAllChannelNotifiers()
        {
            foreach (var item in RegisteredChannelNotifiers)
            {
                item.SetLock(true);
            }
        }

        public static void UnlockAllChannelNotifiers()
        {
            foreach (var item in RegisteredChannelNotifiers)
            {
                item.SetLock(false);
            }
        }

        public static void LockAllNotifiersIfHasChannel(Channel channel)
        {
            foreach (var item in RegisteredChannelNotifiers)
            {
                if (item.HasChannel(channel))
                {
                    item.SetLock(true);
                }
            }
        }

        public static void UnlockAllNotifiersIfHasChannel(Channel channel)
        {
            foreach (var item in RegisteredChannelNotifiers)
            {
                if (item.HasChannel(channel))
                {
                    item.SetLock(false);
                }
            }
        }

        public static void NotifyChannel(Channel channel)
        {
            List<ChannelListener> listeners = ChannelListener.GetChannelListeners(channel);

            foreach (var listener in listeners)
            {
                listener.TriggerListen();
            }
        }

        #endregion

        #region Local

        //Local == all channel notifiers on GameObject
        private void InitializeLocal()
        {
            if (m_GameObjectChannelNotifiers.Count == 0)
            {
                m_GameObjectChannelNotifiers = GetComponents<ChannelNotifier>().ToList();
            }
        }

        //Local == all channel notifiers on GameObject
        public void LockLocal()
        {
            InitializeLocal();
            foreach (var dataSender in m_GameObjectChannelNotifiers)
            {
                dataSender.SetLock(true);
            }
        }

        //Local == all channel notifiers on GameObject
        public void UnlockLocal()
        {
            InitializeLocal();
            foreach (var dataSender in m_GameObjectChannelNotifiers)
            {
                dataSender.SetLock(false);
            }
        }

        //Local == all channel notifiers on GameObject
        public void LockLocalIfHasChannel(Channel dataChannel)
        {
            InitializeLocal();
            foreach (var item in m_GameObjectChannelNotifiers)
            {
                if (item.HasChannel(dataChannel))
                {
                    item.SetLock(true);
                }
            }
        }

        //Local == all channel notifiers on GameObject
        public void UnlockLocalIfHasChannel(Channel dataChannel)
        {
            InitializeLocal();
            foreach (var item in m_GameObjectChannelNotifiers)
            {
                if (item.HasChannel(dataChannel))
                {
                    item.SetLock(false);
                }
            }
        }

        //Local == all channel notifiers on GameObject
        public void CallNotifyOnAllLocal()
        {
            InitializeLocal();
            foreach (var notifier in m_GameObjectChannelNotifiers)
            {
                notifier.Notify();
            }
        }

#endregion

        public void RefreshPossibleListeners()
        {
            m_PossibleChannelListeners.Clear();

            foreach (var listener in ChannelListener.AllChannelListeners)
            {
                if (listener.CanListenTo(this))
                {
                    m_PossibleChannelListeners.Add(listener);
                }
            }
        }

        public void SetLock(bool isLocked)
        {
            m_IsLocked = isLocked;
        }

        public void Notify()
        {
            if (m_IsLocked)
            {
                return;
            }

            if (m_PossibleChannelListeners.Count == 0)
            {
                RefreshPossibleListeners();
            }

            m_OnNotifyAllPossibleListeners.Invoke();

            switch (m_WhenToTriggerActionsOnSelf)
            {
                case WhenToTriggerActionsOnSelf.BeforeNotify:

                    TriggerSelfActions();
                    NotifyListeners();

                    break;
                case WhenToTriggerActionsOnSelf.AfterNotify:

                    NotifyListeners();
                    TriggerSelfActions();

                    break;
                default:
                    break;
            }

            
        }

        public void NotifyListener(ChannelListener listener)
        {
            if (m_IsLocked)
            {
                return;
            }

            switch (m_WhatToTriggerFirstOnNotify)
            {
                case WhatToTriggerFirstOnNotify.OnNotifyToEvent:

                    m_OnNotifyTo.Invoke(listener.gameObject);
                    TriggerListenerActions(listener);

                    break;
                case WhatToTriggerFirstOnNotify.ActionsOnListener:

                    TriggerListenerActions(listener);
                    m_OnNotifyTo.Invoke(listener.gameObject);

                    break;
                default:
                    break;
            }


            listener.Listen(this);
        }

        public bool HasChannel(Channel dataChannel)
        {
            return m_ChannelsToNotifyIn.Contains(dataChannel);
        }

        private void NotifyListeners()
        {
            foreach (var receiver in m_PossibleChannelListeners)
            {
                NotifyListener(receiver);
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
                            $" on Channel Notifier. Warning from {nameof(ChannelNotifier)}" +
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

        private void TriggerListenerActions(ChannelListener listener)
        {
            for (int i = 0; i < m_ActionsToExecuteOnListener.Count; i++)
            {
                if (m_CheckIfActionsOnListenerCanExecuteBeforeExecutingThem)
                {
                    if (m_ActionsToExecuteOnListener[i].CanExecute(listener.gameObject))
                    {
                        m_ActionsToExecuteOnListener[i].Execute(listener.gameObject);
                    }
                    else if (m_DebugWarningsIfActionsDidNotExecute)
                    {
                        Debug.LogWarning(
                            $"Could not execute action of type {m_ActionsToExecuteOnListener[i].GetType().Name}" +
                            $" in Channel Notifier. Warning from {nameof(ChannelNotifier)}" +
                            $" component in gameObject {gameObject.name}");
                    }
                }
                else
                {
                    m_ActionsToExecuteOnListener[i].Execute(listener.gameObject);
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
                        $"Could not execute action on self of type {m_ActionsToExecuteOnListener[i].GetType().Name}" +
                        $" in Channel Listener. Warning from {nameof(ChannelNotifier)}" +
                        $" component in gameObject {gameObject.name}");
                }

            }
        }
    }
}
