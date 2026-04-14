using UnityEngine;
using SilverPillar.Core;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using Sirenix.Serialization;
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
        [Title("Brain")]
        [SerializeField]
        private SO_Ref<Brain> m_BrainRef = new();

        [Title("Events")]
        [OdinSerialize, ShowInInspector]
        private Dictionary<BehaviorAction, BehaviorActionEvent> m_BehaviorActionEvents = null;
        private BrainInstance m_BrainInstance = null;
        private BehaviorActionInstance m_CurrentActionInstance = null;
        private BehaviorActionEvent m_CurrentEvent;

        private void Awake()
        {
            m_BrainInstance = m_BrainRef.Get().CreateInstance(gameObject);
        }

        void Update() 
        {
            var newAction = m_BrainInstance.GetActionInstance();
            
            if (newAction != m_CurrentActionInstance)
            {
                m_CurrentActionInstance?.EndAction();

                if (m_BehaviorActionEvents != null && m_CurrentActionInstance != null && m_BehaviorActionEvents.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
                {
                    m_CurrentEvent.OnEnd?.Invoke();
                }
                
                m_CurrentActionInstance = newAction;

                m_CurrentActionInstance.StartAction();

                if (m_BehaviorActionEvents != null && m_BehaviorActionEvents.TryGetValue(m_CurrentActionInstance.Action, out m_CurrentEvent))
                {
                    m_CurrentEvent.OnStart?.Invoke();
                }
            }

            m_CurrentActionInstance.UpdateAction();

        }
    }
}

