using SilverPillar.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilverPillar.State
{
    [Serializable]
    public class IsInState_CachedCondition : ICachedCondition
    {
        [Title("Data")]
        [SerializeField] private SelfType m_WhichStateMachine;
        [SerializeField, ShowIf(nameof(m_WhichStateMachine), SelfType.CustomGameObject)] private StateMachine m_StateMachine;

        [Title("State")]
        [SerializeField, Tooltip("If left empty or null will check if current state is null")]
        private List<StateTag> m_PossibleStatesToBeIn;

        private GameObject m_Self;

        public ICachedCondition Clone()
        {
            var clone = new IsInState_CachedCondition
            {
                m_WhichStateMachine = m_WhichStateMachine,
                m_StateMachine = m_StateMachine,
                m_PossibleStatesToBeIn = m_PossibleStatesToBeIn != null
                    ? new List<StateTag>(m_PossibleStatesToBeIn)
                    : new List<StateTag>(),
                m_Self = m_Self
            };

            return clone;
        }

        public GameObject GetGameObject()
        {
            return m_Self;
        }

        public bool SetGameObject(GameObject gameObj)
        {
            m_Self = gameObj;

            if (m_WhichStateMachine == SelfType.ThisGameObject)
            {
                m_StateMachine = m_Self.GetComponent<StateMachine>();
            }

            return m_StateMachine != null;
        }

        public bool IsFulfilled()
        {
            if (m_StateMachine == null)
            {
                return false;
            }

            if (m_PossibleStatesToBeIn == null || m_PossibleStatesToBeIn.Count == 0)
            {
                return m_StateMachine.CurrentStateTag == null;
            }

            foreach (var item in m_PossibleStatesToBeIn)
            {
                if (m_StateMachine.CurrentStateTag == item)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
