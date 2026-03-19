using SilverPillar.Core;
using UnityEngine;

namespace SilverPillar.Notifier
{
    using System;
    using UnityEngine;

    namespace SilverPillar.Notifier
    {
        [Serializable]
        public class ChannelNotify_Action : IAction
        {
            public enum NotifyFunction
            {
                Notify,
                LockLocal,
                UnlockLocal,
                CallNotifyOnAllLocal,
                RefreshPossibleListeners
            }

            public enum TriggerMoment
            {
                Start,
                Update,
                End
            }

            [SerializeField]
            private NotifyFunction m_FunctionToCall;

            [SerializeField]
            private TriggerMoment m_WhenToCall = TriggerMoment.Start;

            private ChannelNotifier m_TargetNotifier;

            public IAction Clone()
            {
                return new ChannelNotify_Action
                {
                    m_FunctionToCall = m_FunctionToCall,
                    m_WhenToCall = m_WhenToCall,
                    m_TargetNotifier = m_TargetNotifier,
                };
            }

            public void StartAction()
            {
                if (m_WhenToCall == TriggerMoment.Start)
                {
                    ExecuteSelectedFunction();
                }
            }

            public void UpdateAction()
            {
                if (m_WhenToCall == TriggerMoment.Update)
                {
                    ExecuteSelectedFunction();
                }
            }

            public void EndAction()
            {
                if (m_WhenToCall == TriggerMoment.End)
                {
                    ExecuteSelectedFunction();
                }
            }

            public GameObject GetGameObject()
            {
                return m_TargetNotifier ? m_TargetNotifier.gameObject : null;
            }

            public bool SetGameObject(GameObject gameObj)
            {

                m_TargetNotifier = gameObj?.GetComponent<ChannelNotifier>();

                if (m_TargetNotifier == null)
                {
                    Debug.LogWarning(
                        $"Could not execute {nameof(ChannelNotify_Action)} because no {nameof(ChannelNotifier)} was found.");
                }

                return m_TargetNotifier != null;
            }

            private void ExecuteSelectedFunction()
            {
                if (m_TargetNotifier == null)
                {
                    Debug.LogWarning(
                        $"Could not execute {nameof(ChannelNotify_Action)} because no {nameof(ChannelNotifier)} was found.");
                    return;
                }

                switch (m_FunctionToCall)
                {
                    case NotifyFunction.Notify:
                        m_TargetNotifier.Notify();
                        break;

                    case NotifyFunction.RefreshPossibleListeners:
                        m_TargetNotifier.RefreshPossibleListeners();
                        break;

                    case NotifyFunction.LockLocal:
                        m_TargetNotifier.LockLocal();
                        break;

                    case NotifyFunction.UnlockLocal:
                        m_TargetNotifier.UnlockLocal();
                        break;

                    case NotifyFunction.CallNotifyOnAllLocal:
                        m_TargetNotifier.CallNotifyOnAllLocal();
                        break;

                    default:
                        Debug.LogWarning($"Unhandled {nameof(NotifyFunction)}: {m_FunctionToCall}");
                        break;
                }
            }

        }
    }
}
