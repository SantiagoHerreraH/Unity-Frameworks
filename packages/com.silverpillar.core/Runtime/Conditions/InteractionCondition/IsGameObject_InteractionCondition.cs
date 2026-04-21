
using System;
using UnityEngine;

namespace SilverPillar.Core
{

    [Serializable]
    public class IsGameObject_InteractionCondition : IInteractionCondition
    {
        public enum OperationType
        {
            Equals,
            NotEquals
        }
        [SerializeField]
        private TargetType m_Who;
        [SerializeField]
        private GameObject m_GameObject;
        private GameObject m_Self;

        public IInteractionCondition Clone()
        {
            return new IsGameObject_InteractionCondition
            {
                m_GameObject = this.m_GameObject,
                m_Who = this.m_Who,
                m_Self = this.m_Self
            };
        }

        public GameObject GetGameObject() => m_Self;

        public bool SetGameObject(GameObject self)
        {
            if (self == null)
            {
                return false;
            }

            m_Self = self;
            return true;
        }

        public bool IsFulfilled(GameObject target)
        {
            switch (m_Who)
            {
                case TargetType.Self:
                    return m_Self == m_GameObject;
                case TargetType.Other:
                    return target == m_GameObject;
                default:
                    break;
            }

            return false;
        }

    }
}
