using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using SilverPillar.Core;
using Sirenix.Serialization;

namespace SilverPillar.Notifier
{
    public class ChannelNotifier : SerializedMonoBehaviour
    {
        [Title("Locking")]
        [SerializeField, Tooltip("If it is locked, data sending won't work even if you call it")]
        private bool m_IsLocked = false;

        [Title("Channels")]
        [SerializeField]
        private bool m_NotifyOnAllChannels;
        [SerializeField]
        private List<Channel> m_ChannelsToNotifyIn;
        public List<Channel> ChannelsToNotifyIn { get { return m_ChannelsToNotifyIn; } }

        [Title("Notify Filter")]
        [OdinSerialize, ShowInInspector, Tooltip("If left null, there won't be any filter")]
        private IInteractionCondition m_NotifyIf;

        [Title("Events")]
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

        [Title("Actions")]
        [SerializeField]
        private bool m_DebugWarningsIfActionsDidNotExecute;
        [SerializeField]
        private WhenToTriggerActionsOnSelf m_WhenToTriggerActionsOnSelf;
        [SerializeField]
        private WhatToTriggerFirstOnNotify m_WhatToTriggerFirstOnNotify;

        [Title("Actions On Self")]
        [Button("Check Actions On Self")]
        private void CheckActionsOnSelfButton()
        {
            CheckActionsOnSelf(true);
        }

        [OdinSerialize, ShowInInspector, Tooltip("These actions are executed on listen")]
        private List<ICachedGameAction> m_ActionsToExecuteOnSelf = new();
        private bool m_InitializedSelfActions;

        [Title("Actions On Listener")]

        [OdinSerialize, ShowInInspector, Tooltip("These actions are executed on notify to.")]
        private List<ICachedGameAction> m_ActionsToExecuteOnListener = new();

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

            if (m_NotifyIf != null && m_NotifyIf.GetGameObject() == null)
            {
                m_NotifyIf.SetGameObject(gameObject);
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

        public void AddActionsToExecuteOnListener(List<ICachedGameAction> actions)
        {
            if (actions == null)
            {
                return;
            }

            if (m_ActionsToExecuteOnListener == null)
            {
                m_ActionsToExecuteOnListener = new();
            }

            foreach (var action in actions)
            {
                m_ActionsToExecuteOnListener.Add(action.Clone());
            }

        }

        public void AddActionsToExecuteOnSelf(List<ICachedGameAction> actions)
        {
            if (actions == null)
            {
                return;
            }

            if (m_ActionsToExecuteOnSelf == null)
            {
                m_ActionsToExecuteOnSelf = new();
            }

            foreach (var action in actions)
            {
                m_ActionsToExecuteOnSelf.Add(action.Clone());
            }
        }

        private void NotifyListeners()
        {
            foreach (var listener in m_PossibleChannelListeners)
            {
                if (m_NotifyIf == null || m_NotifyIf.IsFulfilled(listener.gameObject))
                {
                    NotifyListener(listener);
                }
            }
        }

        private void TriggerSelfActions()
        {
            if (m_ActionsToExecuteOnSelf == null)
            {
                return;
            }

            if (!m_InitializedSelfActions)
            {
                foreach (var item in m_ActionsToExecuteOnSelf)
                {
                    item.SetGameObject(gameObject);
                }

                m_InitializedSelfActions = true;
            }

            for (int i = 0; i < m_ActionsToExecuteOnSelf.Count; i++)
            {
                m_ActionsToExecuteOnSelf[i].Execute();
            }
        }

        private void TriggerListenerActions(ChannelListener listener)
        {
            for (int i = 0; i < m_ActionsToExecuteOnListener.Count; i++)
            {
                if (m_ActionsToExecuteOnListener[i].SetGameObject(listener.gameObject))
                {
                    m_ActionsToExecuteOnListener[i].Execute();
                }
                else if (m_DebugWarningsIfActionsDidNotExecute)
                {
                    Debug.LogWarning(
                        $"Could not execute action of type {m_ActionsToExecuteOnListener[i].GetType().Name}" +
                        $" in Channel Notifier. Warning from {nameof(ChannelNotifier)}" +
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
                        $"Could not execute action on self of type {m_ActionsToExecuteOnListener[i].GetType().Name}" +
                        $" in Channel Listener. Warning from {nameof(ChannelNotifier)}" +
                        $" component in gameObject {gameObject.name}");
                }

            }
        }
    }
}
