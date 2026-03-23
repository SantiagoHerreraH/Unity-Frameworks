using SilverPillar.Core;
using System;
using UnityEngine;

namespace SilverPillar.State
{
    [Serializable]
    public class ChangeState_CGAction : ICachedGameAction
    {
        [SerializeField]
        private StateTag m_TargetStateTag;

        private StateMachine m_CachedStateMachine;

        public ChangeState_CGAction() { }

        public ChangeState_CGAction(ChangeState_CGAction other)
        {
            this.m_TargetStateTag = other.m_TargetStateTag;
            this.m_CachedStateMachine = other.m_CachedStateMachine;
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
            return new ChangeState_CGAction(this);
        }
    }


    [Serializable]
    public class ChangeState_Action : IAction
    {
        [SerializeField]
        private StateTag m_TargetStateTag;

        [SerializeField]
        private bool m_NullStateOnEndState = true;

        private StateMachine m_CachedStateMachine;

        public ChangeState_Action() { }

        public ChangeState_Action(ChangeState_Action other)
        {
            this.m_TargetStateTag = other.m_TargetStateTag;
            this.m_NullStateOnEndState = other.m_NullStateOnEndState;
            this.m_CachedStateMachine = other.m_CachedStateMachine;
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

        public IAction Clone()
        {
            return new ChangeState_Action(this);
        }

        public void StartAction()
        {
            if (m_CachedStateMachine != null && m_TargetStateTag != null)
            {
                m_CachedStateMachine.ChangeState(m_TargetStateTag);
            }
        }

        public void UpdateAction()
        {

        }

        public void EndAction()
        {
            if (m_NullStateOnEndState && m_CachedStateMachine != null && m_TargetStateTag != null)
            {
                m_CachedStateMachine.NullCurrentState();
            }
        }
    }
}
