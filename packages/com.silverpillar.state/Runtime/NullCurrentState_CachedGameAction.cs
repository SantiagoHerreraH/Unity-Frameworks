using UnityEngine;
using SilverPillar.Core;
using System;
using Sirenix.OdinInspector;

namespace SilverPillar.State
{
    [Serializable]
    public class NullCurrentState_CachedGameAction : ICachedGameAction
    {
        [SerializeField]
        private SelfType m_StatMachineFrom;

        [SerializeField, ShowIf(nameof(m_StatMachineFrom), SelfType.CustomGameObject)]
        private StateMachine m_StateMachine;

        public NullCurrentState_CachedGameAction() { }

        public ICachedGameAction Clone()
        {
            return new NullCurrentState_CachedGameAction
            {
                m_StatMachineFrom = this.m_StatMachineFrom,
                m_StateMachine = this.m_StateMachine
            };
        }

        public void Execute()
        {
            if (m_StateMachine != null)
            {
                m_StateMachine.NullCurrentState();
            }
        }

        public GameObject GetGameObject()
        {
            return m_StateMachine != null ? m_StateMachine.gameObject : null;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            if (gameObj == null) return false;

            if (m_StatMachineFrom == SelfType.ThisGameObject)
            {
                gameObj.TryGetComponent<StateMachine>(out m_StateMachine);
            }

            return m_StateMachine != null;
        }
    }
}
