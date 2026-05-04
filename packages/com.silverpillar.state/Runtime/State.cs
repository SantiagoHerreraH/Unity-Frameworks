using SilverPillar.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.State
{
    [Serializable]
    public struct StateAndStateTag
    {
        public StateTag StateTag;

        [HideInInspector]
        public State State;
    }

    [Serializable]
    public class State : IAction
    {
        [FoldoutGroup("Conditions")]
        [SerializeField]
        private ICachedCondition m_CanTransitionIf = null;

        [FoldoutGroup("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<IAction> m_Actions = new();

        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent m_OnStart = null;

        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent m_OnUpdate = null;

        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent m_OnEnd = null;

        [FoldoutGroup("Connected States")]
        [SerializeField]
        private bool m_CheckIfCanTransitionToOtherStatesOnUpdate = false;

        [FoldoutGroup("Connected States")]
        [SerializeField]
        private bool m_CanTransitionToAllStates = true;

        [FoldoutGroup("Connected States")]
        [OdinSerialize, ShowInInspector, HideIf(nameof(m_CanTransitionToAllStates))]
        private HashSet<StateTag> m_AllowedStatesToTransitionTo = new();

        private List<StateAndStateTag> m_StatesToChangeTo = new();

        [FoldoutGroup("Transition States")]
        [InfoBox("Transition States are executed before and after the state. They do not need to be connected to this state.")]
        [SerializeField]
        private StateTag m_StartTransitionStateTag;

        public StateTag StartTransitionStateTag => m_StartTransitionStateTag;

        [FoldoutGroup("Transition States")]
        [SerializeField]
        private StateTag m_EndTransitionStateTag;

        public StateTag EndTransitionStateTag => m_EndTransitionStateTag;

        public enum HowToEndState
        {
            OnChangeState,
            OnTimeEnd
        }

        [FoldoutGroup("How To End State")]
        [SerializeField]
        private HowToEndState m_HowToEndState;

        [FoldoutGroup("How To End State")]
        [SerializeField, ShowIf(nameof(m_HowToEndState), HowToEndState.OnTimeEnd), Tooltip("If null, will try to null state on time end")]
        private StateTag m_StateToTransitionToOnTimeEnd;

        [FoldoutGroup("How To End State")]
        [SerializeField, ShowIf(nameof(m_HowToEndState), HowToEndState.OnTimeEnd)]
        private bool m_CanTransitionToOtherStateBeforeCounterEnd = false;

        [FoldoutGroup("How To End State")]
        [OdinSerialize, ShowIf(nameof(m_HowToEndState), HowToEndState.OnTimeEnd)]
        private ICachedScore m_Time;

        private float m_MaxTime;
        private float m_CurrentTime;

        private StateMachine m_StateMachine = null;

        public State() { }

        public State(State other)
        {
            m_CanTransitionIf = other.m_CanTransitionIf?.Clone();
            m_Actions = other.m_Actions?.Select(a => a.Clone()).ToList() ?? new();
            m_OnStart = other.m_OnStart;
            m_OnUpdate = other.m_OnUpdate;
            m_OnEnd = other.m_OnEnd;

            m_CheckIfCanTransitionToOtherStatesOnUpdate = other.m_CheckIfCanTransitionToOtherStatesOnUpdate;
            m_CanTransitionToAllStates = other.m_CanTransitionToAllStates;
            m_AllowedStatesToTransitionTo = new HashSet<StateTag>(other.m_AllowedStatesToTransitionTo);
            m_StatesToChangeTo = new(other.m_StatesToChangeTo);

            m_StartTransitionStateTag = other.m_StartTransitionStateTag;
            m_EndTransitionStateTag = other.m_EndTransitionStateTag;

            m_HowToEndState = other.m_HowToEndState;
            m_StateToTransitionToOnTimeEnd = other.m_StateToTransitionToOnTimeEnd;
            m_CanTransitionToOtherStateBeforeCounterEnd = other.m_CanTransitionToOtherStateBeforeCounterEnd;
            m_Time = other.m_Time?.Clone();

            m_MaxTime = other.m_MaxTime;
            m_CurrentTime = other.m_CurrentTime;
            m_StateMachine = other.m_StateMachine;
        }

        public bool CanEnter()
        {
            return m_CanTransitionIf == null || m_CanTransitionIf.IsFulfilled();
        }

        public bool CanTransitionTo(StateTag stateTag)
        {
            if (m_HowToEndState == HowToEndState.OnTimeEnd &&
                !m_CanTransitionToOtherStateBeforeCounterEnd &&
                m_MaxTime >= 0f &&
                m_CurrentTime < m_MaxTime)
            {
                return false;
            }

            return m_CanTransitionToAllStates || m_AllowedStatesToTransitionTo.Contains(stateTag);
        }

        public IAction Clone()
        {
            return new State(this);
        }

        public GameObject GetGameObject()
        {
            return m_StateMachine != null ? m_StateMachine.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (!gameObj.TryGetComponent(out m_StateMachine))
                return false;

            m_CanTransitionIf?.SetGameObject(gameObj);
            m_Time?.SetGameObject(gameObj);

            m_StatesToChangeTo ??= new();
            m_StatesToChangeTo.Clear();

            if (m_StartTransitionStateTag != null && !m_StateMachine.HasState(m_StartTransitionStateTag))
            {
                Debug.LogError($"StateMachine needs state {m_StartTransitionStateTag.name} as start transition state. Error in {gameObj.name}");
            }

            if (m_EndTransitionStateTag != null && !m_StateMachine.HasState(m_EndTransitionStateTag))
            {
                Debug.LogError($"StateMachine needs state {m_EndTransitionStateTag.name} as end transition state. Error in {gameObj.name}");
            }

            if (m_CanTransitionToAllStates)
            {
                foreach (var item in m_StateMachine.States)
                {
                    m_StatesToChangeTo.Add(new StateAndStateTag
                    {
                        StateTag = item.Key,
                        State = item.Value
                    });
                }
            }
            else
            {
                foreach (var stateTag in m_AllowedStatesToTransitionTo)
                {
                    var state = m_StateMachine.GetState(stateTag);

                    if (state != null)
                    {
                        m_StatesToChangeTo.Add(new StateAndStateTag
                        {
                            StateTag = stateTag,
                            State = state
                        });
                    }
                }
            }

            foreach (var action in m_Actions)
            {
                action.SetGameObject(gameObj);
            }

            return true;
        }

        public void StartAction()
        {
            if (m_StateMachine == null)
                return;

            m_CurrentTime = 0f;
            m_MaxTime = m_Time != null ? m_Time.CalculateScore() : -1f;

            foreach (var action in m_Actions)
            {
                action.StartAction();
            }

            m_OnStart?.Invoke();
        }

        public void UpdateAction()
        {
            if (m_StateMachine == null)
                return;

            foreach (var action in m_Actions)
            {
                action.UpdateAction();
            }

            m_OnUpdate?.Invoke();

            switch (m_HowToEndState)
            {
                case HowToEndState.OnChangeState:
                    UpdateChangeStateTransitions();
                    break;

                case HowToEndState.OnTimeEnd:
                    UpdateTimeEnd();
                    break;
            }
        }

        private void UpdateChangeStateTransitions()
        {
            if (!m_CheckIfCanTransitionToOtherStatesOnUpdate)
                return;

            foreach (var stateData in m_StatesToChangeTo)
            {
                if (stateData.State == null)
                    continue;

                if (stateData.State.CanEnter())
                {
                    m_StateMachine.ChangeState(stateData.StateTag);
                    break;
                }
            }
        }

        private void UpdateTimeEnd()
        {
            if (m_MaxTime < 0f)
                return;

            m_CurrentTime += Time.deltaTime;

            if (m_CurrentTime < m_MaxTime)
                return;

            if (m_StateToTransitionToOnTimeEnd != null)
            {
                m_StateMachine.ChangeState(m_StateToTransitionToOnTimeEnd);
            }
            else
            {
                m_StateMachine.NullCurrentState();
            }
        }

        public void EndAction()
        {
            if (m_StateMachine == null)
                return;

            foreach (var action in m_Actions)
            {
                action.EndAction();
            }

            m_OnEnd?.Invoke();
        }
    }
}