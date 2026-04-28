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
        [SerializeField, Tooltip("If you don't allow null state and this is null, it will get a random state")]
        private StateTag m_StateOnStart = null;
        [FoldoutGroup("Data")]
        [OdinSerialize, ShowInInspector]
        private Dictionary<StateTag, State> m_States = new();


        [FoldoutGroup("Events")]
        [SerializeField]
        private StateEvent m_OnStateChange = null;
        [FoldoutGroup("Events")]
        [SerializeField]
        private UnityEvent m_OnNullState = null;

        public Dictionary<StateTag, State> States { get { return m_States; } }

        [FoldoutGroup("Debug")]
        [SerializeField, ReadOnly]
        private StateTag m_CurrentStateTag = null;
        [FoldoutGroup("Debug")]
        [SerializeField, ReadOnly]
        private State m_CurrentState = null;

        private void Awake()
        {
            var states = m_States.Values;
            foreach (var state in states)
            {
                state.SetGameObject(gameObject);
            }

        }
        private void Start()
        {
            if (m_StateOnStart != null)
            {
                ChangeState(m_StateOnStart);
            }
            else if (!m_AllowNullState && m_States.Count > 0)
            {
                ChangeState(m_States.Keys.FirstOrDefault());
            }
        }

        private void Update()
        {
            m_CurrentState?.UpdateAction();
        }

        public void ChangeState(StateTag stateTag)
        {
            if (stateTag != null && m_CurrentStateTag != stateTag && m_States.ContainsKey(stateTag))
            {
                if (m_CurrentState != null && !m_CurrentState.CanTransitionTo(stateTag))
                {
                    return;
                }
                var newState = m_States[stateTag];
                m_CurrentState?.EndAction();
                m_CurrentState = newState;
                m_CurrentStateTag = stateTag;

                m_OnStateChange?.Invoke(stateTag);

                m_CurrentState.StartAction();
            }

        }

        public void NullCurrentState()
        {
            if (m_AllowNullState)
            {
                m_CurrentState?.EndAction();
                m_CurrentState = null;
                m_CurrentStateTag = null;
                m_OnNullState?.Invoke();
            }
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
    }
}
