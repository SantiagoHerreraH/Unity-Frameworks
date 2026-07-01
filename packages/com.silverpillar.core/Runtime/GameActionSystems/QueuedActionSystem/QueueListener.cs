using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class QueueListener : MonoBehaviour
    {
        [Title("Queue Subscription")]
        [SerializeField]
        private WhenToSubscribe m_WhenToSubscribe;
        [SerializeField]
        private WhenToUnsubscribe m_WhenToUnsubscribe;

        [Title("All Queue Events")]
        [SerializeField]
        private UnityEvent m_OnBeforeExecuteAndPop;
        [SerializeField]
        private UnityEvent m_OnAfterExecuteAndPop;

        [Serializable]
        public struct QueueEvents
        {
            [SerializeField]
            private Queue m_Queue;
            public Queue Queue => m_Queue;

            [SerializeField]
            private UnityEvent m_OnBeforeExecute;

            [SerializeField]
            private UnityEvent m_OnAfterExecute;

            public void Subscribe()
            {
                if (m_Queue == null)
                {
                    Debug.LogError("Null queue in queued events in QueueListener component");
                    return;
                }
                QueueManager.Instance.SubscribeOnBeforeExecuteQueue(m_Queue, InvokeOnBeforeExecute);
                QueueManager.Instance.SubscribeOnAfterExecuteQueue(m_Queue, InvokeOnAfterExecute);
            }

            public void Unsubscribe()
            {
                if (m_Queue == null)
                {
                    Debug.LogError("Null queue in queued events in QueueListener component");
                    return;
                }
                QueueManager.Instance.UnsubscribeOnBeforeExecuteQueue(m_Queue, InvokeOnBeforeExecute);
                QueueManager.Instance.UnsubscribeOnAfterExecuteQueue(m_Queue, InvokeOnAfterExecute);
            }

            private void InvokeOnBeforeExecute()
            {
                m_OnBeforeExecute?.Invoke();
            }

            private void InvokeOnAfterExecute()
            {
                m_OnAfterExecute?.Invoke();
            }
        }

        [Title("Specific Queue Events")]
        [SerializeField]
        private List<QueueEvents> m_QueueEvents;

        public enum WhenToSubscribe
        {
            DontAutoSubscribe,
            OnAwake,
            OnStart,
            OnEnable
        }

        public enum WhenToUnsubscribe
        {
            OnDestroy,
            OnDisable,
        }

        private void Awake()
        {
            if (m_WhenToSubscribe == WhenToSubscribe.OnAwake)
            {
                Subscribe();
            }
        }

        private void Start()
        {
            if (m_WhenToSubscribe == WhenToSubscribe.OnStart)
            {
                Subscribe();
            }
        }

        private void OnEnable()
        {
            if (m_WhenToSubscribe == WhenToSubscribe.OnEnable)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            if (m_WhenToUnsubscribe == WhenToUnsubscribe.OnDisable)
            {
                Unsubscribe();
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToUnsubscribe == WhenToUnsubscribe.OnDestroy)
            {
                Unsubscribe();
            }
        }

        public void Subscribe()
        {
            if (m_OnBeforeExecuteAndPop != null)
            {
                QueueManager.Instance.SubscribeOnBeforeExecuteAndPop(InvokeOnBeforeExecuteAndPop);
            }

            if (m_OnAfterExecuteAndPop != null)
            {
                QueueManager.Instance.SubscribeOnAfterExecuteAndPop(InvokeOnAfterExecuteAndPop);
            }

            if (m_QueueEvents != null)
            {
                for (int i = 0; i < m_QueueEvents.Count; i++)
                {
                    m_QueueEvents[i].Subscribe();
                }
            }
        }

        public void Unsubscribe()
        {
            if (m_OnBeforeExecuteAndPop != null)
            {
                QueueManager.Instance.UnsubscribeOnBeforeExecuteAndPop(InvokeOnBeforeExecuteAndPop);
            }

            if (m_OnAfterExecuteAndPop != null)
            {
                QueueManager.Instance.UnsubscribeOnAfterExecuteAndPop(InvokeOnAfterExecuteAndPop);
            }

            if (m_QueueEvents != null)
            {
                for (int i = 0; i < m_QueueEvents.Count; i++)
                {
                    m_QueueEvents[i].Unsubscribe();
                }
            }

        }

        private void InvokeOnBeforeExecuteAndPop()
        {
            m_OnBeforeExecuteAndPop?.Invoke();
        }

        private void InvokeOnAfterExecuteAndPop()
        {
            m_OnAfterExecuteAndPop?.Invoke();
        }
    }
}
