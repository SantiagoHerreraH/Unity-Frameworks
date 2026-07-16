using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace SilverPillar.Core
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

    public enum HowToRecalculateQueueOrder
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
        private HowToRecalculateQueueOrder m_HowToRecalculateWhenAddingANewAction = HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
        [SerializeField, Tooltip("Refreshing means adding it once again to the queue after it got executed.")]
        private HowToRecalculateQueueOrder m_HowToRecalculateWhenRefreshingAnAction = HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
        [SerializeField]
        private bool m_RecalculateOrderBeforeExecuting;
        [SerializeField, ShowIf(nameof(m_RecalculateOrderBeforeExecuting))]
        private HowToRecalculateQueueOrder m_HowToRecalculateOrderBeforeExecuting = HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;

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
                m_HowToRecalculateWhenAddingANewAction = HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
                m_RecalculateOrderBeforeExecuting = true;
                m_HowToRecalculateOrderBeforeExecuting = HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
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
                case HowToRecalculateQueueOrder.RecalculateOrderWithCurrentPriorities:
                    RecalculateQueueOrderBasedOnCurrentPriority();
                    break;
                case HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder:
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

        public void ClearQueuedActionData()
        {
            m_QueuedActions?.Clear();
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
                    case HowToRecalculateQueueOrder.RecalculateOrderWithCurrentPriorities:

                        RecalculateQueueOrderBasedOnCurrentPriority();

                        break;
                    case HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder:

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
                    case HowToRecalculateQueueOrder.RecalculateOrderWithCurrentPriorities:
                        RecalculateQueueOrderBasedOnCurrentPriority();
                        break;
                    case HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder:
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
}
