using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.EventTools
{
    public class ToggleEvent : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField, Tooltip("The state it starts in won't get triggered in the first Toggle call. Behavior is switch then trigger.")]
        private bool m_InWhichStateToStart;
        [SerializeField]
        private bool m_TriggerStateOnStart;
        private bool m_CurrentState;


        [Header("Events")]
        [SerializeField]
        private UnityEvent<bool> m_On;
        [SerializeField]
        private UnityEvent<bool> m_Off;

        private void Start()
        {
            m_CurrentState = m_InWhichStateToStart;

            if (m_TriggerStateOnStart)
            {
                if (m_CurrentState)
                {
                    m_On.Invoke(m_CurrentState);
                }
                else
                {
                    m_Off.Invoke(m_CurrentState);
                }
            }
        }

        public void Toggle()
        {
            m_CurrentState = !m_CurrentState;

            if (m_CurrentState)
            {
                m_On.Invoke(m_CurrentState);
            }
            else
            {
                m_Off.Invoke(m_CurrentState);
            }
        }

        public void SetState(bool state)
        {
            m_CurrentState = state;
            if (m_CurrentState)
            {
                m_On.Invoke(m_CurrentState);
            }
            else
            {
                m_Off.Invoke(m_CurrentState);
            }
        }
    }
}
