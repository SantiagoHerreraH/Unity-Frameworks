using System;
using UnityEngine;

namespace SilverPillar.Core
{
    [Serializable]
    public class InteractWithOther_Interaction : IInteraction
    {
        private GameObject m_Self;

        public InteractWithOther_Interaction() { }
        public InteractWithOther_Interaction(InteractWithOther_Interaction other)
        {
            m_Self = other.m_Self;
        }

        public IInteraction Clone()
        {
            return new InteractWithOther_Interaction(this);
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public void Interact(GameObject target)
        {
            SimpleInteractionMachine machine = null;

            if (target.TryGetComponent(out machine))
            {
                machine.Interact(m_Self);
            }
        }

        public bool SetSelf(GameObject self)
        {
            if (self != null)
            {
                m_Self = self;
                return true;
            }

            return false;
        }
    }
}
