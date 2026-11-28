using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class Not_InteractionCondition : IInteractionCondition
    {
        [OdinSerialize, ShowInInspector]
        private IInteractionCondition m_InteractionCondition;
        public IInteractionCondition Clone()
        {
            return new Not_InteractionCondition
            {
                m_InteractionCondition = this.m_InteractionCondition.Clone()
            };
        }

        public GameObject GetGameObject()
        {
            return m_InteractionCondition.GetGameObject();
        }

        public bool IsFulfilled(GameObject target)
        {
            return !m_InteractionCondition.IsFulfilled(target);
        }

        public bool SetGameObject(GameObject self)
        {
            return m_InteractionCondition.SetGameObject(self);
        }
    }
}
