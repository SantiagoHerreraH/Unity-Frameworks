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
    //Since this is an action you can nest a state here, I think the serialization limit is 10
    [Serializable]
    public class State : IAction
    {
        [FoldoutGroup("Conditions")]
        [SerializeField]
        private ICachedCondition m_CanTransitionIf = null;

        [FoldoutGroup("Actions")]
        [OdinSerialize, ShowInInspector]
        private List<IAction> m_Actions = new List<IAction>();

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
        private List<StateAndStateTag> m_StatesToTransitionTo = new List<StateAndStateTag>();

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
        [SerializeField, ShowIf(nameof(m_HowToEndState), HowToEndState.OnTimeEnd), 
        Tooltip("This score lets you decide how to calculate the state time. The time will be calculated on State Start")]
        private ScoreGroup m_Time = new();
        private float m_MaxTime;
        private float m_CurrentTime;

        private StateMachine m_StateMachine = null;

        public State() { }

        public State(State other)
        {
            this.m_CanTransitionIf = other.m_CanTransitionIf.Clone();
            this.m_Actions = other.m_Actions.Select(a => a.Clone()).ToList();
            this.m_OnStart = other.m_OnStart;
            this.m_OnUpdate = other.m_OnUpdate;
            this.m_OnEnd = other.m_OnEnd;
            this.m_CheckIfCanTransitionToOtherStatesOnUpdate = other.m_CheckIfCanTransitionToOtherStatesOnUpdate;
            this.m_CanTransitionToAllStates = other.m_CanTransitionToAllStates;
            this.m_AllowedStatesToTransitionTo = new HashSet<StateTag>(other.m_AllowedStatesToTransitionTo);
            this.m_StatesToTransitionTo = new(other.m_StatesToTransitionTo);
            this.m_HowToEndState = other.m_HowToEndState;
            this.m_StateToTransitionToOnTimeEnd = other.m_StateToTransitionToOnTimeEnd;
            this.m_CanTransitionToOtherStateBeforeCounterEnd = other.m_CanTransitionToOtherStateBeforeCounterEnd;
            this.m_Time = other.m_Time;
            this.m_MaxTime = other.m_MaxTime;
            this.m_CurrentTime = other.m_CurrentTime;
            this.m_StateMachine = other.m_StateMachine;
        }
        public bool CanTransitionTo(StateTag stateTag)
        {
            if (m_HowToEndState == HowToEndState.OnTimeEnd && 
                !m_CanTransitionToOtherStateBeforeCounterEnd &&
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
            return m_StateMachine.gameObject;
        }
        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj.TryGetComponent(out m_StateMachine))
            {
                m_CanTransitionIf?.SetGameObject(gameObj);

                if (m_StatesToTransitionTo == null)
                {
                    m_StatesToTransitionTo = new();
                }

                if (m_CanTransitionToAllStates)
                {
                    foreach (var item in m_StateMachine.States)
                    {
                        m_StatesToTransitionTo.Add(new StateAndStateTag { State = item.Value, StateTag = item.Key });
                    }
                }
                else
                {
                    foreach (var stateTag in m_AllowedStatesToTransitionTo)
                    {
                        var state = m_StateMachine.GetState(stateTag);

                        if (state != null)
                        {
                            m_StatesToTransitionTo.Add(new StateAndStateTag { State = state, StateTag = stateTag });
                        }

                    }
                }
                    

                foreach (var action in m_Actions)
                {
                    action.SetGameObject(gameObj);
                }

                return true;
            }

            return false;
        }

        public void StartAction()
        {
            if (m_StateMachine == null) return;

            foreach (var action in m_Actions)
            {
                action.StartAction();
            }

            m_OnStart?.Invoke();

            m_MaxTime = m_Time.CalculateScore(m_StateMachine.gameObject);
        }

        public void UpdateAction()
        {
            if (m_StateMachine == null) return;

            foreach (var action in m_Actions)
            {
                action.UpdateAction();
            }

            m_OnUpdate?.Invoke();

            switch (m_HowToEndState)
            {
                case HowToEndState.OnChangeState:

                    if (m_CheckIfCanTransitionToOtherStatesOnUpdate)
                    {
                        foreach (var stateData in m_StatesToTransitionTo)
                        {
                            if (stateData.State.m_CanTransitionIf.IsFulfilled())
                            {
                                m_StateMachine.ChangeState(stateData.StateTag);
                                break;
                            }
                        }
                    }

                    break;
                case HowToEndState.OnTimeEnd:

                    m_CurrentTime += Time.deltaTime;

                    if (m_CurrentTime >= m_MaxTime)
                    {
                        if (m_StateToTransitionToOnTimeEnd != null)
                        {
                            m_StateMachine.ChangeState(m_StateToTransitionToOnTimeEnd);
                        }
                        else
                        {
                            m_StateMachine.NullCurrentState();
                        }

                    }

                    break;
                default:
                    break;
            }
            
        }
        public void EndAction()
        {
            if (m_StateMachine == null) return;

            foreach (var action in m_Actions)
            {
                action.EndAction();
            }

            m_OnEnd?.Invoke();
        }
        
    }
}
