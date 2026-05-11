using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.State
{
    public class StateMachine : SerializedMonoBehaviour
    {
        [FoldoutGroup("Data")]
        [SerializeField, Tooltip("If this is false, can't null current state.")]
        private bool m_AllowNullState = true;

        [FoldoutGroup("Data")]
        [SerializeField, Tooltip("If you don't allow null state and this is null, it will keep whatever state it had previous to on enable, whether it is null or any other state. If this is null, state on enable will be null.")]
        private StateTag m_StateOnEnable = null;

        [FoldoutGroup("Data")]
        [OdinSerialize, ShowInInspector]
        private Dictionary<StateTag, State> m_States = new();

        [FoldoutGroup("Events")]
        [SerializeField]
        private StateEvent m_OnStateChange = null;

        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent m_OnNullState = null;

        public Dictionary<StateTag, State> States => m_States;

        [FoldoutGroup("Debug")]
        [SerializeField, ReadOnly]
        private StateTag m_CurrentStateTag = null;

        [FoldoutGroup("Debug")]
        [SerializeField, ReadOnly]
        private State m_CurrentState = null;

        private enum TransitionPhase
        {
            None,
            EndTransition,
            StartTransition
        }

        private bool m_IsTransitioning;
        private TransitionPhase m_TransitionPhase = TransitionPhase.None;

        private State m_TransitionSourceState;

        private StateTag m_PendingStateTag;
        private State m_PendingState;
        private bool m_PendingNullState;

        private StateTag m_QueuedStateTag;
        private State m_QueuedState;
        private bool m_HasQueuedState;

        private void Awake()
        {
            foreach (var state in m_States.Values)
            {
                state.SetGameObject(gameObject);
            }
        }

        private void OnEnable()
        {
            if (m_StateOnEnable != null)
            {
                ChangeState(m_StateOnEnable);
            }
            else if (m_AllowNullState)
            {
                NullCurrentState();
            }
        }

        private void Update()
        {
            m_CurrentState?.UpdateAction();
        }

        public void ChangeState(StateTag stateTag)
        {
            if (stateTag == null || !m_States.TryGetValue(stateTag, out State requestedState))
                return;

            if (m_IsTransitioning)
            {
                HandleStateRequestWhileTransitioning(stateTag, requestedState);
                return;
            }

            if (m_CurrentStateTag == stateTag)
                return;

            if (m_CurrentState != null && !m_CurrentState.CanTransitionTo(stateTag))
                return;

            if (!requestedState.CanEnter())
                return;

            BeginTransitionToState(stateTag, requestedState);
        }

        public void NullCurrentState()
        {
            if (m_IsTransitioning)
            {
                AdvanceTransition();
                return;
            }

            if (!m_AllowNullState)
                return;

            BeginTransitionToNull();
        }

        private void HandleStateRequestWhileTransitioning(StateTag requestedTag, State requestedState)
        {
            if (!requestedState.CanEnter())
                return;

            switch (m_TransitionPhase)
            {
                case TransitionPhase.EndTransition:
                    // Previous requested state has NOT started its start transition yet.
                    // Replace it with the newer request.
                    if (m_TransitionSourceState != null &&
                        !m_TransitionSourceState.CanTransitionTo(requestedTag))
                    {
                        return;
                    }

                    m_PendingStateTag = requestedTag;
                    m_PendingState = requestedState;
                    m_PendingNullState = false;
                    break;

                case TransitionPhase.StartTransition:
                    // Previous requested state has already started its start transition.
                    // Queue the newer request to run after the previous request finishes entering.
                    if (m_PendingState != null &&
                        !m_PendingState.CanTransitionTo(requestedTag))
                    {
                        return;
                    }

                    m_QueuedStateTag = requestedTag;
                    m_QueuedState = requestedState;
                    m_HasQueuedState = true;
                    break;
            }
        }

        private void BeginTransitionToState(StateTag targetTag, State targetState)
        {
            m_PendingStateTag = targetTag;
            m_PendingState = targetState;
            m_PendingNullState = false;

            m_TransitionSourceState = m_CurrentState;

            StateTag endTransitionTag = m_CurrentState?.EndTransitionStateTag;

            m_CurrentState?.EndAction();

            if (TryStartTransitionState(endTransitionTag, TransitionPhase.EndTransition))
                return;

            StartPendingStateOrStartTransition();
        }

        private void BeginTransitionToNull()
        {
            m_PendingStateTag = null;
            m_PendingState = null;
            m_PendingNullState = true;

            m_TransitionSourceState = m_CurrentState;

            StateTag endTransitionTag = m_CurrentState?.EndTransitionStateTag;

            m_CurrentState?.EndAction();

            if (TryStartTransitionState(endTransitionTag, TransitionPhase.EndTransition))
                return;

            FinishNullState();
        }

        private bool TryStartTransitionState(StateTag transitionTag, TransitionPhase phase)
        {
            if (transitionTag == null || !m_States.TryGetValue(transitionTag, out State transitionState))
                return false;

            m_IsTransitioning = true;
            m_TransitionPhase = phase;

            m_CurrentState = transitionState;
            m_CurrentStateTag = transitionTag;

            transitionState.StartAction();

            return true;
        }

        private void AdvanceTransition()
        {
            m_CurrentState?.EndAction();

            if (m_TransitionPhase == TransitionPhase.EndTransition)
            {
                StartPendingStateOrStartTransition();
                return;
            }

            if (m_TransitionPhase == TransitionPhase.StartTransition)
            {
                EnterPendingState();

                if (m_HasQueuedState)
                {
                    StateTag queuedTag = m_QueuedStateTag;
                    State queuedState = m_QueuedState;

                    ClearQueuedState();

                    if (queuedTag != null && queuedState != null && m_CurrentStateTag != queuedTag)
                    {
                        BeginTransitionToState(queuedTag, queuedState);
                    }
                }
            }
        }

        private void StartPendingStateOrStartTransition()
        {
            if (m_PendingNullState)
            {
                FinishNullState();
                return;
            }

            StateTag startTransitionTag = m_PendingState?.StartTransitionStateTag;

            if (TryStartTransitionState(startTransitionTag, TransitionPhase.StartTransition))
                return;

            EnterPendingState();
        }

        private void EnterPendingState()
        {
            m_IsTransitioning = false;
            m_TransitionPhase = TransitionPhase.None;
            m_TransitionSourceState = null;

            m_CurrentState = m_PendingState;
            m_CurrentStateTag = m_PendingStateTag;

            m_PendingState = null;
            m_PendingStateTag = null;
            m_PendingNullState = false;

            if (m_CurrentState == null || m_CurrentStateTag == null)
                return;

            m_OnStateChange?.Invoke(m_CurrentStateTag);
            m_CurrentState.StartAction();
        }

        private void FinishNullState()
        {
            m_IsTransitioning = false;
            m_TransitionPhase = TransitionPhase.None;
            m_TransitionSourceState = null;

            m_CurrentState = null;
            m_CurrentStateTag = null;

            m_PendingState = null;
            m_PendingStateTag = null;
            m_PendingNullState = false;

            ClearQueuedState();

            m_OnNullState?.Invoke();
        }

        private void ClearQueuedState()
        {
            m_HasQueuedState = false;
            m_QueuedStateTag = null;
            m_QueuedState = null;
        }

#nullable enable
        public State? GetState(StateTag stateTag)
        {
            if (stateTag != null && m_States.TryGetValue(stateTag, out var state))
            {
                return state;
            }

            Debug.LogWarning($"El estado {stateTag?.name} no está configurado en {gameObject.name}");
            return null;
        }
#nullable disable

        public bool HasState(StateTag stateTag)
        {
            return stateTag != null && m_States.ContainsKey(stateTag);
        }
    }
}