using UnityEngine;
using SilverPillar.Core;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using Sirenix.OdinInspector;

namespace SilverPillar.GOAP
{
    public enum HowToChooseCurrentAction
    {
        MinPathCost,
        MaxScore,
        MaxScoreMinusMinPathCost,
        MaxScoreDividedByMinPathCost
    }


    [Serializable]
    public class BehaviorActionEvent
    {
        public UnityEvent OnStart;
        public UnityEvent OnEnd;
    }

    public class BrainHolder : SerializedMonoBehaviour
    {
        [Serializable]
        public struct EventData
        {
            public BehaviorAction BehaviorAction;
            public BehaviorActionEvent BehaviorActionEvent;

        }
        [Title("Brain")]
        [SerializeField]
        private SO_Ref<Brain> m_BrainRef = new();

        [Title("General Events")]

        [SerializeField]
        private UnityEvent<string> m_OnNewAction;
        [SerializeField]
        private UnityEvent<string> m_OnNewGoal;
        [SerializeField]
        private UnityEvent m_OnActionNulled;
        [SerializeField]
        private UnityEvent m_OnGoalNulled;

        [Title("Behavior Action Events")]
        [SerializeField]
        private List<EventData> m_BehaviorActionEvents = null;
        private Dictionary<BehaviorAction, BehaviorActionEvent> m_BehaviorActionEventsDictionary = null;
        private BrainInstance m_BrainInstance = null;
        private BehaviorActionInstance m_CurrentActionInstance = null;
        private BehaviorActionEvent m_CurrentEvent;
        private CachedCondition m_CurrentGoal;

        [Title("Debug")]
        [SerializeField]
        private BrainInstanceDebugSettings m_DebugSettings;
        [SerializeField]
        private bool m_PrintActionOnActionChange;

        private void Awake()
        {
            m_BrainInstance = m_BrainRef.Get().CreateInstance(gameObject, m_DebugSettings);

            if (m_BehaviorActionEvents != null)
            {
                if (m_BehaviorActionEventsDictionary == null)
                {
                    m_BehaviorActionEventsDictionary = new();
                }
                foreach (var item in m_BehaviorActionEvents)
                {
                    m_BehaviorActionEventsDictionary.Add(item.BehaviorAction, item.BehaviorActionEvent);
                }
            }
        }

        private void OnEnable()
        {
            m_CurrentActionInstance = null;
            m_CurrentEvent = null;
            m_CurrentGoal = null;
            m_OnActionNulled?.Invoke();
            m_OnGoalNulled?.Invoke();
        }

        void Update() 
        {
            var newAction = m_BrainInstance.GetActionInstance();
            var newGoal = m_BrainInstance.GetCurrentGoal();

            if (newGoal != m_CurrentGoal)
            {
                m_CurrentGoal = newGoal;

                if (m_CurrentGoal == null)
                {
                    m_OnGoalNulled?.Invoke();
                }
                else
                {
                    m_OnNewGoal?.Invoke(m_CurrentGoal.name);
                }

            }
            
            if (newAction != m_CurrentActionInstance)
            {
                m_CurrentActionInstance?.EndAction();

                if (m_BehaviorActionEvents != null && m_CurrentActionInstance != null && m_BehaviorActionEventsDictionary.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
                {
                    m_CurrentEvent.OnEnd?.Invoke();
                }
                
                m_CurrentActionInstance = newAction;

                m_OnNewAction?.Invoke(newAction.Action.name);

                m_CurrentActionInstance.StartAction();

                if (m_BehaviorActionEvents != null && m_CurrentActionInstance != null && m_BehaviorActionEventsDictionary.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
                {
                    m_CurrentEvent.OnStart?.Invoke();
                }

                if (m_PrintActionOnActionChange && m_CurrentActionInstance != null)
                {
                    Debug.Log($"{gameObject}'s Brain Holder NEW ACTION is {m_CurrentActionInstance.Action.name}");
                }
            }

            m_CurrentActionInstance?.UpdateAction();

        }

        public void NullCurrentAction()
        {
            if (m_BehaviorActionEvents != null && m_CurrentActionInstance != null && m_BehaviorActionEventsDictionary.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
            {
                m_CurrentEvent.OnEnd?.Invoke();
            }

            m_CurrentActionInstance = null;
            m_CurrentEvent = null;
            m_CurrentGoal = null;
            m_OnActionNulled?.Invoke();
            m_OnGoalNulled?.Invoke();
        }
    }
}

