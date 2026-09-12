using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SilverPillar.State
{

    [Serializable]
    public class ChangeState_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private StateTag m_TargetStateTag;
        [SerializeField]
        private SelfType m_StateMachineToChangeState;
        [SerializeField, ShowIf(nameof(m_StateMachineToChangeState), SelfType.CustomGameObject)]
        private StateMachine m_CachedStateMachine;

        public ChangeState_CachedGameAction() { }

        public ChangeState_CachedGameAction(ChangeState_CachedGameAction other)
        {
            this.m_TargetStateTag = other.m_TargetStateTag;
            this.m_CachedStateMachine = other.m_CachedStateMachine;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj != null && m_StateMachineToChangeState == SelfType.ThisGameObject)
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
            return new ChangeState_CachedGameAction(this);
        }
    }
}
