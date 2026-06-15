using System;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Modules
{
    [Serializable]
    public class BoolModule 
    {
        [SerializeField]
        private bool m_State;

        [SerializeField]
        private UnityEvent<bool> m_OnChangeState;
        [SerializeField]
        private UnityEvent<bool> m_OnSetState;

        public BoolModule() { }
        public BoolModule(bool state)
        {
            m_State = state;
        }

        public void SubscribeOnChangeState(UnityAction<bool> action)
        {
            m_OnChangeState.AddListener(action);
        }

        public void UnsubscribeOnChangeState(UnityAction<bool> action)
        {
            m_OnChangeState.RemoveListener(action);
        }
        public void SubscribeOnSetState(UnityAction<bool> action)
        {
            m_OnSetState.AddListener(action);
        }

        public void UnsubscribeOnSetState(UnityAction<bool> action)
        {
            m_OnSetState.RemoveListener(action);
        }

        public void SetState(bool state)
        {
            bool prevState = m_State;
            m_State = state;

            m_OnSetState?.Invoke(m_State);
            if (prevState != state)
            {
                m_OnChangeState?.Invoke(m_State);
            }
        }

        public bool GetState()
        {
            return m_State;
        }
    }
}
