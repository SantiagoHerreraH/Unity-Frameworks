using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.LookUpTree
{
    [Serializable]
    public class LookUpQueueData
    {
        internal sealed class RuntimeState
        {
            public readonly List<LookUpAction> QueuedActions;
            public readonly int CurrentQueueIndex;
            public readonly Dictionary<LookUpAction, LookUpAction.QueueRuntimeState> ActionStates;

            public RuntimeState(
                List<LookUpAction> queuedActions,
                int currentQueueIndex,
                Dictionary<LookUpAction, LookUpAction.QueueRuntimeState> actionStates)
            {
                QueuedActions = queuedActions;
                CurrentQueueIndex = currentQueueIndex;
                ActionStates = actionStates;
            }
        }

        [Title("Settings")]
        [SerializeField]
        private QueueOrder m_QueueOrder;

        [SerializeField]
        private HowToRecalculateQueueOrder m_HowToRecalculateWhenAddingANewAction =
            HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;

        [SerializeField, Tooltip("Refreshing means adding the action to the queue again after execution.")]
        private HowToRecalculateQueueOrder m_HowToRecalculateWhenRefreshingAnAction =
            HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;

        [SerializeField]
        private bool m_RecalculateOrderBeforeExecuting;

        [SerializeField, ShowIf(nameof(m_RecalculateOrderBeforeExecuting))]
        private HowToRecalculateQueueOrder m_HowToRecalculateOrderBeforeExecuting =
            HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;

        [SerializeField, Range(1000, 2000000000),
         Tooltip("The larger the value, the less often the running queue index is normalized.")]
        private int m_MaxQueueIndexBeforeResetting = 2000000000;

        [Title("Events")]
        [SerializeField]
        private UnityEvent m_OnBeforeExecute;

        [SerializeField]
        private UnityEvent m_OnAfterExecute;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private List<LookUpAction> m_QueuedActions = new();

        private int m_CurrentQueueIndex;

        public int Count => m_QueuedActions == null ? 0 : m_QueuedActions.Count;

        public LookUpQueueData()
        {
        }

        public LookUpQueueData(LookUpQueueData other)
        {
            if (other == null)
            {
                m_QueueOrder = QueueOrder.HighestPriorityFirst;
                m_HowToRecalculateWhenAddingANewAction =
                    HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
                m_HowToRecalculateWhenRefreshingAnAction =
                    HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
                m_RecalculateOrderBeforeExecuting = true;
                m_HowToRecalculateOrderBeforeExecuting =
                    HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder;
                m_MaxQueueIndexBeforeResetting = 2000000000;
                m_QueuedActions = new List<LookUpAction>();
                return;
            }

            m_QueueOrder = other.m_QueueOrder;
            m_HowToRecalculateWhenAddingANewAction =
                other.m_HowToRecalculateWhenAddingANewAction;
            m_HowToRecalculateWhenRefreshingAnAction =
                other.m_HowToRecalculateWhenRefreshingAnAction;
            m_RecalculateOrderBeforeExecuting = other.m_RecalculateOrderBeforeExecuting;
            m_HowToRecalculateOrderBeforeExecuting =
                other.m_HowToRecalculateOrderBeforeExecuting;
            m_MaxQueueIndexBeforeResetting = other.m_MaxQueueIndexBeforeResetting;

            // Runtime queue events must be independent. Sharing the template's
            // UnityEvent instances would make a subscription to one generated queue
            // fire for every other generated queue as well.
            m_OnBeforeExecute = new UnityEvent();
            m_OnAfterExecute = new UnityEvent();

            m_CurrentQueueIndex = 0;
            m_QueuedActions = new List<LookUpAction>();
        }

        public void AddQueuedActionData(LookUpAction queuedAction)
        {
            if (queuedAction == null)
            {
                Debug.LogError($"{nameof(LookUpQueueData)} cannot add a null action.");
                return;
            }

            m_QueuedActions ??= new List<LookUpAction>();

            if (m_QueuedActions.Contains(queuedAction))
            {
                return;
            }

            queuedAction.SetCurrentQueueIndex(m_CurrentQueueIndex);
            queuedAction.CalculatePriority();
            m_QueuedActions.Add(queuedAction);

            RecalculateAccordingTo(
                m_HowToRecalculateWhenAddingANewAction,
                recalculateNone: false);
        }

        public void RemoveQueuedActionData(LookUpAction queuedAction)
        {
            if (queuedAction == null || m_QueuedActions == null)
            {
                return;
            }

            m_QueuedActions.Remove(queuedAction);
        }

        public void ClearQueuedActionData()
        {
            m_QueuedActions?.Clear();
            m_CurrentQueueIndex = 0;
        }

        public IReadOnlyList<LookUpAction> CreateOrderedSnapshot()
        {
            RemoveInvalidActions();
            return new List<LookUpAction>(m_QueuedActions);
        }

        /// <summary>
        /// Returns the order that the queue would use for its next execution in the
        /// current simulated state. The real list is left untouched.
        /// </summary>
        internal IReadOnlyList<LookUpAction> CreateSimulationOrderedSnapshot()
        {
            RemoveInvalidActions();

            List<LookUpAction> snapshot =
                new List<LookUpAction>(m_QueuedActions);

            if (!m_RecalculateOrderBeforeExecuting)
            {
                return snapshot;
            }

            switch (m_HowToRecalculateOrderBeforeExecuting)
            {
                case HowToRecalculateQueueOrder.RecalculateOrderWithCurrentPriorities:
                    snapshot.Sort(CompareQueuedActions);
                    break;

                case HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder:
                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        snapshot[i]?.CalculatePriority();
                    }

                    snapshot.Sort(CompareQueuedActions);
                    break;
            }

            return snapshot;
        }

        internal RuntimeState CaptureRuntimeState()
        {
            RemoveInvalidActions();

            List<LookUpAction> queuedActions = new List<LookUpAction>(m_QueuedActions);
            Dictionary<LookUpAction, LookUpAction.QueueRuntimeState> actionStates =
                new Dictionary<LookUpAction, LookUpAction.QueueRuntimeState>();

            for (int i = 0; i < queuedActions.Count; i++)
            {
                LookUpAction action = queuedActions[i];

                if (action == null || actionStates.ContainsKey(action))
                {
                    continue;
                }

                actionStates.Add(action, action.CaptureQueueRuntimeState());
            }

            return new RuntimeState(
                queuedActions,
                m_CurrentQueueIndex,
                actionStates);
        }

        internal void RestoreRuntimeState(RuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            m_CurrentQueueIndex = state.CurrentQueueIndex;
            m_QueuedActions = new List<LookUpAction>(state.QueuedActions);

            foreach (KeyValuePair<LookUpAction, LookUpAction.QueueRuntimeState> pair
                     in state.ActionStates)
            {
                pair.Key?.RestoreQueueRuntimeState(pair.Value);
            }
        }

        /// <summary>
        /// Simulates the queue work performed before executing a queued action.
        /// Events are intentionally not invoked during tree search.
        /// </summary>
        internal bool BeginSimulatedExecution(LookUpAction action)
        {
            if (action == null)
            {
                return false;
            }

            RemoveInvalidActions();
            m_QueuedActions.Remove(action);

            bool shouldQueueAgain = action.ShouldQueueAgainAfterExecution();
            AdvanceQueueIndex();

            return shouldQueueAgain;
        }

        /// <summary>
        /// Simulates the refresh work performed after a queued action has executed.
        /// </summary>
        internal void CompleteSimulatedExecution(
            LookUpAction action,
            bool shouldQueueAgain)
        {
            if (!shouldQueueAgain || action == null)
            {
                return;
            }

            action.SetCurrentQueueIndex(m_CurrentQueueIndex);
            action.CalculatePriority();
            m_QueuedActions.Add(action);

            RecalculateAccordingTo(
                m_HowToRecalculateWhenRefreshingAnAction,
                recalculateNone: false);
        }

        public void RecalculateQueueOrderBasedOnCurrentPriority()
        {
            RemoveInvalidActions();
            m_QueuedActions.Sort(CompareQueuedActions);
        }

        public void RecalculateQueueOrderBasedOnRecalculatingPriority()
        {
            RemoveInvalidActions();
            m_QueuedActions.Sort(CompareRecalculatedQueuedActions);
        }

        public void ExecuteAndPop()
        {
            RemoveInvalidActions();

            if (m_QueuedActions.Count == 0)
            {
                return;
            }

            if (m_RecalculateOrderBeforeExecuting)
            {
                RecalculateAccordingTo(
                    m_HowToRecalculateOrderBeforeExecuting,
                    recalculateNone: false);
            }

            if (m_QueuedActions.Count == 0)
            {
                return;
            }

            // The snapshot must be captured before the current item is removed. The
            // lookup tree starts at this item and then traverses the following items.
            IReadOnlyList<LookUpAction> queueSnapshot = CreateOrderedSnapshot();
            LookUpAction queuedAction = m_QueuedActions[0];

            // Always remove first. If it must refresh, it is added back after execution
            // with a new queue index and a priority calculated from the resulting state.
            m_QueuedActions.RemoveAt(0);
            bool shouldQueueAgain = queuedAction.ShouldQueueAgainAfterExecution();

            AdvanceQueueIndex();
            m_OnBeforeExecute?.Invoke();

            try
            {
                queuedAction.Execute(queueSnapshot, shouldQueueAgain);
            }
            finally
            {
                m_OnAfterExecute?.Invoke();

                if (shouldQueueAgain && queuedAction != null)
                {
                    queuedAction.SetCurrentQueueIndex(m_CurrentQueueIndex);
                    queuedAction.CalculatePriority();
                    m_QueuedActions.Add(queuedAction);

                    RecalculateAccordingTo(
                        m_HowToRecalculateWhenRefreshingAnAction,
                        recalculateNone: false);
                }
            }
        }

        public void SubscribeOnBeforeExecute(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnBeforeExecute ??= new UnityEvent();
            m_OnBeforeExecute.AddListener(unityAction);
        }

        public void SubscribeOnAfterExecute(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnAfterExecute ??= new UnityEvent();
            m_OnAfterExecute.AddListener(unityAction);
        }

        public void UnsubscribeOnBeforeExecute(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnBeforeExecute?.RemoveListener(unityAction);
        }

        public void UnsubscribeOnAfterExecute(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnAfterExecute?.RemoveListener(unityAction);
        }

        private void AdvanceQueueIndex()
        {
            if (m_CurrentQueueIndex >= m_MaxQueueIndexBeforeResetting)
            {
                NormalizeQueueIndices();
            }

            m_CurrentQueueIndex++;
        }

        private void NormalizeQueueIndices()
        {
            int offset = -m_CurrentQueueIndex;

            for (int i = 0; i < m_QueuedActions.Count; i++)
            {
                m_QueuedActions[i]?.OffsetQueueIndex(offset);
            }

            m_CurrentQueueIndex = 0;
        }

        private void RemoveInvalidActions()
        {
            m_QueuedActions ??= new List<LookUpAction>();
            m_QueuedActions.RemoveAll(action => action == null);
        }

        private void RecalculateAccordingTo(
            HowToRecalculateQueueOrder recalculationMode,
            bool recalculateNone)
        {
            switch (recalculationMode)
            {
                case HowToRecalculateQueueOrder.RecalculateOrderWithCurrentPriorities:
                    RecalculateQueueOrderBasedOnCurrentPriority();
                    break;

                case HowToRecalculateQueueOrder.RecalculatePrioritiesAndOrder:
                    RecalculateQueueOrderBasedOnRecalculatingPriority();
                    break;

                default:
                    if (recalculateNone)
                    {
                        RecalculateQueueOrderBasedOnCurrentPriority();
                    }
                    break;
            }
        }

        private int CompareQueuedActions(LookUpAction a, LookUpAction b)
        {
            float priorityA = a == null ? 0f : a.GetPriority();
            float priorityB = b == null ? 0f : b.GetPriority();
            int comparison = priorityA.CompareTo(priorityB);

            return m_QueueOrder == QueueOrder.LowestPriorityFirst
                ? comparison
                : -comparison;
        }

        private int CompareRecalculatedQueuedActions(LookUpAction a, LookUpAction b)
        {
            float priorityA = a == null ? 0f : a.CalculatePriority();
            float priorityB = b == null ? 0f : b.CalculatePriority();
            int comparison = priorityA.CompareTo(priorityB);

            return m_QueueOrder == QueueOrder.LowestPriorityFirst
                ? comparison
                : -comparison;
        }
    }
}