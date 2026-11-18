
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SilverPillar.Core
{
    [Serializable]
    public class Events_Interaction : IInteraction
    {
        [SerializeField]
        private UnityEvent<GameObject> m_EventOnSelf;

        [SerializeField]
        private UnityEvent<GameObject> m_EventOnOther ;

        [NonSerialized]
        private GameObject m_Self;

        public IInteraction Clone()
        {
            return new Events_Interaction
            {
                m_EventOnSelf = m_EventOnSelf,
                m_EventOnOther = m_EventOnOther
            };
        }

        public GameObject GetSelf()
        {
            return m_Self;
        }

        public void Interact(GameObject target)
        {
            m_EventOnSelf?.Invoke(m_Self);
            m_EventOnOther?.Invoke(target);
        }

        public bool SetSelf(GameObject self)
        {
            m_Self = self;
            return m_Self != null;
        }
    }
}
