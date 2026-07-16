using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class QueueManager : SingletonComponent<QueueManager>
    {
        [Title("Queue Manager Settings")]

        [SerializeField]
        private BehaviourOnNoQueueDataDefined m_BehaviourOnNoQueueDataDefined = BehaviourOnNoQueueDataDefined.ReturnError;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_BehaviourOnNoQueueDataDefined), BehaviourOnNoQueueDataDefined.CreateQueueDataBasedOnDefault)]
        private QueueData m_DefaultQueueData = new();

        [OdinSerialize, ShowInInspector]
        private Dictionary<Queue, QueueData> m_Queues_To_Data = new();

        private List<QueueData> m_Data = new();
        private bool m_Initialized = false;


        [Title("All Queue Events")]
        [SerializeField]
        private UnityEvent m_OnBeforeExecuteAndPop;
        [SerializeField]
        private UnityEvent m_OnAfterExecuteAndPop;

        protected override void OnAwake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            if (m_Queues_To_Data == null)
            {
                m_Queues_To_Data = new Dictionary<Queue, QueueData>();
            }

            if (m_Data == null)
            {
                m_Data = new List<QueueData>();
            }

            m_Data.Clear();

            foreach (var item in m_Queues_To_Data)
            {
                if (item.Value != null && !m_Data.Contains(item.Value))
                {
                    m_Data.Add(item.Value);
                }
            }

            m_Initialized = true;
        }

        public void AddQueuedAction(Queue queue, QueuedAction.Data data)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueueManager)} cannot add queued action because the queue channel is null.");
                return;
            }

            if (data == null)
            {
                Debug.LogError($"{nameof(QueueManager)} cannot add a null queued action data.");
                return;
            }

            if (!TryGetOrCreateQueueData(queue, out QueueData queueData))
            {
                return;
            }

            queueData.AddQueuedActionData(data);
        }

        public void RemoveQueuedAction(Queue queue, QueuedAction.Data data)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                return;
            }

            if (data == null)
            {
                return;
            }

            if (!m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                return;
            }

            queueData?.RemoveQueuedActionData(data);
        }

        public void ExecuteAndPop(Queue queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueueManager)} cannot execute queue because the queue channel is null.");
                return;
            }

            if (!m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            m_OnBeforeExecuteAndPop?.Invoke();
            queueData?.ExecuteAndPop();
            m_OnAfterExecuteAndPop?.Invoke();
        }

        public void SubscribeOnBeforeExecuteAndPop(UnityAction unityAction)
        {
            m_OnBeforeExecuteAndPop ??= new();
            m_OnBeforeExecuteAndPop.AddListener(unityAction);
        }

        public void SubscribeOnAfterExecuteAndPop(UnityAction unityAction)
        {
            m_OnAfterExecuteAndPop ??= new();
            m_OnAfterExecuteAndPop.AddListener(unityAction);
        }

        public void UnsubscribeOnBeforeExecuteAndPop(UnityAction unityAction)
        {
            m_OnBeforeExecuteAndPop ??= new();
            m_OnBeforeExecuteAndPop.RemoveListener(unityAction);
        }

        public void UnsubscribeOnAfterExecuteAndPop(UnityAction unityAction)
        {
            m_OnAfterExecuteAndPop ??= new();
            m_OnAfterExecuteAndPop.RemoveListener(unityAction);
        }

        public void SubscribeOnBeforeExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                queueData.SubscribeOnBeforeExecute(unityAction);
            }
        }

        public void SubscribeOnAfterExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                queueData.SubscribeOnAfterExecute(unityAction);
            }
        }

        public void UnsubscribeOnBeforeExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                queueData.UnsubscribeOnBeforeExecute(unityAction);
            }
        }

        public void UnsubscribeOnAfterExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                queueData.UnsubscribeOnAfterExecute(unityAction);
            }
        }

        public void RecalculateQueueOrderBasedOnCurrentPriority(Queue queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueueManager)} cannot recalculate queue because the queue channel is null.");
                return;
            }

            if (!m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData?.RecalculateQueueOrderBasedOnCurrentPriority();
        }

        public void RecalculateQueueOrderBasedOnRecalculatingPriority(Queue queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueueManager)} cannot recalculate queue because the queue channel is null.");
                return;
            }

            if (!m_Queues_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData?.RecalculateQueueOrderBasedOnRecalculatingPriority();
        }

        public bool TryGetQueueData(Queue queue, out QueueData queueData)
        {
            queueData = null;

            if (!IsValid())
            {
                return false;
            }

            if (queue == null)
            {
                return false;
            }

            return m_Queues_To_Data.TryGetValue(queue, out queueData);
        }

        protected override void OnShutdown()
        {
            m_Data?.Clear();
            m_Initialized = false;
        }

        private bool TryGetOrCreateQueueData(Queue queue, out QueueData queueData)
        {
            queueData = null;

            if (m_Queues_To_Data.TryGetValue(queue, out queueData))
            {
                return true;
            }

            switch (m_BehaviourOnNoQueueDataDefined)
            {
                case BehaviourOnNoQueueDataDefined.ReturnError:
                    Debug.LogError($"{nameof(QueueManager)} has no queue action data defined for queue {queue.name}.");
                    return false;

                case BehaviourOnNoQueueDataDefined.CreateQueueDataBasedOnDefault:
                    if (m_DefaultQueueData == null)
                    {
                        Debug.LogError($"{nameof(QueueManager)} cannot create queue action data because the default queue data is null.");
                        return false;
                    }

                    queueData = new QueueData(m_DefaultQueueData);
                    m_Queues_To_Data.Add(queue, queueData);

                    if (m_Data != null && !m_Data.Contains(queueData))
                    {
                        m_Data.Add(queueData);
                    }

                    return true;

                case BehaviourOnNoQueueDataDefined.DoNothingAndReturnMessage:
                    Debug.Log($"{nameof(QueueManager)} has no queue action data defined for channel {queue.name}.");
                    return false;

                case BehaviourOnNoQueueDataDefined.DoNothing:
                    return false;

                default:
                    return false;
            }
        }

        private void HandleMissingQueueData(Queue queue)
        {
            switch (m_BehaviourOnNoQueueDataDefined)
            {
                case BehaviourOnNoQueueDataDefined.ReturnError:
                    Debug.LogError($"{nameof(QueueManager)} has no queue data defined for channel {queue.name}.");
                    break;

                case BehaviourOnNoQueueDataDefined.CreateQueueDataBasedOnDefault:
                    TryGetOrCreateQueueData(queue, out _);
                    break;

                case BehaviourOnNoQueueDataDefined.DoNothingAndReturnMessage:
                    Debug.Log($"{nameof(QueueManager)} has no queue data defined for channel {queue.name}.");
                    break;

                case BehaviourOnNoQueueDataDefined.DoNothing:
                    break;
            }
        }

        private bool IsValid()
        {
            if (m_Queues_To_Data == null)
            {
                Debug.LogError($"{nameof(QueueManager)} queue dictionary is null.");
                return false;
            }

            if (!m_Initialized)
            {
                Initialize();
            }

            return true;
        }
    }
}