using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SilverPillar.Core
{
    public class QueuedActionManager : SingletonComponent<QueuedActionManager>
    {
        public enum BehaviourOnNoDataDefined
        {
            ReturnError,
            CreateTimerDataBasedOnDefault,
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

                queuedData.Execute();

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

        [SerializeField]
        private BehaviourOnNoDataDefined m_BehaviourOnNoDataDefined = BehaviourOnNoDataDefined.ReturnError;

        [OdinSerialize, ShowInInspector, ShowIf(nameof(m_BehaviourOnNoDataDefined), BehaviourOnNoDataDefined.CreateTimerDataBasedOnDefault)]
        private QueueData m_DefaultQueueData = new();

        [OdinSerialize, ShowInInspector]
        private Dictionary<QueuedActionChannel, QueueData> m_QueueChannels_To_Data = new();

        private List<QueueData> m_Data = new();
        private bool m_Initialized = false;

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

            if (m_QueueChannels_To_Data == null)
            {
                m_QueueChannels_To_Data = new Dictionary<QueuedActionChannel, QueueData>();
            }

            if (m_Data == null)
            {
                m_Data = new List<QueueData>();
            }

            m_Data.Clear();

            foreach (var item in m_QueueChannels_To_Data)
            {
                if (item.Value != null && !m_Data.Contains(item.Value))
                {
                    m_Data.Add(item.Value);
                }
            }

            m_Initialized = true;
        }

        public void AddQueuedAction(QueuedActionChannel queue, QueuedAction.Data data)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueuedActionManager)} cannot add queued action because the queue channel is null.");
                return;
            }

            if (data == null)
            {
                Debug.LogError($"{nameof(QueuedActionManager)} cannot add a null queued action data.");
                return;
            }

            if (!TryGetOrCreateQueueData(queue, out QueueData queueData))
            {
                return;
            }

            queueData.AddQueuedActionData(data);
        }

        public void RemoveQueuedAction(QueuedActionChannel queue, QueuedAction.Data data)
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

            if (!m_QueueChannels_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                return;
            }

            queueData?.RemoveQueuedActionData(data);
        }

        public void ExecuteAndPop(QueuedActionChannel queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueuedActionManager)} cannot execute queue because the queue channel is null.");
                return;
            }

            if (!m_QueueChannels_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData?.ExecuteAndPop();
        }

        public void RecalculateQueueOrderBasedOnCurrentPriority(QueuedActionChannel queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueuedActionManager)} cannot recalculate queue because the queue channel is null.");
                return;
            }

            if (!m_QueueChannels_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData?.RecalculateQueueOrderBasedOnCurrentPriority();
        }

        public void RecalculateQueueOrderBasedOnRecalculatingPriority(QueuedActionChannel queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(QueuedActionManager)} cannot recalculate queue because the queue channel is null.");
                return;
            }

            if (!m_QueueChannels_To_Data.TryGetValue(queue, out QueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData?.RecalculateQueueOrderBasedOnRecalculatingPriority();
        }

        public bool TryGetQueueData(QueuedActionChannel queue, out QueueData queueData)
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

            return m_QueueChannels_To_Data.TryGetValue(queue, out queueData);
        }

        protected override void OnShutdown()
        {
            m_Data?.Clear();
            m_Initialized = false;
        }

        private bool TryGetOrCreateQueueData(QueuedActionChannel queue, out QueueData queueData)
        {
            queueData = null;

            if (m_QueueChannels_To_Data.TryGetValue(queue, out queueData))
            {
                return true;
            }

            switch (m_BehaviourOnNoDataDefined)
            {
                case BehaviourOnNoDataDefined.ReturnError:
                    Debug.LogError($"{nameof(QueuedActionManager)} has no queue data defined for channel {queue.name}.");
                    return false;

                case BehaviourOnNoDataDefined.CreateTimerDataBasedOnDefault:
                    if (m_DefaultQueueData == null)
                    {
                        Debug.LogError($"{nameof(QueuedActionManager)} cannot create queue data because the default queue data is null.");
                        return false;
                    }

                    queueData = new QueueData(m_DefaultQueueData);
                    m_QueueChannels_To_Data.Add(queue, queueData);

                    if (m_Data != null && !m_Data.Contains(queueData))
                    {
                        m_Data.Add(queueData);
                    }

                    return true;

                case BehaviourOnNoDataDefined.DoNothingAndReturnMessage:
                    Debug.Log($"{nameof(QueuedActionManager)} has no queue data defined for channel {queue.name}.");
                    return false;

                case BehaviourOnNoDataDefined.DoNothing:
                    return false;

                default:
                    return false;
            }
        }

        private void HandleMissingQueueData(QueuedActionChannel queue)
        {
            switch (m_BehaviourOnNoDataDefined)
            {
                case BehaviourOnNoDataDefined.ReturnError:
                    Debug.LogError($"{nameof(QueuedActionManager)} has no queue data defined for channel {queue.name}.");
                    break;

                case BehaviourOnNoDataDefined.CreateTimerDataBasedOnDefault:
                    TryGetOrCreateQueueData(queue, out _);
                    break;

                case BehaviourOnNoDataDefined.DoNothingAndReturnMessage:
                    Debug.Log($"{nameof(QueuedActionManager)} has no queue data defined for channel {queue.name}.");
                    break;

                case BehaviourOnNoDataDefined.DoNothing:
                    break;
            }
        }

        private bool IsValid()
        {
            if (m_QueueChannels_To_Data == null)
            {
                Debug.LogError($"{nameof(QueuedActionManager)} queue dictionary is null.");
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