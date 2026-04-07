using System;
using UnityEngine;

namespace SilverPillar.Modules
{
    [Serializable]
    public class BoolModule 
    {
        [SerializeField]
        private bool m_State;

        private Action<bool> m_OnChangeState;
        private Action<bool> m_OnSetState;

        public BoolModule() { }
        public BoolModule(bool state)
        {
            m_State = state;
        }

        public void SubscribeOnChangeState(Action<bool> action)
        {
            m_OnChangeState += action;
        }

        public void UnsubscribeOnChangeState(Action<bool> action)
        {
            m_OnChangeState -= action;
        }
        public void SubscribeOnSetState(Action<bool> action)
        {
            m_OnSetState += action;
        }

        public void UnsubscribeOnSetState(Action<bool> action)
        {
            m_OnSetState -= action;
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
