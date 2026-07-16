using NUnit.Framework;
using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.LookUpTree
{
    public class LookUpTreeManager : SingletonComponent<LookUpTreeManager>
    {
        [Title("Look Up Tree Manager Settings")]
        [SerializeField]
        private BehaviourOnNoQueueDataDefined m_BehaviourOnNoQueueDataDefined =
            BehaviourOnNoQueueDataDefined.ReturnError;

        [OdinSerialize, ShowInInspector,
         ShowIf(nameof(m_BehaviourOnNoQueueDataDefined),
             BehaviourOnNoQueueDataDefined.CreateQueueDataBasedOnDefault)]
        private LookUpQueueData m_DefaultQueueData = new();

        [OdinSerialize, ShowInInspector]
        private Dictionary<Queue, LookUpQueueData> m_Queues_To_Data = new();

        [Title("All Queue Events")]
        [SerializeField]
        private UnityEvent m_OnBeforeExecuteAndPop;

        [SerializeField]
        private UnityEvent m_OnAfterExecuteAndPop;

        private readonly List<LookUpQueueData> m_RuntimeQueueData = new();
        private bool m_Initialized;

        private readonly struct ScoredCandidate
        {
            public readonly int ActionIndex;
            public readonly float RawScore;
            public readonly float UtilityScore;//A normalized version where a higher value is always considered better.

            public ScoredCandidate(int actionIndex, float rawScore, float utilityScore)
            {
                ActionIndex = actionIndex;
                RawScore = rawScore;
                UtilityScore = utilityScore;
            }
        }

        private struct PathStatistics
        {
            public long Count;
            public double Sum;
            public float Best;

            public bool HasPaths => Count > 0;

            //A terminal path means the tree has reached its maximum depth or has no more actions to explore.
            public static PathStatistics Terminal()
            {
                return new PathStatistics
                {
                    Count = 1,
                    Sum = 0d,
                    Best = 0f
                };
            }

            public PathStatistics AddConstant(float utility)
            {
                if (!HasPaths)
                {
                    return this;
                }

                return new PathStatistics
                {
                    Count = Count,
                    Sum = Sum + utility * Count,
                    Best = Best + utility
                };
            }

            public void Merge(PathStatistics other)
            {
                if (!other.HasPaths)
                {
                    return;
                }

                if (!HasPaths)
                {
                    this = other;
                    return;
                }

                Count += other.Count;
                Sum += other.Sum;
                Best = Mathf.Max(Best, other.Best);
            }
        }

        private enum QueueBranchMode
        {
            PreserveQueue,
            CompleteAlreadyPoppedRoot,
            SimulateFullQueueExecution
        }

        private readonly struct QueueBranchState
        {
            public readonly LookUpQueueData.RuntimeState QueueState;
            public readonly LookUpAction.QueueRuntimeState ActionState;
            public readonly bool ShouldQueueAgain;

            public QueueBranchState(
                LookUpQueueData.RuntimeState queueState,
                LookUpAction.QueueRuntimeState actionState,
                bool shouldQueueAgain)
            {
                QueueState = queueState;
                ActionState = actionState;
                ShouldQueueAgain = shouldQueueAgain;
            }
        }


        protected override void OnAwake()
        {
            Initialize();
        }

        public void AddQueuedAction(Queue queue, LookUpAction action)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(LookUpTreeManager)} cannot add an action because the queue is null.");
                return;
            }

            if (action == null)
            {
                Debug.LogError($"{nameof(LookUpTreeManager)} cannot add a null action.");
                return;
            }

            if (!TryGetOrCreateQueueData(queue, out LookUpQueueData queueData))
            {
                return;
            }

            queueData.AddQueuedActionData(action);
        }

        public void RemoveQueuedAction(Queue queue, LookUpAction action)
        {
            if (!IsValid() || queue == null || action == null)
            {
                return;
            }

            if (m_Queues_To_Data.TryGetValue(queue, out LookUpQueueData queueData))
            {
                queueData?.RemoveQueuedActionData(action);
            }
        }

        public void ExecuteAndPop(Queue queue)
        {
            if (!IsValid())
            {
                return;
            }

            if (queue == null)
            {
                Debug.LogError($"{nameof(LookUpTreeManager)} cannot execute a null queue.");
                return;
            }

            if (!TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            m_OnBeforeExecuteAndPop?.Invoke();

            try
            {
                queueData.ExecuteAndPop();
            }
            finally
            {
                m_OnAfterExecuteAndPop?.Invoke();
            }
        }

        public void SubscribeOnBeforeExecuteAndPop(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnBeforeExecuteAndPop ??= new UnityEvent();
            m_OnBeforeExecuteAndPop.AddListener(unityAction);
        }

        public void SubscribeOnAfterExecuteAndPop(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnAfterExecuteAndPop ??= new UnityEvent();
            m_OnAfterExecuteAndPop.AddListener(unityAction);
        }

        public void UnsubscribeOnBeforeExecuteAndPop(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnBeforeExecuteAndPop?.RemoveListener(unityAction);
        }

        public void UnsubscribeOnAfterExecuteAndPop(UnityAction unityAction)
        {
            if (unityAction == null)
            {
                return;
            }

            m_OnAfterExecuteAndPop?.RemoveListener(unityAction);
        }

        public void SubscribeOnBeforeExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (unityAction != null && TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                queueData.SubscribeOnBeforeExecute(unityAction);
            }
        }

        public void SubscribeOnAfterExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (unityAction != null && TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                queueData.SubscribeOnAfterExecute(unityAction);
            }
        }

        public void UnsubscribeOnBeforeExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (unityAction != null && TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                queueData.UnsubscribeOnBeforeExecute(unityAction);
            }
        }

        public void UnsubscribeOnAfterExecuteQueue(Queue queue, UnityAction unityAction)
        {
            if (unityAction != null && TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                queueData.UnsubscribeOnAfterExecute(unityAction);
            }
        }

        public void RecalculateQueueOrderBasedOnCurrentPriority(Queue queue)
        {
            if (!IsValid() || queue == null)
            {
                return;
            }

            if (!TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData.RecalculateQueueOrderBasedOnCurrentPriority();
        }

        public void RecalculateQueueOrderBasedOnRecalculatingPriority(Queue queue)
        {
            if (!IsValid() || queue == null)
            {
                return;
            }

            if (!TryGetExistingQueueData(queue, out LookUpQueueData queueData))
            {
                HandleMissingQueueData(queue);
                return;
            }

            queueData.RecalculateQueueOrderBasedOnRecalculatingPriority();
        }

        public bool TryGetQueueData(Queue queue, out LookUpQueueData queueData)
        {
            queueData = null;

            return IsValid() &&
                   queue != null &&
                   TryGetExistingQueueData(queue, out queueData);
        }

        internal bool TryGetQueueSnapshot(
            Queue queue,
            out IReadOnlyList<LookUpAction> queueSnapshot)
        {
            queueSnapshot = null;

            if (!TryGetQueueData(queue, out LookUpQueueData queueData))
            {
                return false;
            }

            queueSnapshot = queueData.CreateOrderedSnapshot();
            return queueSnapshot.Count > 0;
        }

        internal bool TryChooseActionFromLookUpTree(
            LookUpAction rootAction,
            IReadOnlyList<LookUpAction> queueSnapshot,
            bool rootWillQueueAgainAfterExecution,
            out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            if (rootAction == null)
            {
                return false;
            }

            TryGetQueueData(rootAction.Queue, out LookUpQueueData queueData);

            switch (rootAction.GetLookUpTreeSearchProtocol())
            {
                case LookUpAction.LookUpTreeSearchProtocol.SimulateTreeWithoutCopyingCurrentQueueState:
                    if (queueData != null &&
                        TryChooseActionUsingDynamicQueue(
                            rootAction,
                            queueData,
                            rootWillQueueAgainAfterExecution,
                            out chosenActionIndex))
                    {
                        return true;
                    }

                    // A directly executed action may not belong to a registered queue.
                    // In that case, use the frozen snapshot as a safe fallback.
                    return TryChooseActionUsingCopiedQueueState(
                        rootAction,
                        queueSnapshot,
                        queueData,
                        out chosenActionIndex);

                case LookUpAction.LookUpTreeSearchProtocol.CopyCurrentQueueStateAndThenSimulate:
                default:
                    return TryChooseActionUsingCopiedQueueState(
                        rootAction,
                        queueSnapshot,
                        queueData,
                        out chosenActionIndex);
            }
        }

        private bool TryChooseActionUsingCopiedQueueState(
            LookUpAction rootAction,
            IReadOnlyList<LookUpAction> queueSnapshot,
            LookUpQueueData queueData,
            out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            List<LookUpAction> planningActions =
                BuildPlanningActionList(rootAction, queueSnapshot);

            if (planningActions.Count == 0)
            {
                return false;
            }

            int requestedDepth = rootAction.CalculateLookUpDepth();
            int actualDepth = Mathf.Min(requestedDepth, planningActions.Count);

            if (actualDepth <= 0)
            {
                return false;
            }

            for (int i = 0; i < actualDepth; i++)
            {
                planningActions[i].PrepareForSimulation(actualDepth);
            }

            return TryChooseRootCandidate(
                rootAction,
                queueData,
                QueueBranchMode.PreserveQueue,
                false,
                () => EvaluateCopiedQueuePaths(
                    rootAction,
                    planningActions,
                    queueData,
                    1,
                    actualDepth),
                out chosenActionIndex);
        }

        private bool TryChooseActionUsingDynamicQueue(
            LookUpAction rootAction,
            LookUpQueueData queueData,
            bool rootWillQueueAgainAfterExecution,
            out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            if (queueData == null)
            {
                return false;
            }

            int requestedDepth = rootAction.CalculateLookUpDepth();

            if (requestedDepth <= 0)
            {
                return false;
            }

            rootAction.PrepareForSimulation(requestedDepth);

            return TryChooseRootCandidate(
                rootAction,
                queueData,
                QueueBranchMode.CompleteAlreadyPoppedRoot,
                rootWillQueueAgainAfterExecution,
                () => EvaluateDynamicQueuePaths(
                    rootAction,
                    queueData,
                    1,
                    requestedDepth),
                out chosenActionIndex);
        }

        private bool TryChooseRootCandidate(
            LookUpAction rootAction,
            LookUpQueueData queueData,
            QueueBranchMode queueBranchMode,
            bool rootWillQueueAgainAfterExecution,
            System.Func<PathStatistics> evaluateSuffix,
            out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            LookUpAction.ActionChoosingCriteria rootCriteria =
                rootAction.GetCriteriaForLookUpNode(rootAction, true);

            List<ScoredCandidate> rootCandidates = CollectScoredCandidates(
                rootAction,
                rootCriteria,
                rootAction.CalculateNumberOfPathsToChoosePerNode(),
                queueData,
                queueBranchMode,
                rootWillQueueAgainAfterExecution);

            if (rootCandidates.Count == 0)
            {
                return false;
            }

            bool foundCandidate = false;
            double bestAggregateScore = 0d;

            for (int i = 0; i < rootCandidates.Count; i++)
            {
                ScoredCandidate candidate = rootCandidates[i];

                QueueBranchState branchState = BeginQueueBranch(
                    queueData,
                    rootAction,
                    queueBranchMode,
                    rootWillQueueAgainAfterExecution);

                if (!rootAction.BeginSimulation(candidate.ActionIndex))
                {
                    RestoreQueueBranch(queueData, rootAction, branchState);
                    continue;
                }

                try
                {
                    CompleteQueueBranchBeforeEvaluation(
                        queueData,
                        rootAction,
                        queueBranchMode,
                        branchState);

                    if (!rootAction.TryEvaluateCurrentState(
                            rootCriteria,
                            out float rawScore))
                    {
                        continue;
                    }

                    float utility = LookUpAction.ConvertRawScoreToUtility(
                        rawScore,
                        rootCriteria.HowToOrderScores);

                    PathStatistics suffixStatistics = evaluateSuffix();

                    if (!suffixStatistics.HasPaths)
                    {
                        suffixStatistics = PathStatistics.Terminal();
                    }

                    PathStatistics rootStatistics =
                        suffixStatistics.AddConstant(utility);

                    double aggregateScore = GetAggregateScore(
                        rootStatistics,
                        rootAction.GetChoosingMode());

                    if (!foundCandidate || aggregateScore > bestAggregateScore)
                    {
                        foundCandidate = true;
                        bestAggregateScore = aggregateScore;
                        chosenActionIndex = candidate.ActionIndex;
                    }
                }
                finally
                {
                    rootAction.EndSimulation(candidate.ActionIndex);
                    RestoreQueueBranch(queueData, rootAction, branchState);
                }
            }

            return foundCandidate;
        }


        protected override void OnShutdown()
        {
            if (m_Queues_To_Data != null)
            {
                foreach (KeyValuePair<Queue, LookUpQueueData> pair in m_Queues_To_Data)
                {
                    pair.Value?.ClearQueuedActionData();
                }
            }

            m_RuntimeQueueData.Clear();
            m_Initialized = false;
        }

        private PathStatistics EvaluateCopiedQueuePaths(
            LookUpAction rootAction,
            IReadOnlyList<LookUpAction> planningActions,
            LookUpQueueData queueData,
            int position,
            int depth)
        {
            if (position >= depth || position >= planningActions.Count)
            {
                return PathStatistics.Terminal();
            }

            LookUpAction currentAction = planningActions[position];

            if (currentAction == null)
            {
                return EvaluateCopiedQueuePaths(
                    rootAction,
                    planningActions,
                    queueData,
                    position + 1,
                    depth);
            }

            LookUpAction.ActionChoosingCriteria criteria =
                rootAction.GetCriteriaForLookUpNode(currentAction, false);

            List<ScoredCandidate> candidates = CollectScoredCandidates(
                currentAction,
                criteria,
                rootAction.CalculateNumberOfPathsToChoosePerNode(),
                queueData,
                QueueBranchMode.PreserveQueue,
                false);

            if (candidates.Count == 0)
            {
                return PathStatistics.Terminal();
            }

            PathStatistics combinedStatistics = default;

            for (int i = 0; i < candidates.Count; i++)
            {
                ScoredCandidate candidate = candidates[i];

                QueueBranchState branchState = BeginQueueBranch(
                    queueData,
                    currentAction,
                    QueueBranchMode.PreserveQueue,
                    false);

                if (!currentAction.BeginSimulation(candidate.ActionIndex))
                {
                    RestoreQueueBranch(queueData, currentAction, branchState);
                    continue;
                }

                try
                {
                    CompleteQueueBranchBeforeEvaluation(
                        queueData,
                        currentAction,
                        QueueBranchMode.PreserveQueue,
                        branchState);

                    if (!currentAction.TryEvaluateCurrentState(
                            criteria,
                            out float rawScore))
                    {
                        continue;
                    }

                    float utility = LookUpAction.ConvertRawScoreToUtility(
                        rawScore,
                        criteria.HowToOrderScores);

                    PathStatistics childStatistics = EvaluateCopiedQueuePaths(
                        rootAction,
                        planningActions,
                        queueData,
                        position + 1,
                        depth);

                    if (!childStatistics.HasPaths)
                    {
                        childStatistics = PathStatistics.Terminal();
                    }

                    combinedStatistics.Merge(
                        childStatistics.AddConstant(utility));
                }
                finally
                {
                    currentAction.EndSimulation(candidate.ActionIndex);
                    RestoreQueueBranch(
                        queueData,
                        currentAction,
                        branchState);
                }
            }

            return combinedStatistics;
        }

        private PathStatistics EvaluateDynamicQueuePaths(
            LookUpAction rootAction,
            LookUpQueueData queueData,
            int position,
            int depth)
        {
            if (position >= depth || queueData == null)
            {
                return PathStatistics.Terminal();
            }

            LookUpAction currentAction =
                GetNextDynamicQueueAction(queueData);

            if (currentAction == null)
            {
                return PathStatistics.Terminal();
            }

            currentAction.PrepareForSimulation(depth - position);

            LookUpAction.ActionChoosingCriteria criteria =
                rootAction.GetCriteriaForLookUpNode(currentAction, false);

            List<ScoredCandidate> candidates = CollectScoredCandidates(
                currentAction,
                criteria,
                rootAction.CalculateNumberOfPathsToChoosePerNode(),
                queueData,
                QueueBranchMode.SimulateFullQueueExecution,
                false);

            if (candidates.Count == 0)
            {
                return PathStatistics.Terminal();
            }

            PathStatistics combinedStatistics = default;

            for (int i = 0; i < candidates.Count; i++)
            {
                ScoredCandidate candidate = candidates[i];

                QueueBranchState branchState = BeginQueueBranch(
                    queueData,
                    currentAction,
                    QueueBranchMode.SimulateFullQueueExecution,
                    false);

                if (!currentAction.BeginSimulation(candidate.ActionIndex))
                {
                    RestoreQueueBranch(queueData, currentAction, branchState);
                    continue;
                }

                try
                {
                    CompleteQueueBranchBeforeEvaluation(
                        queueData,
                        currentAction,
                        QueueBranchMode.SimulateFullQueueExecution,
                        branchState);

                    if (!currentAction.TryEvaluateCurrentState(
                            criteria,
                            out float rawScore))
                    {
                        continue;
                    }

                    float utility = LookUpAction.ConvertRawScoreToUtility(
                        rawScore,
                        criteria.HowToOrderScores);

                    PathStatistics childStatistics =
                        EvaluateDynamicQueuePaths(
                            rootAction,
                            queueData,
                            position + 1,
                            depth);

                    if (!childStatistics.HasPaths)
                    {
                        childStatistics = PathStatistics.Terminal();
                    }

                    combinedStatistics.Merge(
                        childStatistics.AddConstant(utility));
                }
                finally
                {
                    currentAction.EndSimulation(candidate.ActionIndex);
                    RestoreQueueBranch(
                        queueData,
                        currentAction,
                        branchState);
                }
            }

            return combinedStatistics;
        }

        private static LookUpAction GetNextDynamicQueueAction(
            LookUpQueueData queueData)
        {
            IReadOnlyList<LookUpAction> orderedActions =
                queueData.CreateSimulationOrderedSnapshot();

            return orderedActions.Count > 0
                ? orderedActions[0]
                : null;
        }


        private List<ScoredCandidate> CollectScoredCandidates(
            LookUpAction nodeAction,
            LookUpAction.ActionChoosingCriteria criteria,
            int maximumCandidateCount,
            LookUpQueueData queueData,
            QueueBranchMode queueBranchMode,
            bool rootWillQueueAgainAfterExecution)
        {
            List<ScoredCandidate> candidates = new List<ScoredCandidate>();

            if (nodeAction == null)
            {
                return candidates;
            }

            int actionCount = nodeAction.GetPossibleActionCount();

            for (int actionIndex = 0; actionIndex < actionCount; actionIndex++)
            {
                QueueBranchState branchState = BeginQueueBranch(
                    queueData,
                    nodeAction,
                    queueBranchMode,
                    rootWillQueueAgainAfterExecution);

                if (!nodeAction.BeginSimulation(actionIndex))
                {
                    RestoreQueueBranch(queueData, nodeAction, branchState);
                    continue;
                }

                try
                {
                    CompleteQueueBranchBeforeEvaluation(
                        queueData,
                        nodeAction,
                        queueBranchMode,
                        branchState);

                    if (!nodeAction.TryEvaluateCurrentState(
                            criteria,
                            out float rawScore))
                    {
                        continue;
                    }

                    float utility = LookUpAction.ConvertRawScoreToUtility(
                        rawScore,
                        criteria.HowToOrderScores);

                    candidates.Add(
                        new ScoredCandidate(actionIndex, rawScore, utility));
                }
                finally
                {
                    nodeAction.EndSimulation(actionIndex);
                    RestoreQueueBranch(queueData, nodeAction, branchState);
                }
            }

            candidates.Sort((left, right) =>
            {
                int scoreComparison = LookUpAction.CompareRawScores(
                    left.RawScore,
                    right.RawScore,
                    criteria.HowToOrderScores);

                return scoreComparison != 0
                    ? scoreComparison
                    : left.ActionIndex.CompareTo(right.ActionIndex);
            });

            if (maximumCandidateCount > 0 &&
                candidates.Count > maximumCandidateCount)
            {
                candidates.RemoveRange(
                    maximumCandidateCount,
                    candidates.Count - maximumCandidateCount);
            }

            return candidates;
        }

        private static QueueBranchState BeginQueueBranch(
            LookUpQueueData queueData,
            LookUpAction action,
            QueueBranchMode mode,
            bool rootWillQueueAgainAfterExecution)
        {
            LookUpQueueData.RuntimeState queueState =
                queueData?.CaptureRuntimeState();

            LookUpAction.QueueRuntimeState actionState =
                action.CaptureQueueRuntimeState();

            bool shouldQueueAgain = rootWillQueueAgainAfterExecution;

            switch (mode)
            {
                case QueueBranchMode.CompleteAlreadyPoppedRoot:
                    // The real queue has already popped the root and advanced its
                    // queue index before LookUpAction.Execute is called.
                    queueData?.RemoveQueuedActionData(action);
                    break;

                case QueueBranchMode.SimulateFullQueueExecution:
                    shouldQueueAgain =
                        queueData != null &&
                        queueData.BeginSimulatedExecution(action);
                    break;
            }

            return new QueueBranchState(
                queueState,
                actionState,
                shouldQueueAgain);
        }

        private static void CompleteQueueBranchBeforeEvaluation(
            LookUpQueueData queueData,
            LookUpAction action,
            QueueBranchMode mode,
            QueueBranchState state)
        {
            switch (mode)
            {
                case QueueBranchMode.PreserveQueue:
                    // Ignore queue additions, removals, and priority changes caused
                    // by the simulated command while keeping its game-state changes.
                    queueData?.RestoreRuntimeState(state.QueueState);
                    action.RestoreQueueRuntimeState(state.ActionState);
                    break;

                case QueueBranchMode.CompleteAlreadyPoppedRoot:
                case QueueBranchMode.SimulateFullQueueExecution:
                    queueData?.CompleteSimulatedExecution(
                        action,
                        state.ShouldQueueAgain);
                    break;
            }
        }

        private static void RestoreQueueBranch(
            LookUpQueueData queueData,
            LookUpAction action,
            QueueBranchState state)
        {
            queueData?.RestoreRuntimeState(state.QueueState);
            action.RestoreQueueRuntimeState(state.ActionState);
        }


        private static double GetAggregateScore(
            PathStatistics statistics,
            LookUpAction.HowToChooseFromPossibleActions choosingMode)
        {
            if (!statistics.HasPaths)
            {
                return double.NegativeInfinity;
            }

            switch (choosingMode)
            {
                case LookUpAction.HowToChooseFromPossibleActions.LookUpTreeScore:
                    return statistics.Best;

                case LookUpAction.HowToChooseFromPossibleActions.AverageLookUpTreeScore:
                    return statistics.Sum / statistics.Count;

                case LookUpAction.HowToChooseFromPossibleActions.SumLookUpTreeScore:
                    return statistics.Sum;

                default:
                    return statistics.Best;
            }
        }

        private static List<LookUpAction> BuildPlanningActionList(
            LookUpAction rootAction,
            IReadOnlyList<LookUpAction> queueSnapshot)
        {
            List<LookUpAction> result = new List<LookUpAction>();

            if (queueSnapshot == null || queueSnapshot.Count == 0)
            {
                result.Add(rootAction);
                return result;
            }

            int rootIndex = -1;

            for (int i = 0; i < queueSnapshot.Count; i++)
            {
                if (ReferenceEquals(queueSnapshot[i], rootAction))
                {
                    rootIndex = i;
                    break;
                }
            }

            if (rootIndex < 0)
            {
                result.Add(rootAction);
                return result;
            }

            for (int i = rootIndex; i < queueSnapshot.Count; i++)
            {
                LookUpAction action = queueSnapshot[i];

                if (action != null)
                {
                    result.Add(action);
                }
            }

            if (result.Count == 0)
            {
                result.Add(rootAction);
            }

            return result;
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_Queues_To_Data ??= new Dictionary<Queue, LookUpQueueData>();
            m_RuntimeQueueData.Clear();

            foreach (KeyValuePair<Queue, LookUpQueueData> pair in m_Queues_To_Data)
            {
                if (pair.Value != null && !m_RuntimeQueueData.Contains(pair.Value))
                {
                    m_RuntimeQueueData.Add(pair.Value);
                }
            }

            m_Initialized = true;
        }

        private bool TryGetOrCreateQueueData(Queue queue, out LookUpQueueData queueData)
        {
            queueData = null;

            if (queue == null)
            {
                return false;
            }

            if (m_Queues_To_Data.TryGetValue(queue, out queueData) && queueData != null)
            {
                return true;
            }

            if (m_Queues_To_Data.ContainsKey(queue))
            {
                m_Queues_To_Data.Remove(queue);
            }

            switch (m_BehaviourOnNoQueueDataDefined)
            {
                case BehaviourOnNoQueueDataDefined.ReturnError:
                    Debug.LogError(
                        $"{nameof(LookUpTreeManager)} has no queue data defined for {queue.name}.");
                    return false;

                case BehaviourOnNoQueueDataDefined.CreateQueueDataBasedOnDefault:
                    if (m_DefaultQueueData == null)
                    {
                        Debug.LogError(
                            $"{nameof(LookUpTreeManager)} cannot create queue data because its default template is null.");
                        return false;
                    }

                    queueData = new LookUpQueueData(m_DefaultQueueData);
                    m_Queues_To_Data.Add(queue, queueData);
                    m_RuntimeQueueData.Add(queueData);
                    return true;

                case BehaviourOnNoQueueDataDefined.DoNothingAndReturnMessage:
                    Debug.Log(
                        $"{nameof(LookUpTreeManager)} has no queue data defined for {queue.name}.");
                    return false;

                case BehaviourOnNoQueueDataDefined.DoNothing:
                default:
                    return false;
            }
        }

        private bool TryGetExistingQueueData(Queue queue, out LookUpQueueData queueData)
        {
            queueData = null;

            return queue != null &&
                   m_Queues_To_Data != null &&
                   m_Queues_To_Data.TryGetValue(queue, out queueData) &&
                   queueData != null;
        }

        private void HandleMissingQueueData(Queue queue)
        {
            if (queue == null)
            {
                return;
            }

            switch (m_BehaviourOnNoQueueDataDefined)
            {
                case BehaviourOnNoQueueDataDefined.ReturnError:
                    Debug.LogError(
                        $"{nameof(LookUpTreeManager)} has no queue data defined for {queue.name}.");
                    break;

                case BehaviourOnNoQueueDataDefined.CreateQueueDataBasedOnDefault:
                    TryGetOrCreateQueueData(queue, out _);
                    break;

                case BehaviourOnNoQueueDataDefined.DoNothingAndReturnMessage:
                    Debug.Log(
                        $"{nameof(LookUpTreeManager)} has no queue data defined for {queue.name}.");
                    break;

                case BehaviourOnNoQueueDataDefined.DoNothing:
                    break;
            }
        }

        private bool IsValid()
        {
            if (!m_Initialized)
            {
                Initialize();
            }

            if (m_Queues_To_Data != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(LookUpTreeManager)} queue dictionary is null.");
            return false;
        }
    }
}