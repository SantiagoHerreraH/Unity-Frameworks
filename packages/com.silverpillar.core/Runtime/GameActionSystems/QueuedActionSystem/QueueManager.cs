using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    public class QueueManager : SingletonComponent<QueueManager>
    {
        public enum BehaviourOnNoQueueDataDefined
        {
            ReturnError,
            CreateQueueDataBasedOnDefault,
            DoNothingAndReturnMessage,
            DoNothing,
        }

        public enum QueueOrder
        {
            LowestPriorityFirst,
            HighestPriorityFirst
        }

        public enum HowToRecalculateOrder
        {
            RecalculateOrderWithCurrentPriorities,
            RecalculatePrioritiesAndOrder
        }

        [Serializable]
        public class QueueData
        {
            [Title("Settings")]
            [SerializeField]
            private QueueOrder m_QueueOrder;

            [SerializeField]
            private HowToRecalculateOrder m_HowToRecalculateWhenAddingANewAction = HowToRecalculateOrder.RecalculatePrioritiesAndOrder;
            [SerializeField, Tooltip("Refreshing meaning adding it once again to the queue after it got executed.")]
            private HowToRecalculateOrder m_HowToRecalculateWhenRefreshingAnAction = HowToRecalculateOrder.RecalculatePrioritiesAndOrder;
            [SerializeField]
            private bool m_RecalculateOrderBeforeExecuting;
            [SerializeField, ShowIf(nameof(m_RecalculateOrderBeforeExecuting))]
            private HowToRecalculateOrder m_HowToRecalculateOrderBeforeExecuting = HowToRecalculateOrder.RecalculatePrioritiesAndOrder;

            [SerializeField, Range(1000, 2000000000), Tooltip("The larger the value, the less often it will reset")]
            private int m_MaxQueueIndexBeforeResetting = 2000000000;

            private int m_CurrentQueueIndex;

            [Title("Events")]

            [SerializeField]
            private UnityEvent m_OnBeforeExecute;
            [SerializeField]
            private UnityEvent m_OnAfterExecute;


            [Title("Debug")]
            [ShowInInspector, ReadOnly]
            private List<QueuedAction.Data> m_QueuedActions = new();

            public int Count => m_QueuedActions == null ? 0 : m_QueuedActions.Count;

            public QueueData() { }

            public QueueData(QueueData other)
            {
                if (other == null)
                {
                    m_QueueOrder = QueueOrder.HighestPriorityFirst;
                    m_HowToRecalculateWhenAddingANewAction = HowToRecalculateOrder.RecalculatePrioritiesAndOrder;
                    m_RecalculateOrderBeforeExecuting = true;
                    m_HowToRecalculateOrderBeforeExecuting = HowToRecalculateOrder.RecalculatePrioritiesAndOrder;
                    m_CurrentQueueIndex = 0;
                    m_QueuedActions = new List<QueuedAction.Data>();
                    return;
                }

                m_QueueOrder = other.m_QueueOrder;
                m_HowToRecalculateOrderBeforeExecuting = other.m_HowToRecalculateOrderBeforeExecuting;

                // Runtime queue should start empty when created from the default template.
                m_CurrentQueueIndex = 0;
                m_QueuedActions = new List<QueuedAction.Data>();
            }

            public void AddQueuedActionData(QueuedAction.Data queuedData)
            {
                if (queuedData == null)
                {
                    Debug.LogError($"{nameof(QueueData)} cannot add a null queued action.");
                    return;
                }

                if (m_QueuedActions == null)
                {
                    m_QueuedActions = new List<QueuedAction.Data>();
                }

                if (m_QueuedActions.Contains(queuedData))
                {
                    return;
                }

                queuedData.SetCurrentQueueIndex(m_CurrentQueueIndex);
                queuedData.CalculatePriority();

                m_QueuedActions.Add(queuedData);

                switch (m_HowToRecalculateWhenAddingANewAction)
                {
                    case HowToRecalculateOrder.RecalculateOrderWithCurrentPriorities:
                        RecalculateQueueOrderBasedOnCurrentPriority();
                        break;
                    case HowToRecalculateOrder.RecalculatePrioritiesAndOrder:
                        RecalculateQueueOrderBasedOnRecalculatingPriority();
                        break;
                    default:
                        break;
                }

            }

            public void RemoveQueuedActionData(QueuedAction.Data queuedData)
            {
                if (m_QueuedActions == null)
                {
                    return;
                }

                m_QueuedActions.Remove(queuedData);
            }

            public void RecalculateQueueOrderBasedOnCurrentPriority()
            {
                if (m_QueuedActions == null)
                {
                    m_QueuedActions = new List<QueuedAction.Data>();
                    return;
                }

                m_QueuedActions.RemoveAll(data => data == null);

                m_QueuedActions.Sort(CompareQueuedActions);
            }

            public void RecalculateQueueOrderBasedOnRecalculatingPriority()
            {
                if (m_QueuedActions == null)
                {
                    m_QueuedActions = new List<QueuedAction.Data>();
                    return;
                }

                m_QueuedActions.RemoveAll(data => data == null);

                m_QueuedActions.Sort(CompareRecalculatedQueuedActions);
            }

            public void ExecuteAndPop()
            {
                if (m_QueuedActions == null || m_QueuedActions.Count == 0)
                {
                    return;
                }

                if (m_RecalculateOrderBeforeExecuting)
                {
                    switch (m_HowToRecalculateOrderBeforeExecuting)
                    {
                        case HowToRecalculateOrder.RecalculateOrderWithCurrentPriorities:

                            RecalculateQueueOrderBasedOnCurrentPriority();

                            break;
                        case HowToRecalculateOrder.RecalculatePrioritiesAndOrder:

                            RecalculateQueueOrderBasedOnRecalculatingPriority();
                            break;
                        default:
                            break;
                    }
                }
               

                if (m_QueuedActions.Count == 0)
                {
                    return;
                }

                QueuedAction.Data queuedData = m_QueuedActions.First();

                bool shouldQueueAgain = queuedData.ShouldQueueAgainAfterExecution();

                if (!shouldQueueAgain)
                {
                    m_QueuedActions.RemoveAt(0);
                }

                ShiftQueueIfNeeded();
                ++m_CurrentQueueIndex;

                m_OnBeforeExecute?.Invoke();
                queuedData.Execute();
                m_OnAfterExecute?.Invoke();

                if (shouldQueueAgain)
                {
                    queuedData.SetCurrentQueueIndex(m_CurrentQueueIndex);
                    queuedData.CalculatePriority();

                    switch (m_HowToRecalculateWhenRefreshingAnAction)
                    {
                        case HowToRecalculateOrder.RecalculateOrderWithCurrentPriorities:
                            RecalculateQueueOrderBasedOnCurrentPriority();
                            break;
                        case HowToRecalculateOrder.RecalculatePrioritiesAndOrder:
                            RecalculateQueueOrderBasedOnRecalculatingPriority();
                            break;
                        default:
                            break;
                    }

                }
                
            }

            public void SubscribeOnBeforeExecute(UnityAction unityAction)
            {
                m_OnBeforeExecute ??= new();
                m_OnBeforeExecute.AddListener(unityAction);
            }

            public void SubscribeOnAfterExecute(UnityAction unityAction)
            {
                m_OnAfterExecute ??= new();
                m_OnAfterExecute.AddListener(unityAction);
            }

            public void UnsubscribeOnBeforeExecute(UnityAction unityAction)
            {
                m_OnBeforeExecute ??= new();
                m_OnBeforeExecute.RemoveListener(unityAction);
            }

            public void UnsubscribeOnAfterExecute(UnityAction unityAction)
            {
                m_OnAfterExecute ??= new();
                m_OnAfterExecute.RemoveListener(unityAction);
            }

            private void ShiftQueueIfNeeded()
            {
                if (m_CurrentQueueIndex >= m_MaxQueueIndexBeforeResetting)
                {
                    foreach (var queuedAction in m_QueuedActions)
                    {
                        queuedAction.OffsetPriority(-m_CurrentQueueIndex);
                    }

                    m_CurrentQueueIndex = 0;
                }
            }

            private int CompareQueuedActions(QueuedAction.Data a, QueuedAction.Data b)
            {
                float priorityA = a == null ? 0f : a.GetPriority();
                float priorityB = b == null ? 0f : b.GetPriority();

                int comparison = priorityA.CompareTo(priorityB);

                switch (m_QueueOrder)
                {
                    case QueueOrder.LowestPriorityFirst:
                        return comparison;

                    case QueueOrder.HighestPriorityFirst:
                        return -comparison;

                    default:
                        return -comparison;
                }
            }

            private int CompareRecalculatedQueuedActions(QueuedAction.Data a, QueuedAction.Data b)
            {
                float priorityA = a == null ? 0f : a.CalculatePriority();
                float priorityB = b == null ? 0f : b.CalculatePriority();

                int comparison = priorityA.CompareTo(priorityB);

                switch (m_QueueOrder)
                {
                    case QueueOrder.LowestPriorityFirst:
                        return comparison;

                    case QueueOrder.HighestPriorityFirst:
                        return -comparison;

                    default:
                        return -comparison;
                }
            }
        }

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