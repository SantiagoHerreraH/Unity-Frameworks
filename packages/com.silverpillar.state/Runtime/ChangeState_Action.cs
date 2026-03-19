using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.State
{
    [Serializable]
    public class ChangeState_Action : ICachedGameAction
    {
        [SerializeField]
        private StateTag m_TargetStateTag;

        private StateMachine m_CachedStateMachine;

        public ChangeState_Action() { }

        public ChangeState_Action(ChangeState_Action other)
        {
            this.m_TargetStateTag = other.m_TargetStateTag;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null)
            {
                return gameObj.TryGetComponent(out m_CachedStateMachine);
            }
            return false;
        }

        public GameObject GetGameObject()
        {
            return m_CachedStateMachine != null ? m_CachedStateMachine.gameObject : null;
        }

        public void Execute()
        {
            if (m_CachedStateMachine != null && m_TargetStateTag != null)
            {
                m_CachedStateMachine.ChangeState(m_TargetStateTag);
            }
        }
        public ICachedGameAction Clone()
        {
            return new ChangeState_Action(this);
        }
    }
}
