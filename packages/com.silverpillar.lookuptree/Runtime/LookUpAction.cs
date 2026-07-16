using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.LookUpTree
{
    public class LookUpAction : SerializedMonoBehaviour
    {
        internal readonly struct QueueRuntimeState
        {
            public readonly int CurrentQueueIndex;
            public readonly float CurrentPriority;
            public readonly int RemainingTimesToQueueAgainAfterExecution;

            public QueueRuntimeState(
                int currentQueueIndex,
                float currentPriority,
                int remainingTimesToQueueAgainAfterExecution)
            {
                CurrentQueueIndex = currentQueueIndex;
                CurrentPriority = currentPriority;
                RemainingTimesToQueueAgainAfterExecution =
                    remainingTimesToQueueAgainAfterExecution;
            }
        }

        [Serializable]
        public enum LookUpTreeSearchProtocol
        {
            SimulateTreeWithoutCopyingCurrentQueueState,
            CopyCurrentQueueStateAndThenSimulate 
        }

        [Serializable]
        public struct ActionChoosingCriteria
        {
            [OdinSerialize, ShowInInspector]
            public ICachedCondition HowToFilterPossibleActions;

            [OdinSerialize, ShowInInspector]
            public ICachedScore HowToScorePossibleActions;

            [OdinSerialize, ShowInInspector]
            public HowToOrderScores HowToOrderScores;
        }

        [Serializable]
        public struct LookUpTreeCriteria
        {
            [SerializeField,
                Tooltip(
                "SimulateTreeWithoutCopyingCurrentQueueState simulates outcomes against the live queue, taking possible queue shifts and additions into account. " +
                "CopyCurrentQueueStateAndThenSimulate freezes the current queue order before simulation, so later shifts or additions are ignored.")]
            public LookUpTreeSearchProtocol LookUpTreeSearchProtocol;
            [OdinSerialize, ShowInInspector]
            public HowToChoosePathsInEachLookUpAction HowToChoosePathsInEachLookUpAction;

            [OdinSerialize, ShowInInspector]
            public IntCachedScore LookUpDepth;

            [OdinSerialize, ShowInInspector,
             Tooltip("Maximum number of branches kept at every node. Zero or a negative value keeps every valid branch.")]
            public IntCachedScore NumberOfPathsToChoosePerNode;

            [SerializeField, ShowIf(nameof(UsesOwnActionChoosingCriteria))]
            public bool ActionChoosingCriteriaIsTheSameAsSelf;

            [OdinSerialize, ShowInInspector, ShowIf(nameof(ShowOtherActionCriteria))]
            public ActionChoosingCriteria ActionChoosingCriteriaOfOtherLookUpActions;

            private bool UsesOwnActionChoosingCriteria =>
                HowToChoosePathsInEachLookUpAction ==
                HowToChoosePathsInEachLookUpAction.UseOwnActionChoosingCriteria;

            private bool ShowOtherActionCriteria =>
                UsesOwnActionChoosingCriteria &&
                !ActionChoosingCriteriaIsTheSameAsSelf;
        }

        public enum HowToChooseFromPossibleActions
        {
            Sequence,
            Random,
            Score,
            LookUpTreeScore,
            AverageLookUpTreeScore,
            SumLookUpTreeScore
        }

        public enum HowToChoosePathsInEachLookUpAction
        {
            UseTheActionChoosingCriteriaOfEachLookUpAction,
            UseOwnActionChoosingCriteria
        }

        [Title("Queue Info")]
        [SerializeField, Tooltip("The queue in which this action will be executed.")]
        private Queue m_Queue;
        public Queue Queue => m_Queue;

        [SerializeField]
        private WhenToStartQueuingAction m_WhenToStartQueuingAction;
        public WhenToStartQueuingAction WhenToStartQueuingAction => m_WhenToStartQueuingAction;

        [SerializeField]
        private WhenToStopQueuingAction m_WhenToStopQueuingAction;
        public WhenToStopQueuingAction WhenToStopQueuingAction => m_WhenToStopQueuingAction;

        [OdinSerialize, ShowInInspector]
        private ICachedScore m_Priority;

        [SerializeField, Tooltip("Negative means infinite requeues. Zero means execute once without requeuing.")]
        private int m_NumberOfTimesToQueueAgainAfterExecution;

        [Title("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<IUndoCommand> m_PossibleActions = new();

        private bool m_HasMoreThanOneAction => m_PossibleActions != null && m_PossibleActions.Count > 1;

        [Title("Action Choosing Criteria")]
        [SerializeField, ShowIf(nameof(m_HasMoreThanOneAction))]
        private HowToChooseFromPossibleActions m_HowToChooseFromPossibleActions;

        private bool SequenceOrRandom =>
            m_HasMoreThanOneAction &&
            (m_HowToChooseFromPossibleActions == HowToChooseFromPossibleActions.Sequence ||
            m_HowToChooseFromPossibleActions == HowToChooseFromPossibleActions.Random);

        [OdinSerialize, ShowInInspector, HideIf(nameof(SequenceOrRandom))]
        private ActionChoosingCriteria m_ActionChoosingCriteriaForSelf;

        [Title("Look Up Tree Criteria")]
        [OdinSerialize, ShowInInspector, HideIf(nameof(SequenceOrRandom))]
        private LookUpTreeCriteria m_LookUpTreeCriteria;

        [ShowInInspector, ReadOnly]
        private float m_CurrentPriority;

        private int m_RemainingTimesToQueueAgainAfterExecution;
        private int m_CurrentQueueIndex;
        private int m_CurrentSequenceIndex;

        private bool m_Initialized;
        private int m_SimulationHistoryDepth = 1;
        private int m_ActiveSimulationCount;

        private readonly List<IUndoCommand> m_SimulationHistorySources = new();
        private readonly List<CommandHistory> m_SimulationHistories = new();

        private void Awake()
        {
            Initialize();
            RegisterData(WhenToStartQueuingAction.OnAwake);
        }

        private void Start()
        {
            RegisterData(WhenToStartQueuingAction.OnStart);
        }

        private void OnEnable()
        {
            RegisterData(WhenToStartQueuingAction.OnEnable);
        }

        private void OnDisable()
        {
            UnregisterData(WhenToStopQueuingAction.OnDisable);
        }

        private void OnDestroy()
        {
            UnregisterData(WhenToStopQueuingAction.OnDestroy);
        }

        public void ExecuteNextInQueue(Queue queuedActionChannel)
        {
            if (LookUpTreeManager.Instance == null)
            {
                Debug.LogError($"{nameof(LookUpTreeManager)} instance was not found.", this);
                return;
            }

            LookUpTreeManager.Instance.ExecuteAndPop(queuedActionChannel);
        }

        /// <summary>
        /// Executes this action directly. When possible, it obtains the current queue
        /// snapshot so lookup-tree modes can inspect the following queued actions.
        /// </summary>
        public void Execute()
        {
            IReadOnlyList<LookUpAction> queueSnapshot = null;

            if (LookUpTreeManager.Instance != null)
            {
                LookUpTreeManager.Instance.TryGetQueueSnapshot(m_Queue, out queueSnapshot);
            }

            Execute(queueSnapshot, false);
        }

        internal void Execute(
            IReadOnlyList<LookUpAction> queueSnapshot,
            bool willQueueAgainAfterExecution)
        {
            Initialize();

            if (!TryChoosePossibleAction(
                    queueSnapshot,
                    willQueueAgainAfterExecution,
                    out int chosenActionIndex))
            {
                return;
            }

            if (!IsValidActionIndex(chosenActionIndex))
            {
                return;
            }

            m_PossibleActions[chosenActionIndex].Execute();
        }

        public void SetCurrentQueueIndex(int currentQueueIndex)
        {
            m_CurrentQueueIndex = currentQueueIndex;
        }

        public float CalculatePriority()
        {
            if (m_Priority == null)
            {
                m_CurrentPriority = 0f;
                return m_CurrentPriority;
            }

            m_CurrentPriority = m_Priority.CalculateScore() + m_CurrentQueueIndex;
            return m_CurrentPriority;
        }

        public float GetPriority()
        {
            return m_CurrentPriority;
        }

        public float OffsetPriority(float offset)
        {
            m_CurrentPriority += offset;
            return m_CurrentPriority;
        }

        /// <summary>
        /// Keeps the stored queue index and the already calculated priority in the same
        /// coordinate system when the queue resets its large running index.
        /// </summary>
        public void OffsetQueueIndex(int offset)
        {
            m_CurrentQueueIndex += offset;
            m_CurrentPriority += offset;
        }

        public bool ShouldQueueAgainAfterExecution()
        {
            if (m_NumberOfTimesToQueueAgainAfterExecution < 0)
            {
                return true;
            }

            if (m_RemainingTimesToQueueAgainAfterExecution <= 0)
            {
                return false;
            }

            m_RemainingTimesToQueueAgainAfterExecution--;
            return true;
        }

        internal HowToChooseFromPossibleActions GetChoosingMode()
        {
            return m_HowToChooseFromPossibleActions;
        }

        internal LookUpTreeSearchProtocol GetLookUpTreeSearchProtocol()
        {
            return m_LookUpTreeCriteria.LookUpTreeSearchProtocol;
        }

        internal QueueRuntimeState CaptureQueueRuntimeState()
        {
            return new QueueRuntimeState(
                m_CurrentQueueIndex,
                m_CurrentPriority,
                m_RemainingTimesToQueueAgainAfterExecution);
        }

        internal void RestoreQueueRuntimeState(QueueRuntimeState state)
        {
            m_CurrentQueueIndex = state.CurrentQueueIndex;
            m_CurrentPriority = state.CurrentPriority;
            m_RemainingTimesToQueueAgainAfterExecution =
                state.RemainingTimesToQueueAgainAfterExecution;
        }

        internal int GetPossibleActionCount()
        {
            return m_PossibleActions == null ? 0 : m_PossibleActions.Count;
        }

        internal int CalculateLookUpDepth()
        {
            if (m_LookUpTreeCriteria.LookUpDepth == null)
            {
                return 1;
            }

            m_LookUpTreeCriteria.LookUpDepth.SetGameObject(gameObject);
            return Mathf.Max(1, m_LookUpTreeCriteria.LookUpDepth.CalculateScoreAsInt());
        }

        internal int CalculateNumberOfPathsToChoosePerNode()
        {
            if (m_LookUpTreeCriteria.NumberOfPathsToChoosePerNode == null)
            {
                return 0;
            }

            m_LookUpTreeCriteria.NumberOfPathsToChoosePerNode.SetGameObject(gameObject);
            return m_LookUpTreeCriteria.NumberOfPathsToChoosePerNode.CalculateScoreAsInt();
        }

        internal ActionChoosingCriteria GetCriteriaForLookUpNode(LookUpAction node, bool isRootNode)
        {
            if (isRootNode || node == null)
            {
                return m_ActionChoosingCriteriaForSelf;
            }

            switch (m_LookUpTreeCriteria.HowToChoosePathsInEachLookUpAction)
            {
                case HowToChoosePathsInEachLookUpAction.UseTheActionChoosingCriteriaOfEachLookUpAction:
                    return node.m_ActionChoosingCriteriaForSelf;

                case HowToChoosePathsInEachLookUpAction.UseOwnActionChoosingCriteria:
                    return m_LookUpTreeCriteria.ActionChoosingCriteriaIsTheSameAsSelf
                        ? m_ActionChoosingCriteriaForSelf
                        : m_LookUpTreeCriteria.ActionChoosingCriteriaOfOtherLookUpActions;

                default:
                    return m_ActionChoosingCriteriaForSelf;
            }
        }

        internal void PrepareForSimulation(int requiredDepth)
        {
            Initialize();

            // A requeued LookUpAction can appear more than once in the same lookup
            // path. Its parent command must remain in history until that path unwinds.
            if (m_ActiveSimulationCount > 0)
            {
                return;
            }

            m_SimulationHistoryDepth = Mathf.Max(1, requiredDepth);
            EnsureSimulationHistories();

            for (int i = 0; i < m_SimulationHistories.Count; i++)
            {
                CommandHistory history = m_SimulationHistories[i];

                if (history == null)
                {
                    continue;
                }

                history.SetDepth(m_SimulationHistoryDepth);
                history.ClearHistory();
            }
        }

        internal bool BeginSimulation(int actionIndex)
        {
            EnsureSimulationHistories();

            if (actionIndex < 0 || actionIndex >= m_SimulationHistories.Count)
            {
                return false;
            }

            CommandHistory history = m_SimulationHistories[actionIndex];

            if (history == null)
            {
                return false;
            }

            history.Execute();
            m_ActiveSimulationCount++;
            return true;
        }

        internal void EndSimulation(int actionIndex)
        {
            if (actionIndex < 0 || actionIndex >= m_SimulationHistories.Count)
            {
                return;
            }

            CommandHistory history = m_SimulationHistories[actionIndex];

            if (history == null)
            {
                return;
            }

            try
            {
                history.Undo();
            }
            finally
            {
                m_ActiveSimulationCount =
                    Mathf.Max(0, m_ActiveSimulationCount - 1);
            }
        }

        internal bool TryEvaluateCurrentState(ActionChoosingCriteria criteria, out float score)
        {
            score = 0f;

            BindCriteria(criteria, gameObject);

            if (criteria.HowToFilterPossibleActions != null &&
                !criteria.HowToFilterPossibleActions.IsFulfilled())
            {
                return false;
            }

            if (criteria.HowToScorePossibleActions != null)
            {
                score = criteria.HowToScorePossibleActions.CalculateScore();
            }

            return !float.IsNaN(score);
        }

        internal static bool IsRawScoreBetter(
            float candidate,
            float currentBest,
            HowToOrderScores order)
        {
            return order == HowToOrderScores.LowestToHighest
                ? candidate < currentBest
                : candidate > currentBest;
        }

        internal static int CompareRawScores(
            float left,
            float right,
            HowToOrderScores order)
        {
            int comparison = left.CompareTo(right);
            return order == HowToOrderScores.LowestToHighest ? comparison : -comparison;
        }

        internal static float ConvertRawScoreToUtility(float score, HowToOrderScores order)
        {
            return order == HowToOrderScores.LowestToHighest ? -score : score;
        }

        private void RegisterData(WhenToStartQueuingAction when)
        {
            if (m_WhenToStartQueuingAction != when)
            {
                return;
            }

            if (m_Queue == null)
            {
                Debug.LogError($"{nameof(LookUpAction)} cannot register because its queue is null.", this);
                return;
            }

            Initialize();
            ResetRequeueCounter();

            if (LookUpTreeManager.Instance == null)
            {
                Debug.LogError($"{nameof(LookUpTreeManager)} instance was not found.", this);
                return;
            }

            LookUpTreeManager.Instance.AddQueuedAction(m_Queue, this);
        }

        private void UnregisterData(WhenToStopQueuingAction when)
        {
            if (m_WhenToStopQueuingAction != when || m_Queue == null)
            {
                return;
            }

            if (LookUpTreeManager.Instance == null)
            {
                return;
            }

            LookUpTreeManager.Instance.RemoveQueuedAction(m_Queue, this);
        }

        private void Initialize()
        {
            if (m_Initialized)
            {
                EnsureSimulationHistories();
                return;
            }

            m_PossibleActions ??= new List<IUndoCommand>();

            for (int i = 0; i < m_PossibleActions.Count; i++)
            {
                IUndoCommand action = m_PossibleActions[i];

                if (action == null)
                {
                    continue;
                }

                if (!action.SetGameObject(gameObject))
                {
                    Debug.LogWarning($"Could not bind possible action at index {i} to {name}.", this);
                }
            }

            m_Priority?.SetGameObject(gameObject);
            BindCriteria(m_ActionChoosingCriteriaForSelf, gameObject);
            BindCriteria(m_LookUpTreeCriteria.ActionChoosingCriteriaOfOtherLookUpActions, gameObject);
            m_LookUpTreeCriteria.LookUpDepth?.SetGameObject(gameObject);
            m_LookUpTreeCriteria.NumberOfPathsToChoosePerNode?.SetGameObject(gameObject);

            ResetRequeueCounter();
            EnsureSimulationHistories();
            m_Initialized = true;
        }

        private void ResetRequeueCounter()
        {
            m_RemainingTimesToQueueAgainAfterExecution =
                m_NumberOfTimesToQueueAgainAfterExecution;
        }

        private bool TryChoosePossibleAction(
            IReadOnlyList<LookUpAction> queueSnapshot,
            bool willQueueAgainAfterExecution,
            out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            if (m_PossibleActions == null || m_PossibleActions.Count == 0)
            {
                return false;
            }

            switch (m_HowToChooseFromPossibleActions)
            {
                case HowToChooseFromPossibleActions.Sequence:
                    return TryChooseInSequence(out chosenActionIndex);

                case HowToChooseFromPossibleActions.Random:
                    return TryChooseRandom(out chosenActionIndex);

                case HowToChooseFromPossibleActions.Score:
                    return TryChooseByImmediateScore(out chosenActionIndex);

                case HowToChooseFromPossibleActions.LookUpTreeScore:
                case HowToChooseFromPossibleActions.AverageLookUpTreeScore:
                case HowToChooseFromPossibleActions.SumLookUpTreeScore:
                    if (LookUpTreeManager.Instance != null &&
                        LookUpTreeManager.Instance.TryChooseActionFromLookUpTree(
                            this,
                            queueSnapshot,
                            willQueueAgainAfterExecution,
                            out chosenActionIndex))
                    {
                        return true;
                    }

                    // A missing manager or an unusable queue should not make the action
                    // completely inert. Fall back to one-step scoring.
                    return TryChooseByImmediateScore(out chosenActionIndex);

                default:
                    return false;
            }
        }

        private bool TryChooseInSequence(out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            int count = GetPossibleActionCount();

            for (int attempt = 0; attempt < count; attempt++)
            {
                int index = m_CurrentSequenceIndex % count;
                m_CurrentSequenceIndex = (m_CurrentSequenceIndex + 1) % count;

                if (!IsValidActionIndex(index))
                {
                    continue;
                }

                chosenActionIndex = index;
                return true;
            }

            return false;
        }

        private bool TryChooseRandom(out int chosenActionIndex)
        {
            chosenActionIndex = -1;

            List<int> validIndices = new List<int>();

            int count = GetPossibleActionCount();

            for (int i = 0; i < count; i++)
            {
                if (IsValidActionIndex(i))
                {
                    validIndices.Add(i);
                }
            }

            if (validIndices.Count == 0)
            {
                return false;
            }

            chosenActionIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            return true;
        }

        private bool TryChooseByImmediateScore(out int chosenActionIndex)
        {
            chosenActionIndex = -1;
            PrepareForSimulation(1);

            bool foundCandidate = false;
            float bestScore = 0f;

            for (int i = 0; i < GetPossibleActionCount(); i++)
            {
                if (!BeginSimulation(i))
                {
                    continue;
                }

                try
                {
                    if (!TryEvaluateCurrentState(m_ActionChoosingCriteriaForSelf, out float score))
                    {
                        continue;
                    }

                    if (!foundCandidate ||
                        IsRawScoreBetter(
                            score,
                            bestScore,
                            m_ActionChoosingCriteriaForSelf.HowToOrderScores))
                    {
                        foundCandidate = true;
                        bestScore = score;
                        chosenActionIndex = i;
                    }
                }
                finally
                {
                    EndSimulation(i);
                }
            }

            return foundCandidate;
        }

        private bool IsValidActionIndex(int index)
        {
            return m_PossibleActions != null &&
                   index >= 0 &&
                   index < m_PossibleActions.Count &&
                   m_PossibleActions[index] != null;
        }

        private void EnsureSimulationHistories()
        {
            m_PossibleActions ??= new List<IUndoCommand>();

            for (int i = 0; i < m_PossibleActions.Count;)
            {
                if (m_PossibleActions[i] == null)
                {
                    m_PossibleActions.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            bool mustRebuild =
                m_SimulationHistories.Count != m_PossibleActions.Count ||
                m_SimulationHistorySources.Count != m_PossibleActions.Count;

            if (!mustRebuild)
            {
                for (int i = 0; i < m_PossibleActions.Count; i++)
                {
                    if (!ReferenceEquals(m_SimulationHistorySources[i], m_PossibleActions[i]))
                    {
                        mustRebuild = true;
                        break;
                    }
                }
            }

            if (!mustRebuild)
            {
                return;
            }

            m_SimulationHistorySources.Clear();
            m_SimulationHistories.Clear();

            for (int i = 0; i < m_PossibleActions.Count; i++)
            {
                IUndoCommand source = m_PossibleActions[i];

                m_SimulationHistorySources.Add(source);

                try
                {
                    CommandHistory history = new CommandHistory(source);
                    history.SetDepth(m_SimulationHistoryDepth);
                    history.SetGameObject(gameObject);
                    m_SimulationHistories.Add(history);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    m_SimulationHistories.Add(null);
                }
            }
        }

        private static void BindCriteria(ActionChoosingCriteria criteria, GameObject target)
        {
            criteria.HowToFilterPossibleActions?.SetGameObject(target);
            criteria.HowToScorePossibleActions?.SetGameObject(target);
        }
    }
}